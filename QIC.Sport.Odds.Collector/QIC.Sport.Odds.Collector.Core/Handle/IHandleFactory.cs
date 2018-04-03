using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QIC.Sport.Odds.Collector.Core.Handle
{
    public interface IHandleFactory
    {
        IHandle CreateHandle(int dataType);
    }
}
