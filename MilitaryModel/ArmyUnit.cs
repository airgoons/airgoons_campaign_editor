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

    public class VehicleAllocation {
        public string VehicleType { get; }  // TODO:  replace with reference to python class
        public int Count { get; }

        public VehicleAllocation(string type, int count) {
            VehicleType = type;
            Count = count;
        }
    }

    public abstract class ArmyUnit {
        public abstract ArmyUnitEchelon Echelon { get; }
        public abstract ArmyUnitType Type { get; }

        protected string? _name = null;
        public string? Name => _name;
        
        protected List<SubordinateAssignment> _subordinates = new();
        public IReadOnlyList<SubordinateAssignment> Subordinates => _subordinates;
        public void AddSubordinate(ArmyUnit subordinate, ArmyUnitAssignment assignment) {
            var subordinateAssignment = new SubordinateAssignment(subordinate, assignment);
            AddSubordinate(subordinateAssignment);
        }
        public void AddSubordinate(SubordinateAssignment subordinate) {
            _subordinates.Add(subordinate);
        }
        public void AddSubordinates(int count, Type typeOfArmyUnit, ArmyUnitAssignment assignment) {
            if (!typeof(ArmyUnit).IsAssignableFrom(typeOfArmyUnit)) {
                throw new ArgumentException("typeOfArmyUnit must derive from ArmyUnit");
            }
            else {
                foreach (var _ in Enumerable.Range(0, count)) {
                    var unit = (ArmyUnit)Activator.CreateInstance(typeOfArmyUnit)!;
                    AddSubordinate(unit, assignment);
                }
            }
        }

        public void SetSubordinates(List<SubordinateAssignment> subordinates) {
            _subordinates = subordinates;
        }

        protected List<VehicleAllocation> _vehicleAllocations = new();
        public IReadOnlyList<VehicleAllocation> VehicleAllocations => _vehicleAllocations;
        public void AddVehicle(string type, int count) {
            var allocation = new VehicleAllocation(type, count);
            AddVehicle(allocation);
        }
        public void AddVehicle(VehicleAllocation allocation) {
            _vehicleAllocations.Add(allocation);
        }
        public void SetVehicles(IReadOnlyList<VehicleAllocation> vehicleAllocations) {
            _vehicleAllocations = vehicleAllocations.ToList();
        }
    }

    public abstract class EchelonArmyUnit : ArmyUnit {
        protected ArmyUnitType _type;
        public override ArmyUnitType Type => _type;
        protected EchelonArmyUnit(ArmyUnitType type, string name = "DEFAULT") : base() {
            _type = type;
        }
    }

    public class Front : EchelonArmyUnit {
        public Front(ArmyUnitType type) : base(type) { }
        public override ArmyUnitEchelon Echelon => ArmyUnitEchelon.FRONT;
    }

    public class Army : EchelonArmyUnit {
        public Army(ArmyUnitType type) : base(type) { }
        public override ArmyUnitEchelon Echelon => ArmyUnitEchelon.ARMY;
    }

    public class Corps : EchelonArmyUnit {
        public Corps(ArmyUnitType type) : base(type) { }
        public override ArmyUnitEchelon Echelon => ArmyUnitEchelon.CORPS;
    }

    public class Division : EchelonArmyUnit {
        public Division(ArmyUnitType type) : base(type) { }
        public override ArmyUnitEchelon Echelon => ArmyUnitEchelon.DIVISION;
    }

    public class Brigade : EchelonArmyUnit {
        public Brigade(ArmyUnitType type) : base(type) { }
        public override ArmyUnitEchelon Echelon => ArmyUnitEchelon.BRIGADE;
    }

    public class Regiment : EchelonArmyUnit {
        public Regiment(ArmyUnitType type) : base(type) { }
        public override ArmyUnitEchelon Echelon => ArmyUnitEchelon.REGIMENT;
    }

    public class Battalion : EchelonArmyUnit {
        public Battalion(ArmyUnitType type) : base(type) { }
        public override ArmyUnitEchelon Echelon => ArmyUnitEchelon.BATTALION;
    }

    public class Company : EchelonArmyUnit {
        public Company(ArmyUnitType type) : base(type) { }
        public override ArmyUnitEchelon Echelon => ArmyUnitEchelon.COMPANY;
    }

    public class Platoon : EchelonArmyUnit {
        public Platoon(ArmyUnitType type) : base(type) { }
        public override ArmyUnitEchelon Echelon => ArmyUnitEchelon.PLATOON;
    }
}
