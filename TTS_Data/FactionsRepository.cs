using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace TTS_Data {
    public static class FactionsRepository {
        public record UnitRecord(
            string Faction,
            string Nation,
            IReadOnlyList<string> ParentPath,
            string UnitName,
            FactionsJsonEntry Entry
        );

        // Loads the raw JSON document (keeps the nested dictionary shape).
        public static JsonDocument? LoadRaw(string path) {
            try {
                var expanded = Environment.ExpandEnvironmentVariables(path);
                if (!File.Exists(expanded)) {
                    Console.Error.WriteLine($"Factions file not found: {expanded}");
                    return null;
                }

                var json = File.ReadAllText(expanded);
                return JsonDocument.Parse(json);
            }
            catch (JsonException jex) {
                Console.Error.WriteLine($"JSON parse error loading factions: {jex.Message}");
                return null;
            }
            catch (Exception ex) {
                Console.Error.WriteLine($"Failed to load factions: {ex}");
                return null;
            }
        }

        // Flattened view: walks the nested structure and returns a list of UnitRecord.
        // The nested structure may have variable-depth intermediate nodes (commands / subordinate commands).
        public static List<UnitRecord> LoadFlattened(string path) {
            var doc = LoadRaw(path);
            if (doc is null) return new List<UnitRecord>();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var results = new List<UnitRecord>();

            bool IsLeaf(JsonElement el) {
                if (el.ValueKind != JsonValueKind.Object) return false;
                // Treat as leaf if it contains any of the expected unit fields.
                return el.TryGetProperty("front_png", out _) ||
                       el.TryGetProperty("front_png_url", out _) ||
                       el.TryGetProperty("back_png", out _) ||
                       el.TryGetProperty("back_png_url", out _);
            }

            void Traverse(JsonElement node, string faction, string nation, List<string> parentPath) {
                if (node.ValueKind != JsonValueKind.Object) return;

                foreach (var prop in node.EnumerateObject()) {
                    var name = prop.Name;
                    var value = prop.Value;

                    if (IsLeaf(value)) {
                        try {
                            var entry = JsonSerializer.Deserialize<FactionsJsonEntry>(value.GetRawText(), options) ?? new FactionsJsonEntry();
                            // parentPath currently contains the chain from the nation downward (excluding the unit name)
                            results.Add(new UnitRecord(faction, nation, parentPath.AsReadOnly(), name, entry));
                        }
                        catch (JsonException) {
                            // skip malformed leaf
                        }
                    }
                    else {
                        // descend: add current node name to parent path
                        parentPath.Add(name);
                        Traverse(value, faction, nation, parentPath);
                        parentPath.RemoveAt(parentPath.Count - 1);
                    }
                }
            }

            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return results;

            foreach (var factionProp in root.EnumerateObject()) {
                var factionName = factionProp.Name;
                var nationsNode = factionProp.Value;
                if (nationsNode.ValueKind != JsonValueKind.Object) continue;

                foreach (var nationProp in nationsNode.EnumerateObject()) {
                    var nationName = nationProp.Name;
                    var commandsNode = nationProp.Value;
                    // start traversal with empty parent path (parents below nation)
                    Traverse(commandsNode, factionName, nationName, new List<string>());
                }
            }

            return results;
        }
    }
}