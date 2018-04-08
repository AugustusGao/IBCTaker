using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QIC.Sport.Odds.Collector.Ibc.OddsManager
{
    public class KeepOddsMatch
    {
        ConcurrentDictionary<int, List<string>> dicOddsId = new ConcurrentDictionary<int, List<string>>();
        public void DeleteOddsIdList(int marketID, List<string> list)
        {
            List<string> oddsIdList;
            if (dicOddsId.TryGetValue(marketID, out oddsIdList))
            {
                oddsIdList.RemoveAll(list.Contains);
            }
        }

        public void UpdateOddsIdList(int marketID, List<string> list)
        {
            List<string> oddsIdList;
            if (!dicOddsId.TryGetValue(marketID, out oddsIdList))
            {
                dicOddsId.TryAdd(marketID, list);
            }
            else
            {
                dicOddsId.AddOrUpdate(marketID, new List<string>(), (k, v) =>
                {
                    var addList = v.Except(list).ToList();
                    if (addList.Any())
                        v.AddRange(addList);
                    return v;
                });
            }
        }

        public List<string> GetMarketList(int marketId = 0)
        {
            List<string> list;
            if (marketId == 0)
            {
                list = new List<string>();
                foreach (var value in dicOddsId.Values)
                {
                    list.AddRange(value);
                }
            }
            else
            {
                dicOddsId.TryGetValue(marketId, out list);
            }

            return list;
        }
        public bool GetRowNum(out int rowNum, out int htRowNum)
        {
            rowNum = dicOddsId.Where(kv => kv.Key == 1 || kv.Key == 3).Max(kv => kv.Value.Count);
            htRowNum = dicOddsId.Where(kv => kv.Key == 2 || kv.Key == 4).Max(kv => kv.Value.Count);
            return true;
        }
    }
}
