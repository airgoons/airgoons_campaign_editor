from formation import FormationType, Formation
from unit_classification import Classification


class Company(Formation):
    def __init__(
        self,
        name: str,
        classification: Classification,
        parent: Formation,
        children: list = []
    ):
        super().__init__(
            name,
            type=FormationType.COMPANY,
            classification=classification,
            parent=parent,
            children=children)


if __name__ == "__main__":
    # verification of formation proof of concept 
    from brigade import Brigade
    from battalion import Battalion

    brigade = Brigade()
    battalion = Battalion("battalion", Classification.MANEUVER, brigade)
    recon_company = Company("recon", Classification.RECON, brigade)
    infantry_company = Company("infantry", Classification.MANEUVER, battalion)

    print("done")
