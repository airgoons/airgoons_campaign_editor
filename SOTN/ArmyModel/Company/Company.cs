using MilitaryModel;

namespace SOTN.ArmyModel.Company {
    public class Company : AlignedArmyUnit {
        internal Company(ArmyUnitType type, string name, string description, Faction faction, Nation? nation, IReadOnlyList<SubordinateAssignment> subordinates, IReadOnlyList<VehicleAllocation> vehicles)
            : base(ArmyUnitEchelon.COMPANY, type, name, description, faction, nation, subordinates, vehicles) { }
    }
}
