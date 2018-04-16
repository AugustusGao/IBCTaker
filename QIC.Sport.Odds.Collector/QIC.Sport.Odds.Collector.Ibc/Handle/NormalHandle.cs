using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using ML.EGP.Sport.Common;
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
            try
            {
                var pd = data as PushDataDto;
                var pm = pd.Param as NormalParam;
                var jArray = FormatToJArray(pd.Data);
                Dictionary<string, KeepOddsMatch> dicUpdateKom = new Dictionary<string, KeepOddsMatch>();
                MatchEntity currentMatchEntity = null;
                foreach (var item in jArray)
                {
                    if (item.ToString().Contains("type"))
                    {
                        KeyValuePair<string, KeepOddsMatch>? kom = null;
                        switch (item["type"].ToString())
                        {
                            //如果type是m，包含的match对象
                            case "m":
                                {
                                    //  发送前一次已经统计完的比赛盘口,再进行新的比赛统计
                                    if (currentMatchEntity != null)
                                    {
                                        MatchCompareRowNumAndMarket(currentMatchEntity, pm);
                                    }
                                    currentMatchEntity = DealMatchInfo(item, pm);
                                    break;
                                }
                            //如果type是o，包含的odds对象
                            case "o": kom = DealOddsInfo(item, pm); break;
                            //如果type是dm,说明该场比赛结束了，可以删除了
                            case "dm": DealDmInfo(item, pm); break;
                            //如果type是do，说明该盘口关闭了
                            case "do": DealDoInfo(item, pm); break;
                            //其他类暂不处理
                            default: break;
                        }
                        if (kom.HasValue && !dicUpdateKom.ContainsKey(kom.Value.Key)) dicUpdateKom.Add(kom.Value.Key, kom.Value.Value);
                    }
                    else
                    {
                        //不包含就说明是其他信息，再做处理
                    }
                }

                //  循环完发送最后一个比赛盘口
                if (currentMatchEntity != null) MatchCompareRowNumAndMarket(currentMatchEntity, pm);

                //  发送只有盘口更新的数据
                if (dicUpdateKom.Any())
                {
                    var ko = KeepOddsManager.Instance.AddOrGetKeepOdds(pm.Stage);
                    foreach (var kv in dicUpdateKom)
                    {
                        var me = matchEntityManager.Get(kv.Key);
                        if (me == null)
                        {
                            logger.Error("Update market cannot find me SrcMatchId = " + kv.Key);
                            continue;
                        }

                        var list = kv.Value.GetOddsIdList();
                        var dic = ko.ToMarketEntityBases(list, 0, pm.Stage);
                        me.CompareToMarket(dic, pm.Stage, pm.LimitMarketIdList);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
                logger.Error(JsonConvert.SerializeObject(data));
            }
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
                LiveInfoManager.Instance.RemoveBySrcMatchId(matchId);

                Console.WriteLine("Remove SrcMatchID = " + matchId);
            }
            catch (Exception e)
            {
                logger.Error(e.ToString());
            }
        }

        private KeyValuePair<string, KeepOddsMatch>? DealOddsInfo(JToken jtoken, NormalParam normalParam)
        {
            try
            {
                KeyValuePair<string, KeepOddsMatch>? updateKom = null;
                var keepOdds = KeepOddsManager.Instance.AddOrGetKeepOdds(normalParam.Stage);
                string oddsId = jtoken["oddsid"].ToString();
                ParseOddsInfo poi = null;
                SrcMarketEntityBase smb = null;
                if (!keepOdds.OddsIdExist(oddsId))
                {
                    //先判断是否是第一次出现，如果不是，并且oddsdic中还不存在，说明之前就不要的，那就直接抛弃
                    if (jtoken["matchid"] == null)
                        return null;
                    //如果是第一次出现，就去dropdic中查找，是否包含这个比赛，如果有，说明是被筛选掉的，抛弃。
                    string matchId = jtoken["matchid"].ToString();
                    if (!matchEntityManager.MatchExist(matchId))
                        return null;
                    var bettype = jtoken["bettype"].ToString();
                    poi = OddsFactory.CreatOdds(bettype);
                    poi.CompareSet(jtoken);
                    //  只处理需要的盘口类型
                    if (normalParam.LimitMarketIdList.Contains(poi.MarketId))
                    {
                        var kom = keepOdds.GetOrAdd(matchId);
                        kom.UpdateOddsIdList(poi.MarketId, new List<string>() { oddsId });

                        if (MarketTools.CheckTwoMakret(poi.MarketId))
                        {
                            smb = keepOdds.AddOrGetMarket<SrcMarketTwo>(oddsId);
                        }
                        else if (poi.MarketId == (int)MarketTypeEnum.H_1X2 || poi.MarketId == (int)MarketTypeEnum.F_1X2)
                        {
                            smb = keepOdds.AddOrGetMarket<SrcMarket1X2>(oddsId);
                        }

                        if (smb != null)
                        {
                            smb.SrcMatchId = matchId;
                            smb.SrcCouID = oddsId;
                            smb.Bettype = bettype;
                            smb.MarketID = poi.MarketId;
                            smb.OddsStatus = poi.OddsStatus;
                        }
                    }
                }
                else
                {
                    //  更新的盘口直接更新缓存中
                    smb = keepOdds.GetMarket<SrcMarketEntityBase>(oddsId);
                    if (smb != null)
                    {
                        poi = OddsFactory.CreatOdds(smb.Bettype);
                        poi.MarketId = smb.MarketID;
                        poi.CompareSet(jtoken);
                        updateKom = new KeyValuePair<string, KeepOddsMatch>(smb.SrcMatchId, keepOdds.GetOrAdd(smb.SrcMatchId));
                    }
                }

                if (smb != null)
                {
                    smb.SetOdds(poi.GetDataArr());

                    JsonSerializerSettings jsetting = new JsonSerializerSettings();
                    jsetting.NullValueHandling = NullValueHandling.Ignore;
                    var str = JsonConvert.SerializeObject(smb, jsetting);

                    Console.WriteLine("market = " + str);
                }
                //else
                //{
                //    //有些盘口是不需要的
                //    logger.Error("Cannot get market  ParseOddsInfo = " + JsonConvert.SerializeObject(poi));
                //}
                return updateKom;
            }
            catch (Exception e)
            {
                logger.Error(e.ToString());
                return null;
            }
        }

        private MatchEntity DealMatchInfo(JToken jtoken, NormalParam normalParam)
        {
            try
            {
                //解析比赛信息
                var pmi = new ParseMatchInfo();
                pmi.CompareSet(jtoken);
                MatchEntity me;
                if (pmi.SportType != null)
                {
                    var sportId = IbcTools.ConvertToSportId(pmi.SportType);
                    var matchDate = GetTime(pmi.KickOffTime);
                    matchDate = matchDate.AddSeconds(-matchDate.Second);
                    var diffMinutes = matchDate.Minute % 5;
                    matchDate = matchDate.AddMinutes(5 - diffMinutes);

                    me = matchEntityManager.GetOrAdd(pmi.MatchId, pmi.LeagueName, pmi.HomeTeamName, pmi.AwayTeamName, matchDate, sportId);

                    me.CompareToStage(normalParam.Stage);
                }
                else
                {
                    me = matchEntityManager.Get(pmi.MatchId);
                }

                if (me == null)
                {
                    logger.Error("DealMatchInfo Cannot find srcMatchId = " + pmi.MatchId);
                    return null;
                }

                //  if stage = 3
                if (normalParam.Stage == 3)
                {
                    var lti = new LiveInfo();
                    lti.SrcMatchId = pmi.MatchId;
                    lti.HomeScore = string.IsNullOrEmpty(pmi.LiveHomeScore) ? lti.HomeScore : Convert.ToInt32(pmi.LiveHomeScore);
                    lti.AwayScore = string.IsNullOrEmpty(pmi.LiveAwayScore) ? lti.AwayScore : Convert.ToInt32(pmi.LiveAwayScore);
                    lti.HomeRed = string.IsNullOrEmpty(pmi.HomeRed) ? lti.HomeRed : Convert.ToInt32(pmi.HomeRed);
                    lti.AwayRed = string.IsNullOrEmpty(pmi.AwayRed) ? lti.AwayRed : Convert.ToInt32(pmi.AwayRed);
                    lti.Phase = pmi.Csstatus == "1" ? 0 : string.IsNullOrEmpty(pmi.Liveperiod) ? (int?)null : pmi.Liveperiod == "1" ? 1 : pmi.Liveperiod == "2" ? 2 : -1;
                    lti.PhaseStartUtc = pmi.Livetimer;

                    var ret = LiveInfoManager.Instance.AddOrUpdate(lti);
                    me.CompareToScore(ret.HomeScore.Value, ret.AwayScore.Value);
                    me.CompareToCard(ret.HomeRed.Value, ret.AwayRed.Value);
                    me.CompareToTime(ret.Phase.Value, ret.LiveTime);
                }

                JsonSerializerSettings jsetting = new JsonSerializerSettings();
                jsetting.NullValueHandling = NullValueHandling.Ignore;
                var str = JsonConvert.SerializeObject(pmi, jsetting);

                Console.WriteLine("match = " + str);
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
