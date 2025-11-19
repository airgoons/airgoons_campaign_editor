using System.Reflection.Metadata.Ecma335;

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

    public class Mantis {
        // MANTIS is a Functional Module of the MOOSE framework for DCS
        // https://flightcontrol-master.github.io/MOOSE_DOCS_DEVELOP/Documentation/Functional.Mantis.html
        public class Keyword {
            private string Name { get; }
            public Keyword(string name) => Name = name;
            public override string ToString() {
                return Name;
            }
        }
        public sealed class Category : Keyword {
            private Category(string name) : base(name) { }
            public static readonly Category EWR = new("EWR");
            public static readonly Category SHORAD = new("SHORAD");
            public static readonly Category AAA = new("AAA");
        }

        public sealed class SamType : Keyword {
            private SamType(string name) : base(name) { }
            public static readonly SamType LONG = new("LONG");
            public static readonly SamType MEDIUM = new("MEDIUM");
            public static readonly SamType POINT = new("POINT");
            public static readonly SamType SHORT = new("SHORT");

        }

        public sealed class SamUnit : Keyword {
            private SamUnit(string name) : base(name) { }
            public static readonly SamUnit Avenger = new("Avenger");
            public static readonly SamUnit Chaparral = new("Chaparral");
            public static readonly SamUnit Hawk = new("Hawk");
            public static readonly SamUnit Linebacker = new("Linebacker");
            public static readonly SamUnit NASAMS = new("NASAMS");
            public static readonly SamUnit Patriot = new("Patriot");
            public static readonly SamUnit Rapier = new("Rapier");
            public static readonly SamUnit Roland = new("Roland");
            public static readonly SamUnit IRIST_SLM = new("IRIS-T SLM");
            public static readonly SamUnit Pantsir_S1 = new("Pantsir S1");
            public static readonly SamUnit TOR_M2 = new("TOR M2");
            public static readonly SamUnit CRAM = new("C-RAM");
            public static readonly SamUnit Silkworm = new("Silkworm");
            public static readonly SamUnit SA2 = new("SA-2");
            public static readonly SamUnit SA3 = new("SA-3");
            public static readonly SamUnit SA5 = new("SA-5");
            public static readonly SamUnit SA6 = new("SA-6");
            public static readonly SamUnit SA7 = new("SA-7");
            public static readonly SamUnit SA8 = new("SA-8");
            public static readonly SamUnit SA9 = new("SA-9");
            public static readonly SamUnit SA10 = new("SA-10");
            public static readonly SamUnit SA11 = new("SA-11");
            public static readonly SamUnit SA13 = new("SA-13");
            public static readonly SamUnit SA15 = new("SA-15");
            public static readonly SamUnit SA19 = new("SA-19");
        }
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
        public VehicleSet(List<Mantis.Keyword> mantisKeywords, List<string> vehicles) {
            NamePrefix = string.Join(" ", mantisKeywords);
            Vehicles = vehicles;
        }

        public VehicleSet(Mantis.Keyword keyword, List<string> vehicles) {
            NamePrefix = keyword.ToString();
            Vehicles = vehicles;
        }

        public VehicleSet(Mantis.Keyword keyword, string vehicle) {
            NamePrefix = keyword.ToString();
            Vehicles = new List<string> { vehicle };
        }

        public VehicleSet(string vehicle) {
            NamePrefix = "";
            Vehicles = new List<string> { vehicle };
        }
    }

    public static class NatoVehicleSets {
        public static readonly VehicleSet Rapier = new VehicleSet(
            Mantis.SamUnit.Rapier,
            new List<string> {
                "dcs.vehicles.AirDefence.rapier_fsa_launcher",
                "dcs.vehicles.AirDefence.rapier_fsa_optical_tracker_unit",
                "dcs.vehicles.AirDefence.rapier_fsa_blindfire_radar"
            }
        );

        public static readonly VehicleSet AAA = new VehicleSet(Mantis.Category.AAA, new List<string> { "dcs.vehicles.AirDefence.Gepard" });
        public static readonly VehicleSet MANPADS_Redeye = new VehicleSet(Mantis.SamType.POINT, "pydcs_extensions.lowdigitsmanpads.MANPADS_Redeye_FIM43C");
    }

    public static class WarsawPactVehicleSets {
        public static readonly VehicleSet SA8 = new VehicleSet(Mantis.SamUnit.SA8, "dcs.vehicles.AirDefence.Osa_9A33_ln");
        public static readonly VehicleSet MANPADS_Strela2_SA7 = new VehicleSet(Mantis.SamType.POINT, "pydcs_extensions.lowdigitsmanpads.MANPADS_Strela2_SA7");
        public static readonly VehicleSet Shilka = new VehicleSet(Mantis.Category.AAA, "dcs.vehicles.AirDefence.ZSU_23_4_Shilka");

    }
}
