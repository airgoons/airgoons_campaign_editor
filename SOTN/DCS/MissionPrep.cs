using MilitaryModel;
using PyDCSInterop;
using Python.Runtime;
using System.Net;

namespace SOTN.DCS {
    public static class MissionPrep {
        public static void GenerateMiz(PyDCS pydcs, string templatePath, string outputPath, IReadOnlyList<ArmyUnit> units) {
            dynamic dcs = pydcs.DCS;
            dynamic terrain = dcs.terrain.GermanyColdWar();
            dynamic miz = dcs.Mission(terrain);
            
            miz.load_file(templatePath);

            // assume units is flattened, do not recurse
            foreach (var unit in units) {
                if (unit == null) {
                    Console.WriteLine("[WARN] unit null, skipping");
                    continue;
                }
                if (unit.Name == null) {
                    Console.WriteLine($"[WARN] MissionPrep:  unit.Name null, skipping");
                    continue;
                }
                if (unit.Position == null) {
                    Console.WriteLine($"[WARN] MissionPrep:  unit position null {unit.Name}, skipping");
                    continue;
                }
                // place trigger zone
                dynamic latlng = dcs.mapping.LatLng(unit.Position.Latitude, unit.Position.Longitude);
                dynamic position = dcs.mapping.Point.from_latlng(latlng, terrain);
                double radius = 200;
                bool hidden = false;
                string name = unit.Name;
                dynamic zone = miz.triggers.add_triggerzone(position, radius, hidden, name);
                

                // Vehicle spawning occurs at the Platoon level only.
                if (unit.Echelon == ArmyUnitEchelon.PLATOON) {
                    if ((unit.DeploymentOrder == DeploymentOrder.COMBAT) || (unit.DeploymentOrder == DeploymentOrder.HQSAM)) {
                        if (unit.Assignment == ArmyUnitAssignment.HEADQUARTERS_AREA) {
                            CreateVehicleGroup(dcs, miz, unit, true);                            
                        }
                        else {
                            CreateVehicleGroup(dcs, miz, unit, false);
                        }
                    }
                    else {
                        Console.WriteLine($"[WARN] unit assignment and deployment order combination not supported: {unit.Name}, {unit.Assignment}, {unit.DeploymentOrder}");
                        continue;
                    }
                }
            }

            // save new miz
            miz.save(outputPath);
        }
        private static dynamic GetMizCountry(dynamic dcs, dynamic miz, Faction faction) {
            if (faction == Faction.NATO) return miz.country(dcs.countries.CombinedJointTaskForcesBlue.name);
            else if (faction == Faction.WarsawPact) return miz.country(dcs.countries.CombinedJointTaskForcesRed.name);
            else throw new NotImplementedException($"[ERROR] unsupported faction {faction}");

        }

        private static void CreateVehicleGroup(dynamic dcs, dynamic miz, ArmyUnit unit, bool staticGroup = false) {
            var country = GetMizCountry(dcs, miz, unit.Faction);
            dynamic latlng = dcs.mapping.LatLng(unit.Position.Latitude, unit.Position.Longitude);
            dynamic position = dcs.mapping.Point.from_latlng(latlng, miz.terrain);

            List<dynamic> vehicleTypes = new();
            foreach (var vehicleAllocation in unit.VehicleAllocations) {
                var vehicleType = PyDCS.ResolvePathOnPyObject(dcs, vehicleAllocation.VehicleType);
                if (vehicleType == null) {
                    Console.WriteLine($"null vehicleType: {vehicleAllocation.VehicleType}");
                    continue;
                }

                for (int i = 0; i < vehicleAllocation.Count; ++i) {
                    vehicleTypes.Add(vehicleType);
                }
            }

            if (!staticGroup) {
                miz.vehicle_group_platoon(country, "wonko", vehicleTypes, position, unit.Heading);
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

            var groupName = "wonko_static";
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
