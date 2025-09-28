using MilitaryModel;
using SOTN.ArmyModel.Battalion;
using SOTN.ArmyModel.Platoon;

namespace SOTN.ArmyModel.Brigade {
    public static class BrigadeFactory {
        public static Brigade CreateBrigade(Faction? faction, Nation? nation, ArmyUnitType type, string name) {
            var (resolvedFaction, resolvedNation) = AlignedArmyUnitFactory.ResolveIdentity(faction, nation);

            var description = $"{resolvedFaction}_{resolvedNation}_{type}_Brigade";

            // A brigade contains 4 companies of the same unit type.
            var subordinateAssignments = new List<SubordinateAssignment>();
            for (int i = 0; i < 4; i++) {
                var subordinateName = $"{name}/{i}BN";
                var battalion = BattalionFactory.CreateBattalion(resolvedFaction, resolvedNation, type, subordinateName);
                subordinateAssignments.Add(new SubordinateAssignment(battalion, ArmyUnitAssignment.FORWARD_DEPLOYABLE));
            }

            for (int i = 0; i < 3; i++) {
                var subordinateName = $"{name}/HQ/{i}PLT";
                var platoon = PlatoonFactory.CreatePlatoon(resolvedFaction, resolvedNation, ArmyUnitType.HEADQUARTERS, subordinateName);
                subordinateAssignments.Add(new SubordinateAssignment(platoon, ArmyUnitAssignment.HEADQUARTERS_AREA));
            }

            // TODO:  Add Brigade air defense

            return new Brigade(type, name, description, resolvedFaction, resolvedNation, subordinateAssignments, Array.Empty<VehicleAllocation>());
        }
    }
}