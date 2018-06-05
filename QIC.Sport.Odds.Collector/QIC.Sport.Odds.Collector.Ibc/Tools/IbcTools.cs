using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ML.EGP.Sport.Common;
using ML.EGP.Sport.Common.Enums;

namespace QIC.Sport.Odds.Collector.Ibc.Tools
{
    public static class IbcTools
    {
        public static int ConvertToMarketId(string bettype)
        {
            switch (bettype)
            {
                case "1":
                case "609":
                    return (int)MarketTypeEnum.F_HDP;
                case "2":
                case "611":
                    return (int)MarketTypeEnum.F_OE;
                case "3":
                case "610":
                    return (int)MarketTypeEnum.F_OU;
                case "5":
                    return (int)MarketTypeEnum.F_1X2;
                case "7":
                    return (int)MarketTypeEnum.H_HDP;
                case "8":
                    return (int)MarketTypeEnum.H_OU;
                case "10":
                    return (int)MarketTypeEnum.OR;
                case "12":
                    return (int)MarketTypeEnum.H_OE;
                case "15":
                    return (int)MarketTypeEnum.H_1X2;
                case "20":
                    return (int)MarketTypeEnum.ML;
                //case "21":
                //    return (int)MarketTypeEnum.ML;
                //case "153":
                //    return (int)MarketTypeEnum.ML;
                default:
                    return 0;
            }
        }

        public static int ConvertToSportId(string sportType)
        {
            switch (sportType)
            {
                case "1": return 1;
                case "2": return 2;
                case "3": return 3;
                case "7": return 17;
                case "8": return 4;
                case "4": return 5;
                case "5": return 7;
                case "9": return 8;
                case "10": return 10;
                case "50": return 11;
                case "6": return 12;
                case "26": return 18;
                case "25": return 20;
                case "43": return 26;
                default: return 0;
            }
        }
    }
}
