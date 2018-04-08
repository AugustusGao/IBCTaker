using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using ML.Infrastructure.IOC;
using QIC.Sport.Odds.Collector.Common;
using QIC.Sport.Odds.Collector.Core.MatchWorkerManager;
using QIC.Sport.Odds.Collector.Core.SubscriptionManager;
using QIC.Sport.Odds.Collector.Ibc.Dto;
using QIC.Sport.Odds.Collector.Ibc.Handle;
using QIC.Sport.Odds.Collector.Ibc.OddsManager;
using QIC.Sport.Odds.Collector.Ibc.Param;

namespace QIC.Sport.Odds.Collector.Ibc
{
    public class IbcWorkManager : BaseMatchWorkManager, IMatchWorkManager
    {
        private ILog logger = LogManager.GetLogger(typeof(IbcWorkManager));
        private GroupLimitedConcurrencyLevelTaskScheduler lcts;
        private TaskFactory factory;
        private KeepOddsManager keepOddsManager = KeepOddsManager.Instance;

        public void Init()
        {
            HandleFactory = new HandleFactory();

            processDataWork = new Thread(DataProcess);
            processDataWork.Start();

            var manager = new IbcSubscriptionManager();
            manager.Init();
            manager.DataReceived += manager_DataReceived;
            IocUnity.RegisterInstance<ISubscriptionManager>("SubscriptionManager", manager);

            SubscriptionManager = manager;

            lcts = new GroupLimitedConcurrencyLevelTaskScheduler(10);
            factory = new TaskFactory(lcts);
        }
        public void Start()
        {
            //  todo 添加登录任务
            var loginParam = new LoginParam()
            {
                Username = "S799H9900130",
                Password = "aaaa1111",
                TakeMode = TakeMode.Pull
            };
            SubscriptionManager.Subscribe(loginParam);
        }

        public void Stop()
        {

        }
        private void manager_DataReceived(object sender, DataReceiveEventArgs e)
        {
            dataQueue.Enqueue(e.Data);
        }
        private void DataProcess()
        {
            while (true)
            {
                if (dataQueue.IsEmpty)
                {
                    Thread.Sleep(100);
                    continue;
                }

                ITakeData data;
                if (!dataQueue.TryDequeue(out data) || data == null) continue;

                var pd = data as PushDataDto;
                var np = pd.Param as NormalParam;
                GroupState gs = new GroupState() { GroupID = np.Stage + "" };
                factory.StartNew((s) =>
                {
                    AssignHandleProcess(data);
                }, gs);
            }
        }

        private void AssignHandleProcess(ITakeData data)
        {
            var handle = HandleFactory.CreateHandle(data.DataType);
            if (handle == null)
            {
                logger.Error("Cannot create handle , DataType = " + data.DataType);
                return;
            }
            handle.ProcessData(data);
        }
        public class GroupState : IGroupFlag
        {
            public string GroupID { get; set; }
        }
    }
}
