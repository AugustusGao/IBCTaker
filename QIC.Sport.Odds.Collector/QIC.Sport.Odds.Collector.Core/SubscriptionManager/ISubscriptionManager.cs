using QIC.Sport.Odds.Collector.Core.MatchWorkerManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QIC.Sport.Odds.Collector.Core.SubscriptionManager
{
    public interface ISubscriptionManager
    {
        void Subscribe(ISubscribeParam param);
        void UnSubscribe(ISubscribeParam param);
        event DataReceivedEventHandler DataReceived;
    }

    public delegate void DataReceivedEventHandler(object sender, DataReceiveEventArgs e);

}
