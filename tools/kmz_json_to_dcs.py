import json
import dcs

def read_kmz_json(json_path):
    with open(json_path, "r") as json_file:
        return json.load(json_file)

def kmz_json_to_dcs(json_path, miz_path):
    kmz_json_data = read_kmz_json(json_path)

    layer_data = kmz_json_data.get("layer_data")

    miz = dcs.Mission(terrain=dcs.terrain.falklands.Falklands())

    for layer, item_data in layer_data.items():
        for key, value in item_data.items():
            name = value.get("name")
            geometry_type = value.get("geometry_type")
            if geometry_type == "Point":
                position = dcs.mapping.Point.from_latlng(dcs.mapping.LatLng(value.get("latitude"), value.get("longitude")), dcs.terrain.falklands.Falklands())
                
                # @TODO handle variable trigger zone radius
                # @TODO handle zone color
                zone = miz.triggers.add_triggerzone(position=position, radius=500, hidden=False, name=value.get("name"))

            else:
                print("[WARN] geometry_type {0} not yet handled".format(geometry_type))

    miz.save(miz_path)

if __name__ == "__main__":
    json_path = "output\\tlaw_kmz.json"
    miz_path = "output\\tlaw.miz"

    kmz_json_to_dcs(json_path, miz_path)

    # @TODO Implement command line execution handling
