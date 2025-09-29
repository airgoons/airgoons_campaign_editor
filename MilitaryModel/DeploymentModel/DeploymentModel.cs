using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Distance;
using NetTopologySuite.Operation.Union;
using System;
using System.Linq;
using System.Collections.Generic;

namespace MilitaryModel.DeploymentModel {
    public static class Distances {
        // meters
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
        private static double GetAngle(double radius, double linearDistance) {
            double theta = Math.Acos(
                Math.Pow(linearDistance, 2) / (2 * radius * linearDistance)
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
        public static double? CalculateUnitHeadingToFLOT(Coordinate unitPosition, LineString flotLine)
        {
            if (unitPosition == null || flotLine == null || flotLine.IsEmpty)
            {
                Console.Error.WriteLine("ERROR: Invalid input—unit position or FLOT is null/empty.");
                return null;
            }

            var P_f0 = DistanceOp.NearestPoints(flotLine, new Point(unitPosition))[0];
            if (P_f0 == null)
            {
                Console.Error.WriteLine("ERROR: Could not determine nearest point on FLOT.");
                return null;
            }

            double dx = P_f0.X - unitPosition.X;
            double dy = P_f0.Y - unitPosition.Y;
            double theta_0 = NormalizeAngle(Math.Atan2(dy, dx) * (180.0 / Math.PI)); // Convert to degrees
            if (double.IsNaN(theta_0))
            {
                Console.Error.WriteLine("ERROR: Invalid heading calculation.");
                return null;
            }

            double? theta_1 = null;
            double? theta_2 = null;
            const double sweepIncrement = 1.0;
            const double sweepLimit = 180.0;

            for (double sweep = sweepIncrement; sweep <= sweepLimit; sweep += sweepIncrement)
            {
                double angleCW = NormalizeAngle(theta_0 - sweep);
                var rayCW = CreateRay(unitPosition, angleCW, 10000);
                if (!rayCW.Intersects(flotLine))
                {
                    theta_1 = sweep;
                    break;
                }
            }

            for (double sweep = sweepIncrement; sweep <= sweepLimit; sweep += sweepIncrement)
            {
                double angleCCW = NormalizeAngle(theta_0 + sweep);
                var rayCCW = CreateRay(unitPosition, angleCCW, 10000);
                if (!rayCCW.Intersects(flotLine))
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
                    return MathAngleToCompass(theta_0);
                }

                double meanMathAngle = NormalizeAngle(angleCW + diff / 2.0);
                return MathAngleToCompass(meanMathAngle);
            }

            if (!theta_1.HasValue && !theta_2.HasValue)
            {
                Console.WriteLine("INFO: Unit is encircled. Returning direct heading.");
                return MathAngleToCompass(theta_0);
            }

            Console.WriteLine("WARN: Partial escape detected. Returning direct heading.");
            return MathAngleToCompass(theta_0);
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
    }
}