using ML.NetComponent.Tcp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QIC.Sport.Odds.Collector.Ibc.Taker.WebSocket.EventArgs
{
    public class MessageReceiveEventArgs
    {
        private string data;

        public string HeadInfo
        {
            get
            {
                var index = data.LastIndexOf('[');
                if (index <= 0) return null;
                return data.Substring(0, index);
            }
        }

        public string RMark
        {
            get
            {
                var index = HeadInfo.LastIndexOf(",[");
                var str = HeadInfo.Substring(index + 1).Trim('[').Trim(',');
                var arr = str.Split(',');
                return arr.Length == 2 ? arr[1].Trim('"') : str.Trim('"');
            }
        }

        public string CId
        {
            get
            {
                var index = HeadInfo.LastIndexOf(",[");
                var str = HeadInfo.Substring(index + 1).Trim('[').Trim(',');
                var arr = str.Split(',');
                return arr.Length == 2 ? arr[0].Trim('"') : "";
            }
        }
        public string Data
        {
            get { return data; }
            set { data = value; }
        }
    }
}
