using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QIC.Sport.Odds.Collector.Core.SubscriptionManager
{
    public class BaseSubscriptionManager
    {
        protected ConcurrentDictionary<string, ISubscribeParam> DicSubscribeParams = new ConcurrentDictionary<string, ISubscribeParam>();
    }
}
