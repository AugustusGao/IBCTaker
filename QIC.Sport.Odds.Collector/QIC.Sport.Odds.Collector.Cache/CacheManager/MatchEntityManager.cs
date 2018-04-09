
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using ML.EGP.Sport.CommandProtocol.Dto.TakeServer;
using ML.Infrastructure.Config;
using Newtonsoft.Json;
using QIC.Sport.Odds.Collector.Cache.CacheEntity;

namespace QIC.Sport.Odds.Collector.Cache.CacheManager
{
    public class MatchEntityManager : IMatchEntityManager
    {
        private readonly ILog logger = LogManager.GetLogger(typeof(MatchEntityManager));
        private int takeType = ConfigSingleton.CreateInstance().GetAppConfig<int>("CollectorType");
        private readonly ConcurrentDictionary<string, MatchEntity> matchDic = new ConcurrentDictionary<string, MatchEntity>();
        private readonly ConcurrentDictionary<int, string> matchIDAndSrcMatchID = new ConcurrentDictionary<int, string>();

        /// <summary>
        /// 添加或者获取比赛对象
        /// </summary>
        /// <param name="srcMatchID"></param>
        /// <param name="sportID"></param>
        /// <param name="matchID"></param>
        /// <param name="iR"></param>
        /// <returns></returns>
        //public MatchEntity AddOrGet(string srcMatchID, int sportID, int matchID)
        //{
        //    MatchEntity me = null;
        //    matchDic.TryGetValue(srcMatchID, out me);
        //    if (me == null)
        //    {
        //        me = new MatchEntity()
        //        {
        //            SrcMatchID = srcMatchID,
        //            MatchID = matchID,
        //            SportID = sportID,
        //        };
        //        matchDic.TryAdd(me.SrcMatchID, me);
        //    }
        //    else
        //    {
        //        me.MatchID = matchID;
        //    }
        //    return me;
        //}
        public MatchEntity GetOrAdd(string srcMatchID, string srcLeague, string srcHome, string srcAway, DateTime srcMatchDate, int sportID)
        {
            srcLeague = srcLeague.Trim().ToUpper();
            srcHome = srcHome.Trim();
            srcAway = srcAway.Trim();
            if (string.IsNullOrEmpty(srcLeague) || string.IsNullOrEmpty(srcHome) || string.IsNullOrEmpty(srcAway)) return null;

            MatchEntity dto = matchDic.GetOrAdd(srcMatchID, new MatchEntity() { SrcMatchID = srcMatchID, SportID = sportID });
            bool isMatchDateChanged;
            bool isNewMatch = CheckMatch(srcMatchID, srcLeague, srcHome, srcAway, srcMatchDate, dto, out isMatchDateChanged);
            if (isNewMatch) SendSrcMatchInfo(dto);
            else if (isMatchDateChanged) SendSrcMatchDate(dto);
            return dto;
        }
        public void MatchLink(string srcMatchID, int matchID)
        {
            MatchEntity dto;
            matchDic.TryGetValue(srcMatchID, out dto);
            if (dto != null)
            {
                dto.MatchID = matchID;

                if (matchID > 0)
                {
                    if (!matchIDAndSrcMatchID.ContainsKey(dto.MatchID))
                    {
                        matchIDAndSrcMatchID.TryAdd(dto.MatchID, dto.SrcMatchID);
                    }
                    else
                    {
                        matchIDAndSrcMatchID[dto.MatchID] = dto.SrcMatchID;
                    }
                }
                else
                {
                    var kv = matchIDAndSrcMatchID.FirstOrDefault(o => o.Value == dto.SrcMatchID);
                    var s = "";
                    matchIDAndSrcMatchID.TryRemove(kv.Key, out s);

                }
            }
            else
            {
                logger.Debug("_cacheMatchLink connot find SrcMatch for link ,srcMatchID= " + srcMatchID + ",matchID = " + matchID + "");
                logger.Debug(string.Join(",", matchDic.Keys.ToList()));
            }
        }
        public MatchEntity Get(string srcMatchID)
        {
            MatchEntity me = null;
            matchDic.TryGetValue(srcMatchID, out me);
            return me;
        }
        public int GetMatchID(string srcMatchID)
        {
            MatchEntity dto;
            matchDic.TryGetValue(srcMatchID, out dto);
            if (dto != null) return dto.MatchID;
            return 0;
        }
        public string GetSrcMatchID(int MatchID)
        {
            string id;
            matchIDAndSrcMatchID.TryGetValue(MatchID, out id);
            return id;
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

        private static bool CheckMatch(string srcMatchID, string srcLeague, string srcHome, string srcAway, DateTime srcMatchDate, MatchEntity dto, out bool isMatchDateChanged)
        {
            bool isNewMatch = false;
            isMatchDateChanged = false;
            if (dto.SrcLeague != srcLeague)
            {
                dto.SrcLeague = srcLeague;
                isNewMatch = true;
            }
            if (dto.SrcHome != srcHome)
            {
                dto.SrcHome = srcHome;
                isNewMatch = true;
            }
            if (dto.SrcAway != srcAway)
            {
                dto.SrcAway = srcAway;
                isNewMatch = true;
            }
            if (dto.SrcMatchDate != srcMatchDate)
            {
                //  比赛名称信息没变化，而时间相差超过24小时，说明是其他日期的比赛，当作新的比赛添加，否则则可能只是比赛的时间变化更新
                if (!isNewMatch)
                {
                    if (Math.Abs((srcMatchDate - dto.SrcMatchDate).TotalHours) > 24 && ((string.IsNullOrEmpty(srcMatchID) || srcMatchID != dto.SrcMatchID)))
                        isNewMatch = true;
                    else isMatchDateChanged = true;
                }
                dto.SrcMatchDate = srcMatchDate;
            }
            return isNewMatch;
        }
        private void SendSrcMatchInfo(MatchEntity dto)
        {
            //生成传输对象
            TakeSrcMatchInfoDto cmdDto = new TakeSrcMatchInfoDto
            {
                SportID = dto.SportID,
                SrcMatchID = dto.SrcMatchID,
                SrcHome = dto.SrcHome,
                SrcAway = dto.SrcAway,
                SrcLeague = dto.SrcLeague,
                SrcMatchDate = dto.SrcMatchDate,
                TakeType = takeType

            };
            LogManager.GetLogger("TestSend").Info(JsonConvert.SerializeObject(cmdDto));
            //iR.Send(TakeServerCommand.SrcMatchInfo, cmdDto);
        }
        private void SendSrcMatchDate(MatchEntity dto)
        {
            var str = JsonConvert.SerializeObject(new TakeSrcMatchDateDto()
            {
                SrcMatchID = dto.SrcMatchID,
                SrcMatchDate = dto.SrcMatchDate,
                TakeType = takeType,
                MatchID = dto.MatchID
            });
            LogManager.GetLogger("TestSend").Info(str);
            //iR.Send(TakeServerCommand.SrcMatchDate, new TakeSrcMatchDateDto() { SrcMatchID = dto.SrcMatchID, SrcMatchDate = dto.SrcMatchDate, TakeType = takeType, MatchID = dto.MatchID });
        }
    }
}
