using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QIC.Sport.Odds.Collector.Ibc.Dto
{
    public class SocketInitDto
    {
        public string Token;
        public string Rid;
        public string Id;
        public string LocalUrl;
        public string Cookie;
        public string WssUrl { get { return string.Format("wss://agnj3.maxbet.com/socket.io/?gid=&token={0}&id={1}&rid={2}&EIO=3&transport=websocket&sid=", Token, Id, Rid); } }
    }
}
