using MilitaryModel;
using SOTN.ArmyModel.Battalion;
using SOTN.ArmyModel.Platoon;

namespace SOTN.ArmyModel.Brigade {
    public static class BrigadeFactory {
        public static Brigade CreateBrigade(Faction? faction, Nation? nation, ArmyUnitType type, string name) {
            var (resolvedFaction, resolvedNation) = AlignedArmyUnitFactory.ResolveIdentity(faction, nation);

            var description = $"{resolvedFaction}_{resolvedNation}_{type}_Brigade";

            // A brigade contains 4 companies of the same unit type.
            var subordinates = new List<ArmyUnit>();
            for (int i = 0; i < 4; i++) {
                var subordinateName = $"{name}/{i}BN";
                var battalion = BattalionFactory.CreateBattalion(resolvedFaction, resolvedNation, type, subordinateName);
                battalion.SetAssignment(ArmyUnitAssignment.FORWARD_DEPLOYABLE);
                subordinates.Add(battalion);
            }

            for (int i = 0; i < 3; i++) {
                var subordinateName = $"{name}/HQ/{i}PLT";
                var platoon = PlatoonFactory.CreatePlatoon(resolvedFaction, resolvedNation, ArmyUnitType.HEADQUARTERS, subordinateName);
                platoon.SetAssignment(ArmyUnitAssignment.HEADQUARTERS_AREA);
                subordinates.Add(platoon);
            }

            var hqsam_name = $"{name}/HQ/SAM_PLT";
            var hqsam_platoon = BrigadeHQSAM.Create(resolvedFaction, resolvedNation, hqsam_name);
            hqsam_platoon.SetAssignment(ArmyUnitAssignment.HQSAM);
            subordinates.Add(hqsam_platoon);

            return new Brigade(type, name, description, resolvedFaction, resolvedNation, subordinates, Array.Empty<VehicleAllocation>());
        }

        internal static class BrigadeHQSAM {
            private static List<VehicleRoleAllocation> _vehicleRoleAllocations = new List<VehicleRoleAllocation> {
                new VehicleRoleAllocation(VehicleRole.AAA, 1),
                new VehicleRoleAllocation(VehicleRole.MANPADS, 1),
                new VehicleRoleAllocation(VehicleRole.CAR, 1)
            };
            internal static Platoon.Platoon Create(Faction faction, Nation? nation, string name) {
                return PlatoonFactory.CreatePlatoon(faction, nation, ArmyUnitType.AIR_DEFENSE, name, _vehicleRoleAllocations);

            }
        }
    }
}