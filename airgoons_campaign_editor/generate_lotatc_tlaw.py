import os
import json
from shutil import rmtree

import tools.kmz_to_json as k2j
import kmz_to_lotatc as k2l
import lotatc


def generate_lotatc_tlaw(
    gmap_url: str,
    kmz_json_path: str,
    lotatc_json_path: str,
    kmz_templates: list
):
    k2j.kmz_to_json(gmap_url, kmz_json_path)
    lotatc_obj = k2l.kmz_to_lotatc(kmz_json_path, lotatc_json_path, kmz_templates)

    with open(lotatc_json_path, 'w') as lotatc_json_file:
        json.dump(lotatc_obj, lotatc_json_file, indent=4)



if __name__ == "__main__":
    #@TODO:  Implement argparse
    gmap_url = "https://www.google.com/maps/d/u/1/kml?mid=1RHYRehltgtfH02o6Yn9JgBmaHt2XqZs"
    
    output_dir = ".\\output"
    kmz_json_path = "{0}\\tlaw_kmz.json".format(output_dir)
    lotatc_json_path = "{0}\\tlaw_lotatc.json".format(output_dir)
    
    blufor_army = k2l.KMZTemplate(
        "blufor_army",
        "BLUFOR Army",
        lotatc.Classification(
            lotatc.Classifications.friend,
            lotatc.Dimensions.land_unit
        )
    )

    blufor_air_force = k2l.KMZTemplate(
        "blufor_air_force",
        "BLUFOR Air Force",
        lotatc.Classification(
            lotatc.Classifications.friend,
            lotatc.Dimensions.land_installation
        )
    )

    blufor_navy = k2l.KMZTemplate(
        "blufor_navy",
        "BLUFOR Navy",
        lotatc.Classification(
            lotatc.Classifications.friend,
            lotatc.Dimensions.sea_surface
        )
    )

    pla_ground_forces = k2l.KMZTemplate(
        "pla_ground_forces",
        "PLA Ground Forces",
        lotatc.Classification(
            lotatc.Classifications.hostile,
            lotatc.Dimensions.land_unit
        )
    )

    pla_air_force = k2l.KMZTemplate(
        "pla_air_force",
        "PLA Air Force",
        lotatc.Classification(
            lotatc.Classifications.hostile,
            lotatc.Dimensions.land_installation
        )
    )
    
    pla_navy = k2l.KMZTemplate(
        "pla_navy",
        "PLA Navy",
        lotatc.Classification(
            lotatc.Classifications.hostile,
            lotatc.Dimensions.sea_surface
        )
    )
    
    
    kmz_templates = [blufor_army, blufor_air_force, blufor_navy, pla_ground_forces, pla_air_force, pla_navy]
    generate_lotatc_tlaw(gmap_url, kmz_json_path, lotatc_json_path, kmz_templates)
