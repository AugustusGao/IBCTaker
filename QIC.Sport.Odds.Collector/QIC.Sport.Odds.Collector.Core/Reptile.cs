using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using ML.EGP.Sport.CommandProtocol;
using ML.EGP.Sport.CommandProtocol.Command;
using ML.EGP.Sport.CommandProtocol.Dto.MarketProvider;
using ML.EGP.Sport.CommandProtocol.Dto.TakeServer;
using ML.EGP.Sport.Common;
using ML.Infrastructure.Config;
using ML.Infrastructure.IOC;
using ML.NetComponent.Tcp;
using QIC.Sport.Inplay.Collector.Core;
using QIC.Sport.Odds.Collector.Cache;
using QIC.Sport.Odds.Collector.Cache.CacheManager;
using QIC.Sport.Odds.Collector.Core.MatchWorkerManager;

namespace QIC.Sport.Odds.Collector.Core
{
    public class Reptile : IReptile, ISocketSink, ICommunicator
    {
        private ILog logger = LogManager.GetLogger(typeof(Reptile));
        private static readonly object lockObj = new object();
        private readonly int collectorType = ConfigSingleton.CreateInstance().GetAppConfig<int>("CollectorType");
        private string serverIP = null;
        private string serverPort = null;
        private IMatchWorkManager matchWorkManager;
        private IMatchEntityManager matchEntityManager;
        private EventSocket socket;//盘口服务器连接socket
        private bool socketConnected = false;
        private static Reptile instance;

        public static IReptile Instance()
        {

            if (instance == null)
            {
                lock (lockObj)
                {
                    if (instance == null)
                    {
                        instance = new Reptile();
                    }
                }
            }
            return instance;
        }
        private Reptile()
        {
            var mem = new MatchEntityManager();
            mem.Init(this);
            matchEntityManager = mem;
            IocUnity.RegisterInstance("MatchEntityManager", matchEntityManager);
            socket = new EventSocket();
            socket.SocketSink = this;
            socket.ServerIP = ConfigSingleton.CreateInstance().GetAppConfig<string>("ServerIP");
            socket.ServerPort = ConfigSingleton.CreateInstance().GetAppConfig<ushort>("ServerPort");
        }
        public void Start()
        {
            socket.AutoConnect = true;
            socket.ServerIP = string.IsNullOrEmpty(serverIP) ? socket.ServerIP : serverIP;
            socket.ServerPort = string.IsNullOrEmpty(serverPort) ? socket.ServerPort : Convert.ToUInt16(serverPort);
            Console.WriteLine("Start Connect Central Server ...");
            socket.Connect();
            matchWorkManager.Start();
        }

        public void Stop()
        {
            matchWorkManager.Stop();
        }

        public void Init(IMatchWorkManager workManager, string ip, string port)
        {
            serverIP = ip;
            serverPort = port;
            matchWorkManager = workManager;
        }

        public bool OnConnect()
        {
            try
            {
                var code = ConfigSingleton.CreateInstance().GetAppConfig<string>("ValidCode");
                Console.WriteLine("Collector Socket Connected Success!");

                //发送登录信息
                TakeServerLoginDto tslLoginDto = new TakeServerLoginDto() { TakeType = collectorType, ValidCode = code };
                string data = ProtocalSerialize.SerializeObject(tslLoginDto);
                ProtocolLogger.WriteLog<TakeServerLoginDto>(tslLoginDto, MainCommand.TakeServer, (ushort)TakeServerCommand.Login, LogerType.Info);

                socket.Send((ushort)MainCommand.TakeServer, (ushort)TakeServerCommand.Login, data);
                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
            }
            return true;
        }

        public bool OnNCRead(ushort mainCmd, ushort subCmd, string data)
        {
            try
            {
                if ((MainCommand)mainCmd != MainCommand.TakeServer) return false;

                switch ((TakeServerCommand)subCmd)
                {
                    case TakeServerCommand.LoginFault:
                        Console.WriteLine("Central socket connection failed");
                        var tsLoginDtoFault = ProtocalSerialize.UnSerializeObject<TakeServerLoginDto>(data);
                        ProtocolLogger.WriteLog(tsLoginDtoFault, (MainCommand)mainCmd, subCmd, LogerType.Info);
                        break;
                    case TakeServerCommand.LoginSuccess:
                        Console.WriteLine("Central socket connection success");
                        var tsLoginDtoSuccess = ProtocalSerialize.UnSerializeObject<TakeServerLoginDto>(data);
                        ProtocolLogger.WriteLog(tsLoginDtoSuccess, (MainCommand)mainCmd, subCmd, LogerType.Info);
                        socketConnected = true;
                        //发送当前比赛管理下 所有的比赛数据（断线重连）
                        //更改为重连后发送所有已经配对的比赛，使其重发完整数据  MatchManager.ForEach(o => o.SendAll());
                        break;
                    case TakeServerCommand.MatchLink:
                        var tmLinkDto = ProtocalSerialize.UnSerializeObject<TakeMatchLinkDto>(data);
                        if (tmLinkDto.TakeType != collectorType) break;
                        ProtocolLogger.WriteLog(tmLinkDto, (MainCommand)mainCmd, subCmd, LogerType.Info);
                        matchEntityManager.MatchLink(tmLinkDto.SrcMatchID, tmLinkDto.MatchID);
                        break;
                    case TakeServerCommand.MatchReset:
                        var tmResetDto = ProtocalSerialize.UnSerializeObject<TakeMatchResetDto>(data);
                        ProtocolLogger.WriteLog(tmResetDto, (MainCommand)mainCmd, subCmd, LogerType.Info);
                        var srcMatchID = matchEntityManager.GetSrcMatchID(tmResetDto.MatchID);
                        matchEntityManager.Reset(srcMatchID);
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
            }
            return true;
        }

        public bool OnWSRead(string data)
        {
            throw new NotImplementedException();
        }

        public bool OnClose()
        {
            Console.WriteLine("Disconnect service !!!");
            logger.Error("Disconnect service !!!");
            socketConnected = false;
            return true;
        }

        public void Send<T>(TakeServerCommand tcmd, T data) where T : class ,new()
        {
            ProtocolLogger.WriteLog(data, MainCommand.TakeServer, (ushort)tcmd, LogerType.Info);
            if (socketConnected)
            {
                socket.Send((ushort)MainCommand.TakeServer, (ushort)tcmd, ProtocalSerialize.SerializeObject(data));
            }
        }
        public void CustomServer(string ip, string port)
        {
            serverIP = ip;
            serverPort = port;
        }
    }
}
