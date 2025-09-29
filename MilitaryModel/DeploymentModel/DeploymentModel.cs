using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Distance;

namespace MilitaryModel.DeploymentModel {
    public static class Distances {
        // meters
        public static readonly int DivisionToBrigade = 3000;
        public static readonly int BrigadeToBattalion = 1000;
        public static readonly int BattalionToCompany = 500;
        public static readonly int CompanyToPlatoon = 100;
    }
    public static class Angles {
        // degrees public static readonly int Default = 15;
    }

    public static class Tools {
        /* Calculate the best heading for the unit based on its position and the FLOT */
        public static double? CalculateUnitHeadingToFLOT(Coordinate unitPosition, LineString flotLine) {
            /*  Function Design:
                P_f0 is the point on the FLOT nearest to the unit's position
                if P_f0 is null, ERROR and return null
                theta_0 is the heading which the unit must travel from its position to reach P_f0
                - if theta_0 is null, ERROR and return null
                theta_1 is the angle relative to theta_0 which no longer intersects with the flot when rotated clockwise from theta_0
                theta_2 is the angle relative to theta_0 which no longer intersects with the flot when rotated counter-clockwise from theta_0
                - if theta_1 and theta_2 are not null, return theta_f
                ---- theta_f = theta_0 + (theta_1 + theta_2) / 2
                - if theta_1 and theta_2 are both null, the unit is encircled , return theta_0
                - if theta_1 or theta_2 is null, WARN the user and return theta_0
            */
            if (unitPosition == null || flotLine == null || flotLine.IsEmpty) {
                Console.Error.WriteLine("ERROR: Invalid input—unit position or FLOT is null/empty.");
                return null;
            }

            // Step 1: Find the closest point on the FLOT to the unit
            var pf0 = DistanceOp.NearestPoints(flotLine, new Point(unitPosition))[0];
            if (pf0 == null) {
                Console.Error.WriteLine("ERROR: Could not determine nearest point on FLOT.");
                return null;
            }

            // Step 2: Compute theta_0 (heading from unit to pf0)
            double dx = pf0.X - unitPosition.X;
            double dy = pf0.Y - unitPosition.Y;
            double theta0 = Math.Atan2(dy, dx) * (180.0 / Math.PI); // Convert to degrees
            if (double.IsNaN(theta0)) {
                Console.Error.WriteLine("ERROR: Invalid heading calculation.");
                return null;
            }

            // Step 3: Sweep clockwise and counter-clockwise to find escape angles
            double? theta1 = null;
            double? theta2 = null;
            const double sweepIncrement = 1.0; // degrees
            const double sweepLimit = 90.0;    // max sweep angle

            for (double sweep = sweepIncrement; sweep <= sweepLimit; sweep += sweepIncrement) {
                // Clockwise
                double angleCW = NormalizeAngle(theta0 + sweep);
                var rayCW = CreateRay(unitPosition, angleCW, 10000);
                if (!rayCW.Intersects(flotLine)) {
                    theta1 = sweep;
                    break;
                }
            }

            for (double sweep = sweepIncrement; sweep <= sweepLimit; sweep += sweepIncrement) {
                // Counter-clockwise
                double angleCCW = NormalizeAngle(theta0 - sweep);
                var rayCCW = CreateRay(unitPosition, angleCCW, 10000);
                if (!rayCCW.Intersects(flotLine)) {
                    theta2 = sweep;
                    break;
                }
            }

            // Step 4: Decision logic
            if (theta1.HasValue && theta2.HasValue) {
                double thetaF = NormalizeAngle(theta0 + (theta1.Value - theta2.Value) / 2.0);
                return thetaF;
            }

            if (!theta1.HasValue && !theta2.HasValue) {
                Console.WriteLine("INFO: Unit is encircled. Returning direct heading.");
                return theta0;
            }

            Console.WriteLine("WARN: Partial escape detected. Returning direct heading.");
            return theta0;
        }

        private static double NormalizeAngle(double angle) {
            angle %= 360;
            return angle < 0 ? angle + 360 : angle;
        }

        private static LineString CreateRay(Coordinate origin, double angleDegrees, double length) {
            double angleRadians = angleDegrees * Math.PI / 180.0;
            double dx = Math.Cos(angleRadians) * length;
            double dy = Math.Sin(angleRadians) * length;
            var end = new Coordinate(origin.X + dx, origin.Y + dy);
            return new LineString(new[] { origin, end });
        }
    }
}