namespace MilitaryModel.ArmyModel
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
        AMPHIB_RECON
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

    public abstract class ArmyUnit {
        public abstract ArmyUnitEchelon Echelon { get; }
        public abstract ArmyUnitType Type { get; }
        
        protected List<SubordinateAssignment> _subordinates = new();
        public IReadOnlyList<SubordinateAssignment> Subordinates => _subordinates;
        public void SetSubordinates(List<SubordinateAssignment> subordinates) {
            _subordinates = subordinates;
        }
    }

    public abstract class EchelonArmyUnit : ArmyUnit {
        protected ArmyUnitType _type;
        public override ArmyUnitType Type => _type;
        protected EchelonArmyUnit(ArmyUnitType type) : base() {
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
