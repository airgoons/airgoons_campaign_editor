using MilitaryModel;

namespace SOTN.ArmyModel {
    /// <summary>
    /// Army unit variant that carries alignment metadata (faction + optional nation).
    /// </summary>
    public class AlignedArmyUnit : ArmyUnit {
        public AlignedArmyUnit(
            ArmyUnitEchelon echelon,
            ArmyUnitType type,
            string name,
            string description,
            Faction faction,
            Nation? nation,
            IReadOnlyList<ArmyUnit> subordinates,
            IReadOnlyList<VehicleAllocation> vehicles)
            : base(echelon, type, name, description, faction, nation, subordinates, vehicles) {
        }
    }
}