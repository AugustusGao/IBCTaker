
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using QIC.Sport.Odds.Collector.Cache.CacheEntity;

namespace QIC.Sport.Odds.Collector.Cache.CacheManager
{
    public class MatchEntityManager : IMatchEntityManager
    {
        private ConcurrentDictionary<string, MatchEntity> matchDic = new ConcurrentDictionary<string, MatchEntity>();
        private ILog _logger = LogManager.GetLogger(typeof(MatchEntityManager));
        /// <summary>
        /// 添加或者获取比赛对象
        /// </summary>
        /// <param name="srcMatchID"></param>
        /// <param name="sportID"></param>
        /// <param name="matchID"></param>
        /// <param name="iR"></param>
        /// <returns></returns>
        public MatchEntity AddOrGet(string srcMatchID, int sportID, int matchID)
        {
            MatchEntity me = null;
            matchDic.TryGetValue(srcMatchID, out me);
            if (me == null)
            {
                me = new MatchEntity()
                {
                    SrcMatchID = srcMatchID,
                    MatchID = matchID,
                    SportID = sportID,
                };
                matchDic.TryAdd(me.SrcMatchID, me);
            }
            else
            {
                me.MatchID = matchID;
            }
            return me;
        }

        public MatchEntity Get(string srcMatchID)
        {
            MatchEntity me = null;
            matchDic.TryGetValue(srcMatchID, out me);
            return me;
        }

        public bool MatchExist(string srcMatchId)
        {
            return matchDic.ContainsKey(srcMatchId);
        }
        public void ForEach(Action<MatchEntity> action)
        {
            foreach (var mkv in matchDic)
            {
                action(mkv.Value);
            }
        }

        public void Link(string srcMatchID, int matchID)
        {
            MatchEntity me = null;
            matchDic.TryGetValue(srcMatchID, out me);
            if (me != null) me.MatchID = matchID;
            else
            {
                _logger.Debug("matchDic connot find SrcMatch for link ,srcMatchID= " + srcMatchID + ",matchID = " + matchID + "");
                _logger.Debug(string.Join(",", matchDic.Keys.ToList()));
            }
        }
        public void Reset(string srcMatchId)
        {
            MatchEntity me = null;
            matchDic.TryGetValue(srcMatchId, out me);

            if (me != null && me.MatchID > 0)
            {
                me.SendAll();
            }
        }
        /// <summary>
        /// 抓水工作组全部账号拿不到数据（异常）
        /// </summary>
        //public void TakerGroupDown(List<PageDto> dtoList)
        //{
        //    foreach (var mkv in matchDic)
        //    {
        //        var match = mkv.Value;
        //        foreach (var dto in dtoList)
        //        {
        //            match.MatchDisappear(dto.Stage, dto.LimitMarket, dto.LimitMatchInfo);
        //        }
        //    }
        //}

        //public void OnePageMatchDisappear(PageDto dto)
        //{
        //    matchDic.Values.ForEach(o => o.MatchDisappear(dto));
        //}
    }
}
