using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QIC.Sport.Odds.Collector.Ibc.Taker.WebSocket.EventArgs;

namespace QIC.Sport.Odds.Collector.Ibc.Taker.WebSocket
{
    public class BaseWebSocketClient
    {
        public delegate void HandShakedEventHandler(Object sender, HandShakeEventArgs e);
        public event HandShakedEventHandler HandShaked;
        protected void OnHandShaked(HandShakeEventArgs e)
        {
            if (HandShaked != null) HandShaked(this, e);
        }
        public delegate void MessageReceivedEventHandler(Object sender, MessageReceiveEventArgs e);
        public event MessageReceivedEventHandler MessageReceived;
        protected void OnMessageReceived(MessageReceiveEventArgs e)
        {
            if (MessageReceived != null) MessageReceived(this, e);
        }

        public delegate void CloseEventHandler(Object sender, CloseEventArgs e);
        public event CloseEventHandler Closed;
        protected void OnClosed(CloseEventArgs e)
        {
            if (Closed != null) Closed(this, e);
        }

        public delegate void NoResponseEventHandler(Object sender, System.EventArgs e);
        public event NoResponseEventHandler NoResponsed;

        protected void OnNoResponsed(System.EventArgs e)
        {
            if (NoResponsed != null) NoResponsed(this, e);
        }
    }
}
