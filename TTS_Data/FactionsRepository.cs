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

        // Build an index mapping known href forms (absolute URIs, raw urls, filenames) to the UnitRecord.
        // This lets you resolve a Style/Icon/Href value to the faction unit that supplies the front image.
        public static Dictionary<string, UnitRecord> BuildFrontHrefIndex(string factionsJsonPath, Uri? baseUri = null) {
            var units = LoadFlattened(factionsJsonPath);
            var index = new Dictionary<string, UnitRecord>(StringComparer.OrdinalIgnoreCase);

            foreach (var unit in units) {
                var entry = unit.Entry;

                // Helper that inserts up to three keys for a resolved URI:
                // 1) full URI string, 2) local filename, 3) local path without query (if available)
                void AddResolvedUri(Uri? uri) {
                    if (uri is null) return;
                    try {
                        var full = uri.ToString();
                        if (!index.ContainsKey(full)) index[full] = unit;

                        var localPath = uri.IsAbsoluteUri ? uri.LocalPath : uri.OriginalString;
                        var filename = string.IsNullOrEmpty(localPath) ? null : Path.GetFileName(localPath);
                        if (!string.IsNullOrEmpty(filename) && !index.ContainsKey(filename)) index[filename] = unit;
                    }
                    catch { /* best-effort; ignore malformed uri */ }
                }

                // 1) raw front_png_url
                if (!string.IsNullOrWhiteSpace(entry.FrontPngUrl)) {
                    var raw = entry.FrontPngUrl!;
                    if (!index.ContainsKey(raw)) index[raw] = unit;

                    if (Uri.TryCreate(raw, UriKind.RelativeOrAbsolute, out var parsed)) {
                        if (!parsed.IsAbsoluteUri && baseUri is not null) {
                            try { AddResolvedUri(new Uri(baseUri, parsed)); }
                            catch { AddResolvedUri(parsed); }
                        }
                        else {
                            AddResolvedUri(parsed);
                        }
                    }
                }

                // 2) front_png (filename or local path)
                if (!string.IsNullOrWhiteSpace(entry.FrontPng)) {
                    var raw = entry.FrontPng!;
                    if (!index.ContainsKey(raw)) index[raw] = unit;

                    if (Path.IsPathRooted(raw)) {
                        try { AddResolvedUri(new Uri(raw)); }
                        catch { /* ignore */ }
                    }
                    else if (baseUri is not null) {
                        try { AddResolvedUri(new Uri(baseUri, raw)); }
                        catch { /* ignore */ }
                    }
                    else {
                        var filename = Path.GetFileName(raw);
                        if (!string.IsNullOrEmpty(filename) && !index.ContainsKey(filename)) index[filename] = unit;
                    }
                }

                // 3) entry.GetFrontUri(baseUri) canonical resolution
                try { AddResolvedUri(entry.GetFrontUri(baseUri)); } catch { /* ignore */ }
            }

            return index;
        }

        // Convenience resolver: attempt to find the UnitRecord that matches a Style/Icon/Href value.
        // It will try exact matches, URI resolution against baseUri, and falling back to filename-only matches.
        public static UnitRecord? FindByHref(string factionsJsonPath, string? href, Uri? baseUri = null) {
            if (string.IsNullOrWhiteSpace(href)) return null;
            var index = BuildFrontHrefIndex(factionsJsonPath, baseUri);

            // direct lookup
            if (index.TryGetValue(href!, out var found)) return found;

            // try as URI (absolute or relative)
            if (Uri.TryCreate(href, UriKind.RelativeOrAbsolute, out var parsed)) {
                if (parsed.IsAbsoluteUri) {
                    if (index.TryGetValue(parsed.ToString(), out found)) return found;
                    var filename = Path.GetFileName(parsed.LocalPath);
                    if (!string.IsNullOrEmpty(filename) && index.TryGetValue(filename, out found)) return found;
                }
                else {
                    // relative: try resolve against provided baseUri
                    if (baseUri is not null) {
                        try {
                            var resolved = new Uri(baseUri, parsed);
                            if (index.TryGetValue(resolved.ToString(), out found)) return found;
                            var filename = Path.GetFileName(resolved.LocalPath);
                            if (!string.IsNullOrEmpty(filename) && index.TryGetValue(filename, out found)) return found;
                        }
                        catch { /* ignore */ }
                    }

                    var filenameRel = Path.GetFileName(parsed.OriginalString);
                    if (!string.IsNullOrEmpty(filenameRel) && index.TryGetValue(filenameRel, out found)) return found;
                }
            }
            else {
                // fallback: try filename of href string itself
                var filename = Path.GetFileName(href!);
                if (!string.IsNullOrEmpty(filename) && index.TryGetValue(filename, out found)) return found;
            }

            return null;
        }
    }
}