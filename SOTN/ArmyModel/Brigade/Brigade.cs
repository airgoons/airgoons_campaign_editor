using MilitaryModel;

namespace SOTN.ArmyModel.Brigade {
    public class Brigade : AlignedArmyUnit {
        internal Brigade(ArmyUnitType type, string name, string description, Faction faction, Nation? nation, IReadOnlyList<SubordinateAssignment> subordinates, IReadOnlyList<VehicleAllocation> vehicles)
            : base(ArmyUnitEchelon.BRIGADE, type, name, description, faction, nation, subordinates, vehicles) { }
    }
}