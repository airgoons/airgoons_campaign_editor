from formation import FormationType, Formation
from unit_classification import Classification


class Battalion(Formation):
    def __init__(
        self,
        name: str,
        classification: Classification,
        parent: Formation,
        children: list = []
    ):
        super().__init__(name, type=FormationType.BATTALION, classification=classification, parent=parent, children=children)
