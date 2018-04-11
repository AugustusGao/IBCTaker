using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ML.EGP.Sport.CommandProtocol.Command;

namespace QIC.Sport.Odds.Collector.Cache
{
    public interface ICommunicator
    {
        void Send<T>(TakeServerCommand tcmd, T data) where T : class ,new();
    }
}
