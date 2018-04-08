using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
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
using QIC.Sport.Odds.Collector.Ibc.Tools;

namespace QIC.Sport.Odds.Collector.Ibc.Handle
{
    public class NormalHandle : IHandle
    {
        private static ILog logger = LogManager.GetLogger(typeof(NormalHandle));
        private static MatchEntityManager matchEntityManager = IocUnity.GetService<IMatchEntityManager>("MatchEntityManager") as MatchEntityManager;

        public void ProcessData(ITakeData data)
        {
            var pd = data as PushDataDto;
            var pm = pd.Param as NormalParam;
            var jArray = FormatToJArray(pd.Data);

            MatchEntity currentMatchEntity;
            bool isNext = false;
            foreach (var item in jArray)
            {


                if (item.ToString().Contains("type"))
                {
                    switch (item["type"].ToString())
                    {
                        //如果type是m，包含的match对象
                        case "m":
                            currentMatchEntity = DealMatchInfo(item, pm);
                            isNext = true;
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
                keepOdds.RemoveSrcMatchId(matchId);

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

                    two = keepOdds.GetMarket<SrcMarketTwo>(oddsId);
                }

                if (two != null)
                {
                    two.SetOdds(new[] { "", poi.Hdp1, poi.HomeOdds, poi.AwayOdds });

                    JsonSerializerSettings jsetting = new JsonSerializerSettings();
                    jsetting.NullValueHandling = NullValueHandling.Ignore;
                    var str = JsonConvert.SerializeObject(two, jsetting);

                    Console.WriteLine("market = " + str);
                }

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
                var diffMinutes = matchDate.Minute % 5;
                matchDate = matchDate.AddMinutes(5 - diffMinutes);

                var me = matchEntityManager.GetOrAdd(pmi.MatchId, pmi.LeagueName, pmi.HomeTeamName, pmi.AwayTeamName, matchDate, sportId);

                me.CompareToStage(normalParam.Stage);

                //  todo Compare RowNum

                //  if stage = 3
                //  todo Compare CompareToScore
                //  todo Compare CompareToCard
                //  todo Compare CompareToTime
                //  todo Compare CompareMarket

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
        public static DateTime GetTime(string timeStamp)
        {
            DateTime dtStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            long lTime = long.Parse(timeStamp + "0000000");
            TimeSpan toNow = new TimeSpan(lTime);
            return dtStart.Add(toNow);
        }
    }
}
