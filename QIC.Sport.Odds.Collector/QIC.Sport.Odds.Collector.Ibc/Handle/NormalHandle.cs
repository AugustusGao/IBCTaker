using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using ML.EGP.Sport.Common.Enums;
using ML.Infrastructure.IOC;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QIC.Sport.Odds.Collector.Cache.CacheEntity;
using QIC.Sport.Odds.Collector.Cache.CacheManager;
using QIC.Sport.Odds.Collector.Core.Handle;
using QIC.Sport.Odds.Collector.Core.MatchWorkerManager;
using QIC.Sport.Odds.Collector.Core.SubscriptionManager;
using QIC.Sport.Odds.Collector.Ibc.Dto;
using QIC.Sport.Odds.Collector.Ibc.OddsManager;
using QIC.Sport.Odds.Collector.Ibc.Param;
using QIC.Sport.Odds.Collector.Ibc.TimeManager;
using QIC.Sport.Odds.Collector.Ibc.Tools;

namespace QIC.Sport.Odds.Collector.Ibc.Handle
{
    public class NormalHandle : IHandle
    {
        private static ILog logger = LogManager.GetLogger(typeof(NormalHandle));
        private static readonly MatchEntityManager matchEntityManager = IocUnity.GetService<IMatchEntityManager>("MatchEntityManager") as MatchEntityManager;

        public void ProcessData(ITakeData data)
        {
            var pd = data as PushDataDto;
            var pm = pd.Param as NormalParam;
            var jArray = FormatToJArray(pd.Data);

            MatchEntity currentMatchEntity = null;
            bool isNext = false;
            foreach (var item in jArray)
            {
                if (item.ToString().Contains("type"))
                {
                    switch (item["type"].ToString())
                    {
                        //如果type是m，包含的match对象
                        case "m":
                            //  发送前一次已经统计完的比赛盘口,再进行新的比赛统计
                            if (currentMatchEntity != null)
                            {
                                MatchCompareRowNumAndMarket(currentMatchEntity, pm);
                            }
                            currentMatchEntity = DealMatchInfo(item, pm);
                            break;
                        //如果type是o，包含的odds对象
                        case "o": DealOddsInfo(item, pm); break;
                        //如果type是dm,说明该场比赛结束了，可以删除了
                        case "dm": DealDmInfo(item, pm); break;
                        //如果type是do，说明该盘口关闭了
                        case "do": DealDoInfo(item, pm); break;
                        //其他类暂不处理
                        default: break;
                    }
                }
                else
                {
                    //不包含就说明是其他信息，再做处理
                }
            }

            //  循环完发送最后一个比赛盘口
            if (currentMatchEntity != null) MatchCompareRowNumAndMarket(currentMatchEntity, pm);
        }

        private void DealDoInfo(JToken jtoken, NormalParam normalParam)
        {
            try
            {
                var keepOdds = KeepOddsManager.Instance.AddOrGetKeepOdds(normalParam.Stage);
                string oddsId = jtoken["oddsid"].ToString();
                Console.WriteLine("Remove Odds = " + oddsId);
                var old = keepOdds.GetMarket<SrcMarketTwo>(oddsId);
                if (old == null) return;

                var kom = keepOdds.GetOrAdd(old.SrcMatchId);
                if (kom == null) return;
                kom.DeleteOddsIdList(old.MarketID, new List<string>() { oddsId });

                //  有盘口移除需要对比RowNum是否变化和对应盘口关盘
                var me = matchEntityManager.Get(old.SrcMatchId);
                if (me != null)
                {
                    MatchCompareRowNumAndMarket(me, normalParam);
                }
                else
                {
                    logger.Error("DealDoInfo cannot find srcMatchId = " + old.SrcMatchId + " ,oddsId = " + old.SrcCouID);
                }

            }
            catch (Exception e)
            {
                logger.Error(e.ToString());
            }
        }

        private void DealDmInfo(JToken jtoken, NormalParam normalParam)
        {
            try
            {
                var keepOdds = KeepOddsManager.Instance.AddOrGetKeepOdds(normalParam.Stage);
                string matchId = jtoken["matchid"].ToString();
                var me = matchEntityManager.Get(matchId);
                me.MatchDisappear(normalParam.Stage, normalParam.LimitMarketIdList, true);
                keepOdds.RemoveBySrcMatchId(matchId);
                LiveTimeManager.Instance.RemoveBySrcMatchId(matchId);

                Console.WriteLine("Remove SrcMatchID = " + matchId);
            }
            catch (Exception e)
            {
                logger.Error(e.ToString());
            }
        }

        private void DealOddsInfo(JToken jtoken, NormalParam normalParam)
        {
            try
            {
                var keepOdds = KeepOddsManager.Instance.AddOrGetKeepOdds(normalParam.Stage);

                string oddsId = jtoken["oddsid"].ToString();
                ParseOddsInfo poi = new ParseOddsInfo();
                poi.CompareSet(jtoken);
                SrcMarketTwo two = null;
                bool isUpdate = false;
                if (!keepOdds.OddsIdExist(oddsId))
                {
                    //先判断是否是第一次出现，如果不是，并且oddsdic中还不存在，说明之前就不要的，那就直接抛弃
                    if (jtoken["matchid"] == null)
                        return;
                    //如果是第一次出现，就去dropdic中查找，是否包含这个比赛，如果有，说明是被筛选掉的，抛弃。
                    string matchId = jtoken["matchid"].ToString();
                    if (!matchEntityManager.MatchExist(matchId))
                        return;

                    //  暂时处理Hdp和OU
                    if (poi.MarketId == 1 || poi.MarketId == 2 || poi.MarketId == 3 || poi.MarketId == 4)
                    {
                        var kom = keepOdds.GetOrAdd(matchId);
                        kom.UpdateOddsIdList(poi.MarketId, new List<string>() { oddsId });

                        two = keepOdds.AddOrGetMarket<SrcMarketTwo>(oddsId);
                        two.SrcMatchId = matchId;
                        two.SrcCouID = oddsId;
                        two.MarketID = poi.MarketId;

                    }
                }
                else
                {
                    //  更新的盘口直接更新缓存中
                    two = keepOdds.GetMarket<SrcMarketTwo>(oddsId);
                    isUpdate = true;
                }

                if (two != null)
                {
                    string hdp = poi.Hdp1;
                    if (two.MarketID == (int)MarketTypeEnum.F_HDP || two.MarketID == (int)MarketTypeEnum.H_HDP)
                    {
                        if (poi.Hdp2 != "0") hdp = poi.Hdp2;
                        else hdp = "-" + hdp;
                    }

                    two.SetOdds(new[] { "", hdp, poi.HomeOdds, poi.AwayOdds });

                    if (isUpdate)
                    {
                        var me = matchEntityManager.Get(two.SrcMatchId);
                        if (me != null)
                        {
                            me.CompareSingleMarket(two.ToMarketEntity(0, normalParam.Stage), normalParam.Stage);
                        }
                        else
                        {
                            logger.Error("Update market cannot find srcMatchId = " + two.SrcMatchId + " oddsId = " + two.SrcCouID);
                        }
                    }

                    JsonSerializerSettings jsetting = new JsonSerializerSettings();
                    jsetting.NullValueHandling = NullValueHandling.Ignore;
                    var str = JsonConvert.SerializeObject(two, jsetting);

                    Console.WriteLine("market = " + str);
                }
                //else
                //{
                //    //有些盘口是不需要的
                //    logger.Error("Cannot get market  ParseOddsInfo = " + JsonConvert.SerializeObject(poi));
                //}
            }
            catch (Exception e)
            {
                logger.Error(e.ToString());
            }
        }

        private MatchEntity DealMatchInfo(JToken jtoken, NormalParam normalParam)
        {
            try
            {
                //解析比赛信息
                var pmi = new ParseMatchInfo();
                pmi.CompareSet(jtoken);
                if (pmi.SportType == null) return null;

                var sportId = IbcTools.ConvertToSportId(pmi.SportType);
                var matchDate = GetTime(pmi.KickOffTime);
                matchDate = matchDate.AddSeconds(-matchDate.Second);
                var diffMinutes = matchDate.Minute % 5;
                matchDate = matchDate.AddMinutes(5 - diffMinutes);

                var me = matchEntityManager.GetOrAdd(pmi.MatchId, pmi.LeagueName, pmi.HomeTeamName, pmi.AwayTeamName, matchDate, sportId);

                me.CompareToStage(normalParam.Stage);

                //  if stage = 3
                if (normalParam.Stage == 3)
                {
                    if (!string.IsNullOrEmpty(pmi.LiveHomeScore) && !string.IsNullOrEmpty(pmi.LiveAwayScore))
                    {
                        me.CompareToScore(Convert.ToInt32(pmi.LiveHomeScore), Convert.ToInt32(pmi.LiveAwayScore));
                    }
                    if (!string.IsNullOrEmpty(pmi.HomeRed) && !string.IsNullOrEmpty(pmi.AwayRed))
                    {
                        me.CompareToCard(Convert.ToInt32(pmi.HomeRed), Convert.ToInt32(pmi.AwayRed));
                    }
                    var lti = new LiveTimeInfo();
                    lti.SrcMatchId = pmi.MatchId;
                    lti.Phase = pmi.Csstatus == "1" ? 0 : pmi.Liveperiod == "1" ? 1 : pmi.Liveperiod == "2" ? 2 : -1;
                    lti.PhaseStartUtc = pmi.Livetimer;

                    me.CompareToTime(lti.Phase, lti.LiveTime);
                    LiveTimeManager.Instance.AddOrUpdate(lti);
                }

                JsonSerializerSettings jsetting = new JsonSerializerSettings();
                jsetting.NullValueHandling = NullValueHandling.Ignore;
                var str = JsonConvert.SerializeObject(pmi, jsetting);

                Console.WriteLine("market = " + str);
                return me;
            }
            catch (Exception e)
            {
                logger.Error(e.ToString());
                return null;
            }
        }

        private void MatchCompareRowNumAndMarket(MatchEntity currentMatchEntity, NormalParam param)
        {
            var keepOdds = KeepOddsManager.Instance.AddOrGetKeepOdds(param.Stage);
            var kom = keepOdds.GetOrAdd(currentMatchEntity.SrcMatchID);
            if (kom != null)
            {
                int htRowNum;
                int rowNum;
                kom.GetRowNum(out rowNum, out htRowNum);
                //  先发RowNum再发盘口
                currentMatchEntity.CompareToRowNum(rowNum, htRowNum, param.Stage);
                var oddsIdList = kom.GetOddsIdList();
                var dic = keepOdds.ToMarketEntityBases(oddsIdList, 0, param.Stage);
                currentMatchEntity.CompareToMarket(dic, param.Stage, param.LimitMarketIdList);
            }
            else
            {
                logger.Error("Cannot find kom srcMatchId = " + currentMatchEntity.SrcMatchID);
            }
        }
        private JArray FormatToJArray(string data)
        {
            var index = data.LastIndexOf('[');
            if (index <= 0) return null;
            data = data.Remove(0, index);
            index = data.IndexOf(']');
            data = data.Remove(index) + ']';
            //序列化为动态数组
            return JsonConvert.DeserializeObject<JArray>(data);
        }
        private static DateTime GetTime(string timeStamp)
        {
            DateTime dtStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            long lTime = long.Parse(timeStamp + "0000000");
            TimeSpan toNow = new TimeSpan(lTime);
            return dtStart.Add(toNow);
        }
    }
}
