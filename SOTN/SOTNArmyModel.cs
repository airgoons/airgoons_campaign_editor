using MilitaryModel.ArmyModel;

namespace SOTN {
    public class WP_INF_Brigade : Brigade {
        public WP_INF_Brigade () : base(ArmyUnitType.INFANTRY) {
            SetSubordinates(new List<SubordinateAssignment> {
                //new SubordinateAssignment(new WP_INF_Battalion(), ArmyUnitAssignment.FORWARD_DEPLOYABLE),
                //new SubordinateAssignment(new WP_INF_Battalion(), ArmyUnitAssignment.FORWARD_DEPLOYABLE),
                //new SubordinateAssignment(new WP_INF_Battalion(), ArmyUnitAssignment.FORWARD_DEPLOYABLE),
                //new SubordinateAssignment(new WP_ARTY_Battalion(), ArmyUnitAssignment.HEADQUARTERS_AREA)
            });
        }
    }
}
