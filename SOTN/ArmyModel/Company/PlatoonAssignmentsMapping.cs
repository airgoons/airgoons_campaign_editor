using MilitaryModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace SOTN.ArmyModel.Company
{
    internal sealed class PlatoonAssignment
    {
        public ArmyUnitType PlatoonType { get; }
        public ArmyUnitAssignment Assignment { get; }
        public int Count { get; }

        public PlatoonAssignment(ArmyUnitType platoonType, ArmyUnitAssignment assignment, int count)
        {
            PlatoonType = platoonType;
            Assignment = assignment;
            Count = Math.Max(0, count);
        }
    }

    internal static class PlatoonAssignmentsMapping
    {
        private static readonly ImmutableDictionary<ArmyUnitType, IReadOnlyList<PlatoonAssignment>> _map;

        static PlatoonAssignmentsMapping()
        {
            _map = new Dictionary<ArmyUnitType, IReadOnlyList<PlatoonAssignment>> {
                [ArmyUnitType.ARMOR] = new[] {
                    new PlatoonAssignment(ArmyUnitType.ARMOR, ArmyUnitAssignment.FORWARD_DEPLOYABLE, 2),
                    new PlatoonAssignment(ArmyUnitType.MECHINF, ArmyUnitAssignment.FORWARD_DEPLOYABLE, 2),
                    new PlatoonAssignment(ArmyUnitType.HEADQUARTERS, ArmyUnitAssignment.HEADQUARTERS_AREA, 1)
                },
                [ArmyUnitType.MECHINF] = new[] {
                    new PlatoonAssignment(ArmyUnitType.ARMOR, ArmyUnitAssignment.FORWARD_DEPLOYABLE, 2),
                    new PlatoonAssignment(ArmyUnitType.MECHINF, ArmyUnitAssignment.FORWARD_DEPLOYABLE, 2),
                    new PlatoonAssignment(ArmyUnitType.HEADQUARTERS, ArmyUnitAssignment.HEADQUARTERS_AREA, 1)
                },
                // Infantry company -> 3 infantry platoons + 1 HQ
                [ArmyUnitType.INFANTRY] = new[] {
                    new PlatoonAssignment(ArmyUnitType.INFANTRY, ArmyUnitAssignment.FORWARD_DEPLOYABLE, 4),
                    new PlatoonAssignment(ArmyUnitType.HEADQUARTERS, ArmyUnitAssignment.HEADQUARTERS_AREA, 1)
                },
                // Recon company -> 3 recon platoons + 1 HQ
                [ArmyUnitType.RECONNAISSANCE] = new[] {
                    new PlatoonAssignment(ArmyUnitType.RECONNAISSANCE, ArmyUnitAssignment.FORWARD_DEPLOYABLE, 4),
                    new PlatoonAssignment(ArmyUnitType.HEADQUARTERS, ArmyUnitAssignment.HEADQUARTERS_AREA, 1)
                },
                // Marine follows infantry organization
                [ArmyUnitType.MARINE] = new[] {
                    new PlatoonAssignment(ArmyUnitType.MARINE, ArmyUnitAssignment.FORWARD_DEPLOYABLE, 4),
                    new PlatoonAssignment(ArmyUnitType.HEADQUARTERS, ArmyUnitAssignment.HEADQUARTERS_AREA, 1)
                },
                [ArmyUnitType.ARTILLERY] = new[] {
                    new PlatoonAssignment(ArmyUnitType.ARTILLERY, ArmyUnitAssignment.FORWARD_DEPLOYABLE, 4),
                    new PlatoonAssignment(ArmyUnitType.HEADQUARTERS, ArmyUnitAssignment.HEADQUARTERS_AREA, 1)
                },
                [ArmyUnitType.MISSILE] = new[] {
                    new PlatoonAssignment(ArmyUnitType.MISSILE, ArmyUnitAssignment.FORWARD_DEPLOYABLE, 4),
                    new PlatoonAssignment(ArmyUnitType.HEADQUARTERS, ArmyUnitAssignment.HEADQUARTERS_AREA, 1)
                },
                [ArmyUnitType.LOGISTICS] = new[] {
                    new PlatoonAssignment(ArmyUnitType.LOGISTICS, ArmyUnitAssignment.HEADQUARTERS_AREA, 4)
                },
                // Default: no subordinate platoons
            }.ToImmutableDictionary();
        }

        public static IReadOnlyList<PlatoonAssignment> GetAssignments(ArmyUnitType companyType)
        {
            if (_map.TryGetValue(companyType, out var list))
                return list;
            return Array.Empty<PlatoonAssignment>();
        }

        public static bool TryGetAssignments(ArmyUnitType companyType, out IReadOnlyList<PlatoonAssignment> assignments)
        {
            if (_map.TryGetValue(companyType, out var list))
            {
                assignments = list;
                return true;
            }

            assignments = Array.Empty<PlatoonAssignment>();
            return false;
        }
    }
}