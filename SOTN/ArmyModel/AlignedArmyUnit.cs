using MilitaryModel;
using System.Collections.Generic;

namespace SOTN.ArmyModel {
    /// <summary>
    /// Army unit variant that carries alignment metadata (faction + optional nation).
    /// </summary>
    public class AlignedArmyUnit : ArmyUnit {
        public Faction Faction { get; }
        public Nation? Nation { get; }

        public AlignedArmyUnit(
            ArmyUnitEchelon echelon,
            ArmyUnitType type,
            string name,
            string description,
            Faction faction,
            Nation? nation,
            IReadOnlyList<SubordinateAssignment> subordinates,
            IReadOnlyList<VehicleAllocation> vehicles)
            : base(echelon, type, name, description, subordinates, vehicles) {
            Faction = faction;
            Nation = nation;
        }
    }
}