using System;
using System.Collections.Generic;
using System.Linq;

namespace MilitaryModel {
    public static class ArmyUnitStatistics {
        public static void PrintUnitStatistics(IReadOnlyList<ArmyUnit> units, bool recurse = false, bool printVehicles = false) {
            List<ArmyUnit> uniqueUnits;
            if (recurse) { uniqueUnits = GetUniqueUnits(units).ToList(); }
            else uniqueUnits = units.ToList();


            // Count all created unitLocations including nested subordinates (unique)
            int totalUnitCount = uniqueUnits.Count;
            Console.WriteLine($"Created Units:  {totalUnitCount}");

            // Count by echelon
            var echelonCounts = new Dictionary<ArmyUnitEchelon,int>();
            foreach (ArmyUnitEchelon e in Enum.GetValues(typeof(ArmyUnitEchelon)).Cast<ArmyUnitEchelon>()) echelonCounts[e] = 0;
            foreach (var u in uniqueUnits) {
                echelonCounts.TryGetValue(u.Echelon, out var cur);
                echelonCounts[u.Echelon] = cur + 1;
            }

            Console.WriteLine("Echelon counts:");
            foreach (var kv in echelonCounts.OrderByDescending(k => k.Value)) {
                Console.WriteLine($"- {kv.Key}: {kv.Value}");
            }

            if (printVehicles) {
                // Total vehicle types across all unique units (do not recurse per-root)
                var totals = SumAllVehicleTypes(uniqueUnits);

                Console.WriteLine();
                Console.WriteLine("Vehicle type totals (descending):");
                foreach (var kv in totals.OrderByDescending(k => k.Value)) {
                    Console.WriteLine($"- {kv.Key}: {kv.Value}");
                }
            }
            Console.WriteLine("-------");
        }

        // Return unique ArmyUnit instances by traversing roots and their subordinates.
        private static IEnumerable<ArmyUnit> GetUniqueUnits(IEnumerable<ArmyUnit> roots) {
            if (roots == null) yield break;
            var seen = new HashSet<ArmyUnit>();
            var stack = new Stack<ArmyUnit>();
            foreach (var root in roots) {
                if (root == null) continue;
                if (seen.Add(root)) stack.Push(root);
                while (stack.Count > 0) {
                    var u = stack.Pop();
                    yield return u;
                    var subs = u.Subordinates ?? Enumerable.Empty<ArmyUnit>();
                    foreach (var unit in subs) {
                        if (unit != null && seen.Add(unit)) stack.Push(unit);
                    }
                }
            }
        }

        // Aggregates vehicle counts for a collection of unique units (no recursion necessary).
        private static Dictionary<string, int> SumAllVehicleTypes(IEnumerable<ArmyUnit> uniqueUnits) {
            var totals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (uniqueUnits == null) return totals;
            foreach (var unit in uniqueUnits) {
                if (unit == null) continue;
                var vehicles = unit.VehicleAllocations ?? Enumerable.Empty<VehicleAllocation>();
                foreach (var v in vehicles) {
                    if (string.IsNullOrWhiteSpace(v.VehicleType)) continue;
                    totals.TryGetValue(v.VehicleType, out var cur);
                    totals[v.VehicleType] = cur + v.Count;
                }
            }
            return totals;
        }

        // The old recursive helpers are retained for compatibility but are no longer used by PrintUnitStatistics.
        private static void AccumulateVehicleTypes(ArmyUnit unit, Dictionary<string, int> totals) {
            if (unit is null) return;

            var vehicles = unit.VehicleAllocations ?? Enumerable.Empty<VehicleAllocation>();
            foreach (var v in vehicles) {
                if (string.IsNullOrWhiteSpace(v.VehicleType)) continue;
                totals.TryGetValue(v.VehicleType, out var cur);
                totals[v.VehicleType] = cur + v.Count;
            }

            var subs = unit.Subordinates ?? Enumerable.Empty<ArmyUnit>();
            foreach (var sub in subs) {
                if (sub != null) {
                    AccumulateVehicleTypes(sub, totals);
                }
            }
        }

        // Count all unitLocations for a list of root Army unit instances (legacy - not used)
        private static int CountAllUnits(IEnumerable<ArmyUnit> roots) {
            int total = 0;
            foreach (var root in roots) {
                total += CountUnits(root);
            }
            return total;
        }

        // Recursively count subordinates for a given unit (legacy - not used)
        private static int CountUnits(ArmyUnit? unit) {
            if (unit is null) return 0;
            int count = 1; // count this unit

            var subs = unit.Subordinates ?? Enumerable.Empty<ArmyUnit>();
            foreach (var sub in subs) {
                if (sub != null) {
                    count += CountUnits(sub);
                }
            }

            return count;
        }

        // New helper: accumulate echelon counts recursively (legacy - not used)
        private static void AccumulateEchelonCounts(ArmyUnit? unit, Dictionary<ArmyUnitEchelon,int> counts) {
            if (unit is null) return;
            counts.TryGetValue(unit.Echelon, out var cur);
            counts[unit.Echelon] = cur + 1;
            var subs = unit.Subordinates ?? Enumerable.Empty<ArmyUnit>();
            foreach (var sub in subs) {
                if (sub != null) AccumulateEchelonCounts(sub, counts);
            }
        }
    }
}