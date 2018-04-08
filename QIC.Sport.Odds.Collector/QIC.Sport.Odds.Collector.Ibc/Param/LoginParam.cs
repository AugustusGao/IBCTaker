using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ML.Infrastructure.Config;
using QIC.Sport.Odds.Collector.Core.SubscriptionManager;

namespace QIC.Sport.Odds.Collector.Ibc.Param
{
    public class LoginParam : BaseParam, ISubscribeParam
    {
        private string webUrl = ConfigSingleton.CreateInstance().GetAppConfig<string>("Ibc");
        public string Username { get; set; }
        public string Password { get; set; }
        public string WebUrl
        {
            get { return webUrl; }
        }
        public string Key
        {
            get { return "Login"; }
        }
    }
}
