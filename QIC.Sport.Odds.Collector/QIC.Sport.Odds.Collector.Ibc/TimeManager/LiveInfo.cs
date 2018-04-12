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
        public int? Phase = -1;
        public string PhaseStartUtc;
        public int? HomeScore;
        public int? AwayScore;
        public int? HomeRed;
        public int? AwayRed;

        public int LiveTime
        {
            get
            {
                if (string.IsNullOrEmpty(PhaseStartUtc) || Phase == 0) return 0;
                var liveTime = (CommonTools.ToUnixTimeSpan(DateTime.Now) - Convert.ToInt64(PhaseStartUtc)) / 60.0;
                return liveTime < 0 ? 0 : (int)Math.Ceiling(liveTime);
            }
        }

        public override bool Equals(object obj)
        {
            LiveInfo li = obj as LiveInfo;
            if (li == null) return true;
            return Phase == li.Phase && PhaseStartUtc == li.PhaseStartUtc
                && HomeScore == li.HomeScore && AwayScore == li.AwayScore
                && HomeRed == li.HomeRed && AwayRed == li.AwayRed;
        }
    }
}
