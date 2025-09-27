using System.Collections.Immutable;


namespace SOTN.ArmyModel {
    /// <summary>
    /// Resolve a VehicleType (string) for a given Nation + VehicleRole.
    /// Resolution order: nation-specific -> faction (NATO / WarsawPact).
    /// </summary>
    internal static class VehicleTypeResolver {
        private static readonly ImmutableDictionary<Nation, IReadOnlyDictionary<VehicleRole, string>> _nationMaps;
        private static readonly ImmutableDictionary<Faction, IReadOnlyDictionary<VehicleRole, string>> _factionMaps;

        static VehicleTypeResolver() {
            // NOTE:  Use strings that represent a PYDCS VehicleType
            _factionMaps = new Dictionary<Faction, IReadOnlyDictionary<VehicleRole, string>> {
                [Faction.NATO] = new Dictionary<VehicleRole, string> {
                    [VehicleRole.AAA] = "",
                    [VehicleRole.AMPHIB_TANK] = "",
                    [VehicleRole.APC] = "",
                    [VehicleRole.ARMORED_SCOUT_VEHICLE] = "",
                    [VehicleRole.CAR] = "",
                    [VehicleRole.GUN] = "",
                    [VehicleRole.IFV] = "",
                    [VehicleRole.INFANTRY] = "",
                    [VehicleRole.LAUNCHER] = "",
                    [VehicleRole.MANPADS] = "",
                    [VehicleRole.SAM_Long] = "",
                    [VehicleRole.SAM_Medium] = "",
                    [VehicleRole.SAM_Short] = "",
                    [VehicleRole.TANK] = "",
                    [VehicleRole.TRUCK] = ""
                },
                [Faction.WarsawPact] = new Dictionary<VehicleRole, string> {
                    [VehicleRole.AAA] = "",
                    [VehicleRole.AMPHIB_TANK] = "",
                    [VehicleRole.APC] = "",
                    [VehicleRole.ARMORED_SCOUT_VEHICLE] = "",
                    [VehicleRole.CAR] = "",
                    [VehicleRole.GUN] = "",
                    [VehicleRole.IFV] = "",
                    [VehicleRole.INFANTRY] = "",
                    [VehicleRole.LAUNCHER] = "",
                    [VehicleRole.MANPADS] = "",
                    [VehicleRole.SAM_Long] = "",
                    [VehicleRole.SAM_Medium] = "",
                    [VehicleRole.SAM_Short] = "",
                    [VehicleRole.TANK] = "",
                    [VehicleRole.TRUCK] = ""
                }
            }.ToImmutableDictionary();

            // Nation specific overrides
            _nationMaps = new Dictionary<Nation, IReadOnlyDictionary<VehicleRole, string>> {
                //[Nation.UnitedStates] = new Dictionary<VehicleRole, string> {
                //    [VehicleRole.TANK] = "M1A1",
                //    [VehicleRole.APC] = "M113"
                //},
                //[Nation.SovietUnion] = new Dictionary<VehicleRole, string> {
                //    [VehicleRole.TANK] = "T-72",
                //    [VehicleRole.APC] = "BMP"
                //}
            }.ToImmutableDictionary();
        }

        private static bool TryGetVehicleType(Nation nation, VehicleRole role, out string vehicleType) {
            // try nation-specific
            if (_nationMaps.TryGetValue(nation, out var nationMap) && nationMap != null
                && nationMap.TryGetValue(role, out var type) && !string.IsNullOrEmpty(type)) {
                vehicleType = type;
                return true;
            }

            // try faction (NATO/WarsawPact)
            if (FactionRegistry.TryGetFactionOf(nation, out var factionInfo) && factionInfo != null) {
                if (_factionMaps.TryGetValue(factionInfo.Id, out var factionMap) && factionMap != null
                    && factionMap.TryGetValue(role, out var factionType) && !string.IsNullOrEmpty(factionType)) {
                    vehicleType = factionType;
                    return true;
                }
            }

            vehicleType = string.Empty;
            return false;
        }

        public static string GetVehicleType(Nation nation, VehicleRole role) {
            if (TryGetVehicleType(nation, role, out var type) && !string.IsNullOrEmpty(type))
                return type;

            throw new KeyNotFoundException($"No VehicleType mapping found for nation '{nation}' (or its faction) and role '{role}'.");
        }
        public static string GetVehicleType(Faction faction, VehicleRole role) {
            if (_factionMaps.TryGetValue(faction, out var factionMap) && factionMap != null
                && factionMap.TryGetValue(role, out var factionType) && !string.IsNullOrEmpty(factionType)) {
                return factionType;
            }

            throw new KeyNotFoundException($"No VehicleType mapping found for faction '{faction}' and role '{role}'.");
        }
    }
}