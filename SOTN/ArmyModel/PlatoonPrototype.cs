using MilitaryModel;


namespace SOTN.ArmyModel {

    internal class PlatoonPrototype {
        internal ArmyUnitType UnitType { get; }
        internal IReadOnlyList<VehicleRoleAllocation> VehicleRoleAllocations { get; }
        internal Faction Faction { get; }
        internal Nation Nation { get; }
        internal bool HasNation { get; }

        internal PlatoonPrototype(ArmyUnitType unitType, List<VehicleRoleAllocation> vehicleRoleAllocations, Nation? nation = null, Faction? faction = null) {
            UnitType = unitType;

            VehicleRoleAllocations = vehicleRoleAllocations is null
                ? Array.Empty<VehicleRoleAllocation>()
                : new List<VehicleRoleAllocation>(vehicleRoleAllocations).AsReadOnly();

            if ((nation == null) && (faction == null)) {
                throw new ArgumentException("nation and faction cannot both be null");
            }

            // If a nation was supplied, store it first (used for validation/resolution).
            if (nation != null) {
                Nation = nation.Value;
                HasNation = true;
            }
            else {
                HasNation = false;
            }

            // If an explicit faction was supplied, honor it.
            if (faction != null) {
                Faction = faction.Value;

                // If both nation and faction were supplied, validate consistency.
                if (HasNation) {
                    if (FactionRegistry.TryGetFactionOf(Nation, out var resolved) && resolved != null) {
                        if (resolved.Id != Faction) {
                            throw new ArgumentException($"Provided faction '{Faction}' does not match nation '{Nation}' (resolved faction: '{resolved.Id}').");
                        }
                    }
                    else {
                        throw new ArgumentException($"Could not resolve faction for nation '{Nation}'");
                    }
                }

                return;
            }

            // No explicit faction supplied -> resolve from nation.
            if (HasNation) {
                if (FactionRegistry.TryGetFactionOf(Nation, out var factionInfo) && factionInfo != null) {
                    Faction = factionInfo.Id;
                }
                else {
                    throw new ArgumentException($"Could not resolve faction for nation '{Nation}'");
                }
            }
        }

        internal static IReadOnlyList<VehicleAllocation> VehicleAllocationsFactory(PlatoonPrototype prototype) {
            if (prototype is null) throw new ArgumentNullException(nameof(prototype));

            var vehicleAllocations = new List<VehicleAllocation>();

            // Previously this iterated nested dictionaries; now we have a flat list of role->count roleAllocations.
            foreach (var allocation in prototype.VehicleRoleAllocations) {
                if (allocation == null) continue;
                var role = allocation.Role;
                var count = allocation.Count;
                if (count <= 0) continue;

                string vehicleType;
                try {
                    vehicleType = prototype.HasNation
                        ? VehicleTypeResolver.GetVehicleType(prototype.Nation, role)
                        : VehicleTypeResolver.GetVehicleType(prototype.Faction, role);
                }
                catch (KeyNotFoundException ex) {
                    throw new KeyNotFoundException($"Could not resolve vehicle type for role '{role}' using {(prototype.HasNation ? $"nation '{prototype.Nation}'" : $"faction '{prototype.Faction}'")}.", ex);
                }

                vehicleAllocations.Add(new VehicleAllocation(vehicleType, count));
            }

            return vehicleAllocations;
        }
    }
}