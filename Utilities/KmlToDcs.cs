using System;
using System.Collections.Generic;
using PyDCSInterop;
using NetTopologySuite.Geometries;
using NetTopologySuite.Features;
using SharpKml.Dom;
using System.ComponentModel.Design;
using System.ComponentModel;

namespace Utilities {
    public static class KmlToDcs {
        internal class UnitDeploymentOrder {
            internal enum DeploymentOrder {
                COMBAT,
                HQSAM,
                NOT_DEPLOYED
            }
            internal UnitLocation UnitLoc { get; }
            internal DeploymentOrder Order { get; }

            internal UnitDeploymentOrder(UnitLocation ul, DeploymentOrder o) {
                UnitLoc = ul;
                Order = o;
            }
        }

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
            var units_to_place = new List<UnitDeploymentOrder>();

            var rearAreas = new List<Placemark>();
            var combatAreas = new List<Placemark>();

            // populate bounding box categories
            if (kmlUnitImporterResult?.BoundingBoxes != null) {
                foreach (var bbox in kmlUnitImporterResult.BoundingBoxes) {
                    var name = bbox.Name;
                    foreach (UnitDeploymentOrder.DeploymentOrder order in Enum.GetValues(typeof(UnitDeploymentOrder.DeploymentOrder))) {
                        if (order == UnitDeploymentOrder.DeploymentOrder.NOT_DEPLOYED) {
                            continue;
                        }

                        var token = order.ToString();
                        if (name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0) {
                            switch (order) {
                                case UnitDeploymentOrder.DeploymentOrder.COMBAT:
                                    combatAreas.Add(bbox);
                                    break;
                                case UnitDeploymentOrder.DeploymentOrder.HQSAM:
                                    rearAreas.Add(bbox);
                                    break;
                            }
                        }
                    }
                }
            }

            foreach (var unitLoc in kmlUnitImporterResult.UnitLocations) {
                bool inRearArea = false;
                bool inCombatArea = false;

                foreach (var bbox in rearAreas) {
                    var polygon = KmlUnitImporter.ExtractPolygonFromGeometry(bbox.Geometry);
                    if (polygon == null) {
                        Console.WriteLine($"[WARN] null polygon in bounding box: {bbox.Name}");
                    }
                    else {
                        if (CoordinateInPolygon(unitLoc.Coordinate, polygon)) { inRearArea = true; break; }
                    }
                }
                foreach (var bbox in combatAreas) {
                    var polygon = KmlUnitImporter.ExtractPolygonFromGeometry(bbox.Geometry);
                    if (polygon == null) {
                        Console.WriteLine($"[WARN] null polygon in bounding box: {bbox.Name}");
                    }
                    else {
                        if (CoordinateInPolygon(unitLoc.Coordinate, polygon)) { inCombatArea = true; break; }
                    }
                }

                var deploymentOrder = UnitDeploymentOrder.DeploymentOrder.NOT_DEPLOYED;
                if (inRearArea) {
                    if (inCombatArea) {
                        deploymentOrder = UnitDeploymentOrder.DeploymentOrder.COMBAT;
                    }
                    else {
                        deploymentOrder = UnitDeploymentOrder.DeploymentOrder.HQSAM;
                    }
                }
                else if (inCombatArea) {
                    deploymentOrder = UnitDeploymentOrder.DeploymentOrder.COMBAT;
                }
                else {
                    // fall through
                }

                if (deploymentOrder != UnitDeploymentOrder.DeploymentOrder.NOT_DEPLOYED) {
                    var unitDeploymentOrder = new UnitDeploymentOrder(unitLoc, deploymentOrder);
                    units_to_place.Add(unitDeploymentOrder);
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

                foreach (var unitDeploymentOrder in units_to_place) {
                    var unit = unitDeploymentOrder.UnitLoc.Unit;
                    var lat = unitDeploymentOrder.UnitLoc.Coordinate.Latitude;
                    var lon = unitDeploymentOrder.UnitLoc.Coordinate.Longitude;
                    var order = unitDeploymentOrder.Order;

                    // add zone for top level
                    // add zones for HQ platoons
                    if (order == UnitDeploymentOrder.DeploymentOrder.COMBAT) {
                        // recursively add zones for forward deployable subordinates and their HQ zones according to deployment doctrine
                    }
                }
                //    var dcs_pos = dcs.mapping.Point.from_latlng(dcs.mapping.LatLng(lat, lng), terrain);
                //    var dcs_zone = mission.triggers.add_triggerzone(dcs_pos, 250, false, unitLocation.Unit.Name);

                mission.save(output_miz_path);

            }
            finally {
                PyDCS.ShutdownPythonRuntime();
            }
        }
    }
}
