using SharpKml.Base;

namespace MilitaryModel {
    

    public enum ArmyUnitEchelon {
        FRONT,
        ARMY,
        CORPS,
        DIVISION,
        BRIGADE,
        REGIMENT,
        BATTALION,
        COMPANY,
        PLATOON,
        NONE_DEFAULT
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
        AIR_DEFENSE,
        NONE_DEFAULT
    }

    public enum ArmyUnitAssignment {
        FORWARD_DEPLOYABLE,
        HEADQUARTERS_AREA
    }

    public enum DeploymentOrder {
        COMBAT,
        HQSAM,
        NOT_DEPLOYED
    }

    public class SubordinateAssignment {
        public ArmyUnitAssignment Assignment { get; }
        public ArmyUnit Subordinate { get; }
        public SubordinateAssignment(ArmyUnit subordinate, ArmyUnitAssignment assignment) {
            Subordinate = subordinate;
            Assignment = assignment;
        }
    }

    public abstract class ArmyUnit {

        public ArmyUnitEchelon Echelon { get; }
        public ArmyUnitType UnitType { get; }
        public string? Name { get; }
        public string? Description { get; }

        private Vector? _position = null;
        public Vector? Position => _position;
        
        public double? _heading = null;
        public double? Heading => _heading;

        private DeploymentOrder _deploymentOrder = DeploymentOrder.NOT_DEPLOYED;
        public DeploymentOrder DeploymentOrder => _deploymentOrder;

        public IReadOnlyList<SubordinateAssignment> SubordinateAssignments { get; }
        public IReadOnlyList<VehicleAllocation> VehicleAllocations { get; }
        public Faction Faction { get; }
        public Nation? Nation { get; }

        public ArmyUnit(
            ArmyUnitEchelon echelon,
            ArmyUnitType type,
            string name,
            string description,
            Faction faction,
            Nation? nation,
            IReadOnlyList<SubordinateAssignment> subordinates,
            IReadOnlyList<VehicleAllocation> vehicles) {
            Faction = faction;
            Nation = nation;
            Echelon = echelon;
            UnitType = type;
            Name = name;
            Description = description;
            SubordinateAssignments = subordinates;
            VehicleAllocations = vehicles;
        }

        public void SetPosition(Vector position) {
            _position = position;
        }

        public void SetHeading(double heading) {
            _heading = heading;
        }

        public void SetDeploymentOrder(DeploymentOrder deploymentOrder) {
            _deploymentOrder = deploymentOrder;
        }
    }
}
