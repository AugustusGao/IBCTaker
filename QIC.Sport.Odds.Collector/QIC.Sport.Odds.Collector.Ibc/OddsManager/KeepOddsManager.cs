using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QIC.Sport.Odds.Collector.Ibc.OddsManager
{
    public class KeepOddsManager
    {
        ConcurrentDictionary<int, KeepOdds> dicKeepOdds = new ConcurrentDictionary<int, KeepOdds>();

        private static readonly KeepOddsManager instance = new KeepOddsManager();
        public static KeepOddsManager Instance
        {
            get
            {
                return instance;
            }
        }
        public KeepOdds AddOrGetKeepOdds(int stage)
        {
            KeepOdds kp;
            if (!dicKeepOdds.TryGetValue(stage, out kp))
            {
                kp = new KeepOdds();
                dicKeepOdds.TryAdd(stage, kp);
            }

            return kp;
        }
    }
}
