using MilitaryModel;
using SOTN.ArmyModel.Company;
using SOTN.ArmyModel.Platoon;

namespace SOTN.ArmyModel.Battalion {
    public static class BattalionFactory {
        public static Battalion CreateBattalion(Faction? faction, Nation? nation, ArmyUnitType type, string name) {
            var (resolvedFaction, resolvedNation) = AlignedArmyUnitFactory.ResolveIdentity(faction, nation);

            var description = $"{resolvedFaction}_{resolvedNation}_{type}_Battalion";

            var subordinates = new List<ArmyUnit>();
            for (int i = 0; i < 4; i++) {
                var subordinateName = $"{name}/{i}CO";
                var company = CompanyFactory.CreateCompany(resolvedFaction, resolvedNation, type, subordinateName);
                company.SetAssignment(ArmyUnitAssignment.FORWARD_DEPLOYABLE);
                subordinates.Add(company);
            }

            for (int i = 0; i < 2; i++) {
                var subordinateName = $"{name}/HQ/{i}PLT";
                var platoon = PlatoonFactory.CreatePlatoon(resolvedFaction, resolvedNation, ArmyUnitType.HEADQUARTERS, subordinateName);
                platoon.SetAssignment(ArmyUnitAssignment.HEADQUARTERS_AREA);
                subordinates.Add(platoon);
            }

            var hqsam_name = $"{name}/HQ/SAM_PLT";
            var hqsam_platoon = BattalionHQSAM.Create(resolvedFaction, resolvedNation, hqsam_name);
            hqsam_platoon.SetAssignment(ArmyUnitAssignment.HQSAM);
            subordinates.Add(hqsam_platoon);

            return new Battalion(type, name, description, resolvedFaction, resolvedNation, subordinates, Array.Empty<VehicleAllocation>());
        }

        internal static class BattalionHQSAM {
            private static List<VehicleRoleAllocation> _vehicleRoleAllocations = new List<VehicleRoleAllocation> {
                new VehicleRoleAllocation(VehicleRole.AAA, 1),
                new VehicleRoleAllocation(VehicleRole.CAR, 1)
            };
            internal static Platoon.Platoon Create(Faction faction, Nation? nation, string name) {
                return PlatoonFactory.CreatePlatoon(faction, nation, ArmyUnitType.AIR_DEFENSE, name, _vehicleRoleAllocations);

            }
        }
    }
}