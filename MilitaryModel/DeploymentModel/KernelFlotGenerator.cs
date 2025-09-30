using System;
using System.Collections.Generic;
using System.Linq;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Buffer;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace MilitaryModel.DeploymentModel {
    public static class KernelFlotGenerator {
        // Public entry: generates separating front(s) from units.
        // Returns a MultiLineString in WGS84 lon/lat (EPSG:4326) or null if none.
        public static Geometry? GenerateFronts(
            IReadOnlyList<ArmyUnit> units,
            double resolutionMeters = 500.0,    // grid cell size in meters
            double sigmaMeters = 18_000.0,        // gaussian kernel bandwidth (meters)
            double kernelWeightMultiplier = 1.0,// additional global scale on weights
            double minLengthMeters = 1.0,     // minimal front length to keep
            int smoothingIterations = 2       // Chaikin smoothing iterations (0 = none)
        ) {
            if (units == null || units.Count == 0) return null;

            // Compute per-faction influence areas (in WGS84 lon/lat)
            var influenceAreas = GenerateInfluenceAreas(units, resolutionMeters, sigmaMeters, kernelWeightMultiplier, 1);
            if (influenceAreas == null || influenceAreas.Count == 0) return null;

            // Filter out null geometries
            var valid = influenceAreas.Where(kv => kv.Value != null).ToList();
            if (valid.Count < 2) {      
                Console.WriteLine("[WARN] Need at least two factions with influence areas to compute fronts.");
                return null;
            }

            // Prepare transforms and geometry factories for length measurement in meters
            var toMeters = KFG_CreateWgs84ToWebMercatorTransform();
            var toLonLat = KFG_CreateWebMercatorToWgs84Transform();
            var geomFactory3857 = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 3857);
            var geomFactory4326 = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

            // Use concurrent bag to collect candidate lines in projected meters (will merge/smooth later)
            var frontLineMetersBag = new ConcurrentBag<LineString>(); // lines in meters (EPSG:3857)

            // For each unique pair of factions compute boundary intersection -> candidate front segments
            var entries = valid.ToArray();

            // Build list of index pairs to process in parallel
            var pairs = new List<(int i, int j)>();
            for (int i = 0; i < entries.Length; i++) {
                var gi = entries[i].Value!;
                var bi = gi.Boundary;
                if (bi.IsEmpty) continue;
                for (int j = i + 1; j < entries.Length; j++) {
                    var gj = entries[j].Value!;
                    var bj = gj.Boundary;
                    if (bj.IsEmpty) continue;
                    pairs.Add((i, j));
                }
            }

            // Parallel processing of pairs: collect intersection lines in meters
            Parallel.ForEach(pairs, pair => {
                var fi = entries[pair.i];
                var fj = entries[pair.j];
                var bi = fi.Value!.Boundary;
                var bj = fj.Value!.Boundary;

                Geometry inter;
                try {
                    inter = bi.Intersection(bj);
                } catch (Exception ex) {
                    // Defensive: if intersection fails for some complex geometry, skip
                    Console.WriteLine($"[WARN] Intersection failed between factions {fi.Key} and {fj.Key}: {ex.Message}");
                    return;
                }
                if (inter == null || inter.IsEmpty) return;

                // Extract LineStrings from the intersection geometry
                var lines = ExtractLineStrings(inter);
                foreach (var line in lines) {
                    // reproject to meters to measure length
                    var lineMeters = (LineString)ReprojectGeometry(line, toMeters, geomFactory3857)!;
                    if (lineMeters == null) continue;

                    double len = lineMeters.Length; // in meters (EPSG:3857)
                    if (len >= minLengthMeters) {
                        frontLineMetersBag.Add(lineMeters);
                    }
                }
            });

            var frontLineMeters = frontLineMetersBag.ToList();

            if (frontLineMeters.Count == 0) {
                Console.WriteLine("[WARN] No contour segments found after computing faction boundaries.");
                return null;
            }

            // Merge nearby segments before smoothing. Use half-resolution as default merge tolerance.
            double mergeTolerance = Math.Max(1.0, resolutionMeters * 0.5);
            var mergedLinesMeters = MergeLineStringsByEndpointProximity(frontLineMeters, mergeTolerance, geomFactory3857);

            // --- NEW: close small gaps and extend extremes before smoothing ---
            // If a merged polyline's endpoints are within 2.5km, close them with a straight segment.
            // Otherwise, for polylines that remain open, extend northern-most 200km north and southern-most 200km south.
            var processedLinesMeters = new List<LineString>();
            double closeThreshold = 2500.0; // 2.5 km
            foreach (var ml in mergedLinesMeters) {
                var coords = ml.Coordinates;
                if (coords.Length < 2) continue;
                var first = coords.First();
                var last = coords.Last();
                double dx = last.X - first.X;
                double dy = last.Y - first.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist <= closeThreshold) {
                    var closed = coords.ToList();
                    closed.Add(new Coordinate(first.X, first.Y));
                    processedLinesMeters.Add(geomFactory3857.CreateLineString(closed.ToArray()));
                } else {
                    processedLinesMeters.Add(ml);
                }
            }

            // Extend northern-most and southern-most lines by 200km if needed
            if (processedLinesMeters.Count > 0) {
                int northernIdx = -1, southernIdx = -1;
                double northY = double.MinValue, southY = double.MaxValue;
                for (int i = 0; i < processedLinesMeters.Count; i++) {
                    var c = processedLinesMeters[i].Coordinates;
                    if (c == null || c.Length == 0) continue;
                    double maxY = c.Max(p => p.Y);
                    double minY = c.Min(p => p.Y);
                    if (maxY > northY) { northY = maxY; northernIdx = i; }
                    if (minY < southY) { southY = minY; southernIdx = i; }
                }

                double extendMeters = 200_000.0; // 200 km

                // Helper: compute outward unit direction based on up-to `maxSegs` near the endpoint.
                static (double ux, double uy) ComputeOutwardUnit(List<Coordinate> pts, bool extendAtStart, int maxSegs, (double fx, double fy) fallback) {
                    int n = pts.Count;
                    if (n < 2) return fallback;
                    int segs = Math.Min(maxSegs, n - 1);
                    int startIdx = extendAtStart ? 0 : Math.Max(0, n - 1 - segs);
                    int endIdx = extendAtStart ? segs - 1 : n - 2;

                    double sx = 0.0, sy = 0.0;
                    for (int k = startIdx; k <= endIdx; k++) {
                        var a = pts[k];
                        var b = pts[k + 1];
                        double dx = b.X - a.X;
                        double dy = b.Y - a.Y;
                        double len = Math.Sqrt(dx * dx + dy * dy);
                        if (len <= 1e-9) continue;
                        sx += dx / len;
                        sy += dy / len;
                    }

                    // If nothing accumulated, fallback
                    double mag = Math.Sqrt(sx * sx + sy * sy);
                    if (mag <= 1e-9) return fallback;

                    // Average unit vector (points inward for start-case; outward for end-case).
                    double ux = sx / mag, uy = sy / mag;

                    // For extension at start we want outward direction (away from interior), so invert.
                    if (extendAtStart) { ux = -ux; uy = -uy; }
                    return (ux, uy);
                }

                if (northernIdx >= 0) {
                    var northernLine = processedLinesMeters[northernIdx];
                    var cList = northernLine.Coordinates.ToList();
                    if (cList.Count >= 2) {
                        bool extendAtStart = cList.First().Y >= cList.Last().Y;
                        // prefer a northward fallback if average vector degenerates
                        var fallback = (0.0, 1.0);
                        var (ux, uy) = ComputeOutwardUnit(cList, extendAtStart, 50, fallback);
                        var dx = ux * extendMeters;
                        var dy = uy * extendMeters;
                        if (extendAtStart) {
                            var p0 = cList.First();
                            cList.Insert(0, new Coordinate(p0.X + dx, p0.Y + dy));
                        } else {
                            var pe = cList.Last();
                            cList.Add(new Coordinate(pe.X + dx, pe.Y + dy));
                        }
                        processedLinesMeters[northernIdx] = geomFactory3857.CreateLineString(cList.ToArray());
                    }
                }

                if (southernIdx >= 0) {
                    var southernLine = processedLinesMeters[southernIdx];
                    var cList = southernLine.Coordinates.ToList();
                    if (cList.Count >= 2) {
                        bool extendAtStart = cList.First().Y <= cList.Last().Y;
                        // prefer a southward fallback if average vector degenerates
                        var fallback = (0.0, -1.0);
                        var (ux, uy) = ComputeOutwardUnit(cList, extendAtStart, 50, fallback);
                        var dx = ux * extendMeters;
                        var dy = uy * extendMeters;
                        if (extendAtStart) {
                            var p0 = cList.First();
                            cList.Insert(0, new Coordinate(p0.X + dx, p0.Y + dy));
                        } else {
                            var pe = cList.Last();
                            cList.Add(new Coordinate(pe.X + dx, pe.Y + dy));
                        }
                        processedLinesMeters[southernIdx] = geomFactory3857.CreateLineString(cList.ToArray());
                    }
                }
            }

            // After merging and endpoint processing, optionally smooth each merged line (in meters) and reproject to lon/lat
            var finalLineGeomsLonLat = new List<Geometry>();
            foreach (var merged in processedLinesMeters) {
                var coords = merged.Coordinates;
                if (coords.Length < 2) continue;

                LineString toAddMeters = merged;
                if (smoothingIterations > 0) {
                    var smoothCoords = ChaikinSmoothCoordinates(coords, smoothingIterations);
                    toAddMeters = geomFactory3857.CreateLineString(smoothCoords);
                }

                if (toAddMeters.Length < minLengthMeters) continue;

                // reproject back to lon/lat
                var lonLine = (LineString)ReprojectGeometry(toAddMeters, toLonLat, geomFactory4326)!;
                if (lonLine != null) finalLineGeomsLonLat.Add(lonLine);
            }

            if (finalLineGeomsLonLat.Count == 0) {
                Console.WriteLine("[WARN] No contour segments remained after merging/smoothing.");
                return null;
            }

            // Merge all line geometries into a single MultiLineString (in WGS84)
            Geometry built = geomFactory4326.BuildGeometry(finalLineGeomsLonLat);
            Geometry unioned = built.Union();

            // Normalize result: return LineString if single, MultiLineString if many
            if (unioned is LineString ls) return ls;
            if (unioned is MultiLineString mls) return mls;

            // If union produced other geometry (e.g., GeometryCollection), try to extract lines
            var extracted = ExtractLineStrings(unioned).ToArray();
            if (extracted.Length == 0) return null;
            if (extracted.Length == 1) return extracted[0];
            return geomFactory4326.CreateMultiLineString(extracted);
        }

        // ---------- utilities ----------

        private static MathTransform KFG_CreateWgs84ToWebMercatorTransform() {
            var csFactory = new CoordinateSystemFactory();
            var ctFactory = new CoordinateTransformationFactory();
            var wgs84 = GeographicCoordinateSystem.WGS84;
            var webMerc = ProjectedCoordinateSystem.WebMercator;
            var trans = ctFactory.CreateFromCoordinateSystems(wgs84, webMerc);
            return trans.MathTransform;
        }

        private static Coordinate KFG_ProjectLonLatToMeters(MathTransform transform, double lon, double lat) {
            double[] from = new[] { lon, lat };
            double[] to = transform.Transform(from);
            return new Coordinate(to[0], to[1]);
        }

        private static MathTransform KFG_CreateWebMercatorToWgs84Transform() {
            var csFactory = new CoordinateSystemFactory();
            var ctFactory = new CoordinateTransformationFactory();
            var wgs84 = GeographicCoordinateSystem.WGS84;
            var webMerc = ProjectedCoordinateSystem.WebMercator;
            var trans = ctFactory.CreateFromCoordinateSystems(webMerc, wgs84);
            return trans.MathTransform;
        }

        private static LineString ReprojectLineString(LineString lsMeters, MathTransform toLonLat, GeometryFactory factory4326) {
            var coords = lsMeters.Coordinates;
            var outCoords = new Coordinate[coords.Length];
            for (int i = 0; i < coords.Length; i++) {
                double[] p = toLonLat.Transform(new[] { coords[i].X, coords[i].Y });
                outCoords[i] = new Coordinate(p[0], p[1]);
            }
            return factory4326.CreateLineString(outCoords);
        }

        // Chaikin smoothing for open polylines. Preserves endpoints.
        private static Coordinate[] ChaikinSmoothCoordinates(Coordinate[] coords, int iterations) {
            if (coords == null || coords.Length < 2 || iterations <= 0) return coords;
            var current = coords.Select(c => new Coordinate(c.X, c.Y)).ToArray();
            for (int it = 0; it < iterations; it++) {
                var next = new List<Coordinate>();
                next.Add(new Coordinate(current[0].X, current[0].Y));
                for (int i = 0; i < current.Length - 1; i++) {
                    var p = current[i];
                    var q = current[i + 1];
                    var pa = new Coordinate(0.75 * p.X + 0.25 * q.X, 0.75 * p.Y + 0.25 * q.Y);
                    var pb = new Coordinate(0.25 * p.X + 0.75 * q.X, 0.25 * p.Y + 0.75 * q.Y);
                    next.Add(pa);
                    next.Add(pb);
                }
                next.Add(new Coordinate(current.Last().X, current.Last().Y));
                current = next.ToArray();
            }
            return current;
        }

        // Merge nearby LineStrings by stitching endpoints within a tolerance.
        private static List<LineString> MergeLineStringsByEndpointProximity(List<LineString> lines, double tolMeters, GeometryFactory factory) {
            var segments = new List<(Point2 p0, Point2 p1)>();
            foreach (var ls in lines) {
                var c = ls.Coordinates;
                if (c == null || c.Length < 2) continue;
                for (int i = 0; i < c.Length - 1; i++) segments.Add((new Point2(c[i].X, c[i].Y), new Point2(c[i + 1].X, c[i + 1].Y)));
            }

            if (segments.Count == 0) return new List<LineString>();

            // Build adjacency map using provided tolerance
            string Key(Point2 p) => $"{Math.Round(p.X / tolMeters)},{Math.Round(p.Y / tolMeters)}";
            var segUsed = new bool[segments.Count];
            var endpointMap = new Dictionary<string, List<int>>();
            for (int i = 0; i < segments.Count; i++) {
                var s = segments[i];
                var k0 = Key(s.p0);
                var k1 = Key(s.p1);
                if (!endpointMap.TryGetValue(k0, out var list0)) { list0 = new List<int>(); endpointMap[k0] = list0; }
                list0.Add(i);
                if (!endpointMap.TryGetValue(k1, out var list1)) { list1 = new List<int>(); endpointMap[k1] = list1; }
                list1.Add(i);
            }

            var polylines = new List<List<Point2>>();
            for (int si = 0; si < segments.Count; si++) {
                if (segUsed[si]) continue;
                segUsed[si] = true;
                var s = segments[si];
                var chain = new LinkedList<Point2>();
                chain.AddLast(s.p0);
                chain.AddLast(s.p1);

                void Extend(LinkedList<Point2> chainLocal, bool forward) {
                    bool extended;
                    do {
                        extended = false;
                        Point2 tip = forward ? chainLocal.Last.Value : chainLocal.First.Value;
                        var key = Key(tip);
                        if (!endpointMap.TryGetValue(key, out var candList)) break;
                        // try to find an unused segment attached to this tip
                        int found = -1;
                        bool foundSwapped = false;
                        foreach (var idx in candList) {
                            if (segUsed[idx]) continue;
                            var seg = segments[idx];
                            if (PointsEqual(seg.p0, tip)) { found = idx; foundSwapped = false; break; }
                            if (PointsEqual(seg.p1, tip)) { found = idx; foundSwapped = true; break; }
                        }
                        if (found >= 0) {
                            var seg = segments[found];
                            segUsed[found] = true;
                            Point2 next = foundSwapped ? seg.p0 : seg.p1;
                            if (forward) chainLocal.AddLast(next); else chainLocal.AddFirst(next);
                            extended = true;
                        }
                    } while (extended);
                }

                Extend(chain, true);
                Extend(chain, false);

                polylines.Add(chain.ToList());
            }

            var result = new List<LineString>();
            foreach (var poly in polylines) {
                var coords = poly.Select(p => new Coordinate(p.X, p.Y)).ToArray();
                if (coords.Length >= 2) result.Add(factory.CreateLineString(coords));
            }
            return result;
        }

        // Marching squares zero-contour extraction.
        // diff: [rows, cols] with ys[] and xs[] arrays giving cell-center coordinates.
        // Returns list of segment pairs in metric coordinates.
        private static List<(Point2 p0, Point2 p1)> MarchingSquaresExtractZeroContour(double[,] values, double[] xs, double[] ys) {
            int rows = values.GetLength(0);
            int cols = values.GetLength(1);
            var segments = new List<(Point2, Point2)>();
            if (rows < 2 || cols < 2) return segments;

            // For each cell, use corner values at cell corners (NW, NE, SE, SW)
            for (int j = 0; j < rows - 1; j++) {
                for (int i = 0; i < cols - 1; i++) {
                    // compute corner positions (we used centers; get corner coordinates by offsetting half cell)
                    double x0 = xs[i] - (xs[1] - xs[0]) / 2.0;
                    double x1 = xs[i + 1] + (xs[1] - xs[0]) / 2.0;
                    double y0 = ys[j] - (ys[1] - ys[0]) / 2.0;
                    double y1 = ys[j + 1] + (ys[1] - ys[0]) / 2.0;
                    // corners:
                    // c00 = top-left (x0,y0) -> value at [j,i]
                    // c10 = top-right (x1,y0) -> [j,i+1]
                    // c11 = bottom-right (x1,y1) -> [j+1,i+1]
                    // c01 = bottom-left (x0,y1) -> [j+1,i]
                    double v00 = values[j, i];
                    double v10 = values[j, i + 1];
                    double v11 = values[j + 1, i + 1];
                    double v01 = values[j + 1, i];

                    int caseIndex = 0;
                    if (v00 > 0) caseIndex |= 1;
                    if (v10 > 0) caseIndex |= 2;
                    if (v11 > 0) caseIndex |= 4;
                    if (v01 > 0) caseIndex |= 8;

                    // skip empty / full
                    if (caseIndex == 0 || caseIndex == 15) continue;

                    // Compute intersections on edges by linear interpolation
                    // edgeTop between v00-v10 at y=y0
                    Point2? eTop = null, eRight = null, eBottom = null, eLeft = null;
                    if ((v00 > 0) != (v10 > 0)) {
                        double t = SafeInterp(v00, v10);
                        double x = Lerp(x0, x1, t);
                        eTop = new Point2(x, y0);
                    }
                    if ((v10 > 0) != (v11 > 0)) {
                        double t = SafeInterp(v10, v11);
                        double y = Lerp(y0, y1, t);
                        eRight = new Point2(x1, y);
                    }
                    if ((v11 > 0) != (v01 > 0)) {
                        double t = SafeInterp(v11, v01);
                        double x = Lerp(x1, x0, t);
                        eBottom = new Point2(x, y1);
                    }
                    if ((v01 > 0) != (v00 > 0)) {
                        double t = SafeInterp(v01, v00);
                        double y = Lerp(y1, y0, t);
                        eLeft = new Point2(x0, y);
                    }

                    // Table of polygonization for 16 cases (non-ambiguous handling: produce one or two segments)
                    // We will add segments connecting computed edge points per case
                    switch (caseIndex) {
                        case 1:
                        case 14:
                            if (eTop != null && eLeft != null) segments.Add((eTop.Value, eLeft.Value));
                            break;
                        case 2:
                        case 13:
                            if (eTop != null && eRight != null) segments.Add((eTop.Value, eRight.Value));
                            break;
                        case 3:
                        case 12:
                            if (eRight != null && eLeft != null) segments.Add((eRight.Value, eLeft.Value));
                            break;
                        case 4:
                        case 11:
                            if (eRight != null && eBottom != null) segments.Add((eRight.Value, eBottom.Value));
                            break;
                        case 5:
                            // ambiguous: two segments: top-left and right-bottom
                            if (eTop != null && eLeft != null) segments.Add((eTop.Value, eLeft.Value));
                            if (eRight != null && eBottom != null) segments.Add((eRight.Value, eBottom.Value));
                            break;
                        case 10:
                            // ambiguous: two segments: top-right and left-bottom
                            if (eTop != null && eRight != null) segments.Add((eTop.Value, eRight.Value));
                            if (eLeft != null && eBottom != null) segments.Add((eLeft.Value, eBottom.Value));
                            break;
                        case 6:
                        case 9:
                            if (eTop != null && eBottom != null) segments.Add((eTop.Value, eBottom.Value));
                            break;
                        case 7:
                        case 8:
                            if (eBottom != null && eLeft != null) segments.Add((eBottom.Value, eLeft.Value));
                            break;
                        default:
                            break;
                    }
                }
            }

            return segments;
        }

        private static double Lerp(double a, double b, double t) => a + (b - a) * t;
        private static double SafeInterp(double vA, double vB) {
            // find t in [0,1] where linear interpolation crosses zero between vA->vB
            double denom = (vA - vB);
            if (Math.Abs(denom) < 1e-12) return 0.5;
            return vA / denom; // note: when vA>0 and vB<0, t in (0,1) satisfies vA + (vB-vA)*t = 0 => t = vA/(vA-vB)
        }

        // Stitch segments (unordered short segments) into polylines by greedy linking.
        private static List<List<Point2>> StitchSegmentsToPolylines(List<(Point2 p0, Point2 p1)> segments) {
            var polylines = new List<List<Point2>>();
            if (segments == null || segments.Count == 0) return polylines;

            // Build adjacency map via rounding to tolerance
            double tol = 1e-6;
            string Key(Point2 p) => $"{Math.Round(p.X / tol)},{Math.Round(p.Y / tol)}";

            var segUsed = new bool[segments.Count];
            var endpointMap = new Dictionary<string, List<int>>();
            for (int i = 0; i < segments.Count; i++) {
                var s = segments[i];
                var k0 = Key(s.p0);
                var k1 = Key(s.p1);
                if (!endpointMap.TryGetValue(k0, out var list0)) { list0 = new List<int>(); endpointMap[k0] = list0; }
                list0.Add(i);
                if (!endpointMap.TryGetValue(k1, out var list1)) { list1 = new List<int>(); endpointMap[k1] = list1; }
                list1.Add(i);
            }

            for (int si = 0; si < segments.Count; si++) {
                if (segUsed[si]) continue;
                // start new polyline with both directions from this segment
                var s = segments[si];
                segUsed[si] = true;
                var chain = new LinkedList<Point2>();
                chain.AddLast(s.p0);
                chain.AddLast(s.p1);

                // extend forward (from tail)
                Extend(chain, true);
                // extend backward (from head)
                Extend(chain, false);

                polylines.Add(chain.ToList());

                void Extend(LinkedList<Point2> chainLocal, bool forward) {
                    bool extended;
                    do {
                        extended = false;
                        Point2 tip = forward ? chainLocal.Last.Value : chainLocal.First.Value;
                        var key = Key(tip);
                        if (!endpointMap.TryGetValue(key, out var candList)) break;
                        // try to find an unused segment attached to this tip
                        int found = -1;
                        bool foundSwapped = false;
                        foreach (var idx in candList) {
                            if (segUsed[idx]) continue;
                            var seg = segments[idx];
                            if (PointsEqual(seg.p0, tip)) { found = idx; foundSwapped = false; break; }
                            if (PointsEqual(seg.p1, tip)) { found = idx; foundSwapped = true; break; }
                        }
                        if (found >= 0) {
                            var seg = segments[found];
                            segUsed[found] = true;
                            Point2 next = foundSwapped ? seg.p0 : seg.p1;
                            if (forward) chainLocal.AddLast(next); else chainLocal.AddFirst(next);
                            extended = true;
                        }
                    } while (extended);
                }
            }

            return polylines;
        }

        private static bool PointsEqual(Point2 a, Point2 b, double eps = 1e-6) {
            return Math.Abs(a.X - b.X) <= eps && Math.Abs(a.Y - b.Y) <= eps;
        }

        // Simple 2D point struct
        private readonly struct Point2 {
            public readonly double X;
            public readonly double Y;
            public Point2(double x, double y) { X = x; Y = y; }
            public override string ToString() => $"({X},{Y})";
        }

        // New public API: compute influence areas per faction (returned geometries are in WGS84 lon/lat)
        public static Dictionary<Faction, Geometry?> GenerateInfluenceAreas(
            IReadOnlyList<ArmyUnit> units,
            double resolutionMeters = 500.0,
            double sigmaMeters = 18_000.0,
            double kernelWeightMultiplier = 1.0,
            double minAreaCells = 1 // minimal number of grid cells to keep a region
        ) {
            if (units == null || units.Count == 0) return new Dictionary<Faction, Geometry?>();

            // Group units by faction (only factions with at least one positioned unit)
            var factionGroups = units
                .Where(u => u != null && u.Position != null && u.Faction != null)
                .GroupBy(u => u.Faction)
                .Select(g => new { Faction = g.Key, Units = g.ToList() })
                .ToList();

            if (factionGroups.Count == 0) return new Dictionary<Faction, Geometry?>();

            // Build transforms
            var transformToMeters = KFG_CreateWgs84ToWebMercatorTransform();
            var transformToLonLat = KFG_CreateWebMercatorToWgs84Transform();

            // Project all units (per faction) into meters
            var projPerFaction = factionGroups.ToDictionary(
                fg => fg.Faction,
                fg => fg.Units.Select(u => KFG_ProjectLonLatToMeters(transformToMeters, u.Position.Longitude, u.Position.Latitude)).ToList()
            );

            // Compute bounding box across all factions
            double pad = Math.Max(3.0 * sigmaMeters, 2.0 * resolutionMeters) + 2000.0;
            double minX = projPerFaction.Min(kv => kv.Value.Min(c => c.X)) - pad;
            double minY = projPerFaction.Min(kv => kv.Value.Min(c => c.Y)) - pad;
            double maxX = projPerFaction.Max(kv => kv.Value.Max(c => c.X)) + pad;
            double maxY = projPerFaction.Max(kv => kv.Value.Max(c => c.Y)) + pad;

            int cols = Math.Max(3, (int)Math.Ceiling((maxX - minX) / resolutionMeters));
            int rows = Math.Max(3, (int)Math.Ceiling((maxY - minY) / resolutionMeters));

            var xs = new double[cols];
            var ys = new double[rows];
            for (int i = 0; i < cols; i++) xs[i] = minX + (i + 0.5) * resolutionMeters;
            for (int j = 0; j < rows; j++) ys[j] = minY + (j + 0.5) * resolutionMeters;

            // Create a kernel density raster per faction
            var twoSigmaSq = 2.0 * sigmaMeters * sigmaMeters;
            if (twoSigmaSq <= 0) twoSigmaSq = 2.0;

            var fieldPerFaction = new Dictionary<Faction, double[,]>();
            foreach (var kv in projPerFaction) {
                var field = new double[rows, cols];
                var pts = kv.Value;
                if (pts.Count == 0) { fieldPerFaction[kv.Key] = field; continue; }

                int nCells = rows * cols;
                var localsBag = new ConcurrentBag<double[]>();
                // parallel per-point accumulation
                Parallel.ForEach(pts, () => new double[nCells], (p, state, local) => {
                    double ux = p.X, uy = p.Y;
                    double w = 1.0 * kernelWeightMultiplier; // uniform weight here; extend if you want echelon weights
                    double radius = 3.0 * sigmaMeters;
                    int maxIspan = Math.Max(1, (int)Math.Ceiling(radius / resolutionMeters));
                    int iCenter = (int)Math.Floor((ux - minX) / resolutionMeters);
                    int jCenter = (int)Math.Floor((uy - minY) / resolutionMeters);
                    int i0 = Math.Max(0, iCenter - maxIspan);
                    int i1 = Math.Min(cols - 1, iCenter + maxIspan);
                    int j0 = Math.Max(0, jCenter - maxIspan);
                    int j1 = Math.Min(rows - 1, jCenter + maxIspan);
                    for (int j = j0; j <= j1; j++) {
                        double dy = ys[j] - uy;
                        double dy2 = dy * dy;
                        int baseIdx = j * cols;
                        for (int i = i0; i <= i1; i++) {
                            double dx = xs[i] - ux;
                            double dist2 = dx * dx + dy2;
                            double val = w * Math.Exp(-dist2 / twoSigmaSq);
                            local[baseIdx + i] += val;
                        }
                    }
                    return local;
                }, local => localsBag.Add(local));

                foreach (var local in localsBag) {
                    if (local == null) continue;
                    for (int j = 0; j < rows; j++) {
                        int baseIdx = j * cols;
                        for (int i = 0; i < cols; i++) {
                            var v = local[baseIdx + i];
                            if (v != 0.0) field[j, i] += v;
                        }
                    }
                }

                fieldPerFaction[kv.Key] = field;
            }

            // For each faction build a binary mask where its field is greater than all others (strict influence)
            var geomFactory3857 = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 3857);
            var results = new ConcurrentDictionary<Faction, Geometry?>();
            // process per-faction region creation in parallel
            Parallel.ForEach(fieldPerFaction, kv => {
                var faction = kv.Key;
                var myField = kv.Value;
                // compute max of other factions at each cell
                var otherMax = new double[rows, cols];
                foreach (var other in fieldPerFaction.Where(x => !x.Key.Equals(faction)).Select(x => x.Value)) {
                    for (int j = 0; j < rows; j++)
                        for (int i = 0; i < cols; i++)
                            otherMax[j, i] = Math.Max(otherMax[j, i], other[j, i]);
                }

                // build polygons for cells where myField > otherMax
                var cellPolygons = new List<Polygon>();
                double half = resolutionMeters * 0.5;
                for (int j = 0; j < rows; j++) {
                    for (int i = 0; i < cols; i++) {
                        if (myField[j, i] <= otherMax[j, i]) continue;
                        // optional small-value check to remove noise
                        // build rectangle polygon for this cell
                        double cx = xs[i], cy = ys[j];
                        var corners = new[] {
                    new Coordinate(cx - half, cy - half),
                    new Coordinate(cx + half, cy - half),
                    new Coordinate(cx + half, cy + half),
                    new Coordinate(cx - half, cy + half),
                    new Coordinate(cx - half, cy - half)
                };
                        var poly = geomFactory3857.CreatePolygon(geomFactory3857.CreateLinearRing(corners));
                        cellPolygons.Add(poly);
                    }
                }

                if (cellPolygons.Count < minAreaCells) {
                    results[faction] = null;
                    return;
                }

                // union cell polygons into a (possibly multi) polygon region
                var built = geomFactory3857.BuildGeometry(cellPolygons);
                var unioned = built.Union(); // union contiguous cells

                // reproject unioned geometry back to lon/lat (EPSG:4326)
                var geom4326 = ReprojectGeometry(unioned, transformToLonLat, NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326));
                results[faction] = geom4326;
            });

            return new Dictionary<Faction, Geometry?>(results);
        }

        // Helper: reprojects Polygon/MultiPolygon/GeometryCollection from meters -> lon/lat using MathTransform
        private static Geometry? ReprojectGeometry(Geometry geomMeters, MathTransform toLonLat, GeometryFactory factory4326) {
            if (geomMeters == null) return null;

            Geometry TransformGeom(Geometry g) {
                switch (g) {
                    case Polygon poly:
                        var shell = TransformLineString(poly.ExteriorRing, toLonLat, factory4326);
                        var holes = new LinearRing[poly.NumInteriorRings];
                        for (int h = 0; h < poly.NumInteriorRings; h++) holes[h] = TransformLineString(poly.GetInteriorRingN(h), toLonLat, factory4326);
                        return factory4326.CreatePolygon(shell, holes);
                    case MultiPolygon mp:
                        var polys = new Polygon[mp.NumGeometries];
                        for (int i = 0; i < mp.NumGeometries; i++) polys[i] = (Polygon)TransformGeom((Polygon)mp.GetGeometryN(i));
                        return factory4326.CreateMultiPolygon(polys);
                    case GeometryCollection gc:
                        var parts = new Geometry[gc.NumGeometries];
                        for (int i = 0; i < gc.NumGeometries; i++) parts[i] = TransformGeom(gc.GetGeometryN(i));
                        return factory4326.CreateGeometryCollection(parts);
                    default:
                        // Fallback: try to handle as linear geometry (LineString / Point)
                        if (g is LineString ls) {
                            return ReprojectLineString(ls, toLonLat, factory4326);
                        }
                        if (g is Point pt) {
                            var p = toLonLat.Transform(new[] { pt.Coordinate.X, pt.Coordinate.Y });
                            return factory4326.CreatePoint(new Coordinate(p[0], p[1]));
                        }
                        return null;
                }
            }

            LinearRing TransformLineString(LineString ring, MathTransform t, GeometryFactory factory) {
                var coords = ring.Coordinates;
                var outCoords = new Coordinate[coords.Length];
                for (int i = 0; i < coords.Length; i++) {
                    var p = t.Transform(new[] { coords[i].X, coords[i].Y });
                    outCoords[i] = new Coordinate(p[0], p[1]);
                }
                return factory.CreateLinearRing(outCoords);
            }

            return TransformGeom(geomMeters);
        }

        // Helper: extract LineString instances from arbitrary geometry (flatten MultiLineString / GeometryCollection)
        public static IEnumerable<LineString> ExtractLineStrings(Geometry g) {
            if (g == null || g.IsEmpty) yield break;
            switch (g) {
                case LineString ls:
                    yield return ls;
                    yield break;
                case MultiLineString mls:
                    for (int i = 0; i < mls.NumGeometries; i++) yield return (LineString)mls.GetGeometryN(i);
                    yield break;
                case GeometryCollection gc:
                    for (int i = 0; i < gc.NumGeometries; i++) {
                        foreach (var sub in ExtractLineStrings(gc.GetGeometryN(i))) yield return sub;
                    }
                    yield break;
                default:
                    yield break;
            }
        }
    }
}