using MilitaryModel;

namespace SOTN.ArmyModel.Platoon {
    public class Platoon : AlignedArmyUnit {
        internal Platoon(ArmyUnitType type, string name, string description, Faction faction, Nation? nation, IReadOnlyList<ArmyUnit> subordinates, IReadOnlyList<VehicleAllocation> vehicles)
            : base(ArmyUnitEchelon.PLATOON, type, name, description, faction, nation, subordinates, vehicles) {}
    }
}
