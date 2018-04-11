using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using ML.Infrastructure.IOC;
using QIC.Sport.Odds.Collector.Cache.CacheManager;

namespace QIC.Sport.Odds.Collector.Ibc.TimeManager
{
    public class LiveTimeManager
    {
        private ILog logger = LogManager.GetLogger(typeof(LiveTimeManager));
        ConcurrentDictionary<string, LiveTimeInfo> dicLiveTimeInfo = new ConcurrentDictionary<string, LiveTimeInfo>();
        private static readonly MatchEntityManager matchEntityManager = IocUnity.GetService<IMatchEntityManager>("MatchEntityManager") as MatchEntityManager;

        private static readonly object lockObj = new object();
        private static LiveTimeManager instance;
        public static LiveTimeManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObj)
                    {
                        if (instance == null)
                        {
                            instance = new LiveTimeManager();
                            instance.Init();
                        }
                    }
                }
                return instance;
            }
        }

        public void AddOrUpdate(LiveTimeInfo liveTimeInfo)
        {
            dicLiveTimeInfo.AddOrUpdate(liveTimeInfo.SrcMatchId, liveTimeInfo, (k, v) =>
            {
                if (v.Equals(liveTimeInfo)) return v;
                v.Phase = liveTimeInfo.Phase;
                v.PhaseStartUtc = liveTimeInfo.PhaseStartUtc;
                return v;
            });
        }

        public void RemoveBySrcMatchId(string srcMatchId)
        {
            LiveTimeInfo lti;
            dicLiveTimeInfo.TryRemove(srcMatchId, out lti);
        }

        private void Init()
        {
            //  初始化启动每5秒计算LiveTime并发送变化到缓存去对比更新
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        Thread.Sleep(5 * 1000);
                        SendLiveTime();
                    }
                    catch (Exception e)
                    {
                        logger.Error(e.ToString());
                    }
                }
            });
        }
        private void SendLiveTime()
        {
            foreach (var timeInfo in dicLiveTimeInfo)
            {
                var me = matchEntityManager.Get(timeInfo.Key);
                if (me == null)
                {
                    logger.Error("LiveTimeManager SendLiveTime cannot find srcMatchId = " + timeInfo.Key);
                    continue;
                }
                else
                {
                    me.CompareToTime(timeInfo.Value.Phase, timeInfo.Value.LiveTime);
                }
            }
        }
    }
}
