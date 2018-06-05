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
using QIC.Sport.Odds.Collector.Common;
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
                        //  同步时间时间处理
                        if (pm.SubscribeType == "time")
                        {
                            var str = item.Next["t"].ToString();
                            var ibcNow = Convert.ToInt64(str.Substring(0, str.Length - 3));
                            var localNow = CommonTools.ToUnixTimeSpan(DateTime.Now);
                            LiveInfoManager.Instance.IbcDiffSyncTime = Convert.ToInt32(localNow - ibcNow);
                            return;
                        }

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
                            case "o": kom = DealOddsInfo(item, pm, currentMatchEntity != null); break;
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
                    foreach (var kv in dicUpdateKom)
                    {
                        var me = matchEntityManager.Get(kv.Key);
                        if (me == null)
                        {
                            logger.Error("Update market cannot find me SrcMatchId = " + kv.Key);
                            continue;
                        }

                        MatchCompareRowNumAndMarket(me, pm);
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
                keepOdds.RemoveMarketByOddsId(oddsId);

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
                Dictionary<string, MatchEntity> delDic = new Dictionary<string, MatchEntity>();
                delDic.Add(matchId, me);
                if (me.SportID == 2)
                {
                    //  移除相关的篮球小节比赛
                    foreach (var q in new[] { "01", "02", "03", "04" })
                    {
                        var m = matchEntityManager.Get(matchId + q);
                        if (m == null) continue;

                        delDic.Add(matchId + q, m);
                    }
                }

                foreach (var kv in delDic)
                {
                    kv.Value.MatchDisappear(normalParam.Stage, normalParam.LimitMarketIdList, true);
                    keepOdds.RemoveBySrcMatchId(kv.Key);
                    LiveInfoManager.Instance.RemoveBySrcMatchId(kv.Key, normalParam.Stage);

                    Console.WriteLine("Remove SrcMatchID = " + kv.Key);
                }
            }
            catch (Exception e)
            {
                logger.Error(e.ToString());
            }
        }

        private KeyValuePair<string, KeepOddsMatch>? DealOddsInfo(JToken jtoken, NormalParam normalParam, bool isExistMatch)
        {
            try
            {
                KeyValuePair<string, KeepOddsMatch>? updateKom = null;
                var keepOdds = KeepOddsManager.Instance.AddOrGetKeepOdds(normalParam.Stage);
                string oddsId = jtoken["oddsid"].ToString();
                ParseOddsInfo poi = null;
                SrcMarketEntityBase smb = null;
                bool isUpdate = false;
                if (!keepOdds.OddsIdExist(oddsId))
                {
                    if (jtoken["matchid"] == null)
                        return null;

                    string matchId = jtoken["matchid"].ToString();
                    if (!matchEntityManager.MatchExist(matchId))
                        return null;
                    var bettype = jtoken["bettype"].ToString();
                    poi = OddsFactory.CreatOdds(bettype);
                    poi.CompareSet(jtoken, false);
                    //  只处理需要的盘口类型
                    if (normalParam.LimitMarketIdList.Contains(poi.MarketId))
                    {
                        KeepOddsMatch kom = null;
                        if (!string.IsNullOrEmpty(poi.Resourceid))
                        {
                            var mainMatch = matchEntityManager.Get(matchId);
                            matchId = matchId + poi.Resourceid;
                            if (mainMatch.SportID == 2)
                            {
                                var quarterMatch = matchEntityManager.Get(matchId);
                                if (quarterMatch == null)
                                {//  自生成篮球小节的比赛
                                    var quarter = poi.Resourceid == "01" ? " - First Quarter" :
                                        poi.Resourceid == "02" ? " - Second Quarter" :
                                        poi.Resourceid == "03" ? " - Third Quarter" :
                                        poi.Resourceid == "03" ? " - Fourth Quarter" : " - UnKnown Quarter";
                                    quarterMatch = matchEntityManager.GetOrAdd(matchId, mainMatch.SrcLeague + quarter, mainMatch.SrcHome, mainMatch.SrcAway, mainMatch.SrcMatchDate, mainMatch.SportID, true);
                                }
                                quarterMatch.CompareToStage(normalParam.Stage);
                                kom = keepOdds.GetOrAdd(matchId);
                                isUpdate = true;    //  以更新盘口处理方式发送小节比赛里的盘口数据
                            }
                        }
                        else
                        {
                            kom = keepOdds.GetOrAdd(matchId);
                            //  如果本次数据块中，新加的当前盘口之前并没有比赛信息，说明这个盘口数据是因为传输数据被分块，导致与比赛信息隔开了再发送的，此时这个盘口应当以更新方式发送
                            if (!isExistMatch) isUpdate = true;
                        }

                        if (kom == null) return null;

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
                        isUpdate = true;
                    }
                }

                if (smb != null)
                {
                    smb.SetOdds(poi.GetDataArr());
                    if (isUpdate) updateKom = new KeyValuePair<string, KeepOddsMatch>(smb.SrcMatchId, keepOdds.GetOrAdd(smb.SrcMatchId));

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
                int sportId = 0;
                MatchEntity me;
                if (pmi.SportType != null)
                {
                    sportId = IbcTools.ConvertToSportId(pmi.SportType);
                    if (sportId == 0) return null;

                    var matchDate = GetTime(pmi.KickOffTime);
                    matchDate = matchDate.AddSeconds(-matchDate.Second);
                    var diffMinutes = matchDate.Minute % 5;
                    matchDate = matchDate.AddMinutes(5 - diffMinutes);
                    var home = string.IsNullOrEmpty(pmi.HomeTeamNameExtension) ? pmi.HomeTeamName : pmi.HomeTeamName + "[" + pmi.HomeTeamNameExtension + "]";
                    var away = string.IsNullOrEmpty(pmi.AwayTeamNameExtension) ? pmi.AwayTeamName : pmi.AwayTeamName + "[" + pmi.AwayTeamNameExtension + "]";
                    if (sportId == 1 && pmi.Isneutral == "1") home = home + " (N)";

                    me = matchEntityManager.GetOrAdd(pmi.MatchId, pmi.LeagueName, home, away, matchDate, sportId);

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
                if (normalParam.Stage == 3 && !me.IsSelfCreate && (me.SportID == 1 || me.SportID == 2))
                {
                    var lti = new LiveInfo();
                    lti.SportId = me.SportID;
                    lti.SrcMatchId = pmi.MatchId;
                    lti.HomeScore = string.IsNullOrEmpty(pmi.LiveHomeScore) ? (int?)null : Convert.ToInt32(pmi.LiveHomeScore);
                    lti.AwayScore = string.IsNullOrEmpty(pmi.LiveAwayScore) ? (int?)null : Convert.ToInt32(pmi.LiveAwayScore);
                    lti.HomeRed = string.IsNullOrEmpty(pmi.HomeRed) ? (int?)null : Convert.ToInt32(pmi.HomeRed);
                    lti.AwayRed = string.IsNullOrEmpty(pmi.AwayRed) ? (int?)null : Convert.ToInt32(pmi.AwayRed);
                    lti.Csstatus = pmi.Csstatus;
                    lti.Liveperiod = pmi.Liveperiod;
                    lti.PhaseStartUtc = pmi.Livetimer;
                    lti.PhaseStartUtcUpdateTime = string.IsNullOrEmpty(pmi.Livetimer) ? (DateTime?)null : DateTime.Now;

                    var ret = LiveInfoManager.Instance.AddOrUpdate(lti);

                    if (me.SportID == 1)
                    {
                        me.CompareToScore(ret.HomeScore.Value, ret.AwayScore.Value);
                        me.CompareToCard(ret.HomeRed.Value, ret.AwayRed.Value);
                    }
                    else if (me.SportID == 2)
                    {
                        if (!string.IsNullOrEmpty(pmi.LiveScore))
                        {
                            var bkScore = JsonConvert.DeserializeObject<BasketballScore>(pmi.LiveScore);
                            var homeScore = Convert.ToInt32(bkScore.h1q) + Convert.ToInt32(bkScore.h2q) +
                                            Convert.ToInt32(bkScore.h3q) + Convert.ToInt32(bkScore.h4q);
                            var awayScore = Convert.ToInt32(bkScore.a1q) + Convert.ToInt32(bkScore.a2q) +
                                            Convert.ToInt32(bkScore.a3q) + Convert.ToInt32(bkScore.a4q);
                            me.CompareToScore(homeScore, awayScore);
                        }
                    }
                    me.CompareToTime(ret.Phase.Value, ret.LiveTime(LiveInfoManager.Instance.IbcDiffSyncTime));
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
                try
                {
                    int htRowNum;
                    int rowNum;
                    kom.GetRowNum(out rowNum, out htRowNum);
                    //  先发RowNum再发盘口
                    currentMatchEntity.CompareToRowNum(rowNum, htRowNum, param.Stage);
                    var oddsIdList = kom.GetOddsIdList();

                    var nl = oddsIdList.Distinct().ToList();
                    if (!oddsIdList.SequenceEqual(nl))
                    {
                        //  有重复的
                        logger.Error("Update market oddsId error = " + JsonConvert.SerializeObject(kom));
                    }

                    var dic = keepOdds.ToMarketEntityBases(nl, 0, param.Stage);
                    currentMatchEntity.CompareToMarket(dic, param.Stage, param.LimitMarketIdList);
                }
                catch (Exception ex)
                {
                    logger.Error(ex.ToString());
                    logger.Error("CompareToMarket Failed kom = " + JsonConvert.SerializeObject(kom));
                }
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
