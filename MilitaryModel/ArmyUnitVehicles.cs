namespace MilitaryModel {
    public enum VehicleRole {
        AAA,
        AMPHIB_TANK,
        APC,
        ARMORED_SCOUT_VEHICLE,
        CAR,
        GUN,
        IFV,
        INFANTRY,
        LAUNCHER,
        MANPADS,
        SAM_Long,
        SAM_Medium,
        SAM_Short,
        TANK,
        TRUCK
    }
    public class VehicleRoleAllocation {
        public VehicleRole Role { get; }
        public int Count { get; }

        public VehicleRoleAllocation(VehicleRole role, int count) {
            Role = role;
            Count = count;
        }
    }

    public class VehicleAllocation {
        public string VehicleType { get; }
        public int Count { get; }
        public VehicleRoleAllocation SourceAllocation { get; }
        public VehicleAllocation(string type, int count, VehicleRoleAllocation sourceAllocation) {
            VehicleType = type;
            Count = count;
            SourceAllocation = sourceAllocation;
        }
    }
}
