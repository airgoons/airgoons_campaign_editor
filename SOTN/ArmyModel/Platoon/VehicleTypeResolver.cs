using System.Collections.Immutable;
using MilitaryModel;

namespace SOTN.ArmyModel.Platoon {
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
                    [VehicleRole.AAA] = "dcs.vehicles.AirDefence.Gepard",
                    [VehicleRole.AMPHIB_TANK] = "dcs.vehicles.Armor.AAV7",
                    [VehicleRole.APC] = "dcs.vehicles.Armor.M_113",
                    [VehicleRole.ARMORED_SCOUT_VEHICLE] = "dcs.vehicles.Armor.M_2_Bradley",
                    [VehicleRole.CAR] = "dcs.vehicles.Unarmed.Hummer",
                    [VehicleRole.GUN] = "dcs.vehicles.Artillery.M_109",
                    [VehicleRole.IFV] = "dcs.vehicles.Armor.M_2_Bradley",
                    [VehicleRole.INFANTRY] = "dcs.vehicles.Infantry.Soldier_M4",
                    [VehicleRole.LAUNCHER] = "dcs.vehicles.Artillery.MLRS",
                    [VehicleRole.MANPADS] = "pydcs_extensions.lowdigitsmanpads.MANPADS_Redeye_FIM43C",
                    [VehicleRole.SAM_Long] = "dcs.vehicles.AirDefence.Rapier_fsa_launcher",  // ?? refactor to List of strings so that air defense units can have all the components they need
                    [VehicleRole.SAM_Medium] = "dcs.vehicles.AirDefence.M48_Chaparral",
                    [VehicleRole.SAM_Short] = "dcs.vehicles.AirDefence.M48_Chaparral",
                    [VehicleRole.TANK] = "dcs.vehicles.Armor.M_1_Abrams",
                    [VehicleRole.TRUCK] = "dcs.vehicles.Unarmed.M_818"
                },
                [Faction.WarsawPact] = new Dictionary<VehicleRole, string> {
                    [VehicleRole.AAA] = "dcs.vehicles.AirDefence.ZSU_23_4_Shilka",
                    [VehicleRole.AMPHIB_TANK] = "dcs.vehicles.Armor.PT_76",
                    [VehicleRole.APC] = "dcs.vehicles.Armor.BTR_80",
                    [VehicleRole.ARMORED_SCOUT_VEHICLE] = "dcs.vehicles.Armor.BRDM_2",
                    [VehicleRole.CAR] = "dcs.vehicles.Unarmed.UAZ_469",
                    [VehicleRole.GUN] = "dcs.vehicles.Artillery.SAU_Msta",
                    [VehicleRole.IFV] = "dcs.vehicles.Armor.BMP_2",
                    [VehicleRole.INFANTRY] = "dcs.vehicles.Infantry.Infantry_AK",
                    [VehicleRole.LAUNCHER] = "dcs.vehicles.Artillery.Smerch",
                    [VehicleRole.MANPADS] = "pydcs_extensions.lowdigitsmanpads.MANPADS_Strela2_SA7",
                    [VehicleRole.SAM_Long] = "dcs.vehicles.AirDefence.SA_11_Buk_LN_9A310M1",  // ?? refactor to List of strings so that air defense units can have all the components they need
                    [VehicleRole.SAM_Medium] = "dcs.vehicles.AirDefence.Kub_2P25_ln",  // ?? refactor to List of strings so that air defense units can have all the components they need
                    [VehicleRole.SAM_Short] = "dcs.vehicles.AirDefence.Osa_9A33_ln",
                    [VehicleRole.TANK] = "dcs.vehicles.Armor.T_72B",
                    [VehicleRole.TRUCK] = "dcs.vehicles.Unarmed.ZIL_131_KUNG"
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