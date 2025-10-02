using MapData;
using SOTN;
using SOTN.ArmyModel;
using SOTN.ArmyModel.Division;
using SOTN.ArmyModel.Brigade;
using SOTN.ArmyModel.Battalion;
using SOTN.ArmyModel.Company;
using SOTN.ArmyModel.Platoon;
using MilitaryModel;
using SharpKml.Dom;

namespace TTSKML {
    public static class KmlUnitImporter {
        public static IReadOnlyList<ArmyUnit> Run(string kmlPath) {
            var kmlFile = KmlDataReader.LoadKmlAsync(kmlPath).Result;
            var placemarks = KmlDataReader.GetPlacemarks(kmlFile);
            var styles = KmlDataReader.GetStyles(kmlFile);

            var factionsPath = @"E:\dev\rs89_tts\Red_Strike_V1_2.vmod_factions.json";
            var factionsData = TTSJSON.FactionsRepository.LoadFlattened(factionsPath);

            var tagsPath = @"E:\dev\rs89_tts\unit_tags\unit_tags.json";
            var unitTags = TTSJSON.UnitTag.LoadUnitTags(tagsPath);

            var units = new List<ArmyUnit>();
            // var bboxes = new List<Placemark>();

            foreach (var placemark in placemarks) {
                var name = placemark.Name;

                var point = ExtractPointFromGeometry(placemark.Geometry);
                var polygon = ExtractPolygonFromGeometry(placemark.Geometry);
                if ((point == null) && polygon == null) {
                    Console.WriteLine($"[WARN] no supported geometry for '{name}'");
                    continue;
                }

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
                TTSJSON.UnitTag? matchedTag = null;
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

                    AlignedArmyUnit? createdUnit = null;
                    switch (unitEchelon) {
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
                            Console.WriteLine($"[WARN] {unitEchelon} factory not implemented [{name}]");
                            break;
                    }
                    if (createdUnit != null) {
                        createdUnit.SetPosition(point.Coordinate);
                        units.Add(createdUnit);
                    }
                }
                else {
                    Console.WriteLine($"[WARN] matchedTag null for placemark.Name '{name}'");
                }
            }

            return units;
        }

        public static SharpKml.Dom.Point? ExtractPointFromGeometry(SharpKml.Dom.Geometry? geometry) {
            if (geometry == null) return null;
            if (geometry is SharpKml.Dom.Point p) return p;
            else return null;  // only handle point
        }
        public static Polygon? ExtractPolygonFromGeometry(SharpKml.Dom.Geometry? geometry) {
            if (geometry == null) return null;
            if (geometry is SharpKml.Dom.Polygon p) return p;
            else return null;  // only handle polygon
        }
    }
}