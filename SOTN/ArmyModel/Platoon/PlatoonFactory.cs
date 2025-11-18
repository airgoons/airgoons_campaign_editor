using MilitaryModel;

namespace SOTN.ArmyModel.Platoon {
    public static class PlatoonFactory {
        public static Platoon CreatePlatoon(Faction? faction, Nation? nation, ArmyUnitType type, string name) {
            var (resolvedFaction, resolvedNation) = AlignedArmyUnitFactory.ResolveIdentity(faction, nation);

            var description = $"{resolvedFaction}_{resolvedNation}_{type}_Platoon";

            var vehicleRoleAllocations = PlatoonVehicleRoleMappings.GetAllocations(resolvedFaction, type);

            var vehicleAllocations = vehicleRoleAllocations
                .Select(roleAllocation => {
                    var vehicleType = resolvedNation != null
                        ? VehicleTypeResolver.GetVehicleType(resolvedNation.Value, roleAllocation.Role)
                        : VehicleTypeResolver.GetVehicleType(resolvedFaction, roleAllocation.Role);

                    return new VehicleAllocation(vehicleType, roleAllocation.Count, roleAllocation);
                })
                .ToList();

            return new Platoon(type, name, description, resolvedFaction, resolvedNation, Array.Empty<ArmyUnit>(), vehicleAllocations);
        }
        public static Platoon CreatePlatoon(Faction? faction, Nation? nation, ArmyUnitType type, string name, IReadOnlyList<VehicleRoleAllocation> vehicleRoleAllocations) {
            var (resolvedFaction, resolvedNation) = AlignedArmyUnitFactory.ResolveIdentity(faction, nation);
            var description = $"{resolvedFaction}_{resolvedNation}_{type}_Platoon";

            var vehicleAllocations = vehicleRoleAllocations
                .Select(roleAllocation => {
                    var vehicleType = resolvedNation != null
                        ? VehicleTypeResolver.GetVehicleType(resolvedNation.Value, roleAllocation.Role)
                        : VehicleTypeResolver.GetVehicleType(resolvedFaction, roleAllocation.Role);

                    return new VehicleAllocation(vehicleType, roleAllocation.Count, roleAllocation);
                })
                .ToList();

            return new Platoon(type, name, description, resolvedFaction, resolvedNation, Array.Empty<ArmyUnit>(), vehicleAllocations);
        }
    }
}
