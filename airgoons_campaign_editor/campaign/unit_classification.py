from enum import Enum

class Classification(Enum, str):
    RECON = "RECON"
    MANEUVER = "MANEUVER"
    AIR_DEFENSE = "AIR_DEFENSE"
    LOGISTICS = "LOGISTICS"
    COMMAND = "COMMAND"
