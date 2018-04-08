﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QIC.Sport.Odds.Collector.Ibc.Dto;

namespace QIC.Sport.Odds.Collector.Ibc.OddsManager
{
    public class KeepOdds
    {
        ConcurrentDictionary<string, KeepOddsMatch> dicSrcMatch = new ConcurrentDictionary<string, KeepOddsMatch>();
        ConcurrentDictionary<string, SrcMarketEntityBase> dicMarket = new ConcurrentDictionary<string, SrcMarketEntityBase>();

        public KeepOddsMatch GetOrAdd(string srcMatchID)
        {
            KeepOddsMatch kom;
            if (!dicSrcMatch.TryGetValue(srcMatchID, out kom))
            {
                kom = new KeepOddsMatch();
                dicSrcMatch.TryAdd(srcMatchID, kom);
            }

            return kom;
        }
        
        public void UpdateMarket(string srcOddsId, string[] oddsArry)
        {

        }

        public void RemoveSrcMatchId(string srcMatchId)
        {
            KeepOddsMatch k;
            dicSrcMatch.TryRemove(srcMatchId, out k);
        }
        public T AddOrGetMarket<T>(string oddsId) where T : SrcMarketEntityBase, new()
        {
            SrcMarketEntityBase b;
            if (!dicMarket.TryGetValue(oddsId, out b))
            {
                b = new T();
                dicMarket.TryAdd(oddsId, b);
            }

            return b as T;
        }

        public bool OddsIdExist(string oddsId)
        {
            return dicMarket.ContainsKey(oddsId);
        }

        public T GetMarket<T>(string oddsId) where T : SrcMarketEntityBase
        {
            SrcMarketEntityBase b;
            dicMarket.TryGetValue(oddsId, out b);
            return b as T;
        }
    }
}
