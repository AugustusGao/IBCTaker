using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QIC.Sport.Odds.Collector.Cache.CacheEntity;

namespace QIC.Sport.Odds.Collector.Cache.CacheManager
{
    public interface IMatchEntityManager
    {
        MatchEntity GetOrAdd(string srcMatchID, string srcLeague, string srcHome, string srcAway, DateTime srcMatchDate, int sportID, bool isSelfCreate = false);
        void MatchLink(string srcMatchID, int matchID);
        MatchEntity Get(string srcMatchID);
        int GetMatchID(string srcMatchID);
        string GetSrcMatchID(int MatchID);
        bool MatchExist(string srcMatchId);
        void ForEach(Action<MatchEntity> action);
        void Reset(string srcMatchId);
    }
}
