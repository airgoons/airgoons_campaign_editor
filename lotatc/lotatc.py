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
Classifications = Enum(
    "Classifications",
    [
        "unknown",
        "assumed_friend",
        "friend",
        "neutral",
        "suspect",
        "hostile"
    ]
)

Dimensions = Enum(
    "Dimensions",
    [
        "unknown",
        "air",
        "land_unit",
        "sea_surface",
        "land_installation",
        "sea_subsurface",
        "activity_event",
        "air_civil",
        "sea_surface_civil",
        "land_civil"
    ]
)

# @NOTE:  Sub Dimensions not implemented
###
        
class Classification(dict):
    def __init__(self,
    classification: Classifications = Classifications.friend,
    dimension: Dimensions = Dimensions.air,
    sub_dimension: str = ""
    ):
        dict.__init__(self,
            classification=classification,
            dimension=dimension,
            sub_dimension=""
        )


class Circle(dict):
    def __init__(self,
        author: str = "",
        brushStyle: int = 1,
        color: str = "#ffff0000",
        colorBg: str = "#33ff0000",
        font: dict = Font(color="#ffb5824d", font="Lato"),
        id: str = "{0c7ce5ea-1b6c-4e2f-997f-3bbd603ce5af}",
        latitude: float = 43.462436190556275,
        lineWidth: int = 1,
        longitude: float = 42.863763861314595,
        name: str = "circle #2995",
        radius: float = 56044.85317359681,
        shared: bool = False,
        text: str = "",
        timestamp: str = "",
        type: str = "circle"
    ):
        dict.__init__(self,
            author=author,
            brushStyle=brushStyle,
            color=color,
            colorBg=colorBg,
            font=font,
            id=id,
            latitude=latitude,
            lineWidth=lineWidth,
            longitude=longitude,
            name=name,
            radius=radius,
            shared=shared,
            text=text,
            timestamp=timestamp,
            type=type
        )


class Point(dict):
    def __init__(self,
        author: str = "",
        brushStyle: int = 1,
        color: str = "#ffff0000",
        colorBg: str = "#33ff0000",
        font: dict = Font(color="#ff010260", font="Lato"),
        id: str = "{39da8771-fa48-4ddd-8ee1-a2955cbd2eb0}",
        latitude: float = 42.75663984466322,
        lineWidth: int = 10,
        longitude: float = 42.18261151756428,
        name: str = "point #1742",
        shared: bool = False,
        text: str = "None",
        timestamp: str = "",
        type: str = "point"
    ):
        dict.__init__(self,
            author=author,
            brushStyle=brushStyle,
            color=color,
            colorBg=colorBg,
            font=font,
            id=id,
            latitude=latitude,
            lineWidth=lineWidth,
            longitude=longitude,
            name=name,
            shared=shared,
            text=text,
            timestamp=timestamp,
            type=type
        )


class Line(dict):
    def __init__(self,
        author: str = "",
        brushStyle: int = 1,
        color: str = "#ffff0000",
        colorBg: str = "#33ff0000",
        id: str = "{9adb8609-384d-4be4-af57-11934b98aa48}",
        lineWidth: int = 1,
        name: str = "line #1153",
        points: list = [],
        shared: bool = False,
        timestamp: str = "",
        type: str = "line"
    ):
        points = [
            {"latitude": 42.36823170702854, "longitude": 42.50670819716012},
            {"latitude": 42.593075043528046, "longitude": 43.03405194719633},
            {"latitude": 42.46353426814357, "longitude": 43.717950872937365},
            {"latitude": 42.732435188128456, "longitude": 43.98711591204864}
        ]

        dict.__init__(self,
            author=author,
            brushStyle=brushStyle,
            color=color,
            colorBg=colorBg,
            id=id,
            lineWidth=lineWidth,
            name=name,
            points=points,
            shared=shared,
            timestamp=timestamp,
            type=type
        )


class Polygon(dict):
    def __init__(self,
        author: str = "",
        brushStyle: int = 1,
        color: str = "#ffff0000",
        colorBg: str = "#33ff0000",
        id: str = "{7ff58c6e-943a-4ee8-83f6-13139dadc41b}",
        lineWidth: int = 1,
        name: str = "polygon #2381",
        points: list = [],
        shared: bool = False,
        timestamp: str = "",
        type: str = "polygon"
    ):
        points = [
            {
                "latitude": 42.24635844180599,
                "longitude": 40.66649823634964
            },
            {
                "latitude": 41.791329259042726,
                "longitude": 41.06475263081174
            },
            {
                "latitude": 42.18736846129762,
                "longitude": 41.79534345106512
            },
            {
                "latitude": 42.441242395363616,
                "longitude": 41.17461591200416
            },
            {
                "latitude": 42.2260232938033,
                "longitude": 41.16088300182719
            }
        ]

        dict.__init__(self,
            author=author,
            brushStyle=brushStyle,
            color=color,
            colorBg=colorBg,
            id=id,
            lineWidth=lineWidth,
            name=name,
            points=points,
            shared=shared,
            timestamp=timestamp,
            type=type
        )

class Corridor(dict):
    def __init__(self,
        author: str = "",
        brushStyle: str = 1,
        color: str = "#ffff0000",
        colorBg: str = "#33ff0000",
        font: dict = Font(color="#ff35c6d7", font="Lato"),
        id: str = "{e286d343-eb3e-44d9-be1b-0b82da78bd24}",
        lineWidth: int = 1,
        name: str = "corridor #3266",
        points: list = [],
        radius: int = 9260,
        shared: bool = False,
        text: str = "Corridor",
        timestamp: str = "",
        type: str = "corridor"
    ):
        points = [
            {
                "latitude": 44.20932705193068,
                "longitude": 42.05352216199017
            },
            {
                "latitude": 44.36268874048932,
                "longitude": 43.682245306611264
            },
            {
                "latitude": 43.99434327453737,
                "longitude": 44.62157636128657
            }
        ]
        
        dict.__init__(self,
            author=author,
            brushStyle=brushStyle,
            color=color,
            colorBg=colorBg,
            font=font,
            id=id,
            lineWidth=lineWidth,
            name=name,
            points=points,
            radius=radius,
            shared=shared,
            text=text,
            timestamp=timestamp,
            type=type
        )


class Orbit(dict):
    def __init__(self,
        author: str = "",
        brushStyle: int = 1,
        color: str = "#ffff0000",
        colorBg: str = "#33ff0000",
        font: dict = Font(color="#ff065f17", font="Lato"),
        headingDeg: float = 103.31841494606454,
        id: str = "{1fbcaa1c-820d-4f4d-be07-12043559fe38}",
        latitude: float = 43.32272739948711,
        length: float = 22851.141026267258,
        lineWidth: int = 4,
        longitude: float = 44.26177411514135,
        name: str = "orbit #1842",
        shared: bool = False,
        text: str = "Orbit",
        timestamp: str = "",
        type: str = "orbit",
        width: float = 9572.000000004353
    ):
        dict.__init__(self,
            author=author,
            brushStyle=brushStyle,
            color=color,
            colorBg=colorBg,
            font=font,
            headingDeg=headingDeg,
            id=id,
            latitude=latitude,
            length=length,
            lineWidth=lineWidth,
            longitude=longitude,
            name=name,
            shared=shared,
            text=text,
            timestamp=timestamp,
            type=type,
            width=width
        )


class Symbol(dict):
    def __init__(self,
        author: str = "",
        brushStyle: int = 1,
        classification: dict = Classification(classification="friend", dimension="air", sub_dimension=""),
        color: str = "#ffff0000",
        colorBg: str = "#33ff0000",
        font: dict = Font(color="#ffd5b97b", font="Lato"),
        id: str = "{337dd9cb-c62b-4ea9-abef-fff8c71328ab}",
        latitude: float = 43.69324441257426,
        lineWidth: int = 10,
        longitude: float = 43.92669110735929,
        name: str = "symbol #2437",
        shared: bool = False,
        text: str = "None",
        timestamp: str = "",
        type: str = "symbol"
    ):
        dict.__init__(self,
            author=author,
            brushStyle=brushStyle,
            classification=classification,
            color=color,
            colorBg=colorBg,
            font=font,
            id=id,
            latitude=latitude,
            lineWidth=lineWidth,
            longitude=longitude,
            name=name,
            shared=shared,
            text=text,
            timestamp=timestamp,
            type=type
        )


class LotATC(dict):
    def __init__(self,
        author: str = "me",
        drawings: list = [],
        enable: bool = True,
        id: str = "{bb5a86a9-6e84-46cf-b70e-b885a7f089e6}",
        name: str = "Default layer",
        shared: bool = False,
        timestamp: str = "",
        type: str = "layer",
        version: str = "hotfixes/220"
    ):
        drawings = [Circle(), Point(), Line(), Polygon(), Corridor(), Orbit(), Symbol()]

        dict.__init__(self,
            author=author,
            drawings=drawings,
            enable=enable,
            id=id,
            name=name,
            shared=shared,
            timestamp=timestamp,
            type=type,
            version=version
        )

if __name__ == "__main__":
    with open("lotatc.json", 'w') as json_file:
        lotatc = LotATC()
        json.dump(lotatc, json_file, indent=4)
