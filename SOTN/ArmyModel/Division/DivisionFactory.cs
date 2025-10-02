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
            for (int i = 0; i < 5; i++) {
                var subordinateName = $"{name}/HQ/{i}PLT";
                var platoon = PlatoonFactory.CreatePlatoon(resolvedFaction, resolvedNation, ArmyUnitType.HEADQUARTERS, subordinateName);
                platoon.SetAssignment(ArmyUnitAssignment.HEADQUARTERS_AREA);
                subordinates.Add(platoon);
            }

            var hqsam_name = $"{name}/HQ/SAM_PLT";
            var hqsam_platoon = DivisionHQSAM.Create(resolvedFaction, resolvedNation, hqsam_name);
            hqsam_platoon.SetAssignment(ArmyUnitAssignment.HQSAM);
            subordinates.Add(hqsam_platoon);

            return new Division(type, name, description, resolvedFaction, resolvedNation, subordinates, Array.Empty<VehicleAllocation>());
        }

        internal static class DivisionHQSAM {
            private static List<VehicleRoleAllocation> _vehicleRoleAllocations = new List<VehicleRoleAllocation> {
                new VehicleRoleAllocation(VehicleRole.SAM_Short, 1),
                new VehicleRoleAllocation(VehicleRole.CAR, 1)
            };
            internal static Platoon.Platoon Create(Faction faction, Nation? nation, string name) {
                return PlatoonFactory.CreatePlatoon(faction, nation, ArmyUnitType.AIR_DEFENSE, name, _vehicleRoleAllocations);

            }
        }
    }
}