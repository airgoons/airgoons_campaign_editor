from enum import Enum

class Status(float, Enum):
    # Unit deployment capability in float percentage
    GREEN = 1.00
    YELLOW = 0.75
    RED = 0.50
    BLACK = 0.25
    DESTROYED = 0.0
