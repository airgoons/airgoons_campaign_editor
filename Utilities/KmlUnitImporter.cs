using MapData;
using SOTN;
using SOTN.ArmyModel;
using SOTN.ArmyModel.Division;
using SOTN.ArmyModel.Brigade;
using SOTN.ArmyModel.Battalion;
using SOTN.ArmyModel.Company;
using SOTN.ArmyModel.Platoon;
using MilitaryModel;
using TTS_Data;

namespace Utilities {
    public static class KmlUnitImporter {
        public static void Run() {
            var raw_kmlPath = @"%USERPROFILE%\Downloads\Sample.kml";
            var kmlPath = Environment.ExpandEnvironmentVariables(raw_kmlPath);
            var kmlFile = KmlDataReader.LoadKmlAsync(kmlPath).Result;
            var placemarks = KmlDataReader.GetPlacemarks(kmlFile);
            var styles = KmlDataReader.GetStyles(kmlFile);

            var factionsPath = @"E:\dev\rs89_tts\Red_Strike_V1_2.vmod_factions.json";
            var factionsData = FactionsRepository.LoadFlattened(factionsPath);

            var tagsPath = @"E:\dev\rs89_tts\unit_tags\unit_tags.json";
            var unitTags = UnitTag.LoadUnitTags(tagsPath);

            var units = new List<AlignedArmyUnit>();

            foreach (var placemark in placemarks) {
                var name = placemark.Name;

                // find style with Id equal to the placemark name
                var matchingStyle = styles.FirstOrDefault(s => s.Id == name);
                string? href = null;
                if (matchingStyle is not null) {
                    // guard nulls when walking Icon chain
                    href = matchingStyle.Icon?.Icon?.Href?.ToString();
                }
                else {
                    Console.WriteLine($"[WARN] matchingStyle null for placemark.Name '{name}'");
                    continue;
                }

                string? frontPng = null;
                // find the faction entry whose FrontPngUrl matches the href and read FrontPng
                dynamic? factionsDatum = null;
                if (!string.IsNullOrWhiteSpace(href)) {
                    factionsDatum = factionsData
                        .FirstOrDefault(fd => string.Equals(fd.Entry.FrontPngUrl, href, StringComparison.OrdinalIgnoreCase));

                    frontPng = factionsDatum?.Entry.FrontPng;
                }
                else {
                    Console.WriteLine($"[WARN] Href null for placemark.Name '{name}'");
                    continue;
                }

                // match frontPng with a UnitTag.Filename (compare filenames case-insensitively)
                UnitTag? matchedTag = null;
                if (!string.IsNullOrWhiteSpace(frontPng)) {
                    var frontName = Path.GetFileName(frontPng) ?? frontPng;
                    matchedTag = unitTags.FirstOrDefault(t =>
                        string.Equals(Path.GetFileName(t.Filename) ?? t.Filename, frontName, StringComparison.OrdinalIgnoreCase));
                }
                else {
                    Console.WriteLine($"[WARN] frontPng null for placemark.Name '{name}'");
                    continue;
                }

                if ((matchedTag != null) && (factionsDatum != null)) {
                    var unitType = matchedTag.UnitType;
                    var unitEchelon = matchedTag.UnitFormation;
                    var nationString = (string?)factionsDatum.Nation;
                    var factionString = (string?)factionsDatum.Faction;

                    // Normalize known odd casing/values
                    if (string.Equals(factionString, "NATO Units", StringComparison.OrdinalIgnoreCase)) {
                        factionString = "NATO";
                    }
                    else if (string.Equals(factionString, "WP Units", StringComparison.OrdinalIgnoreCase)) {
                        factionString = "WarsawPact";
                    }

                    // Local variables to set
                    Faction? faction = null;
                    Nation? nation = null;

                    // Try to parse faction by name (e.g. "NATO", "WarsawPact")
                    if (!string.IsNullOrWhiteSpace(factionString) &&
                        Enum.TryParse<Faction>(factionString, ignoreCase: true, out var parsedFaction)) {
                        faction = parsedFaction;
                    }

                    // Try to map nationString (short name like "US", "FR") to Nation using NationInfo.ShortNameMap
                    if (!string.IsNullOrWhiteSpace(nationString) &&
                        NationInfo.ShortNameMap.TryGetValue(nationString, out var parsedNation)) {
                        nation = parsedNation;
                    }

                    // If faction is still null but nation is known, ResolveIdentity will derive the faction.
                    if (faction == null && nation == null) {
                        // Nothing to resolve; handle as needed (skip, log, etc.)
                        Console.WriteLine($"Placemark {name}: could not determine faction or nation from data (factionString='{factionString}', nationString='{nationString}').");
                        continue;
                    }

                    var resolved = AlignedArmyUnitFactory.ResolveIdentity(faction, nation);
                    var resolvedFaction = resolved.ResolvedFaction;
                    var resolvedNation = resolved.ResolvedNation;

                    // Choose a factory / constructor based on echelon.
                    // If the UnitTag has no echelon set, default to BATTALION.
                    var echelon = unitEchelon ?? MilitaryModel.ArmyUnitEchelon.BATTALION;

                    AlignedArmyUnit? createdUnit = null;
                    switch (echelon) {
                        case MilitaryModel.ArmyUnitEchelon.DIVISION:
                            createdUnit = DivisionFactory.CreateDivision(resolvedFaction, resolvedNation, unitType, name);
                            break;
                        case MilitaryModel.ArmyUnitEchelon.BRIGADE:
                            createdUnit = BrigadeFactory.CreateBrigade(resolvedFaction, resolvedNation, unitType, name);
                            break;
                        case MilitaryModel.ArmyUnitEchelon.REGIMENT:
                            Console.WriteLine($"[INFO] Regiment represented as Brigade: {name}");
                            createdUnit = BrigadeFactory.CreateBrigade(resolvedFaction, resolvedNation, unitType, name);
                            break;
                        case MilitaryModel.ArmyUnitEchelon.BATTALION:
                            createdUnit = BattalionFactory.CreateBattalion(resolvedFaction, resolvedNation, unitType, name);
                            break;
                        case MilitaryModel.ArmyUnitEchelon.COMPANY:
                            createdUnit = CompanyFactory.CreateCompany(resolvedFaction, resolvedNation, unitType, name);
                            break;
                        case MilitaryModel.ArmyUnitEchelon.PLATOON:
                            createdUnit = PlatoonFactory.CreatePlatoon(resolvedFaction, resolvedNation, unitType, name);
                            break;
                        default:
                            Console.WriteLine($"[WARN] {echelon} factory not implemented [{name}]");
                            break;
                    }
                    if (createdUnit != null) {
                        units.Add(createdUnit);
                    }
                }
                else {
                    Console.WriteLine($"[WARN] matchedTag null for placemark.Name '{name}'");
                }
            }

            Console.WriteLine($"Created Units:  {units.Count}");

            // Recursively total vehicle types across all created units and their subordinates
            var totals = SumAllVehicleTypes(units);

            Console.WriteLine();
            Console.WriteLine("Vehicle type totals (descending):");
            foreach (var kv in totals.OrderByDescending(k => k.Value)) {
                Console.WriteLine($"- {kv.Key}: {kv.Value}");
            }
        }

        // Aggregates vehicle counts for a single unit (recurses into subordinates).
        private static void AccumulateVehicleTypes(ArmyUnit unit, Dictionary<string,int> totals) {
            if (unit is null) return;

            // Vehicles at this unit
            var vehicles = unit.VehicleAllocations ?? Enumerable.Empty<VehicleAllocation>();
            foreach (var v in vehicles) {
                if (string.IsNullOrWhiteSpace(v.VehicleType)) continue;
                totals.TryGetValue(v.VehicleType, out var cur);
                totals[v.VehicleType] = cur + v.Count;
            }

            // Recurse into subordinate units
            var subs = unit.SubordinateAssignments ?? Enumerable.Empty<SubordinateAssignment>();
            foreach (var sa in subs) {
                if (sa?.Subordinate != null) {
                    AccumulateVehicleTypes(sa.Subordinate, totals);
                }
            }
        }

        // Convenience entry: sum all vehicle types for a list of root units.
        private static Dictionary<string,int> SumAllVehicleTypes(IEnumerable<AlignedArmyUnit> roots) {
            var totals = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
            foreach (var root in roots) {
                AccumulateVehicleTypes(root, totals);
            }
            return totals;
        }
    }
}