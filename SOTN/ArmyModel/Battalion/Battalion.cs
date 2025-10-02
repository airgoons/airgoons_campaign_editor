using MilitaryModel;

namespace SOTN.ArmyModel.Battalion {
    public class Battalion : AlignedArmyUnit {
        internal Battalion(ArmyUnitType type, string name, string description, Faction faction, Nation? nation, IReadOnlyList<ArmyUnit> subordinates, IReadOnlyList<VehicleAllocation> vehicles)
            : base(ArmyUnitEchelon.BATTALION, type, name, description, faction, nation, subordinates, vehicles) { }
    }
}