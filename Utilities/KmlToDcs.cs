using System;
using System.Collections.Generic;
using PyDCSInterop;
using NetTopologySuite.Geometries;
using NetTopologySuite.Features;
using SharpKml.Dom;
using System.ComponentModel.Design;
using System.ComponentModel;
using SOTN.ArmyModel;

namespace Utilities {
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


        public static void Run(string kmlPath, string venv_path, string pydll_path, string pydcs_extensions_location, string template_path, string output_miz_path) {


            var kmlUnitImporterResult = KmlUnitImporter.Run(kmlPath);
            var units_to_place = new List<AlignedArmyUnit>();

            var rearAreas = new List<Placemark>();
            var combatAreas = new List<Placemark>();

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

            foreach (var unit in kmlUnitImporterResult.Units) {
                var position = unit.Position;
                if (position == null) { continue; }

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

                mission.save(output_miz_path);

                var fronts = MilitaryModel.DeploymentModel.KernelFlotGenerator.GenerateFronts(kmlUnitImporterResult.Units);

                // --- Export units and fronts as KML ---
                var kmlDoc = new SharpKml.Dom.Document();
                kmlDoc.Name = "Units and Fronts";

                // Add unit placemarks
                foreach (var unit in kmlUnitImporterResult.Units) {
                    if (unit.Position == null) continue;
                    var placemark = new SharpKml.Dom.Placemark {
                        Name = unit.Name,
                        Geometry = new SharpKml.Dom.Point {
                            Coordinate = new SharpKml.Base.Vector(unit.Position.Latitude, unit.Position.Longitude)
                        }
                    };
                    kmlDoc.AddFeature(placemark);
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

                // Save KML to file
                var kml = new SharpKml.Dom.Kml { Feature = kmlDoc };
                using (var stream = System.IO.File.Create("output_fronts.kml")) {
                    var serializer = SharpKml.Engine.KmlFile.Create(kml, false);
                    serializer.Save(stream);
                }
            }
            finally {
                PyDCS.ShutdownPythonRuntime();
            }
        }
    }
}
