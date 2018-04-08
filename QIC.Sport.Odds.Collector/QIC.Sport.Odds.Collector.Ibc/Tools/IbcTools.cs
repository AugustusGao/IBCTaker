using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                    return (int)MarketTypeEnum.F_HDP;
                case "2":
                    return (int)MarketTypeEnum.F_OE;
                case "3":
                    return (int)MarketTypeEnum.F_OU;
                case "4":
                    return (int)MarketTypeEnum.H_OE;
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
                default: return 0;
            }
        }
    }
}
