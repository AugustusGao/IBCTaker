using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QIC.Sport.Odds.Collector.Common;

namespace QIC.Sport.Odds.Collector.Ibc.TimeManager
{
    public class LiveInfo
    {
        public string SrcMatchId;
        public int SportId;
        public string Csstatus;
        public string Liveperiod;
        public string PhaseStartUtc;
        public DateTime? PhaseStartUtcUpdateTime;
        public int? HomeScore;
        public int? AwayScore;
        public int? HomeRed;
        public int? AwayRed;
        public int? Phase { get { return Csstatus == "1" ? 0 : string.IsNullOrEmpty(Liveperiod) ? (int?)null : Liveperiod == "1" ? 1 : Liveperiod == "2" ? 2 : Liveperiod == "3" ? 3 : Liveperiod == "4" ? 4 : -1; } }
        public int LiveTime(int ibcDiffSyncTime)
        {
            if (string.IsNullOrEmpty(PhaseStartUtc) || Phase == 0) return 0;
            double liveTime = 0;
            if (SportId == 2)
            {
                liveTime = (CommonTools.ToUnixTimeSpan(PhaseStartUtcUpdateTime.Value) - ibcDiffSyncTime - Convert.ToInt64(PhaseStartUtc)) / 60.0;
                if (liveTime < 0) liveTime = 0;
                return 12 - (int)Math.Floor(liveTime);    //  篮球13分钟由12到0
            }

            liveTime = (CommonTools.ToUnixTimeSpan(DateTime.Now) - ibcDiffSyncTime - Convert.ToInt64(PhaseStartUtc)) / 60.0;
            return liveTime < 0 ? 0 : (int)Math.Floor(liveTime);
        }

        public override bool Equals(object obj)
        {
            LiveInfo li = obj as LiveInfo;
            if (li == null) return true;
            return Csstatus == li.Csstatus
                    && Liveperiod == li.Liveperiod
                    && PhaseStartUtc == li.PhaseStartUtc
                    && HomeScore == li.HomeScore && AwayScore == li.AwayScore
                    && HomeRed == li.HomeRed && AwayRed == li.AwayRed;
        }
    }
}
