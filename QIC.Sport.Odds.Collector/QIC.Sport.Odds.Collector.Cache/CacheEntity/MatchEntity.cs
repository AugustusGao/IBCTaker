using ML.EGP.Sport.CommandProtocol.Command;
using ML.EGP.Sport.CommandProtocol.Dto.TakeServer;
using ML.EGP.Sport.Common;
using ML.EGP.Sport.Common.Enums;
using ML.Infrastructure.Config;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using ML.EGP.Sport.CommandProtocol;
using ML.EGP.Sport.Common.Market;
using Newtonsoft.Json;

namespace QIC.Sport.Odds.Collector.Cache.CacheEntity
{
    public class MatchEntity : IMatchEntity
    {
        public string SrcMatchID { get; set; }
        public int SportID { get; set; }
        public string LeagueName { get; set; }
        public string HomeName { get; set; }
        public string AwayName { get; set; }
        public DateTime MatchDate { get; set; }
        public int Phase { get; set; }
        public int LiveTime { get; set; }
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public int HomeCard { get; set; }
        public int AwayCard { get; set; }
        public int RowNum { get; set; }
        public int HTRowNum { get; set; }
        public int Stage { get; set; }
        public int MatchID { get; set; }
        //CouID-Market
        public Dictionary<long, MarketEntityBase> MarketDic { get; set; }

        public ITestSend Reptile = new TestSend();

        private int takeType = ConfigSingleton.CreateInstance().GetAppConfig<int>("TakeType");

        private ILog _logger = LogManager.GetLogger(typeof(MatchEntity));
        public MatchEntity()
        {
            Phase = -1;
            HomeScore = -1;
            AwayScore = -1;
            MarketDic = new Dictionary<long, MarketEntityBase>();
        }

        /// <summary>
        /// 该页面下这个比赛消失的情况执行关闭
        /// </summary>
        /// <param name="pageID"></param>
        public void MatchDisappear(int stage, List<int> limitMarketList, bool limitMatchInfo)
        {
            if (stage > 0 && stage != Stage) return;

            //循环需要关闭的盘口种类
            List<long> closetList = new List<long>();
            foreach (var m in MarketDic.Values)
            {
                if (!limitMarketList.Contains(m.MarketID)) continue;
                closetList.Add(m.CouID);
            }
            closetList.ForEach(o => { MarketDic.Remove(o); SendCloseCoupon(o); });
            //拿取比赛基础信息 
            if (limitMatchInfo)
            {
                Stage = 0;
                //关行
                RowNum = 0; HTRowNum = 0;
                SendRowNum();
            }
        }

        /// <summary>
        /// 比赛完整的盘口对比
        /// </summary>
        /// <param name="UpdateMarketList">盘口</param>
        /// <param name="dto">页面对象</param>
        public void CompareToMarket(Dictionary<long, MarketEntityBase> marketDic, int stage, List<int> limitMarketList)
        {
            if (stage > 0 && stage != Stage) return;

            List<long> closetList = new List<long>();
            List<long> sendList = new List<long>();

            foreach (var m in MarketDic.Values)
            {
                if (!limitMarketList.Contains(m.MarketID)) continue;
                if (marketDic.ContainsKey(m.CouID) && marketDic[m.CouID].IsIntegrity()) continue;
                closetList.Add(m.CouID);
            }

            foreach (var aum in marketDic.Values)
            {
                MarketEntityBase cmeb;
                if (MarketDic.TryGetValue(aum.CouID, out cmeb))
                {
                    if (!cmeb.CompareSet(aum)) continue;

                    //if (takeType == 3 && Stage == 3 && aum.MarketID < 5 && SportID == 1)
                    //{
                    //    var two = aum as MarketTwo;
                    //    two.Dubious = false;
                    //}
                    sendList.Add(aum.CouID);
                }
                else
                {

                    MarketDic.Add(aum.CouID, aum);
                    //if (takeType == 3 && Stage == 3 && aum.MarketID < 5 && SportID == 1)
                    //{
                    //    var two = aum as MarketTwo;
                    //    two.Dubious = true;
                    //}
                    sendList.Add(aum.CouID);
                }
            }

            closetList.ForEach(o => { MarketDic.Remove(o); SendCloseCoupon(o); });
            sendList.ForEach(o => SendCoupon(marketDic[o]));
        }


        #region 比赛基础信息对比

        /// <summary>
        /// 阶段变化对比
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public bool CompareToStage(int stage)
        {
            if (stage == Stage) return false;

            if (stage > Stage)
            {
                List<long> couIdList = (from m in MarketDic.Values where m.Stage > 0 && m.Stage != stage select m.CouID).ToList();

                couIdList.ForEach(id =>
                {
                    SendCloseCoupon(id);
                    MarketDic.Remove(id);
                });

                Stage = stage;
                SendStage();
            }
            else
            {
                //非正常切换阶段，倒退 记录Log
                _logger.Error("Rewind the stage ,Match Stage = " + Stage + " , this stage = " + stage);
            }
            return true;
        }

        /// <summary>
        /// 行数变化对比
        /// </summary>
        /// <param name="rowNum"></param>
        /// <param name="htRowNum"></param>
        /// <returns></returns>
        public bool CompareToRowNum(int rowNum, int htRowNum, int stage)
        {
            if (stage != Stage) return false;

            if (rowNum == RowNum && htRowNum == HTRowNum) return false;

            RowNum = rowNum;
            HTRowNum = htRowNum;
            if (HTRowNum > RowNum) RowNum = HTRowNum;
            SendRowNum();
            return true;
        }
        public bool CompareToScore(int homeScore, int awayScore)
        {
            if (Stage != 3 || (homeScore == HomeScore && awayScore == AwayScore)) return false;

            //  足球和棒球比分变化先发盘口关闭
            if (SportID == 1 || SportID == 4)
            {
                var closeIDs = MarketDic.Keys.ToList();
                closeIDs.ForEach(id =>
                {
                    MarketDic.Remove(id);
                    SendCloseCoupon(id);
                });
            }

            HomeScore = homeScore;
            AwayScore = awayScore;
            SendScore();
            return true;
        }
        public bool CompareToTime(int phase, int liveTime)
        {
            if (Stage != 3 || (phase == Phase && liveTime == LiveTime)) return false;

            //  处理SBO可能出现HT -> !Live,我们需要变为 2H-00, 只针对足球处理这个问题
            if ((Phase == 0 || Phase == 2) && phase == -1 && SportID == 1)
            {
                Phase = 2;
                LiveTime = 0;
                log4net.LogManager.GetLogger(typeof(MatchEntity)).Info("MatchID = " + MatchID + " ,SrcMatchID = " + SrcMatchID + " ,Phase 0  to  -1");
            }
            else
            {
                Phase = phase;
                LiveTime = liveTime;
            }

            SendTime();
            return true;
        }
        public bool CompareToCard(int homeCard, int awayCard)
        {
            if (Stage != 3 || (homeCard == HomeCard && awayCard == AwayCard)) return false;

            //  足球和棒球红卡变化先发盘口关闭
            if (SportID == 1 || SportID == 4)
            {
                var closeIDs = MarketDic.Keys.ToList();
                closeIDs.ForEach(id =>
                {
                    MarketDic.Remove(id);
                    SendCloseCoupon(id);
                });
            }

            HomeCard = homeCard;
            AwayCard = awayCard;
            SendCard();
            return true;
        }

        #endregion

        #region 各种发送方法
        //发送盘口更新信号
        private void SendCoupon(MarketEntityBase meb)
        {
            if (MatchID == 0) return;
            if (meb == null) return;//记日志
            switch (meb.MarketID)
            {
                case (int)MarketTypeEnum.F_HDP:
                case (int)MarketTypeEnum.F_OU:
                case (int)MarketTypeEnum.H_HDP:
                case (int)MarketTypeEnum.H_OU:
                case (int)MarketTypeEnum.F_OE:
                case (int)MarketTypeEnum.H_OE:
                case (int)MarketTypeEnum.ML:
                    SendMarket<MarketTwo, TakeTwoDto>(meb, TakeServerCommand.SrcTwo);
                    break;
                case (int)MarketTypeEnum.F_1X2:
                case (int)MarketTypeEnum.H_1X2:
                    SendMarket<Market1x2, Take1X2Dto>(meb, TakeServerCommand.Src1x2);
                    break;
                case (int)MarketTypeEnum.F_TG:
                    SendMarket<MarketTotalGoal, TakeTotalGoalDto>(meb, TakeServerCommand.SrcTG);
                    break;
                case (int)MarketTypeEnum.F_FGLG:
                    SendMarket<MarketFglg, TakeFglgDto>(meb, TakeServerCommand.SrcFglg);
                    break;
                case (int)MarketTypeEnum.HTFT:
                    SendMarket<MarketHtft, TakeHtFtDto>(meb, TakeServerCommand.SrcHtft);
                    break;
                case (int)MarketTypeEnum.F_CS:
                    SendMarket<MarketCS, TakeCsDto>(meb, TakeServerCommand.SrcFCS);
                    break;
            }
        }

        /// <summary>
        /// 发送盘口统一方法
        /// </summary>
        /// <typeparam name="TOriginal">Take实体对象类型</typeparam>
        /// <typeparam name="TDestination">发送所需传输对象类型</typeparam>
        /// <param name="market">盘口实体</param>
        /// <param name="cmd">发送盘口命令</param>
        private void SendMarket<TOriginal, TDestination>(MarketEntityBase market, TakeServerCommand cmd)
            where TOriginal : MarketEntityBase
            where TDestination : MarketBase, new()
        {
            TOriginal original = market as TOriginal;
            if (original == null) return;
            TDestination destination = (TDestination)original.ToTakeMarketDto(MatchID, takeType);
            destination.RowIndex = 1;
            Reptile.Send(cmd, destination);
        }
        private void SendCloseCoupon(long couID)
        {
            if (MatchID == 0) return;
            var hdp = MarketTools.GetHdpByCouID(couID);
            var marketID = MarketTools.GetMarketIDByCouID(couID);

            Reptile.Send(TakeServerCommand.MarketClose, new TakeMarketClose()
            {
                CouID = MarketTools.CreateCouID(MatchID, marketID, hdp),
                TakeType = takeType,
                MarketID = marketID,
                MatchID = MatchID,
                Hdp = hdp
            });
        }
        private void SendRowNum()
        {
            if (MatchID != 0)
                Reptile.Send(TakeServerCommand.MatchRowNum, new TakeMatchRowNumDto()
                {
                    RowNum = RowNum,
                    HTRowNum = HTRowNum,
                    MatchID = MatchID,
                    TakeType = takeType
                });
        }
        private void SendScore()
        {
            if (MatchID != 0 && !(HomeScore <= -1) && !(AwayScore <= -1))
                Reptile.Send(TakeServerCommand.MatchScore, new TakeMatchScoreDto()
                {
                    HomeScore = this.HomeScore,
                    AwayScore = this.AwayScore,
                    TakeType = takeType,
                    MatchID = this.MatchID
                });

        }
        private void SendTime()
        {
            if (MatchID != 0)
                Reptile.Send(TakeServerCommand.MatchLiveTime, new TakeMatchLiveTimeDto()
                {
                    Phase = this.Phase,
                    LiveTime = this.LiveTime,
                    MatchID = this.MatchID,
                    TakeType = takeType
                });
        }
        private void SendCard()
        {
            if (MatchID != 0)
                Reptile.Send(TakeServerCommand.MatchRedCard, new TakeMatchRedCardDto()
                {
                    HomeCard = this.HomeCard,
                    AwayCard = this.AwayCard,
                    MatchID = this.MatchID,
                    TakeType = takeType
                });
        }
        private void SendStage()
        {
            if (MatchID != 0)
            {
                Reptile.Send(TakeServerCommand.MatchStage, new TakeMatchStageDto()
                {
                    MatchID = MatchID,
                    Stage = Stage,
                    TakeType = takeType
                });
            }
        }

        public void SendAll()
        {
            //  发送Stage
            SendStage();

            //  发送比分
            SendScore();

            //  发送LiveTime
            SendTime();

            //  发送RowNum
            SendRowNum();

            //  发送红卡
            SendCard();

            //  发送盘口
            foreach (var market in MarketDic.Values)
            {
                SendCoupon(market);
            }
        }
        #endregion

    }
    public interface ITestSend
    {
        void Send<T>(TakeServerCommand tcmd, T data) where T : class ,new();
    }

    public class TestSend : ITestSend
    {
        public void Send<T>(TakeServerCommand tcmd, T data) where T : class, new()
        {
            Console.WriteLine(tcmd + "", JsonConvert.SerializeObject(data));
        }
    }
}
