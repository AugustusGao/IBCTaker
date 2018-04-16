using ML.EGP.Sport.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QIC.Sport.Odds.Collector.Cache.CacheEntity;

namespace QIC.Sport.Odds.Collector.Ibc.Dto
{
    public class SrcMarketEntityBase
    {
        public string SrcMatchId { get; set; }
        public string SrcCouID { get; set; }
        public string Bettype { get; set; }
        public string OddsStatus { get; set; }
        public int MarketID { get; set; }

        internal virtual void SetOdds(string[] oddsArr) { }

        internal virtual MarketEntityBase ToMarketEntity(int matchID, int stage) { return null; }

    }

    internal class SrcMarketTwo : SrcMarketEntityBase
    {
        public decimal? Hdp { get; set; }
        public decimal? HomeOdds { get; set; }
        public decimal? AwayOdds { get; set; }

        internal override void SetOdds(string[] oddsArr)
        {
            //Hdp = GetHdp(oddsArr[1]);
            decimal o;
            Hdp = string.IsNullOrEmpty(oddsArr[1]) ? Hdp : decimal.TryParse(oddsArr[1], out o) ? o : (decimal?)null;
            HomeOdds = string.IsNullOrEmpty(oddsArr[2]) ? HomeOdds : decimal.TryParse(oddsArr[2], out o) ? o : (decimal?)null;
            AwayOdds = string.IsNullOrEmpty(oddsArr[3]) ? AwayOdds : decimal.TryParse(oddsArr[3], out o) ? o : (decimal?)null;
            OddsStatus = string.IsNullOrEmpty(oddsArr[4]) ? OddsStatus = OddsStatus : OddsStatus = oddsArr[4];
        }

        internal override MarketEntityBase ToMarketEntity(int matchID, int stage)
        {
            //if (OddsStatus == "closePrice") return null;
            if (!Hdp.HasValue || !HomeOdds.HasValue || !AwayOdds.HasValue) return null;
            if (Hdp.Value == 0 && HomeOdds.Value == 0 && AwayOdds.Value == 0) return null;

            return new MarketTwo()
            {
                CouID = MarketTools.CreateCouID(matchID, MarketID, Hdp.GetValueOrDefault()),
                MarketID = MarketID,
                Stage = stage,
                Hdp = Hdp.Value,
                HomeOdds = HomeOdds.Value,
                AwayOdds = AwayOdds.Value
            };
        }
    }
    internal class SrcMarket1X2 : SrcMarketEntityBase
    {
        public decimal? Draw { get; set; }
        public decimal? HomeOdds { get; set; }
        public decimal? AwayOdds { get; set; }

        internal override void SetOdds(string[] oddsArr)
        {
            decimal o;
            HomeOdds = string.IsNullOrEmpty(oddsArr[1]) ? HomeOdds : decimal.TryParse(oddsArr[1], out o) ? o : (decimal?)null;
            Draw = string.IsNullOrEmpty(oddsArr[2]) ? Draw : decimal.TryParse(oddsArr[2], out o) ? o : (decimal?)null;
            AwayOdds = string.IsNullOrEmpty(oddsArr[3]) ? AwayOdds : decimal.TryParse(oddsArr[3], out o) ? o : (decimal?)null;
            OddsStatus = string.IsNullOrEmpty(oddsArr[4]) ? OddsStatus = OddsStatus : OddsStatus = oddsArr[4];
        }

        internal override MarketEntityBase ToMarketEntity(int matchID, int stage)
        {
            if (OddsStatus == "closePrice") return null;
            if (!Draw.HasValue || !HomeOdds.HasValue || !AwayOdds.HasValue) return null;
            if (Draw.Value == 0 && HomeOdds.Value == 0 && AwayOdds.Value == 0) return null;

            return new Market1x2()
            {
                CouID = MarketTools.CreateCouID(matchID, MarketID, 0),
                MarketID = MarketID,
                Stage = stage,
                HomeOdds = HomeOdds.Value,
                DrawOdds = Draw.Value,
                AwayOdds = AwayOdds.Value
            };
        }

    }
    internal class SrcMarketTotalGoal : SrcMarketEntityBase
    {
        public decimal? Odds01 { get; set; }
        public decimal? Odds23 { get; set; }
        public decimal? Odds46 { get; set; }
        public decimal? Odds70 { get; set; }
    }
    internal class SrcMarketFglg : SrcMarketEntityBase
    {
        public decimal? HFOdds { get; set; }
        public decimal? HLOdds { get; set; }
        public decimal? AFOdds { get; set; }
        public decimal? ALOdds { get; set; }
        public decimal? NOOdds { get; set; }
    }
    internal class SrcMarketHtft : SrcMarketEntityBase
    {
        public decimal? HHOdds { get; set; }
        public decimal? HAOdds { get; set; }
        public decimal? HDOdds { get; set; }
        public decimal? DHOdds { get; set; }
        public decimal? DAOdds { get; set; }
        public decimal? DDOdds { get; set; }
        public decimal? AHOdds { get; set; }
        public decimal? AAOdds { get; set; }
        public decimal? ADOdds { get; set; }
    }
    internal class SrcMarketCS : SrcMarketEntityBase
    {
        public decimal? Odds00 { get; set; }
        public decimal? Odds01 { get; set; }
        public decimal? Odds02 { get; set; }
        public decimal? Odds03 { get; set; }
        public decimal? Odds04 { get; set; }
        public decimal? Odds10 { get; set; }
        public decimal? Odds11 { get; set; }
        public decimal? Odds12 { get; set; }
        public decimal? Odds13 { get; set; }
        public decimal? Odds14 { get; set; }
        public decimal? Odds20 { get; set; }
        public decimal? Odds21 { get; set; }
        public decimal? Odds22 { get; set; }
        public decimal? Odds23 { get; set; }
        public decimal? Odds24 { get; set; }
        public decimal? Odds30 { get; set; }
        public decimal? Odds31 { get; set; }
        public decimal? Odds32 { get; set; }
        public decimal? Odds33 { get; set; }
        public decimal? Odds34 { get; set; }
        public decimal? Odds40 { get; set; }
        public decimal? Odds41 { get; set; }
        public decimal? Odds42 { get; set; }
        public decimal? Odds43 { get; set; }
        public decimal? Odds44 { get; set; }
        public decimal? Odds50 { get; set; }
        public decimal? Odds05 { get; set; }
        public decimal? AnyOdds { get; set; }
    }
}
