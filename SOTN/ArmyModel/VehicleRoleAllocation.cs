namespace SOTN.ArmyModel {
    internal sealed class VehicleRoleAllocation {
        internal VehicleRole Role { get; }
        internal int Count { get; }

        internal VehicleRoleAllocation(VehicleRole role, int count) {
            Role = role;
            Count = count;
        }
    }
}