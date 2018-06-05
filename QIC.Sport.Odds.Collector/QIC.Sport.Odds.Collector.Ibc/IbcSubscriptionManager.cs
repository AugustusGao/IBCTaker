using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using ML.EGP.Sport.Common.Enums;
using Newtonsoft.Json;
using QIC.Sport.Odds.Collector.Common;
using QIC.Sport.Odds.Collector.Core.SubscriptionManager;
using QIC.Sport.Odds.Collector.Ibc.Dto;
using QIC.Sport.Odds.Collector.Ibc.Param;
using QIC.Sport.Odds.Collector.Ibc.Taker;
using QIC.Sport.Odds.Collector.Ibc.Tools;

namespace QIC.Sport.Odds.Collector.Ibc
{
    public class IbcSubscriptionManager : BaseSubscriptionManager, ISubscriptionManager
    {
        private ILog logger = LogManager.GetLogger(typeof(IbcSubscriptionManager));
        Dictionary<string, string> dicRMark = new Dictionary<string, string>();    //  标记返回的数据对应的SocketParam.Id，参数需要传给下层处理队列

        private PullTaker pullTaker;
        private PushTaker pushTaker;

        private DateTime lastReceiveTime = DateTime.Now;    //  记录最新收到数据时间，超过5分钟没收到发送邮件通知
        private int sendEmailIntervals = 5 * 60;            //  5分钟
        private bool isSended = false;

        public void Init()
        {
            pullTaker = new PullTaker();
            Task.Run(() => TakerWork());
        }
        public void Subscribe(ISubscribeParam param)
        {
            DicSubscribeParams.TryAdd(param.Key, param);
        }

        public void UnSubscribe(ISubscribeParam param)
        {
            ISubscribeParam sp;
            if (DicSubscribeParams.TryRemove(param.Key, out sp))
            {
                var unTopic = "42[\"unsubscribe\",\"" + sp.Key + "\"]";
                pushTaker.UnSubscribe(unTopic);

                logger.Info("pushTaker UnSubscribe  topic = " + sp.Key + " Param = " + JsonConvert.SerializeObject(sp));
            }
        }

        public event DataReceivedEventHandler DataReceived;

        private void TakerWork()
        {
            while (true)
            {
                if ((DateTime.Now - lastReceiveTime).TotalSeconds > sendEmailIntervals && !isSended)
                {
                    EmailTools.SendQQEmail(new MailMessage() { Subject = "IBC Taker", Body = "More than 5 minutes cannot take data!!!" });
                    isSended = true;
                }
                foreach (var kv in DicSubscribeParams)
                {
                    try
                    {
                        var param = kv.Value as BaseParam;
                        if (param == null) continue;
                        if (param.TakeMode == TakeMode.Pull)
                        {
                            if (param.IsSubscribed) continue;

                            pullTaker.Available = false;
                            pullTaker.Init(kv.Value);
                            pullTaker.LoginTask();
                            param.IsSubscribed = true;
                        }
                        else
                        {
                            if (pushTaker == null)
                            {
                                ConnectPushTaker();
                                continue;
                            }
                            if (param.IsSubscribed || !pushTaker.IsConnected) continue;

                            var normalParam = param as NormalParam;
                            pushTaker.Subscribe(normalParam.Topic);
                            param.IsSubscribed = true;
                            logger.Info("Subscribe = " + normalParam.Topic);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex.ToString());
                        logger.Error(JsonConvert.SerializeObject(kv));
                    }
                }
                Thread.Sleep(100);
            }
        }

        private bool ConnectPushTaker(bool isNeedLogin = false)
        {
            return false;
            if (pushTaker != null && pushTaker.IsConnected) return true;

            if (pushTaker == null)
            {
                if (pullTaker.Status != TakerStatus.Started) return false;

                pushTaker = new PushTaker();
                pushTaker.Init(pullTaker.SocketInitDto);
                pushTaker.MessageReceived += PushTaker_MessageReceived;
                pushTaker.Closed += PushTaker_Closed;

                //  设置PullTaker强制关闭socket的委托方法，以便在LoginCheck出现错误的时候要关闭socket再重新连接
                pullTaker.ForceCloseSocket = () => { pushTaker.Close(); };
            }
            else
            {
                //  pushTaker存在，需要重新请求登录尝试重新连接Socket 
                pullTaker.Status = TakerStatus.Stoped;

                while (!pullTaker.AutoLogin()) { Thread.Sleep(5000); }

                pushTaker.Init(pullTaker.SocketInitDto);
            }
            logger.Info(" Start Connect Ibc ...");
            Console.WriteLine(" Start Connect Ibc ...");
            if (!pushTaker.Connect())
            {
                logger.Info(" Connect Ibc socket failed!");
                Console.WriteLine(" Connect Ibc socket failed!");
                return false;
            }
            logger.Info(" Connect Ibc socket success!");
            Console.WriteLine(" Connect Ibc socket success!");
            return true;
        }

        private void PushTaker_Closed(object sender, Taker.WebSocket.EventArgs.CloseEventArgs e)
        {
            Task.Run((() =>
            {
                try
                {
                    logger.Error("PushTaker_Closed CloseEventArgs = " + JsonConvert.SerializeObject(e));
                    foreach (var param in DicSubscribeParams.Values.Cast<BaseParam>())
                    {
                        if (param.TakeMode == TakeMode.Push) param.IsSubscribed = false;
                    }

                    dicRMark.Clear();
                    DataReceived(null, new DataReceiveEventArgs() { Data = new PushDataDto() { DataType = (int)DataType.Exception } });

                    // 重新登录获取参数再连接
                    logger.Info("Ibc reconnect...");
                    ConnectPushTaker();
                }
                catch (Exception ex)
                {
                    logger.Error("CloseEventArgs = " + JsonConvert.SerializeObject(e) + "\n" + e.ToString());
                }
            }));
        }

        private void PushTaker_MessageReceived(object sender, Taker.WebSocket.EventArgs.MessageReceiveEventArgs e)
        {
            lastReceiveTime = DateTime.Now;
            isSended = false;

            if (e == null) return;
            string thisTopic = string.Empty;
            if (string.IsNullOrEmpty(e.HeadInfo) || !e.HeadInfo.Contains("42")) return;

            bool isUpdate = false;
            string cid = null;
            if (e.HeadInfo.Contains("p"))
            {
                isUpdate = true;
                var rMark = e.RMark;
                if (dicRMark.ContainsKey(rMark))
                    cid = dicRMark[rMark];
                else
                    logger.Error("RMark not find  rMark = " + rMark);

            }
            else if (e.HeadInfo.Contains("r"))
            {
                cid = e.CId;
                if (dicRMark.ContainsKey(e.RMark))
                    dicRMark[e.RMark] = cid;
                else
                    dicRMark.Add(e.RMark, cid);
            }
            else
            {
                return;
            }

            if (string.IsNullOrEmpty(cid))
            {
                logger.Error("cid = null  ,headinfo = " + e.HeadInfo);
                return;
            }

            var data = new PushDataDto()
            {
                IsUpdate = isUpdate,
                Param = DicSubscribeParams[cid],
                Data = e.Data,
                DataType = (int)DataType.Normal
            };
            //logger.Info(JsonConvert.SerializeObject(data));
            DataReceived(null, new DataReceiveEventArgs() { Data = data });
        }
    }
}
