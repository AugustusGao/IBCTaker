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
    public class LiveInfoManager
    {
        private ILog logger = LogManager.GetLogger(typeof(LiveInfoManager));
        ConcurrentDictionary<string, LiveInfo> dicLiveTimeInfo = new ConcurrentDictionary<string, LiveInfo>();
        private static readonly MatchEntityManager matchEntityManager = IocUnity.GetService<IMatchEntityManager>("MatchEntityManager") as MatchEntityManager;

        private static readonly object lockObj = new object();
        private static LiveInfoManager instance;
        public static LiveInfoManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObj)
                    {
                        if (instance == null)
                        {
                            instance = new LiveInfoManager();
                            instance.Init();
                        }
                    }
                }
                return instance;
            }
        }

        public LiveInfo AddOrUpdate(LiveInfo liveTimeInfo)
        {
            LiveInfo ret = liveTimeInfo;
            dicLiveTimeInfo.AddOrUpdate(liveTimeInfo.SrcMatchId, liveTimeInfo, (k, v) =>
            {
                if (v.Equals(liveTimeInfo)) return v;
                v.Phase = liveTimeInfo.Phase ?? v.Phase;
                v.PhaseStartUtc = string.IsNullOrEmpty(liveTimeInfo.PhaseStartUtc) ? v.PhaseStartUtc : liveTimeInfo.PhaseStartUtc;
                v.HomeScore = liveTimeInfo.HomeScore ?? v.HomeScore;
                v.AwayScore = liveTimeInfo.AwayScore ?? v.AwayScore;
                v.HomeRed = liveTimeInfo.HomeRed ?? v.HomeRed;
                v.AwayRed = liveTimeInfo.AwayRed ?? v.AwayRed;
                ret = v;
                return v;
            });
            return ret;
        }

        public void RemoveBySrcMatchId(string srcMatchId)
        {
            LiveInfo lti;
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
                    logger.Error("LiveInfoManager SendLiveTime cannot find srcMatchId = " + timeInfo.Key);
                    continue;
                }
                else
                {
                    me.CompareToTime(timeInfo.Value.Phase.Value, timeInfo.Value.Phase == 0 ? 0 : timeInfo.Value.LiveTime);
                }
            }
        }
    }
}
