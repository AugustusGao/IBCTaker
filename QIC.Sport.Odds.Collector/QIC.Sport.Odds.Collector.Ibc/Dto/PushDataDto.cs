using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QIC.Sport.Odds.Collector.Core.MatchWorkerManager;
using QIC.Sport.Odds.Collector.Core.SubscriptionManager;

namespace QIC.Sport.Odds.Collector.Ibc.Dto
{
    public class PushDataDto : ITakeData
    {
        public int SportID { get; set; }
        public int DataType { get; set; }
        public ISubscribeParam Param { get; set; }
        public string Data { get; set; }
        public bool IsUpdate { get; set; }
    }
}
