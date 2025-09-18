import json
from enum import Enum

# Data structures defined at:    https://www.lotatc.com/documentation/client/drawing.html [2024-01-16]

def Font(
    color: str = "#ffb5824d",
    font: str = "Lato"
):
    return dict(color=color, font=font)

###
# https://www.lotatc.com/documentation/client/classification.html
class Classifications(str, Enum):
    unknown:  str = "unknown"
    assumed_friend: str = "assumed_friend"
    friend: str = "friend"
    neutral: str = "neutral"
    suspect: str = "suspect"
    hostile: str = "hostile"

class Dimensions(str, Enum):
    unknown: str = "unknown"
    air: str = "air"
    land_unit: str = "land_unit"
    sea_surface: str = "sea_surface"
    land_installation: str = "land_installation"
    sea_subsurface: str = "sea_subsurface"
    activity_event: str = "activity_event"
    air_civil: str = "air_civil"
    sea_surface_civil: str = "sea_surface_civil"
    land_civil: str = "land_civil"


# @NOTE:  Sub Dimensions not implemented
###
        
class Classification(dict):
    def __init__(self,
    classification: Classifications,
    dimension: Dimensions,
    sub_dimension: str = ""
    ):
        dict.__init__(self,
            classification=classification,
            dimension=dimension,
            sub_dimension=""
        )


class Drawing(dict):
    def __init__(self,
        author: str = "",
        brushStyle: int = 1,
        color: str = "#ffff0000",
        colorBg: str = "#33ff0000",
        id: str = "",
        name: str = "",
        lineWidth: int = 1,
        type: str = "",
        timestamp: str = "",
        shared: bool = False,
        **kwargs
    ):
        dict.__init__(self,
            author=author,
            brushStyle=brushStyle,
            color=color,
            colorBg=colorBg,
            id=id,
            name=name,
            lineWidth=lineWidth,
            type=type,
            timestamp=timestamp,
            shared=shared,
            **kwargs
        )


class Circle(Drawing):
    def __init__(self,
        latitude: float = 0.0,
        longitude: float = 0.0,
        radius: float = 0.0,
        text: str = "",
        font: dict = Font(),
        **kwargs
    ):
        super(Circle, self).__init__(
            type="circle",
            latitude=latitude,
            longitude=longitude,
            radius=radius,
            text=text,
            font=font,
            **kwargs
        )


class Point(Drawing):
    def __init__(self,
        latitude: float = 0.0,
        longitude: float = 0.0,
        text: str = "",
        font: dict = Font(),
        **kwargs
    ):
        super(Point, self).__init__(
            type="point",
            font=font,
            latitude=latitude,
            longitude=longitude,
            text=text,
            **kwargs
        )


class Line(Drawing):
    def __init__(self,
        points: list = [],
        **kwargs
    ):
        super(Line, self).__init__(
            type="line",
            points=points,
            **kwargs
        )

    def add_point(self,
        latitude: float,
        longitude: float
    ):
        self["points"].append(
            dict(
                latitude=latitude,
                longitude=longitude
            )
        )


class Polygon(Drawing):
    def __init__(self,
        points: list = [],
        **kwargs
    ):
        super(Polygon, self).__init__(
            type="polygon",
            points = points,
            **kwargs
        )

class Corridor(Drawing):
    def __init__(self,
        points: list = [],
        radius: int = 100,
        text: str = "",
        font: dict = Font(),
        **kwargs
    ):
        super(Corridor, self).__init__(
            type="corridor",
            points=points,
            radius=radius,
            text=text,
            font=font,
            **kwargs
        )


class Orbit(Drawing):
    def __init__(self,
        latitude: float = 0.0,
        longitude: float = 0.0,
        headingDeg: float = 0.0,
        length: float = 0.0,
        width: float = 0.0,
        text: str = "",
        **kwargs
    ):
        super(Orbit, self).__init__(
            type="orbit",
            latitude=latitude,
            longitude=longitude,
            headingDeg=headingDeg,
            length=length,
            width=width,
            text=text,
            **kwargs
        )


class Symbol(Drawing):
    def __init__(self,
        latitude: float = 0.0,
        longitude: float = 0.0,
        text: str = "",
        font: dict = Font(),
        classification: dict = Classification(classification="friend", dimension="air", sub_dimension=""),
        **kwargs
    ):
        super(Symbol, self).__init__(
            type="symbol",
            latitude=latitude,
            longitude=longitude,
            text=text,
            font=font,
            classification=classification,
            **kwargs
        )


class LotATC():
    class Encoder(json.JSONEncoder):
        def default(self, obj):
            if isinstance(obj, LotATC):
                data = dict(
                    author = obj.author,
                    drawings = obj.drawings,
                    enable = obj.enable,
                    id = obj.id,
                    name = obj.name,
                    shared = obj.shared,
                    timestamp = obj.timestamp,
                    type = obj.type,
                    version = obj.version
                )
                return data

            else:
                return json.JSONEncoder.default(self, obj)


    def __init__(self,
        author: str = "",
        drawings: list = [],
        enable: bool = True,
        id: str = "",
        name: str = "",
        shared: bool = False,
        timestamp: str = "",
        type: str = "layer",
        version: str = "hotfixes/220"
    ):
        self.author = author
        self.drawings = drawings
        self.enable = enable
        self.id = id
        self.name = name
        self.shared = shared
        self.timestamp = timestamp
        self.type = type
        self.version = version

    def save_json(self, json_path):
        with open(json_path, 'w') as json_file:
            json.dump(self, json_file, indent=4, cls=LotATC.Encoder)
