using MilitaryModel;

namespace SOTN.ArmyModel.Division {
    public class Division : AlignedArmyUnit {
        internal Division(ArmyUnitType type, string name, string description, Faction faction, Nation? nation, IReadOnlyList<ArmyUnit> subordinates, IReadOnlyList<VehicleAllocation> vehicles)
            : base(ArmyUnitEchelon.DIVISION, type, name, description, faction, nation, subordinates, vehicles) { }
    }
}