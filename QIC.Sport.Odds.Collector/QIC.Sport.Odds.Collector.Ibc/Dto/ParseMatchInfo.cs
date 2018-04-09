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

        public string AwayTeamId { get; set; }

        public string HomeTeamId { get; set; }

        public string KickOffTime { get; set; }

        public string InjuryTime { get; set; }

        public string LiveAwayScore { get; set; }

        public string LiveHomeScore { get; set; }

        public string AwayRed { get; set; }

        public string HomeRed { get; set; }
        public string Liveperiod { get; set; }
        public string Livetimer { get; set; }
        public string Csstatus { get; set; }
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
                        case "homeid": HomeTeamId = jtoken[item.Name].ToString(); b = true; break;
                        case "hteamnameen": HomeTeamName = jtoken[item.Name].ToString(); b = true; break;
                        case "kickofftime": KickOffTime = jtoken[item.Name].ToString(); b = true; break;
                        case "injurytime": InjuryTime = jtoken[item.Name].ToString(); b = true; break;
                        case "liveawayscore": LiveAwayScore = jtoken[item.Name].ToString(); b = true; break;
                        case "livehomescore": LiveHomeScore = jtoken[item.Name].ToString(); b = true; break;
                        case "awayred": AwayRed = jtoken[item.Name].ToString(); b = true; break;
                        case "homered": HomeRed = jtoken[item.Name].ToString(); b = true; break;
                        case "liveperiod": Liveperiod = jtoken[item.Name].ToString(); b = true; break;
                        case "livetimer": Livetimer = jtoken[item.Name].ToString(); b = true; break;
                        case "csstatus": Csstatus = jtoken[item.Name].ToString(); b = true; break;
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
}
