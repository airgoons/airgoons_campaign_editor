using System.Text.Json;
using System.Text.Json.Serialization;
using MilitaryModel;

namespace TTS_Data {
    public class UnitTag {
        [JsonPropertyName("filename")]
        public string Filename { get; set; } = string.Empty;

        [JsonPropertyName("unit_type")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ArmyUnitType UnitType { get; set; }

        // Make the formation nullable so JSON `null` or a missing property does not throw.
        [JsonPropertyName("unit_formation")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ArmyUnitEchelon? UnitFormation { get; set; }

        // Helper to get a non-null formation with a caller-specified default.
        public ArmyUnitEchelon GetFormationOrDefault(ArmyUnitEchelon defaultFormation) =>
            UnitFormation ?? defaultFormation;

        public static List<UnitTag> LoadUnitTags(string path, ArmyUnitEchelon? applyDefaultFormation = null) {
            try {
                var expanded = Environment.ExpandEnvironmentVariables(path);
                if (!File.Exists(expanded)) {
                    Console.Error.WriteLine($"Tag file not found: {expanded}");
                    return new List<UnitTag>();
                }

                var json = File.ReadAllText(expanded);
                var options = new JsonSerializerOptions {
                    PropertyNameCaseInsensitive = true
                };
                // Ensure string enums in JSON are handled (e.g. "INFANTRY", "BRIGADE")
                options.Converters.Add(new JsonStringEnumConverter());

                var tags = JsonSerializer.Deserialize<List<UnitTag>>(json, options) ?? new List<UnitTag>();

                // Optional: substitute a default formation for entries that were null/missing.
                if (applyDefaultFormation.HasValue) {
                    foreach (var t in tags) {
                        if (!t.UnitFormation.HasValue) t.UnitFormation = applyDefaultFormation.Value;
                    }
                }

                return tags;
            }
            catch (JsonException jex) {
                Console.Error.WriteLine($"JSON error loading tags: {jex.Message}");
                return new List<UnitTag>();
            }
            catch (Exception ex) {
                Console.Error.WriteLine($"Failed to load tags: {ex}");
                return new List<UnitTag>();
            }
        }
    }
}
