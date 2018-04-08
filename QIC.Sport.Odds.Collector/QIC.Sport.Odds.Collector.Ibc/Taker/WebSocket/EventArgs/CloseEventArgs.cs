using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QIC.Sport.Odds.Collector.Ibc.Taker.WebSocket.EventArgs
{
    public class CloseEventArgs : System.EventArgs
    {
        public string ClientID { get; set; }
        public ushort Code { get; set; }
        public string Reason { get; set; }
    }
}
