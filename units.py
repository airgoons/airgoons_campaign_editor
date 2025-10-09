from __future__ import annotations

from abc import ABC, abstractmethod
from enum import StrEnum, auto

from typing import List

import dcs

class UnitEchelon(StrEnum):
    FRONT = auto()
    ARMY = auto()
    CORPS = auto()
    DIVISION = auto()
    BRIGADE = auto()
    REGIMENT = auto()
    BATTALION = auto()
    COMPANY = auto()
    PLATOON = auto()


class UnitType(StrEnum):
    ARMOR = auto()
    ARTILLERY = auto()
    HEADQUARTERS = auto()
    INFANTRY = auto()
    MECHINF = auto()
    MISSILE = auto()
    MARINE = auto()
    RECONNAISSANCE = auto()
    AMPHIB_ARMOR = auto()
    AMPHIB_MECHINF = auto()
    AMPHIB_RECON = auto()


class Unit(ABC):
    @property
    @abstractmethod
    def Echelon(self) -> UnitEchelon:
        pass

    @property
    @abstractmethod
    def Type(self) -> UnitType:
        pass

    def __init__(self):
        self.SubordinateUnits:  List[Unit] = []
        

class FRONT(Unit):
    @property
    def Echelon(self) -> UnitEchelon:
        return UnitEchelon.FRONT
    @property
    def Type(self) -> UnitType:
        return self._unit_type
        
    @abstractmethod
    def __init__(self, unit_type):
        self._unit_type = unit_type

class ARMY(Unit):
    @property
    def Echelon(self) -> UnitEchelon:
        return UnitEchelon.ARMY
    @property
    def Type(self) -> UnitType:
        return self._unit_type
        
    @abstractmethod
    def __init__(self, unit_type):
        self._unit_type = unit_type

class CORPS(Unit):
    @property
    def Echelon(self) -> UnitEchelon:
        return UnitEchelon.CORPS
    @property
    def Type(self) -> UnitType:
        return self._unit_type
        
    @abstractmethod
    def __init__(self, unit_type):
        self._unit_type = unit_type

class DIVISION(Unit):
    @property
    def Echelon(self) -> UnitEchelon:
        return UnitEchelon.DIVISION
    @property
    def Type(self) -> UnitType:
        return self._unit_type
        
    @abstractmethod
    def __init__(self, unit_type):
        self._unit_type = unit_type

class BRIGADE(Unit):
    @property
    def Echelon(self) -> UnitEchelon:
        return UnitEchelon.BRIGADE
    @property
    def Type(self) -> UnitType:
        return self._unit_type
        
    @abstractmethod
    def __init__(self, unit_type):
        self._unit_type = unit_type

class REGIMENT(Unit):
    @property
    def Echelon(self) -> UnitEchelon:
        return UnitEchelon.REGIMENT
    @property
    def Type(self) -> UnitType:
        return self._unit_type
        
    @abstractmethod
    def __init__(self, unit_type):
        self._unit_type = unit_type

class BATTALION(Unit):
    @property
    def Echelon(self) -> UnitEchelon:
        return UnitEchelon.BATTALION
    @property
    def Type(self) -> UnitType:
        return self._unit_type
        
    @abstractmethod
    def __init__(self, unit_type):
        self._unit_type = unit_type

class COMPANY(Unit):
    @property
    def Echelon(self) -> UnitEchelon:
        return UnitEchelon.COMPANY
    @property
    def Type(self) -> UnitType:
        return self._unit_type
        
    @abstractmethod
    def __init__(self, unit_type):
        self._unit_type = unit_type

class PLATOON(Unit):
    @property
    def Echelon(self) -> UnitEchelon:
        return UnitEchelon.PLATOON

    @property
    def Type(self) -> UnitType:
        return self._unit_type
    
    @property
    def Vehicles(self) -> List[dcs.unittype.VehicleType]:
        return self._vehicles
        
    def __init__(self, unit_type: UnitType, vehicles: List[dcs.unittype.VehicleType]):
        self._unit_type = unit_type
        self._vehicles = vehicles

    def add_vehicle(self, vehicle: dcs.unittype.VehicleType):
        self._vehicles.append(vehicle)
