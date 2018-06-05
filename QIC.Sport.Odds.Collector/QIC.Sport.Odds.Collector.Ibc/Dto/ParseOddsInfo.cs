using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using ML.EGP.Sport.Common.Enums;
using Newtonsoft.Json.Linq;
using QIC.Sport.Odds.Collector.Ibc.Tools;

namespace QIC.Sport.Odds.Collector.Ibc.Dto
{
    public static class OddsFactory
    {
        public static ParseOddsInfo CreatOdds(string bettype)
        {
            ParseOddsInfo poi = null;
            //根据不同的bettype，转换成相应的对象
            switch (bettype)
            {
                case "1":
                case "2":
                case "3":
                case "4":
                case "7":
                case "8":
                case "12":
                case "20":
                case "21":
                case "609":
                case "610":
                case "611":
                case "153": poi = new ParseTwoOdds(); break;
                case "5":
                case "15": poi = new Parse1x2Odds(); break;
                //case "10": poi = new MarketOR(); break;
                default: break;
            }
            return poi;
        }
    }
    public class ParseOddsInfo
    {
        public string OddsId { get; set; }
        //所属比赛Id
        public string MatchId { get; set; }
        //自己的类别，根据BetType来生成
        public int MarketId { get; set; }
        //状态
        public string OddsStatus { get; set; }
        public string Resourceid { get; set; }
        public virtual bool CompareSet(JToken jtoken, bool isUpdate = true)
        {
            return false;
        }

        public virtual string[] GetDataArr()
        {
            return null;
        }
    }

    public class ParseTwoOdds : ParseOddsInfo
    {
        private ILog logger = LogManager.GetLogger(typeof(ParseTwoOdds));
        public string HomeOdds { get; set; }
        public string AwayOdds { get; set; }
        public string Hdp1 { get; set; }
        public string Hdp2 { get; set; }
        public override bool CompareSet(JToken jtoken, bool isUpdate = true)
        {

            bool b = false;
            if (!isUpdate) Hdp1 = "0";
            foreach (JProperty item in jtoken)
            {
                try
                {
                    switch (item.Name)
                    {
                        case "bettype": MarketId = IbcTools.ConvertToMarketId(jtoken[item.Name].ToString()); b = true; break;
                        case "oddsid": OddsId = jtoken[item.Name].ToString(); b = true; break;
                        case "matchid": MatchId = jtoken[item.Name].ToString(); b = true; break;
                        case "oddsstatus": OddsStatus = jtoken[item.Name].ToString(); b = true; break;
                        case "odds1a": HomeOdds = jtoken[item.Name].ToString(); b = true; break;
                        case "odds2a": AwayOdds = jtoken[item.Name].ToString(); b = true; break;
                        case "hdp1": Hdp1 = jtoken[item.Name].ToString(); b = true; break;
                        case "hdp2": Hdp2 = jtoken[item.Name].ToString(); b = true; break;
                        case "resourceid": Resourceid = jtoken[item.Name].ToString(); b = true; break;
                        default: break;
                    }
                }
                catch
                {
                    logger.Error("MarketTwo CompareSet Failed" + jtoken.ToString());
                    continue;
                }
            }

            return b;
        }

        public override string[] GetDataArr()
        {
            string hdp = Hdp1;
            if (MarketId == (int)MarketTypeEnum.F_HDP || MarketId == (int)MarketTypeEnum.H_HDP)
            {
                if (!string.IsNullOrEmpty(Hdp1) && !string.IsNullOrEmpty(Hdp2))
                {
                    if (Hdp2 != "0") hdp = Hdp2;
                    else hdp = "-" + hdp;
                }
                else if (!string.IsNullOrEmpty(Hdp1))
                {
                    hdp = "-" + Hdp1;
                }
                else if (!string.IsNullOrEmpty(Hdp2))
                {
                    hdp = Hdp2;
                }
            }

            return new[] { "", hdp, HomeOdds, AwayOdds, OddsStatus };
        }
    }

    public class Parse1x2Odds : ParseOddsInfo
    {

        private ILog logger = LogManager.GetLogger(typeof(Parse1x2Odds));
        public string HomeOdds { get; set; }
        public string AwayOdds { get; set; }
        public string DrawOdds { get; set; }
        public override bool CompareSet(JToken jtoken, bool isUpdate = true)
        {
            bool b = false;
            foreach (JProperty item in jtoken)
            {
                try
                {
                    switch (item.Name)
                    {
                        case "bettype": MarketId = IbcTools.ConvertToMarketId(jtoken[item.Name].ToString()); b = true; break;
                        case "oddsid": OddsId = jtoken[item.Name].ToString(); b = true; break;
                        case "matchid": MatchId = jtoken[item.Name].ToString(); b = true; break;
                        case "oddsstatus": OddsStatus = jtoken[item.Name].ToString(); b = true; break;
                        case "com1": HomeOdds = jtoken[item.Name].ToString(); b = true; break;
                        case "com2": AwayOdds = jtoken[item.Name].ToString(); b = true; break;
                        case "comx": DrawOdds = jtoken[item.Name].ToString(); b = true; break;
                        default: break;
                    }
                }
                catch
                {
                    logger.Error("Market1x2 CompareSet Failed" + jtoken.ToString());
                    continue;
                }
            }

            return b;
        }
        public override string[] GetDataArr()
        {
            return new[] { "", HomeOdds, DrawOdds, AwayOdds, OddsStatus };
        }
    }

}
