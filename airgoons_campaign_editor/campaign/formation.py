from enum import Enum

from unit_classification import Classification

class FormationType(str, Enum):
    FORMATION = "FORMATION"  # default for abstract class
    DIVISION = "DIVISION"
    BRIGADE = "BRIGADE"
    BATTALION = "BATTALION"
    COMPANY = "COMPANY"
    PLATOON = "PLATOON"
    FIRETEAM = "FIRETEAM"
    # @TODO NAVY Things


class Formation():
    """
    Abstract class to encapsulate common functionality of all possible formation types
    """
    def __init__(
        self,
        name: str = "<DEFAULT FORMATION>",

        type: FormationType = FormationType.FORMATION,
        classification: Classification = Classification.COMMAND,
        
        parent = None,                          # parent formation
        children: list = [],                    # list of subordinate formations
    
        position: list = [0,0],                 # floats in the form [latitude, longitude]

        # override in subclass
        maneuverability: float = 1,             # determines radius of zone in which the HQ will be randomly placed (meters)
        advance_direction: float = 0,           # direction of the central advancement vector (degrees True)
        advance_distance_max: float = 100,      # maxium advancement distance (meters) of any subordinate unit
        advance_distance_min: float = 10,       # minimum advancement distance (meters) of any subordinate unit
        advance_cone_angle: float = 0,           # size of advancement cone (degrees)
    ):
        self.name = name
        self.type = type
        self.classification = classification
        self.parent = parent
        self.children = children
        self.position = position
        self.maneuverability = maneuverability
        self.advance_direction = advance_direction
        self.advance_distance_max = advance_distance_max
        self.advance_distance_min = advance_distance_min
        self.advance_cone_angle = advance_cone_angle

    def set_position(self, latitude, longitude):
        self.position = [latitude, longitude]

    def best_deployment_distance(self):
        """Override in subclass"""
        return self.advance_distance_max
