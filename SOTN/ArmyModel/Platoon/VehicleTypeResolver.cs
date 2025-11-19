using System.Collections.Immutable;
using MilitaryModel;

namespace SOTN.ArmyModel.Platoon {
    /// <summary>
    /// Resolve a VehicleType (string) for a given Nation + VehicleRole.
    /// Resolution order: nation-specific -> faction (NATO / WarsawPact).
    /// </summary>
    /// 
    internal static class VehicleTypeResolver {
        private static readonly ImmutableDictionary<Nation, IReadOnlyDictionary<VehicleRole, VehicleSet>> _nationMaps;
        private static readonly ImmutableDictionary<Faction, IReadOnlyDictionary<VehicleRole, VehicleSet>> _factionMaps;

        static VehicleTypeResolver() {
            // NOTE:  Use strings that represent a PYDCS VehicleType
            _factionMaps = new Dictionary<Faction, IReadOnlyDictionary<VehicleRole, VehicleSet>> {
                [Faction.NATO] = new Dictionary<VehicleRole, VehicleSet> {
                    [VehicleRole.AAA] = NatoVehicleSets.AAA,
                    [VehicleRole.AMPHIB_TANK] = new VehicleSet("dcs.vehicles.Armor.AAV7"),
                    [VehicleRole.APC] = new VehicleSet("dcs.vehicles.Armor.M_113"),
                    [VehicleRole.ARMORED_SCOUT_VEHICLE] = new VehicleSet("dcs.vehicles.Armor.M_2_Bradley"),
                    [VehicleRole.CAR] = new VehicleSet("dcs.vehicles.Unarmed.Hummer"),
                    [VehicleRole.GUN] = new VehicleSet("dcs.vehicles.Artillery.M_109"),
                    [VehicleRole.IFV] = new VehicleSet("dcs.vehicles.Armor.M_2_Bradley"),
                    [VehicleRole.INFANTRY] = new VehicleSet("dcs.vehicles.Infantry.Soldier_M4"),
                    [VehicleRole.LAUNCHER] = new VehicleSet("dcs.vehicles.Artillery.MLRS"),
                    [VehicleRole.MANPADS] = NatoVehicleSets.MANPADS_Redeye,
                    [VehicleRole.SAM_Long] = NatoVehicleSets.Rapier,
                    [VehicleRole.SAM_Medium] = NatoVehicleSets.Rapier,
                    [VehicleRole.SAM_Short] = new VehicleSet("dcs.vehicles.AirDefence.M48_Chaparral"),
                    [VehicleRole.TANK] = new VehicleSet("dcs.vehicles.Armor.M_1_Abrams"),
                    [VehicleRole.TRUCK] = new VehicleSet("dcs.vehicles.Unarmed.M_818")
                },
                [Faction.WarsawPact] = new Dictionary<VehicleRole, VehicleSet> {
                    [VehicleRole.AAA] = new VehicleSet("dcs.vehicles.AirDefence.ZSU_23_4_Shilka"),
                    [VehicleRole.AMPHIB_TANK] = new VehicleSet("dcs.vehicles.Armor.PT_76"),
                    [VehicleRole.APC] = new VehicleSet("dcs.vehicles.Armor.BTR_80"),
                    [VehicleRole.ARMORED_SCOUT_VEHICLE] = new VehicleSet("dcs.vehicles.Armor.BRDM_2"),
                    [VehicleRole.CAR] = new VehicleSet("dcs.vehicles.Unarmed.UAZ_469"),
                    [VehicleRole.GUN] = new VehicleSet("dcs.vehicles.Artillery.SAU_Msta"),
                    [VehicleRole.IFV] = new VehicleSet("dcs.vehicles.Armor.BMP_2"),
                    [VehicleRole.INFANTRY] = new VehicleSet("dcs.vehicles.Infantry.Infantry_AK"),
                    [VehicleRole.LAUNCHER] = new VehicleSet("dcs.vehicles.Artillery.Smerch"),
                    [VehicleRole.MANPADS] = WarsawPactVehicleSets.MANPADS_Strela2_SA7,
                    [VehicleRole.SAM_Long] = new VehicleSet("dcs.vehicles.AirDefence.SA_11_Buk_LN_9A310M1"),
                    [VehicleRole.SAM_Medium] = WarsawPactVehicleSets.SA8,
                    [VehicleRole.SAM_Short] = WarsawPactVehicleSets.SA8,
                    [VehicleRole.TANK] = new VehicleSet("dcs.vehicles.Armor.T_72B"),
                    [VehicleRole.TRUCK] = new VehicleSet("dcs.vehicles.Unarmed.ZIL_131_KUNG")
                }
            }.ToImmutableDictionary();

            // Nation specific overrides
            _nationMaps = new Dictionary<Nation, IReadOnlyDictionary<VehicleRole, VehicleSet>> {
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

        private static bool TryGetVehicleType(Nation nation, VehicleRole role, out VehicleSet? vehicleSet) {
            // try nation-specific
            if (_nationMaps.TryGetValue(nation, out var nationMap) && nationMap != null
                && nationMap.TryGetValue(role, out var set) && (set != null)) {
                vehicleSet = set;
                return true;
            }

            // try faction (NATO/WarsawPact)
            if (FactionRegistry.TryGetFactionOf(nation, out var factionInfo) && factionInfo != null) {
                if (_factionMaps.TryGetValue(factionInfo.Id, out var factionMap) && factionMap != null
                    && factionMap.TryGetValue(role, out var factionSet) && (factionSet != null)) {
                    vehicleSet = factionSet;
                    return true;
                }
            }

            vehicleSet = null;
            return false;
        }

        public static VehicleSet GetVehicleSet(Nation nation, VehicleRole role) {
            if (TryGetVehicleType(nation, role, out var set) && (set != null))
                return set;

            throw new KeyNotFoundException($"No VehicleType mapping found for nation '{nation}' (or its faction) and role '{role}'.");
        }
        public static VehicleSet GetVehicleSet(Faction faction, VehicleRole role) {
            if (_factionMaps.TryGetValue(faction, out var factionMap) && factionMap != null
                && factionMap.TryGetValue(role, out var factionSet) && (factionSet != null)) {
                return factionSet;
            }

            throw new KeyNotFoundException($"No VehicleType mapping found for faction '{faction}' and role '{role}'.");
        }
    }
}