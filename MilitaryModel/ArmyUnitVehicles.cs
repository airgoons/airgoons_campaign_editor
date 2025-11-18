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
        public VehicleSet Set { get; }
        public int Count { get; }
        public VehicleRoleAllocation SourceAllocation { get; }
        public VehicleAllocation(VehicleSet set, int count, VehicleRoleAllocation sourceAllocation) {
            this.Set = set;
            Count = count;
            SourceAllocation = sourceAllocation;
        }
    }

    public class VehicleSet {
        public string NamePrefix { get; }
        public IReadOnlyList<string> Vehicles { get; }
        public VehicleSet(string namePrefix, List<string> vehicles) {
            NamePrefix = namePrefix;
            Vehicles = vehicles;
        }
        public static VehicleSet BasicSingleVehicleSet(string vehicle) {
            var namePrefix = vehicle.Split('.').Last();
            return new VehicleSet(namePrefix, new List<string> { vehicle });
        }
    }

    public static class NatoVehicleSets {
        public static readonly VehicleSet Rapier = new VehicleSet(
            "Rapier",
            new List<string> {
                "dcs.vehicles.AirDefence.rapier_fsa_launcher",
                "dcs.vehicles.AirDefence.rapier_fsa_optical_tracker_unit",
                "dcs.vehicles.AirDefence.rapier_fsa_blindfire_radar"
            }
        );
    }
}
