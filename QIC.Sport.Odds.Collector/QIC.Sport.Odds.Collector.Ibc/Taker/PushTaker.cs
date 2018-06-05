using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using log4net;
using ML.Infrastructure.Config;
using ML.NetComponent.Http;
using Newtonsoft.Json;
using QIC.Sport.Odds.Collector.Ibc.Dto;
using QIC.Sport.Odds.Collector.Ibc.Taker.WebSocket;
using QIC.Sport.Odds.Collector.Ibc.Taker.WebSocket.EventArgs;
using WebSocketSharp;
using WebSocketSharp.Net;
using CloseEventArgs = WebSocketSharp.CloseEventArgs;
using Timer = System.Timers.Timer;

namespace QIC.Sport.Odds.Collector.Ibc.Taker
{
    public class PushTaker : BaseWebSocketClient, IWebSocketClient
    {
        private readonly ILog logger = LogManager.GetLogger(typeof(PushTaker));
        private bool isClosed = false;
        private bool isConnected = false;
        private WebSocketSharp.WebSocket socket;
        private Timer checkTimer;
        private int timeOut = ConfigSingleton.CreateInstance().GetAppConfig<int>("WebSocketWaitTime", 10);

        public bool IsConnected { get { return isConnected; } }
        public void Init(SocketInitDto dto)
        {
            socket = new WebSocketSharp.WebSocket(dto.WssUrl);
            socket.WaitTime = new TimeSpan(0, 0, 0, timeOut);
            socket.Origin = dto.LocalUrl;
            socket.OnMessage += Socket_OnMessage;
            socket.OnError += Socket_OnError;
            socket.OnClose += Socket_OnClose;

            checkTimer = new Timer();
            checkTimer.Interval = 25 * 1000;
            checkTimer.AutoReset = true;
            checkTimer.Elapsed += HeartCheck;
        }

        private void Socket_OnClose(object sender, CloseEventArgs e)
        {
            try
            {
                isClosed = true;
                isConnected = false;
                checkTimer.Stop();
                logger.Error("Socket_OnClose, CloseEventArgs = " + JsonConvert.SerializeObject(e));
                OnClosed(new WebSocket.EventArgs.CloseEventArgs() { Code = e.Code, Reason = e.Reason });
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
                logger.Error("Socket_OnError, ErrorEventArgs = " + JsonConvert.SerializeObject(e));
                socket.Close(CloseStatusCode.Normal);   //  触发OnClose事件
            }
            catch (Exception ex)
            {
                logger.Error("socket_OnError args = " + JsonConvert.SerializeObject(e) + "\n" + ex.ToString());
            }
        }

        private void Socket_OnMessage(object sender, MessageEventArgs e)
        {
            logger.Debug(e.Data);
            if (e.Data.Contains("Not authorized") || e.Data.Contains("disconnect") || e.Data.Contains("error") || e.Data.Contains("verif") || e.Data.Contains("failed"))
            {
                logger.Error("Socket_OnMessage, ErrorEventArgs = " + e.Data);
                socket.Close(CloseStatusCode.Normal);   //  触发OnClose事件
                return;
            }
            OnMessageReceived(new MessageReceiveEventArgs() { Data = e.Data });
        }

        public void Close()
        {
            socket.Close(CloseStatusCode.Normal);
        }

        public bool Connect()
        {
            socket.Connect();
            isConnected = socket.ReadyState == WebSocketState.Open;
            checkTimer.Start();

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
        private void HeartCheck(object sender, ElapsedEventArgs e)
        {
            if (socket != null)
                if (socket.ReadyState == WebSocketState.Open)
                    socket.Send("2");
        }
    }
}
