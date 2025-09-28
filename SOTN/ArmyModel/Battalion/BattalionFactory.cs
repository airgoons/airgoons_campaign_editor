using MilitaryModel;
using SOTN.ArmyModel.Company;
using SOTN.ArmyModel.Platoon;

namespace SOTN.ArmyModel.Battalion {
    public static class BattalionFactory {
        public static Battalion CreateBattalion(Faction? faction, Nation? nation, ArmyUnitType type, string name) {
            var (resolvedFaction, resolvedNation) = AlignedArmyUnitFactory.ResolveIdentity(faction, nation);

            var description = $"{resolvedFaction}_{resolvedNation}_{type}_Battalion";

            var subordinateAssignments = new List<SubordinateAssignment>();
            for (int i = 0; i < 4; i++) {
                var subordinateName = $"{name}/{i}CO";
                var company = CompanyFactory.CreateCompany(resolvedFaction, resolvedNation, type, subordinateName);
                subordinateAssignments.Add(new SubordinateAssignment(company, ArmyUnitAssignment.FORWARD_DEPLOYABLE));
            }

            for (int i = 0; i < 3; i++) {
                var subordinateName = $"{name}/HQ/{i}PLT";
                var platoon = PlatoonFactory.CreatePlatoon(resolvedFaction, resolvedNation, ArmyUnitType.HEADQUARTERS, subordinateName);
                subordinateAssignments.Add(new SubordinateAssignment(platoon, ArmyUnitAssignment.HEADQUARTERS_AREA));
            }

            // TODO:  Add Battalion air defense

            return new Battalion(type, name, description, resolvedFaction, resolvedNation, subordinateAssignments, Array.Empty<VehicleAllocation>());
        }
    }
}