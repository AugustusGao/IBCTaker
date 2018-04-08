using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using ML.Infrastructure.Config;
using ML.Infrastructure.IOC;
using QIC.Sport.Inplay.Collector.Core;
using QIC.Sport.Odds.Collector.Cache.CacheManager;
using QIC.Sport.Odds.Collector.Core.MatchWorkerManager;

namespace QIC.Sport.Odds.Collector.Core
{
    public class Reptile : IReptile
    {
        private ILog logger = LogManager.GetLogger(typeof(Reptile));
        private static readonly object lockObj = new object();
        private readonly int collectorType = ConfigSingleton.CreateInstance().GetAppConfig<int>("CollectorType");
        private readonly string ip = ConfigSingleton.CreateInstance().GetAppConfig<string>("CentralServerIP");
        private readonly int port = ConfigSingleton.CreateInstance().GetAppConfig<int>("CentralServerPort");
        private IMatchWorkManager matchWorkManager;
        private IMatchEntityManager matchEntityManager;
        private static Reptile instance;

        public static IReptile Instance()
        {

            if (instance == null)
            {
                lock (lockObj)
                {
                    if (instance == null)
                    {
                        instance = new Reptile();
                    }
                }
            }
            return instance;
        }
        private Reptile()
        {
            matchEntityManager = new MatchEntityManager();
            IocUnity.RegisterInstance("MatchEntityManager", matchEntityManager);

        }
        public void Start()
        {
            matchWorkManager.Start();
        }

        public void Stop()
        {
            matchWorkManager.Stop();
        }

        public void Init(IMatchWorkManager workManager)
        {
            matchWorkManager = workManager;
        }
    }
}
