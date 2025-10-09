using System.Collections.Immutable;
using MilitaryModel;

namespace SOTN.ArmyModel.Platoon {
    internal static class PlatoonVehicleRoleMappings
    {
        private static readonly ImmutableDictionary<Faction, ImmutableDictionary<ArmyUnitType, IReadOnlyList<VehicleRoleAllocation>>> _map;

        static PlatoonVehicleRoleMappings()
        {
            _map = new Dictionary<Faction, ImmutableDictionary<ArmyUnitType, IReadOnlyList<VehicleRoleAllocation>>> {
                [Faction.NATO] = new Dictionary<ArmyUnitType, IReadOnlyList<VehicleRoleAllocation>> {
                    [ArmyUnitType.ARMOR] = ImmutableArray.Create(
                        new VehicleRoleAllocation(VehicleRole.TANK, 4)
                    ),
                    [ArmyUnitType.ARTILLERY] = ImmutableArray.Create(
                        new VehicleRoleAllocation(VehicleRole.GUN, 4),
                        new VehicleRoleAllocation(VehicleRole.TRUCK, 2)
                    ),
                    [ArmyUnitType.HEADQUARTERS] = ImmutableArray.Create(
                        new VehicleRoleAllocation(VehicleRole.CAR, 5),
                        new VehicleRoleAllocation(VehicleRole.TRUCK, 5)
                    ),
                    [ArmyUnitType.INFANTRY] = ImmutableArray.Create(
                        new VehicleRoleAllocation(VehicleRole.INFANTRY, 5),
                        new VehicleRoleAllocation(VehicleRole.TRUCK, 2),
                        new VehicleRoleAllocation(VehicleRole.APC, 2)
                    ),
                    [ArmyUnitType.MECHINF] = ImmutableArray.Create(
                        new VehicleRoleAllocation(VehicleRole.IFV, 4)
                    ),
                    [ArmyUnitType.MISSILE] = ImmutableArray.Create(
                        new VehicleRoleAllocation(VehicleRole.LAUNCHER, 1),
                        new VehicleRoleAllocation(VehicleRole.TRUCK, 4)
                    ),
                    // Per design, MARINE = INFANTRY
                    [ArmyUnitType.MARINE] = ImmutableArray.Create(
                        new VehicleRoleAllocation(VehicleRole.INFANTRY, 5),
                        new VehicleRoleAllocation(VehicleRole.TRUCK, 2),
                        new VehicleRoleAllocation(VehicleRole.APC, 2)
                    ),
                    [ArmyUnitType.RECONNAISSANCE] = ImmutableArray.Create(
                        new VehicleRoleAllocation(VehicleRole.ARMORED_SCOUT_VEHICLE, 4)
                    ),
                    // NATO has no Amphib units
                    //[ArmyUnitType.AMPHIB_ARMOR] = ImmutableArray.Create(
                    //    new VehicleRoleAllocation(VehicleRole.AMPHIB_TANK, 4)
                    //),
                    //// Per design, AMPHIB_MECHINF = AMPHIB_ARMOR
                    //[ArmyUnitType.AMPHIB_MECHINF] = ImmutableArray.Create(
                    //    new VehicleRoleAllocation(VehicleRole.AMPHIB_TANK, 4)
                    //),
                    //// Per design, AMPHIB_MECHINF = AMPHIB_ARMOR
                    //[ArmyUnitType.AMPHIB_RECON] = ImmutableArray.Create(
                    //    new VehicleRoleAllocation(VehicleRole.AMPHIB_TANK, 4)
                    //),
                    [ArmyUnitType.LOGISTICS] = ImmutableArray.Create(
                        new VehicleRoleAllocation(VehicleRole.TRUCK, 8)
                    ),
                    // No Air Defense prototype supported, must create unique per VehicleRole due to it being a special case
                    //[ArmyUnitType.AIR_DEFENSE] = ImmutableArray.Create()
                }.ToImmutableDictionary(),
                [Faction.WarsawPact] = new Dictionary<ArmyUnitType, IReadOnlyList<VehicleRoleAllocation>> {
                    [ArmyUnitType.ARMOR] = ImmutableArray.Create(
                        new VehicleRoleAllocation(VehicleRole.TANK, 3)
                    ),
                    [ArmyUnitType.ARTILLERY] = ImmutableArray.Create(
                        new VehicleRoleAllocation(VehicleRole.GUN, 4),
                        new VehicleRoleAllocation(VehicleRole.TRUCK, 2)
                    ),
                    [ArmyUnitType.HEADQUARTERS] = ImmutableArray.Create(
                        new VehicleRoleAllocation(VehicleRole.CAR, 5),
                        new VehicleRoleAllocation(VehicleRole.TRUCK, 5)
                    ),
                    [ArmyUnitType.INFANTRY] = ImmutableArray.Create(
                        new VehicleRoleAllocation(VehicleRole.INFANTRY, 5),
                        new VehicleRoleAllocation(VehicleRole.TRUCK, 2),
                        new VehicleRoleAllocation(VehicleRole.APC, 2)
                    ),
                    [ArmyUnitType.MECHINF] = ImmutableArray.Create(
                        new VehicleRoleAllocation(VehicleRole.IFV, 4)
                    ),
                    [ArmyUnitType.MISSILE] = ImmutableArray.Create(
                        new VehicleRoleAllocation(VehicleRole.LAUNCHER, 1),
                        new VehicleRoleAllocation(VehicleRole.TRUCK, 4)
                    ),
                    // Per design, MARINE = INFANTRY
                    [ArmyUnitType.MARINE] = ImmutableArray.Create(
                        new VehicleRoleAllocation(VehicleRole.INFANTRY, 5),
                        new VehicleRoleAllocation(VehicleRole.TRUCK, 2),
                        new VehicleRoleAllocation(VehicleRole.APC, 2)
                    ),
                    [ArmyUnitType.RECONNAISSANCE] = ImmutableArray.Create(
                        new VehicleRoleAllocation(VehicleRole.ARMORED_SCOUT_VEHICLE, 4)
                    ),
                    [ArmyUnitType.AMPHIB_ARMOR] = ImmutableArray.Create(
                        new VehicleRoleAllocation(VehicleRole.AMPHIB_TANK, 4)
                    ),
                    // Per design, AMPHIB_MECHINF = AMPHIB_ARMOR
                    [ArmyUnitType.AMPHIB_MECHINF] = ImmutableArray.Create(
                        new VehicleRoleAllocation(VehicleRole.AMPHIB_TANK, 4)
                    ),
                    // Per design, AMPHIB_MECHINF = AMPHIB_ARMOR
                    [ArmyUnitType.AMPHIB_RECON] = ImmutableArray.Create(
                        new VehicleRoleAllocation(VehicleRole.AMPHIB_TANK, 4)
                    ),
                    [ArmyUnitType.LOGISTICS] = ImmutableArray.Create(
                        new VehicleRoleAllocation(VehicleRole.TRUCK, 8)
                    ),
                    // No Air Defense prototype supported, must create unique per VehicleRole due to it being a special case
                    //[ArmyUnitType.AIR_DEFENSE] = ImmutableArray.Create()
                }.ToImmutableDictionary()
            }.ToImmutableDictionary();
        }

        public static IReadOnlyList<VehicleRoleAllocation> GetAllocations(Faction faction, ArmyUnitType unitType)
        {
            if (_map.TryGetValue(faction, out var unitMap) && unitMap != null && unitMap.TryGetValue(unitType, out var allocs))
                return allocs;
            return Array.Empty<VehicleRoleAllocation>();
        }

        public static bool TryGetAllocations(Faction faction, ArmyUnitType unitType, out IReadOnlyList<VehicleRoleAllocation> allocations)
        {
            if (_map.TryGetValue(faction, out var unitMap) && unitMap != null && unitMap.TryGetValue(unitType, out var allocs))
            {
                allocations = allocs;
                return true;
            }
            allocations = Array.Empty<VehicleRoleAllocation>();
            return false;
        }
    }
}