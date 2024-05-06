from enum import Enum

class Classification(str, Enum):
    RECON = "RECON"
    MANEUVER = "MANEUVER"
    AIR_DEFENSE = "AIR_DEFENSE"
    LOGISTICS = "LOGISTICS"
    COMMAND = "COMMAND"
