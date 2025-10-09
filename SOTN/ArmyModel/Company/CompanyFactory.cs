using MilitaryModel;
using SOTN.ArmyModel.Platoon;

namespace SOTN.ArmyModel.Company {
    public static class CompanyFactory {
        public static Company CreateCompany(Faction? faction, Nation? nation, ArmyUnitType type, string name)
        {
            var (resolvedFaction, resolvedNation) = AlignedArmyUnitFactory.ResolveIdentity(faction, nation);

            var description = $"{resolvedFaction}_{resolvedNation}_{type}_Company";

            // Build subordinate platoons according to the mapping for this company type.
            var subordinateDefs = PlatoonAssignmentsMapping.GetAssignments(type);

            var subordinates = new List<ArmyUnit>();
            for (int i = 0; i < 4; i++) {
                var subordinateName = $"{name}/{i}PLT";
                var company = PlatoonFactory.CreatePlatoon(resolvedFaction, resolvedNation, type, subordinateName);
                company.SetAssignment(ArmyUnitAssignment.FORWARD_DEPLOYABLE);
                subordinates.Add(company);
            }

            for (int i = 0; i < 1; i++) {
                var subordinateName = $"{name}/HQ/{i}PLT";
                var platoon = PlatoonFactory.CreatePlatoon(resolvedFaction, resolvedNation, ArmyUnitType.HEADQUARTERS, subordinateName);
                platoon.SetAssignment(ArmyUnitAssignment.HEADQUARTERS_AREA);
                subordinates.Add(platoon);
            }

            return new Company(type, name, description, resolvedFaction, resolvedNation, subordinates, Array.Empty<VehicleAllocation>());
        }
    }
}
