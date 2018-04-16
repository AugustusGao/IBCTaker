using System;
using System.Collections.Generic;
using System.Linq;
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

namespace QIC.Sport.Odds.Collector.Ibc
{
    public class IbcSubscriptionManager : BaseSubscriptionManager, ISubscriptionManager
    {
        private ILog logger = LogManager.GetLogger(typeof(IbcSubscriptionManager));
        Dictionary<string, string> dicRMark = new Dictionary<string, string>();    //  标记返回的数据对应的SocketParam.Id，参数需要传给下层处理队列

        private PullTaker pullTaker;
        private PushTaker pushTaker;

        public void Init()
        {
            pullTaker = new PullTaker();
            Task.Run(() => InitPushTaker());
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
                var np = sp as NormalParam;
                pushTaker.UnSubscribe(np.Topic);

                logger.Info("pushTaker UnSubscribe  topic = " + np.Topic + " Param = " + JsonConvert.SerializeObject(sp));
            }
        }

        public event DataReceivedEventHandler DataReceived;

        private void InitPushTaker()
        {
            //  检查PullTaker初始化成功有参数后将生成MatchMenu的订阅参数加入订阅
            while (pullTaker.Status != TakerStatus.Started || pullTaker.SocketInitDto == null)
            {
                Thread.Sleep(1000);
            }

            SocketParam sp = new SocketParam()
            {
                id = "c13",
                rev = 0,
                condition = new Condition()
                {
                    marketid = "L",
                    sporttype = new[] { 1 },
                    bettype = new[] { 1, 3, 5, 7, 8, 15 },
                    sorting = "n"
                }
            };

            //Subscribe(new NormalParam() { Stage = (int)MatchStageEnum.Live, TakeMode = TakeMode.Push, SocketParam = sp });
            GetAllParam().ForEach(Subscribe);
        }

        private List<NormalParam> GetAllParam()
        {
            List<NormalParam> list = new List<NormalParam>();
            SocketParam sp = new SocketParam()
            {
                id = "c13",
                rev = 0,
                condition = new Condition()
                {
                    marketid = "L",
                    sporttype = new[] { 1 },
                    bettype = new[] { 1, 3, 5, 7, 8, 15 },
                    sorting = "n"
                }
            };
            list.Add(new NormalParam() { Stage = (int)MatchStageEnum.Live, TakeMode = TakeMode.Push, SocketParam = sp });

            sp = new SocketParam()
            {
                id = "c12",
                rev = 0,
                condition = new Condition()
                {
                    marketid = "T",
                    sporttype = new[] { 1 },
                    bettype = new[] { 1, 3, 5, 7, 8, 15 },
                    sorting = "n"
                }
            };
            list.Add(new NormalParam() { Stage = (int)MatchStageEnum.Today, TakeMode = TakeMode.Push, SocketParam = sp });

            sp = new SocketParam()
            {
                id = "c11",
                rev = 0,
                condition = new Condition()
                {
                    marketid = "E",
                    sporttype = new[] { 1 },
                    bettype = new[] { 1, 3, 5, 7, 8, 15 },
                    sorting = "n"
                }
            };
            //list.Add(new NormalParam() { Stage = (int)MatchStageEnum.Early, TakeMode = TakeMode.Push, SocketParam = sp });
            return list;
        }
        private void TakerWork()
        {
            while (true)
            {
                foreach (var kv in DicSubscribeParams)
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
                Thread.Sleep(100);
            }
        }

        private bool ConnectPushTaker()
        {
            if (pushTaker != null && pushTaker.IsConnected) return true;
            pushTaker = new PushTaker();
            pushTaker.Init(pullTaker.SocketInitDto);
            pushTaker.MessageReceived += PushTaker_MessageReceived;
            pushTaker.Closed += PushTaker_Closed;
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
            try
            {
                logger.Error("PushTaker_Closed CloseEventArgs = " + JsonConvert.SerializeObject(e));
                foreach (var param in DicSubscribeParams.Values.Cast<BaseParam>())
                {
                    if (param.TakeMode == TakeMode.Push) param.IsSubscribed = false;
                }
                // 重新登录获取参数再连接
                logger.Info("Ibc reconnect...");
                ConnectPushTaker();
            }
            catch (Exception ex)
            {
                logger.Error("CloseEventArgs = " + JsonConvert.SerializeObject(e) + "\n" + e.ToString());
            }
        }

        private void PushTaker_MessageReceived(object sender, Taker.WebSocket.EventArgs.MessageReceiveEventArgs e)
        {
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
                var kv = dicRMark.FirstOrDefault(o => o.Value.Contains(cid));
                if (string.IsNullOrEmpty(kv.Value))
                {
                    dicRMark.Add(e.RMark, cid);
                }
                else
                {
                    dicRMark[kv.Key] = cid;
                }
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
