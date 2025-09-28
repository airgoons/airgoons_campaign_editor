namespace MilitaryModel
{
    public enum ArmyUnitEchelon {
        FRONT,
        ARMY,
        CORPS,
        DIVISION,
        BRIGADE,
        REGIMENT,
        BATTALION,
        COMPANY,
        PLATOON
    }

    public enum ArmyUnitType {
        ARMOR,
        ARTILLERY,
        HEADQUARTERS,
        INFANTRY,
        MECHINF,
        MISSILE,
        MARINE,
        RECONNAISSANCE,
        AMPHIB_ARMOR,
        AMPHIB_MECHINF,
        AMPHIB_RECON,
        LOGISTICS,
        AIR_DEFENSE
    }

    public enum ArmyUnitAssignment {
        FORWARD_DEPLOYABLE,
        HEADQUARTERS_AREA
    }

    public class SubordinateAssignment {
        public ArmyUnitAssignment Assignment { get; }
        public ArmyUnit Subordinate { get; }
        public SubordinateAssignment(ArmyUnit subordinate, ArmyUnitAssignment assignment) {
            Subordinate = subordinate;
            Assignment = assignment;
        }
    }

    public class ArmyUnit {
        public ArmyUnitEchelon Echelon { get; }
        public ArmyUnitType UnitType { get; }
        public string? Name { get; }
        public string? Description { get; }

        public IReadOnlyList<SubordinateAssignment> SubordinateAssignments { get; }
        public IReadOnlyList<VehicleAllocation> VehicleAllocations { get; }

        public ArmyUnit(
            ArmyUnitEchelon echelon,
            ArmyUnitType type,
            string name,
            string description,
            IReadOnlyList<SubordinateAssignment> subordinates,
            IReadOnlyList<VehicleAllocation> vehicles) {

            Echelon = echelon;
            UnitType = type;
            Name = name;
            Description = description;
            SubordinateAssignments = subordinates;
            VehicleAllocations = vehicles;
        }
    }
}
