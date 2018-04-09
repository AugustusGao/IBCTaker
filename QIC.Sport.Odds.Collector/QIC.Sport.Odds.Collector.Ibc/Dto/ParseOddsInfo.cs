using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json.Linq;
using QIC.Sport.Odds.Collector.Ibc.Tools;

namespace QIC.Sport.Odds.Collector.Ibc.Dto
{
    public class ParseOddsInfo
    {
        private ILog logger = LogManager.GetLogger(typeof(ParseOddsInfo));
        public string OddsId { get; set; }

        //所属比赛Id
        public string MatchId { get; set; }

        //自己的类别，根据BetType来生成
        public int MarketId { get; set; }

        //状态
        public string OddsStatus { get; set; }
        public string HomeOdds { get; set; }

        public string AwayOdds { get; set; }

        public string Hdp1 { get; set; }

        public string Hdp2 { get; set; }

        public bool CompareSet(JToken jtoken)
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
                        case "odds1a": HomeOdds = jtoken[item.Name].ToString(); b = true; break;
                        case "odds2a": AwayOdds = jtoken[item.Name].ToString(); b = true; break;
                        case "hdp1": Hdp1 = jtoken[item.Name].ToString(); b = true; break;
                        case "hdp2": Hdp2 = jtoken[item.Name].ToString(); b = true; break;
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
    }
}
