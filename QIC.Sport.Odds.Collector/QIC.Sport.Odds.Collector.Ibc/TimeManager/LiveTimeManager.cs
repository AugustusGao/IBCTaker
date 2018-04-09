using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ML.Infrastructure.IOC;
using QIC.Sport.Odds.Collector.Cache.CacheManager;

namespace QIC.Sport.Odds.Collector.Ibc.TimeManager
{
    public class LiveTimeManager
    {
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

        public void Init()
        {
            //  初始化启动每10秒计算LiveTime并发送变化到缓存去对比更新

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
    }
}
