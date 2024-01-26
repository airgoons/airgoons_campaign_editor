import os
import urllib.request
from zipfile import ZipFile
import datetime
import json
from os import remove
from shutil import rmtree
import argparse
from enum import Enum

import geopandas as gpd
import fiona
import shapely


class GeometryTypes(str, Enum):
    Point: str = str(shapely.geometry.point.Point)
    Polygon: str = str(shapely.geometry.polygon.Polygon)
    LineString: str = str(shapely.geometry.linestring.LineString)


class Geometry(dict):
    def __init__(self,
        geometry_type: GeometryTypes,
        **kwargs
    ):
        dict.__init__(self,
            geometry_type=geometry_type,
            **kwargs
        )
    
    def get_geometry_type(self):
        return self.get("geometry_type", "")


class Point(Geometry):
    def __init__(self,
        latitude: float = 0.0,
        longitude: float = 0.0
    ):
        super(Point, self).__init__(
            geometry_type=GeometryTypes.Point,
            latitude=latitude,
            longitude=longitude
        )

    def get_latitude(self):
        return self.get("latitude", 0.0)
    
    def get_longitude(self):
        return self.get("longitude", 0.0)


class Polygon(Geometry):
    def __init__(self,
        coordinates: list = []  # where each item is [lat, long, altitude]
    ):
        super(Polygon, self).__init__(
            geometry_type=GeometryTypes.Polygon,
            coordinates=coordinates
        )

    def get_coordinates(self):
        return self.get("coordinates", [])


class LineString(Geometry):
    def __init__(self,
        coordinates: list = []
    ):
        super(LineString, self).__init__(
            geometry_type=GeometryTypes.LineString,
            coordinates=coordinates
        )

    def get_coordinates(self):
        return self.get("coordinates", [])
        

class KMZItem(dict):
    def __init__(self,
        name: str,
        description: str,
        geometry: Geometry
    ):
        dict.__init__(self,
            name=name,
            description=description,
            geometry=geometry
        )
    
    def get_name(self):
        return self.get("name", "")
    
    def get_description(self):
        return self.get("description", "")
    
    def get_geometry(self):
        return self.get("geometry", None)


class KMZLayer(dict):
    def __init__(self,
        name: str,
        items: list = []
    ):
        dict.__init__(self,
            name=name,
            items=items
        )
    
    def get_name(self):
        return self.get("name", "")
    
    def get_items(self):
        return self.get("items", [])


class KMZ():
    class Encoder(json.JSONEncoder):
        def default(self, obj):
            if isinstance(obj, KMZ):
                data = dict(
                    url = obj.url,
                    timestamp_utc = obj.timestamp_utc,
                    layers = obj.layers
                )
                return data

            else:
                return json.JSONEncoder.default(self, obj)


    @staticmethod
    def get_data_from_url(kmz_url: str):
        def cleanup(kmz_filename, temp_dir):
            if os.path.exists(kmz_filename):
                remove(kmz_filename)
            
            if os.path.exists(temp_dir):
                rmtree(temp_dir)

        ### CONFIG
        fiona.drvsupport.supported_drivers['KML'] = 'rw'    # enable default KML support
        temp_dir = ".\\temp"
        temp_kmz_filename = "temp.kmz"
        kml_path = "{0}\\doc.kml".format(temp_dir)  # expected path for Google Map KML downloads
        ### END CONFIG

        cleanup(temp_kmz_filename, temp_dir)

        urllib.request.urlretrieve(kmz_url, temp_kmz_filename)
        with ZipFile(temp_kmz_filename, 'r') as kmz_data:
            kmz_data.extractall(temp_dir)

        layers = []
        for layer in fiona.listlayers(kml_path):
            layers.append(
                dict(
                    name = layer,
                    dataframe = gpd.read_file(kml_path, layer=layer, driver="KML")
                )
            )

        cleanup(temp_kmz_filename, temp_dir)

        data = dict(
            url = kmz_url,
            timestamp_utc = str(datetime.datetime.now(datetime.timezone.utc)),
            layers = layers
        )

        return data

    def __init__(self,
        url: str,
    ):
        kmz_data = KMZ.get_data_from_url(url)

        kmz_layers = []
        for layer_data in kmz_data.get("layers"):
            layer_name = layer_data.get("name", "")

            layer_dataframe = layer_data.get("dataframe", None)
            
            kmz_items = []
            for item_name, item_description, item_geometry in layer_dataframe.values:
                geometry = None
                if type(item_geometry) == shapely.geometry.point.Point:
                    geometry = Point(
                        latitude = item_geometry.y,
                        longitude = item_geometry.x
                    )

                elif type(item_geometry) == shapely.geometry.polygon.Polygon:
                    geometry = Polygon(
                        coordinates = list(item_geometry.exterior.coords)
                    )

                elif type(item_geometry) == shapely.geometry.linestring.LineString:
                    geometry = LineString(
                        coordinates = list(item_geometry.coords)
                    )

                else:
                    print("[WARN] Geometry type not supported, data may be corrupted: ({0}|{1}):  {2}".format(layer_name, item_name, type(geometry)))  # @TODO implement proper logging

                kmz_item = KMZItem(item_name, item_description, geometry)
                kmz_items.append(kmz_item)
            
            kmz_layer = KMZLayer(layer_name, kmz_items)
            kmz_layers.append(kmz_layer)

            self.url = kmz_data.get("url")
            self.timestamp_utc = kmz_data.get("timestamp_utc")
            self.layers = kmz_layers

    def save_json(self, json_path):
        with open(json_path, 'w') as json_file:
            json.dump(self, json_file, indent=4, cls=KMZ.Encoder)

    def to_json(self):
        return json.dumps(self, indent=4, cls=KMZ.Encoder)
