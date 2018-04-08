using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QIC.Sport.Odds.Collector.Common;

namespace QIC.Sport.Odds.Collector.Ibc.Param
{
    public class BaseParam
    {
        public TakeMode TakeMode;
        public DataType DataType;
        public bool IsSubscribed;
        public virtual string GetUrl() { return null; }
    }
}
