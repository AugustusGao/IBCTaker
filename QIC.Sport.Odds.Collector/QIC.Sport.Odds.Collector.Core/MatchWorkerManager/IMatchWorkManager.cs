using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QIC.Sport.Odds.Collector.Core.Handle;

namespace QIC.Sport.Odds.Collector.Core.MatchWorkerManager
{
    public interface IMatchWorkManager
    {
        void Start();
        void Stop();
        
    }
}
