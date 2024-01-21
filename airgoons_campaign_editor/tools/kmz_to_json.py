import os
import urllib.request
from zipfile import ZipFile
import datetime
import json
from os import remove
from shutil import rmtree
import argparse

import geopandas as gpd
import fiona
import shapely

### CONFIG
fiona.drvsupport.supported_drivers['KML'] = 'rw'    # enable default KML support
temp_dir = ".\\temp"
temp_kmz_filename = "temp.kmz"
expected_google_maps_kml_path = "{0}\\doc.kml".format(temp_dir)
### END CONFIG

def kmz_to_json(
    kmz_online_url=None,
    json_output_path="output.json",
    kmz_filename=temp_kmz_filename,
    kml_target=expected_google_maps_kml_path
):    
    cleanup(temp_kmz_filename)
    
    if kmz_online_url is None:
        raise ValueError("No URL input")

    urllib.request.urlretrieve(kmz_online_url, kmz_filename)
    with ZipFile(kmz_filename, 'r') as kmz_data:
        kmz_data.extractall(temp_dir)

    data = {
        "url": kmz_online_url,
        "timestamp_utc": str(datetime.datetime.now(datetime.timezone.utc)),
        "layer_data": {}
    }

    for layer in fiona.listlayers(kml_target):
        df = gpd.read_file(kml_target, layer=layer, driver="KML")

        data["layer_data"][layer] = {}

        for name, description, geometry in df.values:
            if name in data["layer_data"][layer]:
                print("[ERROR] Duplicate key exists in layer data:  {0}|{1}".format(layer, name))
            else:
                entry = {
                    "layer":            layer,
                    "name":             name,
                    "description":      description,
                }

                if type(geometry) == shapely.geometry.point.Point:
                    entry["geometry_type"] = "Point"
                    entry["latitude"] = geometry.y
                    entry["longitude"] = geometry.x

                elif type(geometry) == shapely.geometry.polygon.Polygon:
                    entry["geometry_type"] = "Polygon"
                    entry["coordinates"] = list(geometry.exterior.coords)

                elif type(geometry) == shapely.geometry.linestring.LineString:
                    entry["geometry_type"] = "LineString"
                    entry["coordinates"] = list(geometry.coords)

                else:
                    print("[WARN] Geometry type not supported, data may be corrupted: ({0}|{1}):  {2}".format(layer, name, type(geometry)))  # @TODO implement proper logging
                
                data["layer_data"][layer][name] = entry

    with open(json_output_path, 'w') as json_file:
        json_file.write(json.dumps(data, indent=4))

    cleanup(kmz_filename)

def cleanup(kmz_filename):
    if os.path.exists(kmz_filename):
        remove(kmz_filename)
    
    if os.path.exists(temp_dir):
        rmtree(temp_dir)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        prog="kmz_to_json.py",
        description="Pulls data from a given Google Maps KML URL and outputs the data into a more easily consumed JSON file"
    )

    parser.add_argument("--url", "-u", required=True, help="URL for Google Maps KMZ")
    parser.add_argument("--output", "-o", required=False, default="output.json", help="The output filename")

    args = parser.parse_args()
    
    kmz_to_json(kmz_online_url=args.url, json_output_path=args.output)
