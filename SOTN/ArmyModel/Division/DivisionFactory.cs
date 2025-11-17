using MilitaryModel;
using SOTN.ArmyModel.Brigade;
using SOTN.ArmyModel.Platoon;

namespace SOTN.ArmyModel.Division {
    public static class DivisionFactory {
        public static Division CreateDivision(Faction? faction, Nation? nation, ArmyUnitType type, string name) {
            var (resolvedFaction, resolvedNation) = AlignedArmyUnitFactory.ResolveIdentity(faction, nation);

            var description = $"{resolvedFaction}_{resolvedNation}_{type}_Division";

            // A division contains 4 brigades of the same unit type.
            var subordinates = new List<ArmyUnit>();
            for (int i = 0; i < 4; i++) {
                var subordinateName = $"{name}/{i}BDE";
                var brigade = BrigadeFactory.CreateBrigade(resolvedFaction, resolvedNation, type, subordinateName);
                brigade.SetAssignment(ArmyUnitAssignment.FORWARD_DEPLOYABLE);
                subordinates.Add(brigade);
            }
            for (int i = 0; i < 1; i++) {
                var subordinateName = $"{name}/HQ/{i}PLT";
                var platoon = PlatoonFactory.CreatePlatoon(resolvedFaction, resolvedNation, ArmyUnitType.HEADQUARTERS, subordinateName);
                platoon.SetAssignment(ArmyUnitAssignment.HEADQUARTERS_AREA);
                subordinates.Add(platoon);
            }

            var hqsam_name = $"{name}/HQ/SAM_PLT";
            var hqsam_platoons = DivisionHQSAM.Create(resolvedFaction, resolvedNation, hqsam_name);

            subordinates = subordinates.Concat(hqsam_platoons).ToList();

            return new Division(type, name, description, resolvedFaction, resolvedNation, subordinates, Array.Empty<VehicleAllocation>());
        }

        internal static class DivisionHQSAM {
            private static List<VehicleRoleAllocation> _vehicleRoleAllocations = new List<VehicleRoleAllocation> {
                new VehicleRoleAllocation(VehicleRole.AAA, 1),
                new VehicleRoleAllocation(VehicleRole.MANPADS, 1),
                new VehicleRoleAllocation(VehicleRole.SAM_Short, 1)
            };
            internal static List<ArmyUnit> Create(Faction faction, Nation? nation, string name) {
                var platoons = new List<ArmyUnit>();
                int i = 0;
                foreach (var roleAlloc in _vehicleRoleAllocations) {
                    // MANTIS filter
                    var specificName = $"{roleAlloc.Role.ToString()} {name}-{i}";
                    var platoon = PlatoonFactory.CreatePlatoon(faction, nation, ArmyUnitType.AIR_DEFENSE, specificName, new List<VehicleRoleAllocation> { roleAlloc });
                    platoon.SetAssignment(ArmyUnitAssignment.HQSAM);
                    platoons.Add(platoon);

                    ++i;
                }
                return platoons;
            }
        }
    }
}