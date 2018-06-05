using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using ML.Infrastructure.Config;
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
        private bool isClosed = false;
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

        }

        public void Stop()
        {
            isClosed = true;
        }
        private void manager_DataReceived(object sender, DataReceiveEventArgs e)
        {
            dataQueue.Enqueue(e.Data);
        }
        private void DataProcess()
        {
            while (!isClosed)
            {
                try
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
                    GroupState gs = new GroupState() { GroupID = np == null ? "1" : np.Stage + "" };
                    factory.StartNew((s) =>
                    {
                        AssignHandleProcess(data);
                    }, gs);
                }
                catch (Exception e)
                {
                    logger.Error(e.ToString());
                }
            }
        }

        private void AssignHandleProcess(ITakeData data)
        {
            try
            {
                var handle = HandleFactory.CreateHandle(data.DataType);
                if (handle == null)
                {
                    logger.Error("Cannot create handle , DataType = " + data.DataType);
                    return;
                }
                handle.ProcessData(data);
            }
            catch (Exception e)
            {
                logger.Error(e.ToString());
            }
        }
        public class GroupState : IGroupFlag
        {
            public string GroupID { get; set; }
        }
    }
}
