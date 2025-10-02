using PyDCSInterop;
using NetTopologySuite.Geometries;
using SharpKml.Dom;
using MilitaryModel;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;

namespace TTSKML {
    public static class ProcessKML {
        internal static bool CoordinateInPolygon(SharpKml.Base.Vector coordinate, SharpKml.Dom.Polygon polygon) {
            var result = false;

            var coordinates = polygon.OuterBoundary.LinearRing.Coordinates;

            var ntsCoordinates = new List<NetTopologySuite.Geometries.Coordinate>();
            foreach (SharpKml.Base.Vector vector in coordinates) {
                ntsCoordinates.Add(new NetTopologySuite.Geometries.Coordinate(vector.Longitude, vector.Latitude));
            }

            var geometryFactory = new GeometryFactory();
            var ntsPolygon = geometryFactory.CreatePolygon(ntsCoordinates.ToArray());

            var pointToCheck = geometryFactory.CreatePoint(new NetTopologySuite.Geometries.Coordinate(coordinate.Longitude, coordinate.Latitude));

            result = ntsPolygon.Contains(pointToCheck);

            return result;
        }

        // Helper: compute destination lat/lon given start lat/lon, bearing (degrees) and distance (meters)
        private static (double lat, double lon) DestinationPoint(double latDeg, double lonDeg, double bearingDeg, double distanceMeters) {
            const double R = 6378137.0; // Earth radius in meters (WGS84)
            double bearing = DegreeToRadian(bearingDeg);
            double lat1 = DegreeToRadian(latDeg);
            double lon1 = DegreeToRadian(lonDeg);
            double dr = distanceMeters / R;

            double sinLat2 = Math.Sin(lat1) * Math.Cos(dr) + Math.Cos(lat1) * Math.Sin(dr) * Math.Cos(bearing);
            double lat2 = Math.Asin(sinLat2);
            double y = Math.Sin(bearing) * Math.Sin(dr) * Math.Cos(lat1);
            double x = Math.Cos(dr) - Math.Sin(lat1) * Math.Sin(lat2);
            double lon2 = lon1 + Math.Atan2(y, x);

            return (RadianToDegree(lat2), NormalizeLongitude(RadianToDegree(lon2)));
        }

        private static double DegreeToRadian(double d) => d * Math.PI / 180.0;
        private static double RadianToDegree(double r) => r * 180.0 / Math.PI;
        private static double NormalizeLongitude(double lon) {
            // normalize to [-180,180]
            while (lon > 180.0) lon -= 360.0;
            while (lon < -180.0) lon += 360.0;
            return lon;
        }

        public static IReadOnlyList<ArmyUnit> GenerateUnitTree(IReadOnlyList<ArmyUnit>? units, NetTopologySuite.Geometries.Geometry? fronts, double deploymentPercentage = 0.50, bool debug = true) {
            var units_to_place = new List<ArmyUnit>();
            var rearAreas = new List<Placemark>();
            var combatAreas = new List<Placemark>();
            var headingLines = new List<(SharpKml.Dom.LineString line, string color, string name)>();

            List<ArmyUnit> everyUnit = new();

            var globalVisited = new HashSet<ArmyUnit>(new ReferenceByRefComparer());

            if (units == null) return everyUnit;

            foreach (var unit in units) {
                if (unit == null) continue;
                var position = unit.Position;
                if (position == null) { continue; }

                // If this root was already processed as a subordinate of an earlier root, skip it.
                if (globalVisited.Contains(unit)) continue;

                // determine deployment heading:
                var coord = new Coordinate(position.Longitude, position.Latitude);
                var heading = MilitaryModel.DeploymentModel.Tools.CalculateUnitHeadingToFLOT(coord, fronts);
                double headingVal;
                if (heading == null) {
                    Console.WriteLine($"[WARN] null heading after calculation {unit.Name} - falling back to 0 degrees");
                    headingVal = 0.0; // fallback heading
                } else {
                    headingVal = heading.Value;
                }
                unit.SetHeading(headingVal);

                // ApplyDoctrinalDeployment (this returns a collection that used an internal visited set
                // scoped to the single call). Filter the returned units against our globalVisited set
                // so units already produced by earlier roots are not duplicated or re-processed.
                var allUnits = MilitaryModel.DeploymentModel.Tools.ApplyDoctrinalDeployment(unit, fronts, deploymentPercentage);
                if (allUnits == null) continue;

                var newUnits = allUnits.Where(u => u != null && !globalVisited.Contains(u)).ToList();

                // Mark newly observed units as visited and add them to everyUnit
                foreach (var u in newUnits) {
                    if (u == null) continue;
                    globalVisited.Add(u);
                    everyUnit.Add(u);
                }
            }

            return everyUnit;
        }

        private static void ExportKml(List<ArmyUnit> units, NetTopologySuite.Geometries.Geometry? fronts, List<(SharpKml.Dom.LineString line, string color, string name)> headingLines) {
            // --- Export units and fronts as KML ---
            var kmlDoc = new SharpKml.Dom.Document();
            kmlDoc.Name = "Units and Fronts";

            // Add unit placemarks
            foreach (var unit in units) {
                if (unit.Position == null) continue;
                var placemark = new SharpKml.Dom.Placemark {
                    Name = unit.Name,
                    Geometry = new SharpKml.Dom.Point {
                        Coordinate = new SharpKml.Base.Vector(unit.Position.Latitude, unit.Position.Longitude)
                    }
                };
            }

            // Add front linestring placemarks
            if (fronts != null) {
                var lines = MilitaryModel.DeploymentModel.KernelFlotGenerator.ExtractLineStrings(fronts) as System.Collections.IEnumerable;
                if (lines != null) {
                    int idx = 1;
                    foreach (var lineObj in lines) {
                        var line = lineObj as NetTopologySuite.Geometries.LineString;
                        if (line == null) continue;
                        var coords = new SharpKml.Dom.CoordinateCollection();
                        foreach (var c in line.Coordinates) {
                            coords.Add(new SharpKml.Base.Vector(c.Y, c.X)); // KML uses lat,lon
                        }
                        var placemark = new SharpKml.Dom.Placemark {
                            Name = $"Front {idx}",
                            Geometry = new SharpKml.Dom.LineString {
                                Coordinates = coords
                            }
                        };
                        kmlDoc.AddFeature(placemark);
                        idx++;
                    }
                }
            }

            // Add heading linestring placemarks with color
            foreach (var (line, color, name) in headingLines) {
                var coords = new SharpKml.Dom.CoordinateCollection();
                foreach (var c in line.Coordinates) {
                    coords.Add(new SharpKml.Base.Vector(c.Latitude, c.Longitude)); // KML uses lat,lon
                }
                var placemark = new SharpKml.Dom.Placemark {
                    Name = $"{name} Heading",
                    Geometry = new SharpKml.Dom.LineString {
                        Coordinates = coords
                    },
                    StyleUrl = new Uri($"#{color}-line", UriKind.Relative)
                };
                kmlDoc.AddFeature(placemark);
            }

            // Add styles for blue and red lines (Color32 expects ABGR: alpha, blue, green, red)
            var blueLineStyle = new SharpKml.Dom.Style {
                Id = "blue-line",
                Line = new SharpKml.Dom.LineStyle {
                    Color = new SharpKml.Base.Color32(255, 255, 0, 0), // blue (A,B,G,R)
                    Width = 3
                }
            };
            var redLineStyle = new SharpKml.Dom.Style {
                Id = "red-line",
                Line = new SharpKml.Dom.LineStyle {
                    Color = new SharpKml.Base.Color32(255, 0, 0, 255), // red (A,B,G,R)
                    Width = 3
                }
            };
            var grayLineStyle = new SharpKml.Dom.Style {
                Id = "gray-line",
                Line = new SharpKml.Dom.LineStyle {
                    Color = new SharpKml.Base.Color32(255, 128, 128, 128), // gray (symmetric)
                    Width = 3
                }
            };
            kmlDoc.AddStyle(blueLineStyle);
            kmlDoc.AddStyle(redLineStyle);
            kmlDoc.AddStyle(grayLineStyle);

            // Save KML to file
            var kml = new SharpKml.Dom.Kml { Feature = kmlDoc };
            using (var stream = System.IO.File.Create("output_fronts.kml")) {
                var serializer = SharpKml.Engine.KmlFile.Create(kml, false);
                serializer.Save(stream);
            }
        }

        private sealed class ReferenceByRefComparer : IEqualityComparer<ArmyUnit> {
            public bool Equals(ArmyUnit? x, ArmyUnit? y) => ReferenceEquals(x, y);
            public int GetHashCode(ArmyUnit obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }
}
