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

            var subordinateAssignments = new List<SubordinateAssignment>();
            foreach (var def in subordinateDefs)
            {
                for (int i = 0; i < def.Count; i++)
                {
                    var subordinateName = $"{name}/{i}PLT_{type.ToString()}";
                    var platoon = PlatoonFactory.CreatePlatoon(resolvedFaction, resolvedNation, def.PlatoonType, subordinateName);
                    subordinateAssignments.Add(new SubordinateAssignment(platoon, def.Assignment));
                }
            }

            return new Company(type, name, description, resolvedFaction, resolvedNation, subordinateAssignments, Array.Empty<VehicleAllocation>());
        }
    }
}
