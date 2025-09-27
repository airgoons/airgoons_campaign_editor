using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using MilitaryModel;

namespace SOTN.ArmyModel
{
    internal static class StandardPlatoonFactory
    {
        public static Platoon Create(ArmyUnitType type, Faction? faction = null, Nation? nation = null)
        {
            if (faction == null && nation == null)
                throw new ArgumentException("Either faction or nation must be provided to resolve vehicle types.");

            // Resolve faction (explicit wins, otherwise resolve from nation)
            Faction factionToUse;
            if (faction != null)
            {
                factionToUse = faction.Value;
            }
            else
            {
                if (!FactionRegistry.TryGetFactionOf(nation!.Value, out var factionInfo) || factionInfo == null)
                    throw new ArgumentException($"Could not resolve faction for nation '{nation}'.");
                factionToUse = factionInfo.Id;
            }

            var roleAllocations = PlatoonVehicleRoleMappings.GetAllocations(factionToUse, type).ToList();
            var prototype = new PlatoonPrototype(type, roleAllocations, nation, factionToUse);
            var vehicleAllocations = PlatoonPrototype.VehicleAllocationsFactory(prototype);

            var platoon = new Platoon(type);
            platoon.SetVehicles(vehicleAllocations);

            return platoon;
        }
    }
}