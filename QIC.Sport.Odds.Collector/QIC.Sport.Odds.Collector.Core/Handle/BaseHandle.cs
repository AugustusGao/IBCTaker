using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ML.Infrastructure.IOC;
using QIC.Sport.Odds.Collector.Core.MatchWorkerManager;
using QIC.Sport.Odds.Collector.Core.SubscriptionManager;

namespace QIC.Sport.Odds.Collector.Core.Handle
{
    public class BaseHandle
    {
        protected readonly ISubscriptionManager SubscriptionManager = IocUnity.GetService<ISubscriptionManager>("SubscriptionManager");

    }
}
