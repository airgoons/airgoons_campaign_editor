from unit_status import Status
from unit_classification import Classification
from math import max

class Brigade():
    def __init__(
        self,
        parent_name: str = "<DEFAULT PARENT NAME>",
        name: str = "<DEFAULT NAME>",
        
        # Unit Status (from campaign force tracker)
        status_recon: Status = Status.GREEN,                # recon unit deployment capability
        status_manuever: Status = Status.GREEN,             # maneuver element (e.g. infantry or tank battalion) deployment capability
        status_air_defense: Status = Status.GREEN,          # air defense deployment capability
        status_logistics: Status = Status.GREEN,            # logistics deployment capability
        status_command: Status = Status.GREEN,              # command deployment capability

        # Unit Composition (defined by subclass)
        battalions: list = [],                              # list of subordinate Battalions
        companies: list = [],                               # list of subordinate Companies not under a Battalion command

        # Unit Position (from campaign map)
        position: list = [],                                # floats in the form [latitude, longitude]
        
        # Unit Deployment (defined by subclass)
        maneuverability: float = 500,                       # determines radius of zone in which the HQ will be randomly placed (meters)
        advance_direction: float = 0,                       # direction of the central advancement vector (degrees True)
        advance_distance_max: float = 50000,                # maxium advancement distance (meters) of any subordinate unit
        advance_distance_min: float = 100,                  # minimum advancement distance (meters) of any subordinate unit
        advance_cone_angle: float = 0,                      # size of advancement cone (degrees)

    ):
        self.classification = Classification.COMMAND  # brigades are always only HQ units

        # Unit Status
        self.status_recon = status_recon
        self.status_manuever = status_manuever
        self.status_air_defense = status_air_defense
        self.status_logistics = status_logistics
        self.status_command = status_command

        # Unit Composition
        self.battalions = battalions
        self.companies = companies

        # Unit Position (from campaign map)
        self.position = position

        # Unit Deployment
        self.maneuverability = maneuverability
        self.advance_direction = advance_direction
        self.advance_distance_max = advance_distance_max
        self.advance_distance_min = advance_distance_min
        self.advance_cone_angle = advance_cone_angle

    def best_deployment_distance(self):
        """
        The briagde is only able to place units as far ahead of the hq as advance_distance_max
        The brigade's command capability and logistics capability compound to reduce the effective deployment distance
        The brigade is always able to place units as far ahead of the hq as advance_distance_min

        Example:
            Brigade Logistics and HQ areas have been hit hard, the brigade is unable to project power as effectively away from HQ.

            status_logistics RED (50%), status_command YELLOW (75%) => Brigade is only able to advance 37.5% of its baseline advancement distance
            if advance_distance_max = 50 km, then the brigade's most forward unit can only be 18.75 km away (with some variance introduced by that unit's maneuverability)

        Example:
            Logistics command capability are DESTROYED, so all units retreat to near Brigade HQ

            (0% * 0%) * 50km) = 0 km, return advance_distance_min
        """
        distance = max((self.status_command * self.status_logistics) * self.advance_distance_max, self.advance_distance_min)
        return distance
