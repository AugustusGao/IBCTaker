using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;
using QIC.Sport.Odds.Collector.Ibc.Dto;
using QIC.Sport.Odds.Collector.Ibc.Taker.WebSocket;
using QIC.Sport.Odds.Collector.Ibc.Taker.WebSocket.EventArgs;
using WebSocketSharp;
using CloseEventArgs = WebSocketSharp.CloseEventArgs;

namespace QIC.Sport.Odds.Collector.Ibc.Taker
{
    public class PushTaker : BaseWebSocketClient, IWebSocketClient
    {
        private readonly ILog logger = LogManager.GetLogger(typeof(PushTaker));
        private bool isClosed = false;
        private bool isConnected = false;
        private WebSocketSharp.WebSocket socket;

        public bool IsConnected { get { return isConnected; } }
        public void Init(SocketInitDto dto)
        {
            socket = new WebSocketSharp.WebSocket(dto.WssUrl);
            socket.Origin = dto.LocalUrl;
            socket.OnMessage += Socket_OnMessage;
            socket.OnError += Socket_OnError;
            socket.OnClose += Socket_OnClose;

        }

        private void Socket_OnClose(object sender, CloseEventArgs e)
        {
            try
            {
                isClosed = true;
                isConnected = false;
                //logger.Error("wsSocket_OnClose  ClientID = " + ReaditClientID + ", args = " + JsonConvert.SerializeObject(e));
                //OnClosed(new QIC.Sport.Signal.Collector.Bet365.WebSocket.EventParam.CloseEventArgs() { ClientID = ReaditClientID, Code = e.Code, Reason = e.Reason });
            }
            catch (Exception ex)
            {
                logger.Error("socket_OnClose args = " + JsonConvert.SerializeObject(e) + "\n" + ex.ToString());
            }
        }

        private void Socket_OnError(object sender, ErrorEventArgs e)
        {
            try
            {
                isClosed = true;
                isConnected = false;
                //logger.Error("wsSocket_OnClose  ClientID = " + ReaditClientID + ", args = " + JsonConvert.SerializeObject(e));
                //OnClosed(new QIC.Sport.Signal.Collector.Bet365.WebSocket.EventParam.CloseEventArgs() { ClientID = ReaditClientID, Code = e.Code, Reason = e.Reason });
            }
            catch (Exception ex)
            {
                logger.Error("socket_OnError args = " + JsonConvert.SerializeObject(e) + "\n" + ex.ToString());
            }
        }

        private void Socket_OnMessage(object sender, MessageEventArgs e)
        {
            logger.Debug(e.Data);

            OnMessageReceived(new MessageReceiveEventArgs() { Data = e.Data });
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public bool Connect()
        {
            socket.Connect();
            isConnected = socket.ReadyState == WebSocketState.Open;
            Task.Run(() => HeartCheck());
            return isConnected;
        }

        public void Subscribe(string topic)
        {
            socket.Send(topic);
        }

        public void UnSubscribe(string topic)
        {
            socket.Send(topic);
        }
        private void HeartCheck()
        {
            while (isConnected)
            {
                Thread.Sleep(25000);
                if (socket != null)
                    if (socket.ReadyState == WebSocketState.Open)
                        socket.Send("2");
            }
        }
    }
}
