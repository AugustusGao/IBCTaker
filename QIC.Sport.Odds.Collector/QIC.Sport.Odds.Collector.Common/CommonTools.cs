using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QIC.Sport.Odds.Collector.Common
{
    public static class CommonTools
    {
        public static long ToUnixTimeSpan(DateTime date)
        {
            var startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1)); // 当地时区
            return (long)(date - startTime).TotalSeconds; // 相差秒数
        }
    }
}
