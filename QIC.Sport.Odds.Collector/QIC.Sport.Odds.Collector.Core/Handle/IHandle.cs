using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QIC.Sport.Odds.Collector.Core.MatchWorkerManager;

namespace QIC.Sport.Odds.Collector.Core.Handle
{
    public interface IHandle
    {
        void ProcessData(ITakeData data);
    }
}
