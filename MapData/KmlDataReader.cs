using System.Collections.Immutable;
using SharpKml.Dom;
using SharpKml.Engine;


namespace MapData {
    public static class KmlDataReader {
        public static async Task<KmlFile?> LoadKmlAsync(string source) {
            try {
                if (Uri.TryCreate(source, UriKind.Absolute, out var uri) &&
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)) {

                    using var httpClient = new HttpClient();
                    using var stream = await httpClient.GetStreamAsync(uri);
                    return KmlFile.Load(stream);
                }
                else if (File.Exists(source)) {
                    using var stream = File.OpenRead(source);
                    return KmlFile.Load(stream);
                }
                else {
                    throw new ArgumentException("Invalid source URI or File Path");
                }
            }
            catch (Exception ex) {
                Console.Error.WriteLine(ex.ToString());
                return null;
            }
        }

        public static ImmutableList<Placemark> GetPlacemarks(KmlFile? kmlFile) {
            return kmlFile?.Root.Flatten().OfType<Placemark>().ToImmutableList() ?? ImmutableList<Placemark>.Empty;
        }
    }
}