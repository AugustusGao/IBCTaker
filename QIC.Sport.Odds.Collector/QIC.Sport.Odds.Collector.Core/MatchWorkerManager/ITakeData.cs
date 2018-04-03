using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QIC.Sport.Odds.Collector.Core.MatchWorkerManager
{
    public interface ITakeData
    {
        int SportID { get; set; }
        int DataType { get; set; }
    }
}
