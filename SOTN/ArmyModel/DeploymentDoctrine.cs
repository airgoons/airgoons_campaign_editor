using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOTN.ArmyModel {
    public static class DeploymentDoctrine {
        public static class Distances {
            // meters
            public static readonly int DivisionToBrigade    = 3000;
            public static readonly int BrigadeToBattalion   = 1000;
            public static readonly int BattalionToCompany   =  500;
            public static readonly int CompanyToPlatoon     =  100;
        }
    }
}
