from typing import Set

from dcs import task
from dcs.planes import PlaneType
from dcs.weapons_data import Weapons

from pydcs_extensions.modsupport import planemod
from pydcs_extensions.weapon_injector import inject_weapons


class WeaponsSu22:
    SU22_BA52 = {"clsid": "{SU22_BA52}", "name": "BA-58 Vyuga ELINT Pod", "weight": 100}
    SU22_KKR = {"clsid": "{SU22_KKR}", "name": "KKR-1 Reconnaissance Pod", "weight": 2000}
    SU22_PTB_800 = {"clsid": "{SU-22-800}", "name": "Su-22 PTB-800", "weight": 0}
    SU22_RBF = {"clsid": "{SU22_RBF}", "name": "BIntake Covers and Ladder", "weight": 0}
    SU22_SPPU = {"clsid": "{22_SPPU}", "name": "SPPU-22-01 Gunpod (Forward)", "weight": 300}


inject_weapons(WeaponsSu22)


@planemod
class Su_22(PlaneType):
    id = "SU22"
    height = 5.13
    width = 13.68
    length = 19.26
    fuel_max = 7200
    max_speed = 2650
    chaff = 128
    flare = 128
    charge_total = 256
    chaff_charge_size = 1
    flare_charge_size = 1
    category = "Interceptor"
    radio_frequency = 127.5

    livery_name = "SU22"  # from type

    class Pylon1:
        pass
        # R_98MR = (1, Weapons.R_98MR)
        # R_98MT = (1, Weapons.R_98MT)
        # R_8M1R = (1, Weapons.R_8M1R)
        # R_8M1T = (1, Weapons.R_8M1T)
        # UB_32A___32_x_UnGd_Rkts__57_mm_S_5KO_HEAT_Frag = (
        #     1,
        #     Weapons.UB_32A___32_x_UnGd_Rkts__57_mm_S_5KO_HEAT_Frag,
        # )
        # UB_16_57UMP___16_x_UnGd_Rkts__57_mm_S_5KO_HEAT_Frag = (
        #     1,
        #     Weapons.UB_16_57UMP___16_x_UnGd_Rkts__57_mm_S_5KO_HEAT_Frag,
        # )
        # S_24B___240mm_UnGd_Rkt__235kg__HE_Frag___Low_Smk_ = (
        #     1,
        #     Weapons.S_24B___240mm_UnGd_Rkt__235kg__HE_Frag___Low_Smk_,
        # )
        # OFAB_100_120___100_kg_GP_Bomb_LD = (1, Weapons.OFAB_100_120___100_kg_GP_Bomb_LD)
        # OFAB_250_270___250_kg_GP_Bomb_LD = (1, Weapons.OFAB_250_270___250_kg_GP_Bomb_LD)
        # FAB_500M_62___500_kg_GP_Bomb_LD = (1, Weapons.FAB_500M_62___500_kg_GP_Bomb_LD)

    class Pylon2:
        pass
        # APU_60_1M_with_R_60__AA_8_Aphid____IR_AAM = (
        #     2,
        #     Weapons.APU_60_1M_with_R_60__AA_8_Aphid____IR_AAM,
        # )
        # APU_60_1M_with_R_60M__AA_8_Aphid_B____IR_AAM = (
        #     2,
        #     Weapons.APU_60_1M_with_R_60M__AA_8_Aphid_B____IR_AAM,
        # )

    class Pylon3:
        pass
        # SPPU_22_1___2_x_23mm__GSh_23L_Autocannon_Pod = (
        #     3,
        #     Weapons.SPPU_22_1___2_x_23mm__GSh_23L_Autocannon_Pod,
        # )
        # PTB_600 = (3, Weapons.PTB_600)
        # UB_32A___32_x_UnGd_Rkts__57_mm_S_5KO_HEAT_Frag = (
        #     3,
        #     Weapons.UB_32A___32_x_UnGd_Rkts__57_mm_S_5KO_HEAT_Frag,
        # )
        # UB_16_57UMP___16_x_UnGd_Rkts__57_mm_S_5KO_HEAT_Frag = (
        #     3,
        #     Weapons.UB_16_57UMP___16_x_UnGd_Rkts__57_mm_S_5KO_HEAT_Frag,
        # )
        # S_24B___240mm_UnGd_Rkt__235kg__HE_Frag___Low_Smk_ = (
        #     3,
        #     Weapons.S_24B___240mm_UnGd_Rkt__235kg__HE_Frag___Low_Smk_,
        # )
        # OFAB_100_120___100_kg_GP_Bomb_LD = (3, Weapons.OFAB_100_120___100_kg_GP_Bomb_LD)
        # OFAB_250_270___250_kg_GP_Bomb_LD = (3, Weapons.OFAB_250_270___250_kg_GP_Bomb_LD)
        # FAB_500M_62___500_kg_GP_Bomb_LD = (3, Weapons.FAB_500M_62___500_kg_GP_Bomb_LD)

    class Pylon4:
        pass
    #     SPPU_22_1___2_x_23mm__GSh_23L_Autocannon_Pod = (
    #         4,
    #         Weapons.SPPU_22_1___2_x_23mm__GSh_23L_Autocannon_Pod,
    #     )
    #     PTB_600 = (4, Weapons.PTB_600)
    #     UB_32A___32_x_UnGd_Rkts__57_mm_S_5KO_HEAT_Frag = (
    #         4,
    #         Weapons.UB_32A___32_x_UnGd_Rkts__57_mm_S_5KO_HEAT_Frag,
    #     )
    #     UB_16_57UMP___16_x_UnGd_Rkts__57_mm_S_5KO_HEAT_Frag = (
    #         4,
    #         Weapons.UB_16_57UMP___16_x_UnGd_Rkts__57_mm_S_5KO_HEAT_Frag,
    #     )
    #     S_24B___240mm_UnGd_Rkt__235kg__HE_Frag___Low_Smk_ = (
    #         4,
    #         Weapons.S_24B___240mm_UnGd_Rkt__235kg__HE_Frag___Low_Smk_,
    #     )
    #     OFAB_100_120___100_kg_GP_Bomb_LD = (4, Weapons.OFAB_100_120___100_kg_GP_Bomb_LD)
    #     OFAB_250_270___250_kg_GP_Bomb_LD = (4, Weapons.OFAB_250_270___250_kg_GP_Bomb_LD)
    #     FAB_500M_62___500_kg_GP_Bomb_LD = (4, Weapons.FAB_500M_62___500_kg_GP_Bomb_LD)
    #
    # class Pylon5:
    #     APU_60_1M_with_R_60__AA_8_Aphid____IR_AAM = (
    #         5,
    #         Weapons.APU_60_1M_with_R_60__AA_8_Aphid____IR_AAM,
    #     )
    #     APU_60_1M_with_R_60M__AA_8_Aphid_B____IR_AAM = (
    #         5,
    #         Weapons.APU_60_1M_with_R_60M__AA_8_Aphid_B____IR_AAM,
    #     )

    class Pylon6:
        pass
        # R_98MR = (6, Weapons.R_98MR)
        # R_98MT = (6, Weapons.R_98MT)
        # R_8M1R = (6, Weapons.R_8M1R)
        # R_8M1T = (6, Weapons.R_8M1T)
        # UB_32A___32_x_UnGd_Rkts__57_mm_S_5KO_HEAT_Frag = (
        #     6,
        #     Weapons.UB_32A___32_x_UnGd_Rkts__57_mm_S_5KO_HEAT_Frag,
        # )
        # UB_16_57UMP___16_x_UnGd_Rkts__57_mm_S_5KO_HEAT_Frag = (
        #     6,
        #     Weapons.UB_16_57UMP___16_x_UnGd_Rkts__57_mm_S_5KO_HEAT_Frag,
        # )
        # S_24B___240mm_UnGd_Rkt__235kg__HE_Frag___Low_Smk_ = (
        #     6,
        #     Weapons.S_24B___240mm_UnGd_Rkt__235kg__HE_Frag___Low_Smk_,
        # )
        # OFAB_100_120___100_kg_GP_Bomb_LD = (6, Weapons.OFAB_100_120___100_kg_GP_Bomb_LD)
        # OFAB_250_270___250_kg_GP_Bomb_LD = (6, Weapons.OFAB_250_270___250_kg_GP_Bomb_LD)
        # FAB_500M_62___500_kg_GP_Bomb_LD = (6, Weapons.FAB_500M_62___500_kg_GP_Bomb_LD)

    class Pylon7:
        pass

    class Pylon8:
        pass

    class Pylon9:
        pass

    class Pylon10   :
        pass



    pylons: Set[int] = {1, 2, 3, 4, 5, 6, 7, 8, 9, 10}

    tasks = [
        task.PinpointStrike,
        task.Reconnaissance,
        task.GroundAttack,
        task.CAS,
        task.AFAC,
        task.SEAD,
        task.RunwayAttack,
        task.AntishipStrike
    ]
    task_default = task.CAS
