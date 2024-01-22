import json
import lotatc

class KMZTemplate(dict):
    def __init__(self,
        name: str,
        kmz_layer: str,
        classification: lotatc.Classification
    ):
        dict.__init__(self,
            name=name,
            kmz_layer=kmz_layer,
            classification=classification
        )

    @staticmethod
    def load_kmz_templates(
        template_json_path: str,
    ):
        kmz_templates = []

        with open(template_json_path, 'r') as json_file:
            data = json.load(json_file)

            for item in data:
                name = item.get("name")
                kmz_layer = item.get("kmz_layer")
                classification = item.get("classification")
                classification_obj = lotatc.Classification(
                    lotatc.Classifications(classification.get("classification")),
                    lotatc.Dimensions(classification.get("dimension")),
                )

                template = KMZTemplate(name, kmz_layer, classification_obj)
                kmz_templates.append(template)

        return kmz_templates

    @staticmethod
    def save_kmz_templates(
        template_json_path: str,
        kmz_templates: list = []
    ):
        with open(template_json_path, 'w') as json_file:
            json.dump(kmz_templates, json_file, indent=4)


def kmz_to_lotatc(
    kmz_json_path: str,
    output_path:  str = "lotatc.json",
    kmz_templates: list = []
):
    kmz_data = {}

    with open(kmz_json_path, 'r') as kmz_json_file:
        kmz_data = json.load(kmz_json_file)

    layer_data = kmz_data.get("layer_data", None)

    lotatc_obj = lotatc.LotATC()

    if layer_data is None:
        print("[ERROR] Empty KMZ layer data")
        return
    else:
        for layer, item_data in layer_data.items():
            for kmz_template in kmz_templates:
                if layer == kmz_template.get("kmz_layer"):
                    for unit_name, unit_data in item_data.items():
                        unit_symbol = lotatc.Symbol(
                            latitude = unit_data.get("latitude"),
                            longitude = unit_data.get("longitude"),
                            classification=kmz_template.get("classification")
                        )
                        unit_symbol["name"] = unit_name
                        lotatc_obj.add_drawing(unit_symbol)

    return lotatc_obj
