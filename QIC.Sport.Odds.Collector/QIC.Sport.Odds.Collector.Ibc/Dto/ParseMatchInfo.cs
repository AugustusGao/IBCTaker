using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json.Linq;

namespace QIC.Sport.Odds.Collector.Ibc.Dto
{
    public class ParseMatchInfo
    {
        private ILog logger = LogManager.GetLogger(typeof(ParseMatchInfo));
        public string MatchId { get; set; }

        public string SportType { get; set; }
        public string LeagueName { get; set; }
        public string AwayTeamName { get; set; }

        public string HomeTeamName { get; set; }

        public string AwayTeamNameExtension { get; set; }

        public string HomeTeamNameExtension { get; set; }

        public string AwayTeamId { get; set; }

        public string HomeTeamId { get; set; }

        public string KickOffTime { get; set; }

        public string InjuryTime { get; set; }

        public string LiveAwayScore { get; set; }

        public string LiveHomeScore { get; set; }
        public string LiveScore { get; set; }  //  篮球的
        public string AwayRed { get; set; }

        public string HomeRed { get; set; }
        public string Liveperiod { get; set; }
        public string Livetimer { get; set; }
        public string Csstatus { get; set; }
        public string Isneutral { get; set; }
        public bool CompareSet(JToken jtoken)
        {
            bool b = false;
            foreach (JProperty item in jtoken)
            {
                try
                {
                    switch (item.Name)
                    {
                        case "matchid": MatchId = jtoken[item.Name].ToString(); b = true; break;
                        case "sporttype": SportType = jtoken[item.Name].ToString(); b = true; break;
                        case "leaguenameen": LeagueName = jtoken[item.Name].ToString(); b = true; break;
                        case "awayid": AwayTeamId = jtoken[item.Name].ToString(); b = true; break;
                        case "ateamnameen": AwayTeamName = jtoken[item.Name].ToString(); b = true; break;
                        case "ateamname2en": AwayTeamNameExtension = jtoken[item.Name].ToString(); b = true; break;
                        case "homeid": HomeTeamId = jtoken[item.Name].ToString(); b = true; break;
                        case "hteamnameen": HomeTeamName = jtoken[item.Name].ToString(); b = true; break;
                        case "hteamname2en": HomeTeamNameExtension = jtoken[item.Name].ToString(); b = true; break;
                        case "kickofftime": KickOffTime = jtoken[item.Name].ToString(); b = true; break;
                        case "injurytime": InjuryTime = jtoken[item.Name].ToString(); b = true; break;
                        case "liveawayscore": LiveAwayScore = jtoken[item.Name].ToString(); b = true; break;
                        case "livehomescore": LiveHomeScore = jtoken[item.Name].ToString(); b = true; break;
                        case "awayred": AwayRed = jtoken[item.Name].ToString(); b = true; break;
                        case "homered": HomeRed = jtoken[item.Name].ToString(); b = true; break;
                        case "liveperiod": Liveperiod = jtoken[item.Name].ToString(); b = true; break;
                        case "livetimer": Livetimer = jtoken[item.Name].ToString(); b = true; break;
                        case "csstatus": Csstatus = jtoken[item.Name].ToString(); b = true; break;
                        case "live_score": LiveScore = jtoken[item.Name].ToString(); b = true; break;
                        case "isneutral": Isneutral = jtoken[item.Name].ToString(); b = true; break; ;
                        default: break;
                    }
                }
                catch
                {
                    logger.Error("SportsMatchInfo CompareSet Failed" + jtoken.ToString());
                    continue;
                }
            }
            return b;
        }
    }

    public class BasketballScore
    {
        public string llp { get; set; }
        public string hls { get; set; }
        public string h1q { get; set; }
        public string h2q { get; set; }
        public string h3q { get; set; }
        public string h4q { get; set; }
        public string a1q { get; set; }
        public string a2q { get; set; }
        public string a3q { get; set; }
        public string a4q { get; set; }
        public string hot { get; set; }
        public string aot { get; set; }
        public string v { get; set; }
        public string bkg { get; set; }
        public string bkmb { get; set; }
        public string bkom { get; set; }
    }

}
