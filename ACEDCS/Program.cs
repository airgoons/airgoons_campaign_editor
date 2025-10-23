using MilitaryModel;
using TTSKML;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using PyDCSInterop;
using Microsoft.Extensions.Configuration;


namespace ACEDCS {
    public class Settings
    {
        public required string VenvPath { get; set; }
        public required string PyDllPath { get; set; }
        public required string PyDcsExtensionsPath { get; set; }
        public required string KmlPath { get; set; }
        public required string TemplateMizPath { get; set; }
        public required string OutputMizPath { get; set; }
        public required string FactionsPath { get; set; }
        public required string TagsPath { get; set; }
        public bool CreateTopLevelPlacemarks { get; set; } = true;
        public bool SpawnUnits { get; set; } = true;
        public bool SpawnPlacemarkCarsOnly { get; set; } = false;
    }

    public class Targets
    {
        public required List<string> UnitNames { get; set; } = new();
    }

    internal class Program {
        static void Main(string[] args) {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("acedcs_config.json", optional: false, reloadOnChange: false)
                .AddJsonFile("targets.json", optional: false, reloadOnChange: false)
                .AddCommandLine(args)
                .Build();

            Settings? settingsConfig = config.GetRequiredSection("Settings").Get<Settings>();
            Targets? targets = config.GetRequiredSection("Targets").Get<Targets>();

            var topLevelUnits = KmlUnitImporter.Run(settingsConfig.KmlPath, settingsConfig.FactionsPath, settingsConfig.TagsPath);

            var targetUnits = SelectTargetUnits(topLevelUnits, targets?.UnitNames);
            var boundingBoxes = GenerateBoundingBoxes(targetUnits);
            var filteredUnits = FilterUnits(topLevelUnits, boundingBoxes);

            var fronts = MilitaryModel.DeploymentModel.KernelFlotGenerator.GenerateFronts(topLevelUnits, 500, 18_000, 1, 1, 2);

            var generatedUnits = ProcessKML.GenerateUnitTree(filteredUnits, fronts, 0.5, true);
            ArmyUnitStatistics.PrintUnitStatistics(generatedUnits, false, true);

            try {
                var pydcs = new PyDCS(settingsConfig.VenvPath, settingsConfig.PyDllPath, false, settingsConfig.PyDcsExtensionsPath);

                SOTN.DCS.MissionPrep.GenerateMiz(pydcs, settingsConfig.TemplateMizPath, settingsConfig.OutputMizPath, topLevelUnits, generatedUnits, fronts, settingsConfig.CreateTopLevelPlacemarks, settingsConfig.SpawnUnits, settingsConfig.SpawnPlacemarkCarsOnly);
            }
            finally {
                PyDCS.ShutdownPythonRuntime();
            }


        }
        public class BoundingBox {
            public string Name { get; }
            public SharpKml.Dom.Polygon Polygon { get; }
            public DeploymentOrder DeploymentOrder { get; }
            public BoundingBox(string name, SharpKml.Dom.Polygon polygon, DeploymentOrder deploymentOrder) {
                Name = name;
                Polygon = polygon;
                DeploymentOrder = deploymentOrder;
            }

        }
        private static IReadOnlyList<ArmyUnit> SelectTargetUnits(IReadOnlyList<ArmyUnit>? units, List<string>? targets) {
            if (units == null || targets == null || targets.Count == 0) {
                return new List<ArmyUnit>();
            }

            // Normalize target names for case-insensitive exact matching
            var targetSet = new HashSet<string>(
                targets.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t!.Trim()),
                StringComparer.OrdinalIgnoreCase
            );

            var results = new List<ArmyUnit>();

            void Visit(ArmyUnit? u) {
                if (u == null) return;

                var name = u.Name?.Trim();
                if (!string.IsNullOrWhiteSpace(name) && targetSet.Contains(name)) {
                    results.Add(u);
                }

                var subs = u.Subordinates;
                if (subs == null) return;
                foreach (var sub in subs) {
                    if (sub != null) Visit(sub);
                }
            }

            foreach (var root in units) Visit(root);

            return results;
        }
        private static List<BoundingBox> GenerateBoundingBoxes(IReadOnlyList<ArmyUnit>? units, double combat_radius = 9_000, double hqsam_radius = 27_000) {
            if (units == null || units.Count == 0) { return new List<BoundingBox>(); }
            var result = new List<BoundingBox>();

            // create transforms: WGS84 <-> WebMercator (meters)
            var csFactory = new CoordinateSystemFactory();
            var ctFactory = new CoordinateTransformationFactory();
            var wgs84 = GeographicCoordinateSystem.WGS84;
            var webMerc = ProjectedCoordinateSystem.WebMercator;
            var toMeters = ctFactory.CreateFromCoordinateSystems(wgs84, webMerc).MathTransform;
            var toLonLat = ctFactory.CreateFromCoordinateSystems(webMerc, wgs84).MathTransform;

            // angles for north-oriented hexagon (degrees clockwise from north)
            var angles = new double[] { 0.0, 60.0, 120.0, 180.0, 240.0, 300.0 };

            foreach (var unit in units) {
                if (unit == null) continue;
                var pos = unit.Position; if (pos == null) continue;

                // extract lat/lon from SharpKml.Base.Vector-like object
                double lat, lon;
                try {
                    lat = (double)pos.GetType().GetProperty("Latitude")!.GetValue(pos)!;
                    lon = (double)pos.GetType().GetProperty("Longitude")!.GetValue(pos)!;
                }
                catch {
                    var latProp = pos.GetType().GetProperty("Lat") ?? pos.GetType().GetProperty("Latitude");
                    var lonProp = pos.GetType().GetProperty("Lon") ?? pos.GetType().GetProperty("Longitude");
                    if (latProp == null || lonProp == null) continue;
                    lat = (double?)latProp.GetValue(pos) ?? 0.0;
                    lon = (double?)lonProp.GetValue(pos) ?? 0.0;
                }

                // project center to meters
                double[] cxy = toMeters.Transform(new[] { lon, lat }); // [x, y]
                double cx = cxy[0]; double cy = cxy[1];

                SharpKml.Dom.Polygon BuildHexagon(double radiusMeters) {
                    // build vertex positions in meters
                    var verts = new List<(double x, double y)>();
                    foreach (var deg in angles) {
                        double rad = deg * Math.PI / 180.0;
                        double dx = radiusMeters * Math.Sin(rad); // east
                        double dy = radiusMeters * Math.Cos(rad); // north
                        verts.Add((cx + dx, cy + dy));
                    }
                    // close
                    if (verts.Count > 0) verts.Add(verts[0]);

                    // reproject back to lon/lat and create SharpKml coordinate list
                    var coords = new SharpKml.Dom.CoordinateCollection();
                    foreach (var v in verts) {
                        double[] p = toLonLat.Transform(new[] { v.x, v.y }); // [lon, lat]
                        double plat = p[1]; double plon = p[0];
                        coords.Add(new SharpKml.Base.Vector(plat, plon));
                    }

                    var ring = new SharpKml.Dom.LinearRing();
                    ring.Coordinates = coords;
                    var outer = new SharpKml.Dom.OuterBoundary();
                    outer.LinearRing = ring;
                    var poly = new SharpKml.Dom.Polygon();
                    poly.OuterBoundary = outer;
                    return poly;
                }

                var combat_bbox = new BoundingBox($"BBOX_COMBAT_{unit.Name}", BuildHexagon(combat_radius), DeploymentOrder.COMBAT);
                var hqsam_bbox = new BoundingBox($"BBOX_HQSAM_{unit.Name}", BuildHexagon(hqsam_radius), DeploymentOrder.HQSAM);

                result.Add(combat_bbox);
                result.Add(hqsam_bbox);
            }

            return result;
        }
        private static IReadOnlyList<ArmyUnit> FilterUnits(IReadOnlyList<ArmyUnit> units, IReadOnlyList<BoundingBox> bboxes) {
            var result = new List<ArmyUnit>();
            if (units == null || units.Count == 0 || bboxes == null || bboxes.Count == 0) return result;

            // prepare NTS geometry factory (lat/lon coordinates)
            var geomFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

            // precompute NTS polygons for each bbox
            var bboxGeoms = new List<(BoundingBox bbox, NetTopologySuite.Geometries.Polygon poly)>();
            foreach (var bb in bboxes) {
                try {
                    var kmlPoly = bb.Polygon;
                    var ring = kmlPoly?.OuterBoundary?.LinearRing;
                    if (ring == null) continue;
                    var coords = ring.Coordinates;
                    if (coords == null || coords.Count == 0) continue;
                    var ntsCoords = new List<NetTopologySuite.Geometries.Coordinate>();
                    foreach (var v in coords) {
                        // SharpKml.Base.Vector: Latitude, Longitude
                        double plat = v.Latitude; double plon = v.Longitude;
                        ntsCoords.Add(new NetTopologySuite.Geometries.Coordinate(plon, plat));
                    }
                    // ensure closed ring
                    if (!ntsCoords[0].Equals2D(ntsCoords[ntsCoords.Count - 1])) {
                        ntsCoords.Add(new NetTopologySuite.Geometries.Coordinate(ntsCoords[0].X, ntsCoords[0].Y));
                    }
                    var linear = geomFactory.CreateLinearRing(ntsCoords.ToArray());
                    var poly = geomFactory.CreatePolygon(linear);
                    bboxGeoms.Add((bb, poly));
                }
                catch {
                    // skip malformed bbox
                    continue;
                }
            }

            foreach (var unit in units) {
                if (unit == null) continue;
                var pos = unit.Position; if (pos == null) continue;

                var pnt = geomFactory.CreatePoint(new NetTopologySuite.Geometries.Coordinate(pos.Longitude, pos.Latitude));

                bool insideCombat = false;
                bool insideHqSam = false;

                foreach (var (bb, poly) in bboxGeoms) {
                    if (poly.Contains(pnt) || poly.Covers(pnt)) {
                        if (bb.DeploymentOrder == DeploymentOrder.COMBAT) insideCombat = true;
                        if (bb.DeploymentOrder == DeploymentOrder.HQSAM) insideHqSam = true;
                    }
                }

                if (insideCombat) {
                    unit.SetDeploymentOrder(DeploymentOrder.COMBAT);
                }
                else if (insideHqSam && !insideCombat) {
                    unit.SetDeploymentOrder(DeploymentOrder.HQSAM);
                }
                else {
                    unit.SetDeploymentOrder(DeploymentOrder.NOT_DEPLOYED);
                }

                if (insideCombat || insideHqSam) {
                    result.Add(unit);
                }
            }

            return result;
        }
    }
}
