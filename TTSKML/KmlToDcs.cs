using PyDCSInterop;
using NetTopologySuite.Geometries;
using SharpKml.Dom;
using SOTN.ArmyModel;
using MilitaryModel;

namespace TTSKML {
    public static class KmlToDcs {
        internal static bool CoordinateInPolygon(SharpKml.Base.Vector coordinate, SharpKml.Dom.Polygon polygon) {
            var result = false;

            var coordinates = polygon.OuterBoundary.LinearRing.Coordinates;

            var ntsCoordinates = new List<NetTopologySuite.Geometries.Coordinate>();
            foreach (SharpKml.Base.Vector vector in coordinates) {
                ntsCoordinates.Add(new NetTopologySuite.Geometries.Coordinate(vector.Longitude, vector.Latitude));
            }

            var geometryFactory = new GeometryFactory();
            var ntsPolygon = geometryFactory.CreatePolygon(ntsCoordinates.ToArray());

            var pointToCheck = geometryFactory.CreatePoint(new NetTopologySuite.Geometries.Coordinate(coordinate.Longitude, coordinate.Latitude));

            result = ntsPolygon.Contains(pointToCheck);

            return result;
        }

        // Helper: compute destination lat/lon given start lat/lon, bearing (degrees) and distance (meters)
        private static (double lat, double lon) DestinationPoint(double latDeg, double lonDeg, double bearingDeg, double distanceMeters) {
            const double R = 6378137.0; // Earth radius in meters (WGS84)
            double bearing = DegreeToRadian(bearingDeg);
            double lat1 = DegreeToRadian(latDeg);
            double lon1 = DegreeToRadian(lonDeg);
            double dr = distanceMeters / R;

            double sinLat2 = Math.Sin(lat1) * Math.Cos(dr) + Math.Cos(lat1) * Math.Sin(dr) * Math.Cos(bearing);
            double lat2 = Math.Asin(sinLat2);
            double y = Math.Sin(bearing) * Math.Sin(dr) * Math.Cos(lat1);
            double x = Math.Cos(dr) - Math.Sin(lat1) * Math.Sin(lat2);
            double lon2 = lon1 + Math.Atan2(y, x);

            return (RadianToDegree(lat2), NormalizeLongitude(RadianToDegree(lon2)));
        }

        private static double DegreeToRadian(double d) => d * Math.PI / 180.0;
        private static double RadianToDegree(double r) => r * 180.0 / Math.PI;
        private static double NormalizeLongitude(double lon) {
            // normalize to [-180,180]
            while (lon > 180.0) lon -= 360.0;
            while (lon < -180.0) lon += 360.0;
            return lon;
        }

        public static void Run(string kmlPath, string venv_path, string pydll_path, string pydcs_extensions_location, string template_path, string output_miz_path) {
            var kmlUnitImporterResult = KmlUnitImporter.Run(kmlPath);
            var fronts = MilitaryModel.DeploymentModel.KernelFlotGenerator.GenerateFronts(kmlUnitImporterResult.Units, 500, 18_000, 1, 1, 2);

            var units_to_place = new List<AlignedArmyUnit>();
            var rearAreas = new List<Placemark>();
            var combatAreas = new List<Placemark>();

            // New: collect heading lines for KML export
            var headingLines = new List<(SharpKml.Dom.LineString line, string color, string name)>();
            // populate bounding box categories
            if (kmlUnitImporterResult?.BoundingBoxes != null) {
                foreach (var bbox in kmlUnitImporterResult.BoundingBoxes) {
                    var name = bbox.Name;
                    foreach (MilitaryModel.DeploymentOrder order in Enum.GetValues(typeof(MilitaryModel.DeploymentOrder))) {
                        if (order == MilitaryModel.DeploymentOrder.NOT_DEPLOYED) {
                            continue;
                        }

                        var token = order.ToString();
                        if (name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0) {
                            switch (order) {
                                case MilitaryModel.DeploymentOrder.COMBAT:
                                    combatAreas.Add(bbox);
                                    break;
                                case MilitaryModel.DeploymentOrder.HQSAM:
                                    rearAreas.Add(bbox);
                                    break;
                            }
                        }
                    }
                }
            }

            List<ArmyUnit> everyUnit = new();
            foreach (var unit in kmlUnitImporterResult.Units) {
                var position = unit.Position;
                if (position == null) { continue; }

                // determine deployment heading:
                var coord = new Coordinate(position.Longitude, position.Latitude);
                var heading = MilitaryModel.DeploymentModel.Tools.CalculateUnitHeadingToFLOT(coord, fronts);
                if (heading == null) {
                    Console.WriteLine($"[WARN] null heading after calculation {unit.Name}");
                    continue;
                }
                else {
                    unit.SetHeading(heading.Value);

                    // ApplyDoctrinalDeployment
                    var allUnits = MilitaryModel.DeploymentModel.Tools.ApplyDoctrinalDeployment(unit, fronts, 100);
                    everyUnit.AddRange(allUnits);

                    foreach (var u in allUnits) {
                        // --- Create heading line ---
                        // create a line 4500 meters from the unit in the heading direction
                        double startLat = u.Position.Latitude;
                        double startLon = u.Position.Longitude;
                        var (endLat, endLon) = DestinationPoint(startLat, startLon, u.Heading.Value, 4500.0);

                        var coords = new SharpKml.Dom.CoordinateCollection();
                        coords.Add(new SharpKml.Base.Vector(startLat, startLon)); // KML uses lat,lon
                        coords.Add(new SharpKml.Base.Vector(endLat, endLon));
                        var line = new SharpKml.Dom.LineString { Coordinates = coords };
                        string color = "gray";
                        switch (unit.Faction) {
                            case Faction.NATO:
                                color = "blue";
                                break;
                            case Faction.WarsawPact:
                                color = "red";
                                break;
                            default:
                                break;
                            
                        }

                        headingLines.Add((line, color, u.Name ?? "Unit"));
                    }
                }

                

                // determine deployment order
                bool inRearArea = false;
                bool inCombatArea = false;

                foreach (var bbox in rearAreas) {
                    var polygon = KmlUnitImporter.ExtractPolygonFromGeometry(bbox.Geometry);
                    if (polygon == null) {
                        Console.WriteLine($"[WARN] null polygon in bounding box: {bbox.Name}");
                    }
                    else {
                        if (CoordinateInPolygon(unit.Position, polygon)) { inRearArea = true; break; }
                    }
                }
                foreach (var bbox in combatAreas) {
                    var polygon = KmlUnitImporter.ExtractPolygonFromGeometry(bbox.Geometry);
                    if (polygon == null) {
                        Console.WriteLine($"[WARN] null polygon in bounding box: {bbox.Name}");
                    }
                    else {
                        if (CoordinateInPolygon(unit.Position, polygon)) { inCombatArea = true; break; }
                    }
                }

                var deploymentOrder = MilitaryModel.DeploymentOrder.NOT_DEPLOYED;
                if (inRearArea) {
                    if (inCombatArea) {
                        deploymentOrder = MilitaryModel.DeploymentOrder.COMBAT;
                    }
                    else {
                        deploymentOrder = MilitaryModel.DeploymentOrder.HQSAM;
                    }
                }
                else if (inCombatArea) {
                    deploymentOrder = MilitaryModel.DeploymentOrder.COMBAT;
                }
                else {
                    // fall through
                }

                unit.SetDeploymentOrder(deploymentOrder);
                if (unit.DeploymentOrder != MilitaryModel.DeploymentOrder.NOT_DEPLOYED) {
                    units_to_place.Add(unit);
                }
            }

            try {
                // initialize pydcs
                var pydcs = new PyDCS(venv_path, pydll_path, true, pydcs_extensions_location);
                var dcs = pydcs.DCS;
                var pydcs_extensions = pydcs.PydcsExtensions;

                var terrain = dcs.terrain.GermanyColdWar();
                var mission = dcs.Mission(terrain);


                mission.load_file(template_path);

                foreach (var unit in units_to_place) {
                    var lat = unit.Position?.Latitude;
                    var lon = unit.Position?.Longitude;
                    var order = unit.DeploymentOrder;

                    // add zone for top level
                    // add zones for HQ platoons
                    if (order == MilitaryModel.DeploymentOrder.COMBAT) {
                        // recursively add zones for forward deployable subordinates and their HQ zones according to deployment doctrine
                    }
                }
                //    var dcs_pos = dcs.mapping.Point.from_latlng(dcs.mapping.LatLng(lat, lng), terrain);
                //    var dcs_zone = mission.triggers.add_triggerzone(dcs_pos, 250, false, unitLocation.Unit.Name);

                // export data to files
                ExportKml(everyUnit, fronts, headingLines);
                mission.save(output_miz_path);
            }
            finally {
                PyDCS.ShutdownPythonRuntime();
            }
        }

        private static void ExportKml(List<ArmyUnit> units, NetTopologySuite.Geometries.Geometry? fronts, List<(SharpKml.Dom.LineString line, string color, string name)> headingLines) {
            // --- Export units and fronts as KML ---
            var kmlDoc = new SharpKml.Dom.Document();
            kmlDoc.Name = "Units and Fronts";

            // Add unit placemarks
            foreach (var unit in units) {
                if (unit.Position == null) continue;
                var placemark = new SharpKml.Dom.Placemark {
                    Name = unit.Name,
                    Geometry = new SharpKml.Dom.Point {
                        Coordinate = new SharpKml.Base.Vector(unit.Position.Latitude, unit.Position.Longitude)
                    }
                };
                // kmlDoc.AddFeature(placemark);
            }

            // Add front linestring placemarks
            if (fronts != null) {
                var lines = MilitaryModel.DeploymentModel.KernelFlotGenerator.ExtractLineStrings(fronts) as System.Collections.IEnumerable;
                if (lines != null) {
                    int idx = 1;
                    foreach (var lineObj in lines) {
                        var line = lineObj as NetTopologySuite.Geometries.LineString;
                        if (line == null) continue;
                        var coords = new SharpKml.Dom.CoordinateCollection();
                        foreach (var c in line.Coordinates) {
                            coords.Add(new SharpKml.Base.Vector(c.Y, c.X)); // KML uses lat,lon
                        }
                        var placemark = new SharpKml.Dom.Placemark {
                            Name = $"Front {idx}",
                            Geometry = new SharpKml.Dom.LineString {
                                Coordinates = coords
                            }
                        };
                        kmlDoc.AddFeature(placemark);
                        idx++;
                    }
                }
            }

            // Add heading linestring placemarks with color
            foreach (var (line, color, name) in headingLines) {
                var coords = new SharpKml.Dom.CoordinateCollection();
                foreach (var c in line.Coordinates) {
                    coords.Add(new SharpKml.Base.Vector(c.Latitude, c.Longitude)); // KML uses lat,lon
                }
                var placemark = new SharpKml.Dom.Placemark {
                    Name = $"{name} Heading",
                    Geometry = new SharpKml.Dom.LineString {
                        Coordinates = coords
                    },
                    StyleUrl = new Uri($"#{color}-line", UriKind.Relative)
                };
                kmlDoc.AddFeature(placemark);
            }

            // Add styles for blue and red lines (Color32 expects ABGR: alpha, blue, green, red)
            var blueLineStyle = new SharpKml.Dom.Style {
                Id = "blue-line",
                Line = new SharpKml.Dom.LineStyle {
                    Color = new SharpKml.Base.Color32(255, 255, 0, 0), // blue (A,B,G,R)
                    Width = 3
                }
            };
            var redLineStyle = new SharpKml.Dom.Style {
                Id = "red-line",
                Line = new SharpKml.Dom.LineStyle {
                    Color = new SharpKml.Base.Color32(255, 0, 0, 255), // red (A,B,G,R)
                    Width = 3
                }
            };
            var grayLineStyle = new SharpKml.Dom.Style {
                Id = "gray-line",
                Line = new SharpKml.Dom.LineStyle {
                    Color = new SharpKml.Base.Color32(255, 128, 128, 128), // gray (symmetric)
                    Width = 3
                }
            };
            kmlDoc.AddStyle(blueLineStyle);
            kmlDoc.AddStyle(redLineStyle);
            kmlDoc.AddStyle(grayLineStyle);

            // Save KML to file
            var kml = new SharpKml.Dom.Kml { Feature = kmlDoc };
            using (var stream = System.IO.File.Create("output_fronts.kml")) {
                var serializer = SharpKml.Engine.KmlFile.Create(kml, false);
                serializer.Save(stream);
            }
        }
    }
}
