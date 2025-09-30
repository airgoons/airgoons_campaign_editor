using System;
using System.IO;
using System.Text.Json.Serialization;

namespace TTSKML.TTSJSON {
    public class FactionsJsonEntry {
        [JsonPropertyName("front_png")]
        public string? FrontPng { get; set; }

        [JsonPropertyName("front_png_url")]
        public string? FrontPngUrl { get; set; }

        [JsonPropertyName("back_png")]
        public string? BackPng { get; set; }

        [JsonPropertyName("back_png_url")]
        public string? BackPngUrl { get; set; }

        // Convenience checks
        public bool HasFront => !string.IsNullOrWhiteSpace(FrontPngUrl) || !string.IsNullOrWhiteSpace(FrontPng);
        public bool HasBack => !string.IsNullOrWhiteSpace(BackPngUrl) || !string.IsNullOrWhiteSpace(BackPng);
    }
}
