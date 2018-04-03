using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QIC.Sport.Odds.Collector.Core.Handle;
using QIC.Sport.Odds.Collector.Core.SubscriptionManager;

namespace QIC.Sport.Odds.Collector.Core.MatchWorkerManager
{
    public class BaseMatchWorkManager
    {
        protected ISubscriptionManager SubscriptionManager;
        protected IHandleFactory HandleFactory;

        protected ConcurrentQueue<ITakeData> dataQueue = new ConcurrentQueue<ITakeData>();
        protected Thread processDataWork;
        public ConcurrentQueue<ITakeData> DataQueueForTest { get { return dataQueue; } }
    }
}
