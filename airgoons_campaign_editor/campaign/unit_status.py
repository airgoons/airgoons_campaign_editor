from enum import Enum

class Status(Enum):
    # Unit deployment capability in percentage
    GREEN = 100
    YELLOW = 75
    RED = 50
    BLACK = 25
    DESTROYED = 0
