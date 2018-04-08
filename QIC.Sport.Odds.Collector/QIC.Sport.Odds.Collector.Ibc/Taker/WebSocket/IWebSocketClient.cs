using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QIC.Sport.Odds.Collector.Ibc.Dto;

namespace QIC.Sport.Odds.Collector.Ibc.Taker.WebSocket
{
    public interface IWebSocketClient
    {
        void Init(SocketInitDto dto);
        void Close();
        bool Connect();
        void Subscribe(string topic);
        void UnSubscribe(String topic);

        /// <summary>
        /// 连接成功事件
        /// </summary>
        event BaseWebSocketClient.HandShakedEventHandler HandShaked;

        /// <summary>
        /// 接收数据的事件
        /// </summary>
        event BaseWebSocketClient.MessageReceivedEventHandler MessageReceived;
        /// <summary>
        /// 连接断开事件
        /// </summary>
        event BaseWebSocketClient.CloseEventHandler Closed;
    }
}
