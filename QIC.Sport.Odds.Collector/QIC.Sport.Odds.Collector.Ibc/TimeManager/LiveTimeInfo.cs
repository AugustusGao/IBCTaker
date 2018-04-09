using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QIC.Sport.Odds.Collector.Common;

namespace QIC.Sport.Odds.Collector.Ibc.TimeManager
{
    public class LiveTimeInfo
    {
        public string SrcMatchId;
        public int Phase = -1;
        public string PhaseStartUtc;

        public int LiveTime
        {
            get
            {
                if (string.IsNullOrEmpty(PhaseStartUtc) || Phase == 0) return 0;
                var liveTime = (CommonTools.ToUnixTimeSpan(DateTime.Now) - Convert.ToInt64(PhaseStartUtc)) / 60.0;
                return liveTime < 0 ? 0 : (int)liveTime;
            }
        }

        public override bool Equals(object obj)
        {
            LiveTimeInfo lti = obj as LiveTimeInfo;
            if (lti == null) return true;
            return Phase == lti.Phase && PhaseStartUtc == lti.PhaseStartUtc;
        }
    }
}
