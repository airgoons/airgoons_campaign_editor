using MilitaryModel;
using SOTN.ArmyModel.Brigade;
using SOTN.ArmyModel.Platoon;
using System;
using System.Collections.Generic;

namespace SOTN.ArmyModel.Division {
    public static class DivisionFactory {
        public static Division CreateDivision(Faction? faction, Nation? nation, ArmyUnitType type, string name) {
            var (resolvedFaction, resolvedNation) = AlignedArmyUnitFactory.ResolveIdentity(faction, nation);

            var description = $"{resolvedFaction}_{resolvedNation}_{type}_Division";

            // A division contains 4 brigades of the same unit type.
            var subordinateAssignments = new List<SubordinateAssignment>();
            for (int i = 0; i < 4; i++) {
                var subordinateName = $"{name}/{i}BDE";
                var brigade = BrigadeFactory.CreateBrigade(resolvedFaction, resolvedNation, type, subordinateName);
                subordinateAssignments.Add(new SubordinateAssignment(brigade, ArmyUnitAssignment.FORWARD_DEPLOYABLE));
            }
            for (int i = 0; i < 5; i++) {
                var subordinateName = $"{name}/HQ/{i}PLT";
                var platoon = PlatoonFactory.CreatePlatoon(resolvedFaction, resolvedNation, ArmyUnitType.HEADQUARTERS, name);
                subordinateAssignments.Add(new SubordinateAssignment(platoon, ArmyUnitAssignment.HEADQUARTERS_AREA));
            }

            // TODO:  Add Division level air defense

            return new Division(type, name, description, resolvedFaction, resolvedNation, subordinateAssignments, Array.Empty<VehicleAllocation>());
        }
    }
}