using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QIC.Sport.Odds.Collector.Core.MatchWorkerManager;

namespace QIC.Sport.Odds.Collector.Ibc.Dto
{
    public class PullDataDto : ITakeData
    {
        public int SportID { get; set; }
        public int DataType { get; set; }
    }
}
