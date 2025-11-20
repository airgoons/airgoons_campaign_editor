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
        HEADQUARTERS_AREA,
        HQSAM,
        NONE_DEFAULT
    }

    public enum DeploymentOrder {
        COMBAT,
        HQSAM,
        NOT_DEPLOYED
    }

    public abstract class ArmyUnit {

        public ArmyUnitEchelon Echelon { get; }
        public ArmyUnitType UnitType { get; }
        public string? Name { get; set; }
        public string? Description { get; }

        private Vector? _position = null;
        public Vector? Position => _position;
        
        public double? _heading = null;
        public double? Heading => _heading;

        private DeploymentOrder _deploymentOrder = DeploymentOrder.NOT_DEPLOYED;
        public DeploymentOrder DeploymentOrder => _deploymentOrder;
        private ArmyUnitAssignment _assignment = ArmyUnitAssignment.NONE_DEFAULT;
        public ArmyUnitAssignment Assignment => _assignment;

        public IReadOnlyList<ArmyUnit> Subordinates { get; }

        private List<VehicleAllocation> _vehicleAllocations = new();
        public IReadOnlyList<VehicleAllocation> VehicleAllocations => _vehicleAllocations;
        public Faction Faction { get; }
        public Nation? Nation { get; }

        public ArmyUnit(
            ArmyUnitEchelon echelon,
            ArmyUnitType type,
            string name,
            string description,
            Faction faction,
            Nation? nation,
            IReadOnlyList<ArmyUnit> subordinates,
            IReadOnlyList<VehicleAllocation> vehicles,
            ArmyUnitAssignment assignment = ArmyUnitAssignment.NONE_DEFAULT) {
            Faction = faction;
            Nation = nation;
            Echelon = echelon;
            UnitType = type;
            Name = name;
            Description = description;
            Subordinates = subordinates;
            _vehicleAllocations.AddRange(vehicles);
            _assignment = assignment;
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
        public void SetAssignment(ArmyUnitAssignment assignment) {
            _assignment = assignment;
        }

        public void SetVehicleAllocations(List<VehicleAllocation> vehicleAllocations) {
            _vehicleAllocations = vehicleAllocations;
        }
    }
}
