from dcs import unittype

from pydcs_extensions.modsupport import vehiclemod


@vehiclemod
class MANPADS_Blowpipe(unittype.VehicleType):
    id = "Soldier_Blowpipe_LDM"
    name = "MANPADS Blowpipe"
    detection_range = 7500
    threat_range = 3500
    air_weapon_dist = 3500

@vehiclemod
class MANPADS_Redeye_FIM43C(unittype.VehicleType):
    id = "Soldier_Redeye_LDM"
    name = "MANPADS FIM-43C Redeye"
    detection_range = 7500
    threat_range = 4500
    air_weapon_dist = 4500

@vehiclemod
class MANPADS_Strela2_SA7(unittype.VehicleType):
    id = "Soldier_Strela2_LDM"
    name = "MANPADS Strela-2 SA-7"
    detection_range = 7500
    threat_range = 3200
    air_weapon_dist = 3200

@vehiclemod
class MANPADS_Strela2M_SA7B(unittype.VehicleType):
    id = "Soldier_Strela2M_LDM"
    name = "MANPADS Strela-2M SA-7B"
    detection_range = 7500
    threat_range = 4200
    air_weapon_dist = 4200
