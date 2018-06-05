using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QIC.Sport.Odds.Collector.Core.MatchWorkerManager;

namespace QIC.Sport.Inplay.Collector.Core
{
    public interface IReptile
    {
        void Start();
        void Stop();
        void Init(IMatchWorkManager workManager, string ip, string port);
    }
}
