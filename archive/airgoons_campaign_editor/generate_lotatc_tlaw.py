import json

import lotatc
import kmz

latest_kmz = kmz.KMZ("https://www.google.com/maps/d/u/1/kml?mid=1RHYRehltgtfH02o6Yn9JgBmaHt2XqZs")

lotatc_layers = []
for kmz_layer in latest_kmz.layers:
    if(kmz_layer.get_name() == "ABM Information"):
        drawings = []
        for kmz_item in kmz_layer.get_items():
            if kmz_item.get_geometry().get_geometry_type() == kmz.GeometryTypes.LineString:
                drawing = lotatc.Line(
                    author="wonkotron",
                    name=kmz_item.get_name(),
                    lineWidth=4,
                    color="#FFFFFFFF"
                )

                for (longitude, latitude, altitude) in kmz_item.get_geometry().get_coordinates():
                    drawing.add_point(latitude, longitude)

                drawings.append(drawing)
        
        lotatc_layer = lotatc.LotATC(
            author="wonkotron",
            name=kmz_layer.get_name(),
            drawings=drawings
        )
        lotatc_layers.append(lotatc_layer)

    elif(kmz_layer.get_name() == "PLA Ground Forces"):
        drawings = []
        for kmz_item in kmz_layer.get_items():
            if "ADA BN" in kmz_item.get_name():
                lines = kmz_item.get_description().split("<br>")
                
                for line in lines:
                    if "threat," in line:
                        _, distance_str = line.split(", ")
                        distance_nm = float(distance_str)
                        distance_m = 1852 * distance_nm

                        drawing = lotatc.Circle(
                            name=kmz_item.get_name(),
                            text=kmz_item.get_name(),
                            author="wonkotron",
                            latitude = kmz_item.get_geometry().get_latitude(),
                            longitude = kmz_item.get_geometry().get_longitude(),
                            radius = distance_m,
                            lineWidth = 1,
                            color = "#FF2400",
                            colorBg = "#00ff0000"
                        )

                        drawings.append(drawing)

        lotatc_layer = lotatc.LotATC(
            author="wonkotron",
            name="Threat Rings",
            drawings=drawings
        )
        lotatc_layers.append(lotatc_layer)

    elif(kmz_layer.get_name() == "AOs, Waypoints, Inftrastructure"):
        drawings = []
        for kmz_item in kmz_layer.get_items():
            if "Rio Pescado Bridge" in kmz_item.get_name():
                drawing = lotatc.Symbol(
                    name=kmz_item.get_name(),
                    text=kmz_item.get_name(),
                    author="wonkotron",
                    latitude = kmz_item.get_geometry().get_latitude(),
                    longitude = kmz_item.get_geometry().get_longitude(),
                    classification = lotatc.Classification(
                        lotatc.Classifications.neutral,
                        lotatc.Dimensions.land_installation
                    )
                )
                drawings.append(drawing)
        
        lotatc_layer = lotatc.LotATC(
            author="wonkotron",
            name="Inftrastructure",
            drawings=drawings
        )
        lotatc_layers.append(lotatc_layer)

for lotatc_layer in lotatc_layers:
    json_path = ".\\output\\{0}_{1}.json".format(lotatc_layer.name, lotatc_layer.author)
    lotatc_layer.save_json(json_path)

print("done")
