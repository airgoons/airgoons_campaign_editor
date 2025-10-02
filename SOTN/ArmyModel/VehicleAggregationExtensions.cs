using MilitaryModel;
using System.Collections.Immutable;


namespace SOTN.ArmyModel {
    public static class VehicleAggregationExtensions {
        /// <summary>
        /// Aggregate vehicle counts for the provided root unit and all subordinate units (recursively).
        /// Returns an immutable dictionary mapping vehicle type -> total count.
        /// Handles potential cycles by tracking visited unit references.
        /// </summary>
        public static ImmutableDictionary<string, int> AggregateVehicleCounts(this ArmyUnit root) {
            if (root is null) throw new ArgumentNullException(nameof(root));

            var totals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var visited = new HashSet<ArmyUnit>(ReferenceEqualityComparer.Instance);
            var stack = new Stack<ArmyUnit>();
            stack.Push(root);

            while (stack.Count > 0) {
                var unit = stack.Pop();
                if (!visited.Add(unit)) continue;

                // Aggregate this unit's vehicle allocations
                var allocs = unit.VehicleAllocations ?? Array.Empty<VehicleAllocation>();
                foreach (var a in allocs) {
                    if (a is null) continue;
                    if (totals.TryGetValue(a.VehicleType, out var current))
                        totals[a.VehicleType] = current + a.Count;
                    else
                        totals[a.VehicleType] = a.Count;
                }

                // Push subordinates for processing
                var subs = unit.Subordinates ?? Array.Empty<ArmyUnit>();
                foreach (var sub in subs) {
                    if (sub != null)
                        stack.Push(sub);
                }
            }

            return totals.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);
        }

        // Small helper to use reference equality in HashSet<ArmyUnit>
        private sealed class ReferenceEqualityComparer : IEqualityComparer<ArmyUnit> {
            public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();
            public bool Equals(ArmyUnit? x, ArmyUnit? y) => ReferenceEquals(x, y);
            public int GetHashCode(ArmyUnit obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}