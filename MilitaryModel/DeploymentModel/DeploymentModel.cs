using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Distance;
using NetTopologySuite.Operation.Union;
using System;
using System.Linq;
using System.Collections.Generic;
using SharpKml.Base;

namespace MilitaryModel.DeploymentModel {
    public static class Distances {
        // meters
        // Increased spacing to reduce overlap at map scale and allow vehicle/formation footprints
        public static readonly double DivisionToBrigade = 3000;
        public static readonly double BrigadeToBrigade = DivisionToBrigade / 2;
        public static readonly double BrigadeToBattalion = 1000;
        public static readonly double BattalionToBattalion = BrigadeToBattalion / 2;
        public static readonly double BattalionToCompany = 500;
        public static readonly double CompanyToCompany = BattalionToCompany / 2;
        public static readonly double CompanyToPlatoon = 100;
        public static readonly double PlatoonToPlatoon = CompanyToPlatoon / 2;
    }

    public static class Angles {
        private static double GetAngle(double radius, double doubleDistance) {
            double theta = Math.Acos(
                Math.Pow(doubleDistance, 2) / (2 * radius * doubleDistance)
            );
            return theta;
        }
        public static readonly double BrigadeToBrigade = GetAngle(Distances.DivisionToBrigade, Distances.BrigadeToBrigade);
        public static readonly double BattalionToBattalion = GetAngle(Distances.BrigadeToBattalion, Distances.BattalionToBattalion);
        public static readonly double CompanyToCompany = GetAngle(Distances.BattalionToCompany, Distances.CompanyToCompany);
        public static readonly double PlatoonToPlatoon = GetAngle(Distances.CompanyToPlatoon, Distances.PlatoonToPlatoon);
    }

    public static class Tools
    {
        /* Calculate the best heading for the unit based on its position and the FLOT */
        public static double? CalculateUnitHeadingToFLOT(Coordinate unitPosition, Geometry? flotLine)
        {
            if (unitPosition == null || flotLine == null || flotLine.IsEmpty)
            {
                Console.Error.WriteLine("ERROR: Invalid input—unit position or FLOT is null/empty.");
                return null;
            }

            LineString? selectedLine = null;
            double minDist = double.MaxValue;

            if (flotLine is LineString ls)
            {
                selectedLine = ls;
                var np = DistanceOp.NearestPoints(ls, new Point(unitPosition))[0];
                minDist = Math.Sqrt(Math.Pow(np.X - unitPosition.X, 2) + Math.Pow(np.Y - unitPosition.Y, 2));
            }
            else if (flotLine is MultiLineString mls)
            {
                for (int i = 0; i < mls.NumGeometries; i++)
                {
                    var candidate = mls.GetGeometryN(i) as LineString;
                    if (candidate == null || candidate.IsEmpty) continue;
                    var np = DistanceOp.NearestPoints(candidate, new Point(unitPosition))[0];
                    double dist = Math.Sqrt(Math.Pow(np.X - unitPosition.X, 2) + Math.Pow(np.Y - unitPosition.Y, 2));
                    if (dist < minDist)
                    {
                        minDist = dist;
                        selectedLine = candidate;
                    }
                }
            }
            else
            {
                Console.Error.WriteLine("ERROR: FLOT geometry must be a LineString or MultiLineString.");
                return null;
            }

            if (selectedLine == null)
            {
                Console.Error.WriteLine("ERROR: Could not find a valid LineString in FLOT.");
                return null;
            }

            var P_f0 = DistanceOp.NearestPoints(selectedLine, new Point(unitPosition))[0];
            if (P_f0 == null)
            {
                Console.Error.WriteLine("ERROR: Could not determine nearest point on FLOT.");
                return null;
            }

            double dx = P_f0.X - unitPosition.X;
            double dy = P_f0.Y - unitPosition.Y;
            double distanceToFLOT = Math.Sqrt(dx * dx + dy * dy);
            double maxRayLength = 2.0 * distanceToFLOT;
            double theta_0 = NormalizeAngle(Math.Atan2(dy, dx) * (180.0 / Math.PI)); // Convert to degrees
            if (double.IsNaN(theta_0))
            {
                Console.Error.WriteLine("ERROR: Invalid heading calculation.");
                return null;
            }

            
            return Math.Round(MathAngleToCompass(theta_0), 1);
            // skip ray sweep -- wonko debug

            double? theta_1 = null;
            double? theta_2 = null;
            const double sweepIncrement = 1.0;
            const double sweepLimit = 180.0;

            for (double sweep = sweepIncrement; sweep <= sweepLimit; sweep += sweepIncrement)
            {
                double angleCW = NormalizeAngle(theta_0 - sweep);
                var rayCW = CreateRay(unitPosition, angleCW, maxRayLength);
                if (!rayCW.Intersects(selectedLine))
                {
                    theta_1 = sweep;
                    break;
                }
            }

            for (double sweep = sweepIncrement; sweep <= sweepLimit; sweep += sweepIncrement)
            {
                double angleCCW = NormalizeAngle(theta_0 + sweep);
                var rayCCW = CreateRay(unitPosition, angleCCW, maxRayLength);
                if (!rayCCW.Intersects(selectedLine))
                {
                    theta_2 = -sweep;
                    break;
                }
            }

            if (theta_1.HasValue && theta_2.HasValue)
            {
                double angleCW = NormalizeAngle(theta_0 - theta_1.Value);
                double angleCCW = NormalizeAngle(theta_0 - theta_2.Value);
                double diff = ShortestSignedAngle(angleCW, angleCCW);
                if (Math.Abs(Math.Abs(diff) - 180.0) < 1e-9)
                {
                    return Math.Round(MathAngleToCompass(theta_0), 1);
                }

                double meanMathAngle = NormalizeAngle(angleCW + diff / 2.0);
                return Math.Round(MathAngleToCompass(meanMathAngle), 1);
            }

            if (!theta_1.HasValue && !theta_2.HasValue)
            {
                Console.WriteLine("INFO: Unit is encircled. Returning direct heading.");
                return Math.Round(MathAngleToCompass(theta_0), 1);
            }

            Console.WriteLine("WARN: Partial escape detected. Returning direct heading.");
            return Math.Round(MathAngleToCompass(theta_0), 1);
        }

        private static double NormalizeAngle(double angle)
        {
            angle %= 360;
            return angle < 0 ? angle + 360 : angle;
        }

        private static double ShortestSignedAngle(double from, double to)
        {
            double diff = (to - from + 540.0) % 360.0 - 180.0;
            return diff;
        }

        private static double MathAngleToCompass(double mathAngle)
        {
            return NormalizeAngle(90.0 - mathAngle);
        }

        private static LineString CreateRay(Coordinate origin, double angleDegrees, double length)
        {
            double angleRadians = angleDegrees * Math.PI / 180.0;
            double dx = Math.Cos(angleRadians) * length;
            double dy = Math.Sin(angleRadians) * length;
            var end = new Coordinate(origin.X + dx, origin.Y + dy);
            return new LineString(new[] { origin, end });
        }

        // --- New: doctrinal deployment application ---
        public static List<ArmyUnit> ApplyDoctrinalDeployment(ArmyUnit root, Geometry? flotLine = null, double deploymentPercentage = 100.0)
        {
            var collected = new List<ArmyUnit>();
            if (root == null) return collected;

            var visited = new HashSet<ArmyUnit>(ReferenceEqualityComparer.Instance);
            ApplyDoctrinalDeploymentInternal(root, collected, visited, flotLine, deploymentPercentage);
            return collected;
        }

        private static void ApplyDoctrinalDeploymentInternal(ArmyUnit root, List<ArmyUnit> collected, HashSet<ArmyUnit> visited, Geometry? flotLine, double deploymentPercentage)
        {
            if (root == null) return;
            if (!visited.Add(root)) return; // prevent cycles

            // ensure top-level has position and heading; if not, skip placement for children
            bool canPlaceChildren = root.Position != null && root.Heading != null;

            // Place forward-deployable subordinates along an arc centered on parent's heading
            var forwardSubs = root.SubordinateAssignments
                .Where(sa => sa.Assignment == ArmyUnitAssignment.FORWARD_DEPLOYABLE && sa.Subordinate != null)
                .Select(sa => sa.Subordinate)
                .ToList();

            if (canPlaceChildren && forwardSubs.Count > 0 && deploymentPercentage > 0.0)
            {
                // choose separation distance based on parent->child echelon
                double lineLength = GetSeparationDistance(root.Echelon, forwardSubs.First().Echelon); // total available line length (meters)
                // peer spacing along the line (desired distance between peers)
                double peerSpacing = forwardSubs.First().Echelon switch
                {
                    ArmyUnitEchelon.BRIGADE => Distances.BrigadeToBrigade,
                    ArmyUnitEchelon.BATTALION => Distances.BattalionToBattalion,
                    ArmyUnitEchelon.COMPANY => Distances.CompanyToCompany,
                    ArmyUnitEchelon.PLATOON => Distances.PlatoonToPlatoon,
                    _ => Distances.CompanyToCompany,
                };

                double centerHeading = root.Heading.Value; // degrees compass

                // determine how many subordinates to position based on percentage
                int countToPlace = (int)Math.Round(forwardSubs.Count * Math.Clamp(deploymentPercentage, 0.0, 100.0) / 100.0);
                // If user requested some deployment but rounding produced 0, place at least one
                if (deploymentPercentage > 0.0 && countToPlace == 0)
                {
                    countToPlace = 1;
                }
                countToPlace = Math.Max(0, Math.Min(forwardSubs.Count, countToPlace));

                if (countToPlace > 0)
                {
                    int startIndex = (forwardSubs.Count - countToPlace) / 2; // center the selected subset within the list

                    // Build the list of subordinates that will be positioned (centered slice)
                    var selectedSubs = forwardSubs.Skip(startIndex).Take(countToPlace).ToList();

                    // desired span for placed group using peerSpacing (no implicit clamping)
                    double desiredSpan = (selectedSubs.Count > 1) ? (selectedSubs.Count - 1) * peerSpacing : 0.0;
                    double actualSpacing = (selectedSubs.Count > 1) ? desiredSpan / (selectedSubs.Count - 1) : 0.0;  // actualSpacing = peerSpacing...

                    // orientation of the placement line: perpendicular to parent's heading
                    double lineOrientation = NormalizeAngle(centerHeading + 90.0);

                    // midpoint of the placement line is at `lineLength` meters forward from the parent along parent's heading
                    var (midLat, midLon) = DestinationPoint(root.Position.Latitude, root.Position.Longitude, centerHeading, lineLength);

                    // compute half-span so first and last are symmetric around midpoint
                    double halfSpan = desiredSpan / 2.0;
                    // the first point offset relative to midpoint (negative half-span)
                    double firstOffset = -halfSpan;

                    for (int k = 0; k < selectedSubs.Count; k++) {
                        var sub = selectedSubs[k];
                        if (sub == null) continue;

                        // offset along the line from reference point (meters)
                        // The reference point is where the midpoint of the line between first and last subordinates should be
                        double offsetAlongLine = firstOffset + k * actualSpacing;

                        // compute bearing and distance for destination point
                        // Always use the absolute value for distance, and adjust bearing based on sign
                        double bearing = lineOrientation;
                        double distance = Math.Abs(offsetAlongLine);
                        
                        // If offset is negative, adjust the bearing to go in the opposite direction
                        if (offsetAlongLine < 0) {
                            bearing = NormalizeAngle(lineOrientation + 180.0);
                        }

                        var (finalLat, finalLon) = DestinationPoint(midLat, midLon, bearing, distance);
                        sub.SetPosition(new Vector(finalLat, finalLon));

                        sub.SetHeading(centerHeading);
                        // Recalculate subordinate heading based on FLOT if provided, otherwise default to parent heading
                        //double? recalculatedHeading = null;
                        //if (flotLine != null)
                        //{
                        //    var coord = new Coordinate(finalLon, finalLat); // Coordinate(X=lon, Y=lat)
                        //    recalculatedHeading = CalculateUnitHeadingToFLOT(coord, flotLine);
                        //}
                        //if (recalculatedHeading.HasValue)
                        //{
                        //    sub.SetHeading(recalculatedHeading.Value);
                        //}
                        //else
                        //{
                        //    sub.SetHeading(centerHeading);
                        //}

                        // debug: log placement details
                        try {
                            // Calculate actual midpoint for verification
                            double firstSubOffset = firstOffset;
                            double lastSubOffset = firstOffset + (selectedSubs.Count - 1) * actualSpacing;
                            double calculatedMidpoint = (firstSubOffset + lastSubOffset) / 2.0;
                        }
                        catch { }
                    }
                }
            }

            // Optionally, place headquarters-area subordinates near the parent (cluster)
            var hqSubs = root.SubordinateAssignments
                .Where(sa => sa.Assignment == ArmyUnitAssignment.HEADQUARTERS_AREA && sa.Subordinate != null)
                .Select(sa => sa.Subordinate)
                .ToList();
            if (canPlaceChildren && hqSubs.Count > 0 && deploymentPercentage > 0.0)
            {
                // cluster radius small fraction of parent separation (or fixed)
                double clusterRadius = 0.2 * GetSeparationDistance(root.Echelon, hqSubs.First().Echelon);
                if (clusterRadius <= 0) clusterRadius = 50.0;

                int countToPlace = (int)Math.Round(hqSubs.Count * Math.Clamp(deploymentPercentage, 0.0, 100.0) / 100.0);
                if (deploymentPercentage > 0.0 && countToPlace == 0)
                {
                    countToPlace = 1;
                }
                countToPlace = Math.Max(0, Math.Min(hqSubs.Count, countToPlace));
                int startIndex = (hqSubs.Count - countToPlace) / 2;

                for (int i = 0; i < hqSubs.Count; i++)
                {
                    if (i < startIndex || i >= startIndex + countToPlace) continue;
                    var sub = hqSubs[i];
                    if (sub == null) continue;
                    double angle = NormalizeAngle(root.Heading.Value + i * (360.0 / Math.Max(1, hqSubs.Count)));
                    var (lat, lon) = DestinationPoint(root.Position.Latitude, root.Position.Longitude, angle, clusterRadius);
                    sub.SetPosition(new Vector(lat, lon));

                    sub.SetHeading(root.Heading.Value);
                    // Recalculate HQ subordinate heading based on FLOT if provided, otherwise default to parent heading
                    // disable heading recalculation
                    //double? hqRecalc = null;
                    //if (flotLine != null)
                    //{
                    //    var coord = new Coordinate(lon, lat);
                    //    hqRecalc = CalculateUnitHeadingToFLOT(coord, flotLine);
                    //}
                    //if (hqRecalc.HasValue)
                    //{
                    //    sub.SetHeading(hqRecalc.Value);
                    //}
                    //else
                    //{
                    //    sub.SetHeading(root.Heading.Value);
                    //}

                    // Add headquarters-area subordinate to collected in pre-order and mark visited to avoid duplicate processing
                    if (visited.Add(sub))
                    {
                        collected.Add(sub);
                    }
                }
            }

            // Add root to collected before recursing so list is pre-order; change placement if needed
            // Only add the root once (defensive: check by reference to avoid duplicates)
            if (!collected.Any(u => ReferenceEquals(u, root)))
            {
                collected.Add(root);
            }

            // Recurse into all subordinates that were positioned (regardless of echelon)
            foreach (var sa in root.SubordinateAssignments ?? Enumerable.Empty<SubordinateAssignment>())
            {
                if (sa?.Subordinate == null) continue;

                // Only recurse into positioned subordinates that haven't already been visited
                if (sa.Subordinate.Position != null && !visited.Contains(sa.Subordinate))
                {
                    ApplyDoctrinalDeploymentInternal(sa.Subordinate, collected, visited, flotLine, deploymentPercentage);
                }
            }
        }

        // local reference equality comparer to avoid cycles
        private sealed class ReferenceEqualityComparer : IEqualityComparer<ArmyUnit> {
            public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();
            public bool Equals(ArmyUnit? x, ArmyUnit? y) => ReferenceEquals(x, y);
            public int GetHashCode(ArmyUnit obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }

        private static double GetSeparationDistance(ArmyUnitEchelon parent, ArmyUnitEchelon child)
        {
            // Map separation by subordinate echelon so that each subordinate echelon
            // is placed on a circle whose radius is doctrinally defined.
            // Brigades -> placed at DivisionToBrigade from their Division, etc.
            return child switch
            {
                ArmyUnitEchelon.BRIGADE => Distances.DivisionToBrigade,
                ArmyUnitEchelon.BATTALION => Distances.BrigadeToBattalion,
                ArmyUnitEchelon.COMPANY => Distances.BattalionToCompany,
                ArmyUnitEchelon.PLATOON => Distances.CompanyToPlatoon,
                // fallback: use peer-level defaults for same-level spacing
                ArmyUnitEchelon.DIVISION => Distances.DivisionToBrigade,
                _ => 500.0,
            };
        }

        private static double GetPeerAngleRad(ArmyUnitEchelon echelon)
        {
            return echelon switch
            {
                ArmyUnitEchelon.BRIGADE => Angles.BrigadeToBrigade,
                ArmyUnitEchelon.BATTALION => Angles.BattalionToBattalion,
                ArmyUnitEchelon.COMPANY => Angles.CompanyToCompany,
                ArmyUnitEchelon.PLATOON => Angles.PlatoonToPlatoon,
                _ => Angles.CompanyToCompany,
            };
        }

        // geodesic destination point (WGS84)
        private static (double lat, double lon) DestinationPoint(double latDeg, double lonDeg, double bearingDeg, double distanceMeters)
        {
            const double R = 6378137.0; // Earth radius (meters)
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
            while (lon > 180.0) lon -= 360.0;
            while (lon < -180.0) lon += 360.0;
            return lon;
        }
    }
}