using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QIC.Sport.Odds.Collector.Core.Handle;

namespace QIC.Sport.Odds.Collector.Ibc.Handle
{
    public class HandleFactory : IHandleFactory
    {
        public IHandle CreateHandle(int dataType)
        {
            switch (dataType)
            {
                case (int)DataType.Normal: return new NormalHandle();
                default: return null;
            }
        }
    }
}
