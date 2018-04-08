using ML.EGP.Sport.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ML.EGP.Sport.CommandProtocol.Dto.TakeServer;
using ML.EGP.Sport.Common.Market;

namespace QIC.Sport.Odds.Collector.Cache.CacheEntity
{
    /*
     该类别里面包含所有抓水需要抓取的种类
     */
    public class MarketEntityBase
    {
        public long CouID { get; set; }

        public int MarketID { get; set; }

        public int Stage { get; set; }

        /// <summary>
        /// 检查是否更新并设定新值
        /// </summary>
        /// <param name="meb"></param>
        /// <returns></returns>
        internal virtual bool CompareSet(MarketEntityBase meb) { return false; }

        internal virtual MarketBase ToTakeMarketDto(int matchID, int takeType) { return null; }

        internal virtual bool IsIntegrity() { return false; }


    }

    public class MarketTwo : MarketEntityBase
    {
        public decimal Hdp { get; set; }
        public decimal HomeOdds { get; set; }
        public decimal AwayOdds { get; set; }
        public bool Dubious { get; set; }
        public decimal Tax { get { return OddsTools.ToTax(HomeOdds, AwayOdds); } }

        /// <summary>
        /// 对比是否一致，如果不一致更新
        /// </summary>
        /// <param name="hdp"></param>
        /// <param name="homeOdds"></param>
        /// <param name="awayOdds"></param>
        /// <returns></returns>
        internal override bool CompareSet(MarketEntityBase meb)
        {
            var two = meb as MarketTwo;
            if (Hdp == two.Hdp && HomeOdds == two.HomeOdds && AwayOdds == two.AwayOdds) return false;

            HomeOdds = two.HomeOdds;
            AwayOdds = two.AwayOdds;
            Hdp = two.Hdp;
            return true;
        }

        internal override MarketBase ToTakeMarketDto(int matchID, int takeType)
        {
            return new TakeTwoDto()
            {
                MatchID = matchID,
                TakeType = takeType,
                MarketID = MarketID,
                Tax = Tax,
                CouID = MarketTools.CreateCouID(matchID, MarketID, Hdp),
                Hdp = Hdp,
                HomeOdds = HomeOdds,
                AwayOdds = AwayOdds,
                Dubious = Dubious
            };
        }

        internal override bool IsIntegrity()
        {
            return !(HomeOdds == 0m || AwayOdds == 0m || CouID == 0);
        }
    }

    public class Market1x2 : MarketEntityBase
    {
        public decimal HomeOdds { get; set; }
        public decimal AwayOdds { get; set; }
        public decimal DrawOdds { get; set; }

        internal override bool CompareSet(MarketEntityBase meb)
        {
            var oneX2 = meb as Market1x2;
            if (HomeOdds == oneX2.HomeOdds && AwayOdds == oneX2.AwayOdds && DrawOdds == oneX2.DrawOdds) return false;
            HomeOdds = oneX2.HomeOdds;
            AwayOdds = oneX2.AwayOdds;
            DrawOdds = oneX2.DrawOdds;
            return true;
        }

        internal override MarketBase ToTakeMarketDto(int matchID, int takeType)
        {
            return new Take1X2Dto()
            {
                MatchID = matchID,
                TakeType = takeType,
                MarketID = MarketID,
                CouID = MarketTools.CreateCouID(matchID, MarketID, 0),
                DrawOdds = DrawOdds,
                HomeOdds = HomeOdds,
                AwayOdds = AwayOdds,
            };
        }

        internal override bool IsIntegrity()
        {
            return !(HomeOdds == 0m || AwayOdds == 0m || DrawOdds == 0m || CouID == 0);
        }
    }

    public class MarketTotalGoal : MarketEntityBase
    {
        public decimal Odds01 { get; set; }
        public decimal Odds23 { get; set; }
        public decimal Odds46 { get; set; }
        public decimal Odds70 { get; set; }

        internal override bool CompareSet(MarketEntityBase meb)
        {
            var tg = meb as MarketTotalGoal;
            if (Odds01 == tg.Odds01 && Odds23 == tg.Odds23 && Odds46 == tg.Odds46 && Odds70 == tg.Odds70) return false;
            Odds01 = tg.Odds01;
            Odds23 = tg.Odds23;
            Odds46 = tg.Odds46;
            Odds70 = tg.Odds70;
            return true;
        }
        internal override MarketBase ToTakeMarketDto(int matchID, int takeType)
        {
            return new TakeTotalGoalDto()
                {
                    MatchID = matchID,
                    TakeType = takeType,
                    MarketID = MarketID,
                    CouID = MarketTools.CreateCouID(matchID, MarketID, 0),
                    Odds01 = Odds01,
                    Odds23 = Odds23,
                    Odds46 = Odds46,
                    Odds70 = Odds70,
                };
        }

        internal override bool IsIntegrity()
        {
            if (CouID == 0) return false;
            if (Odds01 == 0m && Odds23 == 0m && Odds46 == 0m && Odds70 == 0m) return false;
            return true;
        }
    }

    public class MarketFglg : MarketEntityBase
    {
        public decimal HFOdds { get; set; }
        public decimal HLOdds { get; set; }
        public decimal AFOdds { get; set; }
        public decimal ALOdds { get; set; }
        public decimal NOOdds { get; set; }

        internal override bool CompareSet(MarketEntityBase meb)
        {
            var mk = meb as MarketFglg;
            if (HFOdds == mk.HFOdds
                && HLOdds == mk.HLOdds
                && AFOdds == mk.AFOdds
                && ALOdds == mk.ALOdds
                && NOOdds == mk.NOOdds
                )
                return false;
            HFOdds = mk.HFOdds;
            HLOdds = mk.HLOdds;
            AFOdds = mk.AFOdds;
            ALOdds = mk.ALOdds;
            NOOdds = mk.NOOdds;
            return true;
        }
        internal override MarketBase ToTakeMarketDto(int matchID, int takeType)
        {
            return new TakeFglgDto()
                {
                    MatchID = matchID,
                    TakeType = takeType,
                    MarketID = MarketID,
                    CouID = MarketTools.CreateCouID(matchID, MarketID, 0),
                    HFOdds = HFOdds,
                    HLOdds = HLOdds,
                    AFOdds = AFOdds,
                    ALOdds = ALOdds,
                    NGOdds = NOOdds,
                };
        }
        internal override bool IsIntegrity()
        {
            if (CouID == 0) return false;
            if (HFOdds == 0m && HLOdds == 0m && AFOdds == 0m && ALOdds == 0m && NOOdds == 0m) return false;
            return true;
        }
    }

    public class MarketHtft : MarketEntityBase
    {
        public decimal HHOdds { get; set; }
        public decimal HAOdds { get; set; }
        public decimal HDOdds { get; set; }
        public decimal DHOdds { get; set; }
        public decimal DAOdds { get; set; }
        public decimal DDOdds { get; set; }
        public decimal AHOdds { get; set; }
        public decimal AAOdds { get; set; }
        public decimal ADOdds { get; set; }
        internal override bool CompareSet(MarketEntityBase meb)
        {
            var mk = meb as MarketHtft;
            if (HHOdds == mk.HHOdds
                && HAOdds == mk.HAOdds
                && HDOdds == mk.HDOdds
                && DHOdds == mk.DHOdds
                && DAOdds == mk.DAOdds
                && DDOdds == mk.DDOdds
                && AHOdds == mk.AHOdds
                && AAOdds == mk.AAOdds
                && ADOdds == mk.ADOdds
                )
                return false;
            HHOdds = mk.HHOdds;
            HAOdds = mk.HAOdds;
            HDOdds = mk.HDOdds;
            DHOdds = mk.DHOdds;
            DAOdds = mk.DAOdds;
            DDOdds = mk.DDOdds;
            AHOdds = mk.AHOdds;
            AAOdds = mk.AAOdds;
            ADOdds = mk.ADOdds;
            return true;
        }
        internal override MarketBase ToTakeMarketDto(int matchID, int takeType)
        {
            return new TakeHtFtDto()
            {
                MatchID = matchID,
                TakeType = takeType,
                MarketID = MarketID,
                CouID = MarketTools.CreateCouID(matchID, MarketID, 0),
                HHOdds = HHOdds,
                HAOdds = HAOdds,
                HDOdds = HDOdds,
                DHOdds = DHOdds,
                DAOdds = DAOdds,
                DDOdds = DDOdds,
                AHOdds = AHOdds,
                AAOdds = AAOdds,
                ADOdds = ADOdds,
            };
        }

        internal override bool IsIntegrity()
        {
            if (CouID == 0) return false;
            if (HHOdds == 0m
                && HAOdds == 0m
                && HDOdds == 0m
                && DHOdds == 0m
                && DAOdds == 0m
                && DDOdds == 0m
                && AHOdds == 0m
                && AAOdds == 0m
                && ADOdds == 0m
                )
                return false;
            return true;
        }
    }

    public class MarketCS : MarketEntityBase
    {
        public decimal Odds00 { get; set; }
        public decimal Odds01 { get; set; }
        public decimal Odds02 { get; set; }
        public decimal Odds03 { get; set; }
        public decimal Odds04 { get; set; }
        public decimal Odds10 { get; set; }
        public decimal Odds11 { get; set; }
        public decimal Odds12 { get; set; }
        public decimal Odds13 { get; set; }
        public decimal Odds14 { get; set; }
        public decimal Odds20 { get; set; }
        public decimal Odds21 { get; set; }
        public decimal Odds22 { get; set; }
        public decimal Odds23 { get; set; }
        public decimal Odds24 { get; set; }
        public decimal Odds30 { get; set; }
        public decimal Odds31 { get; set; }
        public decimal Odds32 { get; set; }
        public decimal Odds33 { get; set; }
        public decimal Odds34 { get; set; }
        public decimal Odds40 { get; set; }
        public decimal Odds41 { get; set; }
        public decimal Odds42 { get; set; }
        public decimal Odds43 { get; set; }
        public decimal Odds44 { get; set; }
        public decimal Odds50 { get; set; }
        public decimal Odds05 { get; set; }
        public decimal AnyOdds { get; set; }
        internal override bool CompareSet(MarketEntityBase meb)
        {
            var mk = meb as MarketCS;
            if (Odds00 == mk.Odds00
                && Odds01 == mk.Odds01
                && Odds02 == mk.Odds02
                && Odds03 == mk.Odds03
                && Odds04 == mk.Odds04
                && Odds10 == mk.Odds10
                && Odds11 == mk.Odds11
                && Odds12 == mk.Odds12
                && Odds13 == mk.Odds13
                && Odds14 == mk.Odds14
                && Odds20 == mk.Odds20
                && Odds21 == mk.Odds21
                && Odds22 == mk.Odds22
                && Odds23 == mk.Odds23
                && Odds24 == mk.Odds24
                && Odds30 == mk.Odds30
                && Odds31 == mk.Odds31
                && Odds32 == mk.Odds32
                && Odds33 == mk.Odds33
                && Odds34 == mk.Odds34
                && Odds40 == mk.Odds40
                && Odds41 == mk.Odds41
                && Odds42 == mk.Odds42
                && Odds43 == mk.Odds43
                && Odds44 == mk.Odds44
                && Odds50 == mk.Odds50
                && Odds05 == mk.Odds05
                && AnyOdds == mk.AnyOdds
                )
                return false;

            Odds00 = mk.Odds00;
            Odds01 = mk.Odds01;
            Odds02 = mk.Odds02;
            Odds03 = mk.Odds03;
            Odds04 = mk.Odds04;
            Odds10 = mk.Odds10;
            Odds11 = mk.Odds11;
            Odds12 = mk.Odds12;
            Odds13 = mk.Odds13;
            Odds14 = mk.Odds14;
            Odds20 = mk.Odds20;
            Odds21 = mk.Odds21;
            Odds22 = mk.Odds22;
            Odds23 = mk.Odds23;
            Odds24 = mk.Odds24;
            Odds30 = mk.Odds30;
            Odds31 = mk.Odds31;
            Odds32 = mk.Odds32;
            Odds33 = mk.Odds33;
            Odds34 = mk.Odds34;
            Odds40 = mk.Odds40;
            Odds41 = mk.Odds41;
            Odds42 = mk.Odds42;
            Odds43 = mk.Odds43;
            Odds44 = mk.Odds44;
            Odds50 = mk.Odds50;
            Odds05 = mk.Odds05;
            AnyOdds = mk.AnyOdds;
            return true;
        }
        internal override MarketBase ToTakeMarketDto(int matchID, int takeType)
        {
            return new TakeCsDto()
            {
                MatchID = matchID,
                TakeType = takeType,
                MarketID = MarketID,
                CouID = MarketTools.CreateCouID(matchID, MarketID, 0),
                Odds00 = Odds00,
                Odds01 = Odds01,
                Odds02 = Odds02,
                Odds03 = Odds03,
                Odds04 = Odds04,
                Odds10 = Odds10,
                Odds11 = Odds11,
                Odds12 = Odds12,
                Odds13 = Odds13,
                Odds14 = Odds14,
                Odds20 = Odds20,
                Odds21 = Odds21,
                Odds22 = Odds22,
                Odds23 = Odds23,
                Odds24 = Odds24,
                Odds30 = Odds30,
                Odds31 = Odds31,
                Odds32 = Odds32,
                Odds33 = Odds33,
                Odds34 = Odds34,
                Odds40 = Odds40,
                Odds41 = Odds41,
                Odds42 = Odds42,
                Odds43 = Odds43,
                Odds44 = Odds44,
                Odds50 = Odds50,
                Odds05 = Odds05,
                AnyOdds = AnyOdds
            };
        }

        internal override bool IsIntegrity()
        {
            if (CouID == 0) return false;
            if (Odds00 == 0m
                && Odds01 == 0m
                && Odds02 == 0m
                && Odds03 == 0m
                && Odds04 == 0m
                && Odds10 == 0m
                && Odds11 == 0m
                && Odds12 == 0m
                && Odds13 == 0m
                && Odds14 == 0m
                && Odds20 == 0m
                && Odds21 == 0m
                && Odds22 == 0m
                && Odds23 == 0m
                && Odds24 == 0m
                && Odds30 == 0m
                && Odds31 == 0m
                && Odds32 == 0m
                && Odds33 == 0m
                && Odds34 == 0m
                && Odds40 == 0m
                && Odds41 == 0m
                && Odds42 == 0m
                && Odds43 == 0m
                && Odds44 == 0m
                && Odds50 == 0m
                && Odds05 == 0m
                && AnyOdds == 0m
                )
                return false;
            return true;
        }
    }
}
