using System;
using System.IO;
using System.Text.Json.Serialization;

namespace TTS_Data {
    /// <summary>
    /// Represents a single unit entry in factions.json:
    /// { "front_png": "...", "front_png_url": "...", "back_png": "...", "back_png_url": "..." }
    /// Provides helpers to safely inspect and resolve the available image references.
    /// </summary>
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
        public bool HasBack  => !string.IsNullOrWhiteSpace(BackPngUrl)  || !string.IsNullOrWhiteSpace(BackPng);

        /// <summary>
        /// Returns the best available URI for the front image.
        /// If a full URL is present it is returned; if only a filename is present and a baseUri is provided it will be combined.
        /// If a local absolute path is present it will be converted to a file:// URI.
        /// Returns null if nothing resolvable exists.
        /// </summary>
        public Uri? GetFrontUri(Uri? baseUri = null) => ResolveUri(FrontPngUrl, FrontPng, baseUri);

        /// <summary>
        /// Returns the best available URI for the back image (same resolution rules as front).
        /// </summary>
        public Uri? GetBackUri(Uri? baseUri = null) => ResolveUri(BackPngUrl, BackPng, baseUri);

        private static Uri? ResolveUri(string? url, string? filename, Uri? baseUri) {
            // prefer explicit URL
            if (!string.IsNullOrWhiteSpace(url)) {
                // try absolute first
                if (Uri.TryCreate(url, UriKind.Absolute, out var absolute)) return absolute;

                // try relative against baseUri
                if (Uri.TryCreate(url, UriKind.Relative, out var relative) && baseUri is not null) {
                    try { return new Uri(baseUri, relative); }
                    catch { /* fall through */ }
                }

                // last attempt to parse (may succeed as absolute)
                if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var any)) return any;
            }

            // fallback to filename: if baseUri provided, combine; if absolute local path, produce file:// URI
            if (!string.IsNullOrWhiteSpace(filename)) {
                if (Path.IsPathRooted(filename)) {
                    try { return new Uri(filename); }
                    catch { /* fall through */ }
                }

                if (baseUri is not null) {
                    try { return new Uri(baseUri, filename); }
                    catch { /* fall through */ }
                }
            }

            return null;
        }

        public override string ToString() =>
            $"Front: {(HasFront ? (FrontPngUrl ?? FrontPng) : "<none>")}, Back: {(HasBack ? (BackPngUrl ?? BackPng) : "<none>")}";
    }
}
