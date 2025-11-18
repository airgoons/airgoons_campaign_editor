using MilitaryModel;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Distance;
using NetTopologySuite.Utilities;
using PyDCSInterop;
using Python.Runtime;
using SOTN.ArmyModel.Platoon;
using System.Net;
using System.Xml.Linq;

namespace SOTN.DCS {
    public static class MissionPrep {
        public static void GenerateMiz(PyDCS pydcs, string templatePath, string outputPath, IReadOnlyList<ArmyUnit> topLevelUnits, Geometry? fronts) {
            dynamic dcs = pydcs.DCS;
            dynamic terrain = dcs.terrain.GermanyColdWar();
            dynamic miz = dcs.Mission(terrain);

            miz.load_file(templatePath);

            AddFronts(pydcs, miz, fronts);
            AddUnits(pydcs, miz, topLevelUnits);

            miz.save(outputPath);
        }
        
        private static void AddFronts(PyDCS pydcs, dynamic startMiz, Geometry? fronts)
        {
            dynamic dcs = pydcs.DCS;
            dynamic terrain = dcs.terrain.GermanyColdWar();

            dynamic layer = startMiz.drawings.get_layer(dcs.drawing.drawings.StandardLayer.Common);

            if (fronts != null)
            {
                var lines = MilitaryModel.DeploymentModel.KernelFlotGenerator.ExtractLineStrings(fronts) as System.Collections.IEnumerable;
                if (lines != null)
                {
                    int i = 0;
                    foreach (var lineObj in lines)
                    {
                        ++i;  // 1 index the lines for naming convention
                        var line = lineObj as NetTopologySuite.Geometries.LineString;
                        var front_name = $"FLOT_{i}";

                        if (line == null) continue;
                        var coords = new SharpKml.Dom.CoordinateCollection();

                        //dynamic centroid = dcs.mapping.Point.from_latlng(dcs.mapping.LatLng(line.Centroid.Y, line.Centroid.X), terrain);
                        dynamic zerozero = dcs.mapping.Point(0, 0, terrain);

                        List<dynamic> points = new();
                        foreach (var coord in line.Coordinates)
                        {
                            dynamic point = dcs.mapping.Point.from_latlng(dcs.mapping.LatLng(coord.Y, coord.X), terrain);
                            points.Add(point);
                        }

                        
                        dynamic drawing = dcs.drawing.line.LineDrawing(
                            true,                                   // visible
                            zerozero,                               // position
                            front_name,                                 // name
                            dcs.drawing.Rgba(193,0,0,255),          // color [vaha's FLOT color choice]
                            layer.name,                             // layer_name, end of Drawing() args
                            false,                                  // closed
                            8,                                      // line_thickness
                            dcs.drawing.drawing.LineStyle.Boundry1, // line_style
                            dcs.drawing.line.LineMode.Segments,     // line_mode
                            points                                  // points
                        );

                        layer.add_drawing(drawing);
                    }
                }
            }
        }

        private static void AddUnits(PyDCS pydcs, dynamic miz, IReadOnlyList<ArmyUnit> units) {
            dynamic dcs = pydcs.DCS;
            dynamic terrain = dcs.terrain.GermanyColdWar();

            foreach (var unit in units) {
                if (unit == null) continue;
                if (unit.Position == null) continue;
                if (unit.Name == null) continue;

                // Add Trigger Zone
                dynamic latlng = dcs.mapping.LatLng(unit.Position.Latitude, unit.Position.Longitude);
                dynamic position = dcs.mapping.Point.from_latlng(latlng, terrain);
                double radius = 200;
                bool hidden = false;
                string name = unit.Name;
                dynamic zone = miz.triggers.add_triggerzone(position, radius, hidden, name);


                if ((unit.Echelon == ArmyUnitEchelon.BRIGADE) || (unit.Echelon == ArmyUnitEchelon.DIVISION)) {
                    unit.SetAssignment(ArmyUnitAssignment.HQSAM);
                    unit.SetDeploymentOrder(DeploymentOrder.HQSAM);

                    // do not recurse into factory created subordinate ArmyUnits, only work with Platoons for now
                    foreach (var platoon in unit.Subordinates.Where(u => u.Echelon == ArmyUnitEchelon.PLATOON)) {
                        platoon.SetPosition(unit.Position);


                        if (unit.Heading != null) {
                            platoon.SetHeading(unit.Heading.Value);
                        }
                        else {
                            if (unit.Faction == Faction.NATO) {
                                platoon.SetHeading(90);
                            }
                            else {
                                platoon.SetHeading(270);
                            }
                        }

                        /*
                         to ensure units aren't overlapping im hoping to establish some spawn areas within the trigger zone:
                            logi 0-10m from point
                            sam 75-100m from point
                            aaa 250-350m from point
                            manpads 950-1000m from point
                        */
                        double min_distance = 0;
                        double max_distance = 0;

                        // Add HQ Statics
                        if (platoon.Assignment == ArmyUnitAssignment.HEADQUARTERS_AREA) {
                            min_distance = 0;
                            max_distance = 10;

                            CreateVehicleGroup(pydcs, miz, platoon, true, zone, min_distance, max_distance);
                        }

                        // Add HQ SAMs
                        if (platoon.Assignment == ArmyUnitAssignment.HQSAM) {
                            var unit0 = platoon.VehicleAllocations.FirstOrDefault();
                            if (unit0 != null) {
                                switch (unit0.SourceAllocation.Role) {
                                    case VehicleRole.MANPADS:
                                        min_distance = 950;
                                        max_distance = 1000;
                                        break;
                                    case VehicleRole.AAA:
                                        min_distance = 250;
                                        max_distance = 350;
                                        break;
                                    case VehicleRole.SAM_Short:
                                    case VehicleRole.SAM_Medium:
                                        min_distance = 75;
                                        max_distance = 100;
                                        break;

                                    default:
                                        throw new NotImplementedException($"HQSAM spawn logic for VehicleRole not implemented [{unit0.SourceAllocation.Role}]");

                                }
                            }

                            CreateVehicleGroup(pydcs, miz, platoon, false, zone, min_distance, max_distance);
                        }
                    }
                }
            }
        }

        private static dynamic GetMizCountry(dynamic dcs, dynamic miz, Faction faction) {
            if (faction == Faction.NATO) return miz.country(dcs.countries.CombinedJointTaskForcesBlue.name);
            else if (faction == Faction.WarsawPact) return miz.country(dcs.countries.CombinedJointTaskForcesRed.name);
            else throw new NotImplementedException($"[ERROR] unsupported faction {faction}");

        }

        private static void CreateVehicleGroup(PyDCS pydcs, dynamic miz, ArmyUnit unit, bool staticGroup = false, dynamic ? zone = null, double min_distance = 0, double max_distance = 0) {
            dynamic dcs = pydcs.DCS;
            dynamic extensions = pydcs.PydcsExtensions;
            var country = GetMizCountry(dcs, miz, unit.Faction);
            dynamic latlng = dcs.mapping.LatLng(unit.Position.Latitude, unit.Position.Longitude);

            dynamic position = dcs.mapping.Point.from_latlng(latlng, miz.terrain);  // default behavior
            if (zone != null) {
                // place randomly in zone
                position = position.random_point_within(max_distance, min_distance);
            }

            List<dynamic> vehicleTypes = new();
            foreach (var vehicleAllocation in unit.VehicleAllocations) {
                foreach (var vehicle in vehicleAllocation.Set.Vehicles) {
                    dynamic? vehicleType = null;
                    try {
                        vehicleType = PyDCS.ResolvePathOnPyObject(dcs, vehicle);
                    }
                    catch {
                        // Console.WriteLine($"[WARN] {vehicleAllocation.VehicleType} not found in dcs, trying pydcs_extensions");

                        try {
                            vehicleType = PyDCS.ResolvePathOnPyObject(extensions, vehicle);
                        }
                        catch {
                            Console.WriteLine($"[ERROR] {vehicle} not found in dcs or pydcs_extensions");
                        }
                    }
                    if (vehicleType == null) {
                        Console.WriteLine($"null vehicleType: {vehicle}");
                        continue;
                    }

                    for (int i = 0; i < vehicleAllocation.Count; ++i) {
                        vehicleTypes.Add(vehicleType);
                    }
                }
           }

            if (!staticGroup) {
                var group_name = "";
                if (unit.Assignment == ArmyUnitAssignment.HQSAM) {
                    if (unit.Faction == Faction.NATO) {
                        group_name = $"BSAM {unit.Name}";
                    }
                    else {
                        group_name = $"RSAM {unit.Name}";
                    }
                }

                dynamic group = miz.vehicle_group_platoon(country, group_name, vehicleTypes, position, unit.Heading);
                group.hidden_on_planner = true;
                group.hidden_on_mfd = true;
            }
            else {
                CreateStaticVehicleGroup(dcs, miz, unit, country, position, vehicleTypes);
            }
        }

        private static void CreateStaticVehicleGroup(dynamic dcs, dynamic miz, ArmyUnit unit, dynamic country, dynamic position, List<dynamic> vehicleTypes, double spacing = 10) {
            dynamic staticPoint = dcs.point.StaticPoint(position);

            // minimum spacing is 10 meters for my own sanity
            var enforcedSpacing = Math.Max(spacing, 10);
            var headingRadians = (unit.Heading.Value - 90) * Math.PI / 180.0;
            var deltaX = enforcedSpacing * Math.Cos(headingRadians);
            var deltaY = enforcedSpacing * Math.Sin(headingRadians);

            var groupName = $"{unit.Name}_HQ_STATIC";
            // thanks ralree  https://github.com/ralreegorganon/calvinball/blob/cefdb8a8d974337fc03d53e934a17b7570c8ee6d/calvinball/mission.py#L232
            
            var sg = dcs.unitgroup.StaticGroup(miz.next_unit_id(), groupName);

            for (int i = 0; i < vehicleTypes.Count; ++i) {
                var s = dcs.unit.Static(miz.next_unit_id(), groupName, vehicleTypes[i], miz.terrain);
                s.position.x = position.x + (i * deltaX);
                s.position.y = position.y + (i * deltaY);

                s.heading = unit.Heading;

                sg.add_unit(s);
            }
            sg.hidden = false;
            sg.dead = false;
            sg.add_point(staticPoint);

            country.add_static_group(sg);
        }
    }
}
