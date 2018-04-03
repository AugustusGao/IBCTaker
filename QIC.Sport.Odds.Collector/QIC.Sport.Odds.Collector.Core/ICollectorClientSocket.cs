using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QIC.Sport.Inplay.Collector.Core
{
    public interface ICommunicator
    {
        void Send<T>(int cmd, T data) where T : class;

    }
}
