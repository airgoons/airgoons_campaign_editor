using System.Collections.Immutable;

namespace SOTN {
    public enum Faction {
        NATO,
        WarsawPact
    }

    public enum Nation {
        // NATO
        AlliedCommandEuropeMobileForce,
        Belgium,
        Canada,
        Denmark,
        France,
        Netherlands,
        Spain,
        UnitedKingdom,
        UnitedStates,
        WestGermany,
        Luxembourg,
        Italy,
        Norway,
        Sweden,
        // Warsaw Pact
        Czechoslovakia,
        EastGermany,
        Poland,
        SovietUnion,
        Hungary
    }

    public static class NationInfo {
        // Nation -> short name
        public static readonly ImmutableDictionary<Nation, string> ShortNames;
        // short name (case-insensitive) -> Nation
        public static readonly ImmutableDictionary<string, Nation> ShortNameMap;

        static NationInfo() {
            // map Nation to short name
            var map = new Dictionary<Nation, string> {
                { Nation.AlliedCommandEuropeMobileForce, "AMF" },
                { Nation.Belgium, "BE" },
                { Nation.Canada, "CN" },
                { Nation.Denmark, "DN" },
                { Nation.France, "FR" },
                { Nation.Netherlands, "NL" },
                { Nation.Spain, "SP" },
                { Nation.UnitedKingdom, "UK" },
                { Nation.UnitedStates, "US" },
                { Nation.WestGermany, "WG" },
                { Nation.Luxembourg, "LUX" },
                { Nation.Italy, "IT" },
                { Nation.Norway, "Norway" },
                { Nation.Sweden, "Sweden" },
                { Nation.Czechoslovakia, "CZ" },
                { Nation.EastGermany, "EG" },
                { Nation.Poland, "PO" },
                { Nation.SovietUnion, "SO" },
                { Nation.Hungary, "Hungary" }
            };

            ShortNames = map.ToImmutableDictionary();

            // Build reverse map with case-insensitive string comparer.
            var reverse = new Dictionary<string, Nation>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in map) {
                // If duplicates exist in values they will be ignored (first wins).
                if (!reverse.ContainsKey(kv.Value))
                    reverse[kv.Value] = kv.Key;
            }

            ShortNameMap = reverse.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);
        }

        public static string GetShortName(this Nation nation) =>
            ShortNames.TryGetValue(nation, out var s) ? s : nation.ToString();

        private static bool TryParseFromShortName(string shortName, out Nation result) {
            if (shortName is null) { result = default; return false; }
            return ShortNameMap.TryGetValue(shortName, out result);
        }

        public static Nation ParseFromShortName(string shortName) {
            if (TryParseFromShortName(shortName, out var n)) return n;
            throw new ArgumentException($"Unknown nation short name: {shortName}", nameof(shortName));
        }
    }

    // Strongly-typed, immutable representation of a faction and its nations.
    public sealed record FactionInfo(Faction Id, string Name, ImmutableArray<Nation> Nations) {
        public bool Contains(Nation nation) => Nations.Contains(nation);

        public FactionInfo WithAdded(Nation nation) =>
            this with { Nations = Nations.Add(nation) };

        public FactionInfo WithRemoved(Nation nation) =>
            this with { Nations = Nations.Remove(nation) };
    }

    // Encapsulates lookup/management operations so callers don't work with raw dictionaries.
    internal static class FactionRegistry {
        // Public read-only views
        public static readonly ImmutableArray<FactionInfo> All;
        public static readonly IReadOnlyDictionary<Faction, FactionInfo> ById;
        public static readonly IReadOnlyDictionary<string, FactionInfo> ByName;
        public static readonly IReadOnlyDictionary<Nation, FactionInfo> ByNation;

        static FactionRegistry() {
            var nato = ImmutableArray.Create(
                Nation.AlliedCommandEuropeMobileForce,
                Nation.Belgium,
                Nation.Canada,
                Nation.Denmark,
                Nation.France,
                Nation.Netherlands,
                Nation.Spain,
                Nation.UnitedKingdom,
                Nation.UnitedStates,
                Nation.WestGermany,
                Nation.Luxembourg,
                Nation.Italy,
                Nation.Norway,
                Nation.Sweden
            );

            var warsawPact = ImmutableArray.Create(
                Nation.Czechoslovakia,
                Nation.EastGermany,
                Nation.Poland,
                Nation.SovietUnion,
                Nation.Hungary
            );

            var defs = new[]
            {
                new FactionInfo(Faction.NATO, "NATO", nato),
                new FactionInfo(Faction.WarsawPact, "WarsawPact", warsawPact)
            };

            All = defs.ToImmutableArray();
            ById = All.ToDictionary(f => f.Id);
            ByName = All.ToDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase);

            // Build reverse lookup from nation -> faction (first match wins).
            var nationMap = new Dictionary<Nation, FactionInfo>();
            foreach (var f in All) {
                foreach (var n in f.Nations) {
                    if (!nationMap.ContainsKey(n)) // keep first association, avoid duplicates
                        nationMap[n] = f;
                }
            }
            ByNation = nationMap;
        }

        public static IEnumerable<Nation> GetNations(Faction faction) =>
            ById.TryGetValue(faction, out var info) ? info.Nations : ImmutableArray<Nation>.Empty;

        public static bool TryGetFactionOf(Nation nation, out FactionInfo? faction) =>
            ByNation.TryGetValue(nation, out faction);
    }
}