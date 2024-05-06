from formation import FormationType, Formation
from unit_status import Status
from unit_classification import Classification

class Brigade(Formation):
    """Brigade level abstraction requiring further implementation at the campaign specific level"""
    def __init__(
        self,
        name: str = "<DEFAULT BRIGADE>",
        
        # Unit Status (from campaign force tracker)
        status_recon: Status = Status.GREEN,                # recon unit deployment capability
        status_manuever: Status = Status.GREEN,             # maneuver element (e.g. infantry or tank battalion) deployment capability
        status_air_defense: Status = Status.GREEN,          # air defense deployment capability
        status_logistics: Status = Status.GREEN,            # logistics deployment capability
        status_command: Status = Status.GREEN,              # command deployment capability

        # From campaign map
        position: list = [0, 0]
    ):
        super().__init__(name, type=FormationType.BRIGADE, classification=Classification.COMMAND, position=position)

        self.status_recon = status_recon
        self.status_manuever = status_manuever
        self.status_air_defense = status_air_defense
        self.status_logistics = status_logistics
        self.status_command = status_command

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
