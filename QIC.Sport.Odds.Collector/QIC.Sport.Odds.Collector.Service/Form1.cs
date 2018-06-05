using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using log4net;
using Microsoft.Practices.ObjectBuilder2;
using ML.EGP.Sport.Common.Enums;
using ML.Infrastructure.Config;
using ML.Infrastructure.IOC;
using QIC.Sport.Inplay.Collector.Core;
using QIC.Sport.Odds.Collector.Cache.CacheManager;
using QIC.Sport.Odds.Collector.Common;
using QIC.Sport.Odds.Collector.Core;
using QIC.Sport.Odds.Collector.Core.SubscriptionManager;
using QIC.Sport.Odds.Collector.Ibc;
using QIC.Sport.Odds.Collector.Ibc.Dto;
using QIC.Sport.Odds.Collector.Ibc.Handle;
using QIC.Sport.Odds.Collector.Ibc.OddsManager;
using QIC.Sport.Odds.Collector.Ibc.Param;
using QIC.Sport.Odds.Collector.Ibc.TimeManager;
using QIC.Sport.Odds.Collector.Ibc.Tools;
using QIC.Sport.Odds.Collector.Resources;
using QIC.Sport.Odds.Collector.Resources.Dto;

namespace QIC.Sport.Odds.Collector.Service
{
    public partial class Form1 : Form
    {
        private TextBoxWriter tw;
        private System.Threading.Mutex _mutex;
        private IReptile reptile;
        private IbcWorkManager workManager;
        private ISubscriptionManager subsManager;
        private IMatchEntityManager matchManager;
        private ILog logger = LogManager.GetLogger(typeof(Form1));
        private Dictionary<int, UrlLine> dicUrlLine = new Dictionary<int, UrlLine>();
        private Dictionary<int, ServerLine> dicServerLine = new Dictionary<int, ServerLine>();
        private int takeType = ConfigSingleton.CreateInstance().GetAppConfig<int>("CollectorType");
        private int[] CurrentState;
        private string user;
        private string passWord;
        public Form1()
        {
            InitializeComponent();
            tw = new TextBoxWriter(this.textBox1);
            Console.SetOut(tw);
            InitForm();
        }

        private void InitForm()
        {
            try
            {
                Check();
                var account = ResourcesProvider.GetAccount();
                if (account != null)
                {
                    user = account[0];
                    passWord = account[1];
                    textBox2.Text = user;
                    textBox3.Text = passWord;
                }
                if (takeType == 3 && (account == null || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(passWord))) MessageBox.Show("Need user and password, set first and restart please!");

                SetLine(takeType);
                if (!dicServerLine.Any())
                {
                    MessageBox.Show("Set ServerLine and Restart!");
                    return;
                }
                if (!dicUrlLine.Any())
                {
                    MessageBox.Show("Add Url and Restart!");
                    return;
                }
                if (CurrentState == null)
                {
                    MessageBox.Show("Select Server and  Url  Restart!");
                    return;
                }
                else
                {
                    comboBox1.SelectedIndex = CurrentState[0];
                    comboBox2.SelectedIndex = CurrentState[1];
                }
            }
            catch (Exception e)
            {
                logger.Error(e.ToString());
            }
        }
        private void btnInit_Click(object sender, EventArgs e)
        {
            var url = comboBox2.Text;
            var str = (string)comboBox1.SelectedItem;
            Task.Run(() =>
            {
                reptile = Reptile.Instance();

                Console.WriteLine("Init WorkManager");
                workManager = new IbcWorkManager();
                workManager.Init();

                var id = Convert.ToInt32(str.Substring(4));
                reptile.Init(workManager, dicServerLine[id].ServerIP, dicServerLine[id].ServerPort);
                reptile.Start();
                subsManager = IocUnity.GetService<ISubscriptionManager>("SubscriptionManager");
                matchManager = IocUnity.GetService<IMatchEntityManager>("MatchEntityManager");

                if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(passWord))
                {
                    Console.WriteLine("Has no user or passWord");
                    return;
                }

                var loginParam = new LoginParam()
                {
                    Username = user,
                    Password = passWord,
                    WebUrl = url,
                    TakeMode = TakeMode.Pull
                };
                subsManager.Subscribe(loginParam);

                var ibcSyncTimeParam = new NormalParam()
                {
                    Stage = (int)MatchStageEnum.Live,
                    TakeMode = TakeMode.Push,
                    SubscribeType = "time",
                    TimeParam = new SyncTimeParam()
                    {
                        id = "c1",
                        rev = 0,
                        condition = CommonTools.ToUnixTimeSpan(DateTime.Now) + ""
                    }
                };
                subsManager.Subscribe(ibcSyncTimeParam);
            });
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            //btnTest_Click(sender, e); return;
            Task.Run(() =>
            {
                GetAllIbcParam().ForEach(p => subsManager.Subscribe(p));
                Console.WriteLine("Start Ok.");
            });

        }
        private List<NormalParam> GetAllIbcParam()
        {
            List<NormalParam> list = new List<NormalParam>();
            SocketParam sp = new SocketParam()
            {
                id = "c13",
                rev = 0,
                condition = new Condition()
                {
                    marketid = "L",
                    sporttype = new[] { 1 },
                    bettype = new[] { 1, 3, 5, 7, 8, 15 },
                    sorting = "n"
                }
            };
            list.Add(new NormalParam() { Stage = (int)MatchStageEnum.Live, TakeMode = TakeMode.Push, SocketParam = sp });

            sp = new SocketParam()
            {
                id = "c12",
                rev = 0,
                condition = new Condition()
                {
                    marketid = "T",
                    sporttype = new[] { 1 },
                    bettype = new[] { 1, 3, 5, 7, 8, 15 },
                    sorting = "n"
                }
            };
            list.Add(new NormalParam() { Stage = (int)MatchStageEnum.Today, TakeMode = TakeMode.Push, SocketParam = sp });

            sp = new SocketParam()
            {
                id = "c11",
                rev = 0,
                condition = new Condition()
                {
                    marketid = "E",
                    sporttype = new[] { 1 },
                    bettype = new[] { 1, 3, 5, 7, 8, 15 },
                    sorting = "n"
                }
            };
            list.Add(new NormalParam() { Stage = (int)MatchStageEnum.Early, TakeMode = TakeMode.Push, SocketParam = sp });

            //  其他运动参数
            //  现在IBC不支持同时订阅其他运动，只能一种运动一个订阅
            var ibcSportIds = new[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 25, 26, 43, 50 };

            var otherLives = ibcSportIds.Select(id => new SocketParam()
            {
                id = "c23" + id,
                rev = 0,
                condition = new Condition()
                {
                    marketid = "L",
                    sporttype = new[] { id },
                    bettype = new[] { 1, 2, 3, 5, 7, 8, 12, 15, 20, 609, 610, 611 },
                    sorting = "n"
                }
            }).ToList();
            otherLives.ForEach(p => list.Add(new NormalParam() { Stage = (int)MatchStageEnum.Live, TakeMode = TakeMode.Push, SocketParam = p }));

            var otherTodays = ibcSportIds.Select(id => new SocketParam()
            {
                id = "c22" + id,
                rev = 0,
                condition = new Condition()
                {
                    marketid = "T",
                    sporttype = new[] { id },
                    bettype = new[] { 1, 2, 3, 5, 7, 8, 12, 15, 20, 609, 610, 611 },
                    sorting = "n"
                }
            }).ToList();
            otherTodays.ForEach(p => list.Add(new NormalParam() { Stage = (int)MatchStageEnum.Today, TakeMode = TakeMode.Push, SocketParam = p }));

            var otherEarlys = ibcSportIds.Select(id => new SocketParam()
            {
                id = "c21" + id,
                rev = 0,
                condition = new Condition()
                {
                    marketid = "E",
                    sporttype = new[] { id },
                    bettype = new[] { 1, 2, 3, 5, 7, 8, 12, 15, 20, 609, 610, 611 },
                    sorting = "n"
                }
            }).ToList();
            otherEarlys.ForEach(p => list.Add(new NormalParam() { Stage = (int)MatchStageEnum.Early, TakeMode = TakeMode.Push, SocketParam = p }));

            return list;
        }

        private void btnEditUrl_Click(object sender, EventArgs e)
        {
            try
            {
                var uef = new UrlEditorForm();
                uef.ShowDialog();
                ShowUrls(uef.lines);

                MessageBox.Show("Edit complete !\n Need to re-select and restart!");
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
            }
        }
        private void ShowUrls(List<UrlLine> lines)
        {
            dicUrlLine = lines.ToDictionary(o => o.LineID, o => o);
            comboBox2.Items.Clear();
            comboBox2.Text = "";
            foreach (var value in dicUrlLine.Values)
            {
                comboBox2.Items.Add(value.Url);
            }
            if (comboBox2.Items.Count > 0) comboBox2.SelectedIndex = 0;
            ResourcesProvider.SetLineState(new[] { comboBox1.SelectedIndex, comboBox2.SelectedIndex });
            dicUrlLine = lines.ToDictionary(o => o.LineID, o => o);
        }
        private void SetLine(int takeType)
        {
            dicServerLine = ResourcesProvider.GetServerLines() == null ? dicServerLine : ResourcesProvider.GetServerLines().ToDictionary(o => o.LineID, o => o);
            dicUrlLine = ResourcesProvider.GetUrlLines() == null ? dicUrlLine : ResourcesProvider.GetUrlLines().Where(o => o.TakeType == takeType).ToDictionary(o => o.LineID, o => o);
            CurrentState = ResourcesProvider.GetServerIndexAndUrlIndex();
            if (dicServerLine != null && dicServerLine.Any()) dicServerLine.Values.ForEach(v => comboBox1.Items.Add(string.Format("Line {0}", v.LineID)));
            if (dicUrlLine != null && dicUrlLine.Any()) dicUrlLine.Values.ForEach(v => comboBox2.Items.Add(v.Url));

        }
        private void Check()
        {
            bool ret;
            _mutex = new System.Threading.Mutex(true, Application.ProductName + takeType, out ret);
            if (!ret)
            {
                MessageBox.Show(null, "The Taker has been Started,Warning !!!\n\nPlease Exit Now!!!", Application.ProductName + takeType, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //   提示信息，可以删除。   
                System.Environment.Exit(System.Environment.ExitCode);//退出程序   
            }
        }
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (reptile != null)
                reptile.Stop();
            System.Environment.Exit(System.Environment.ExitCode);
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            var path = @"C:\Users\Gaushee\Desktop\temp1.txt";
            var txt = File.ReadAllText(path);
            var handle = new NormalHandle();
            var pm = new NormalParam()
            {
                Stage = 3,
                TakeMode = TakeMode.Push,
                SocketParam = new SocketParam() { condition = new Condition() { sporttype = new[] { 1 } } }
            };
            var data = new PushDataDto() { Data = txt, Param = pm, };
            handle.ProcessData(data);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            var p = GetAllIbcParam().FirstOrDefault(o => o.Stage == 3 && o.SocketParam.condition.sporttype.Contains(1));
            CheckedAndSubscribe(checkBox1.Checked, p);
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            var p = GetAllIbcParam().FirstOrDefault(o => o.Stage == 2 && o.SocketParam.condition.sporttype.Contains(1));
            CheckedAndSubscribe(checkBox2.Checked, p);
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            var p = GetAllIbcParam().FirstOrDefault(o => o.Stage == 1 && o.SocketParam.condition.sporttype.Contains(1));
            CheckedAndSubscribe(checkBox3.Checked, p);
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            var p = GetAllIbcParam().Where(o => o.Stage == 3 && !o.SocketParam.condition.sporttype.Contains(1)).ToList();
            p.ForEach(o => CheckedAndSubscribe(checkBox5.Checked, o));
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            var p = GetAllIbcParam().Where(o => o.Stage == 2 && !o.SocketParam.condition.sporttype.Contains(1)).ToList();
            p.ForEach(o => CheckedAndSubscribe(checkBox5.Checked, o));
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            var p = GetAllIbcParam().Where(o => o.Stage == 1 && !o.SocketParam.condition.sporttype.Contains(1)).ToList();
            p.ForEach(o => CheckedAndSubscribe(checkBox5.Checked, o));
        }

        private void CheckedAndSubscribe(bool isChecked, NormalParam param)
        {
            if (isChecked)
            {
                subsManager.Subscribe(param);
            }
            else
            {
                subsManager.UnSubscribe(param);

                var ko = KeepOddsManager.Instance.AddOrGetKeepOdds(param.Stage);
                var closeSportId = param.SocketParam.condition.sporttype.Select(o => IbcTools.ConvertToSportId(o + "")).ToList();
                matchManager.ForEach(me =>
                {
                    if (me.Stage == param.Stage && closeSportId.Contains(me.SportID))
                    {
                        ko.RemoveBySrcMatchId(me.SrcMatchID); if (me.Stage == 3)
                            LiveInfoManager.Instance.RemoveBySrcMatchId(me.SrcMatchID, me.Stage);
                        me.MatchDisappear(me.Stage, param.LimitMarketIdList, true);
                    }
                });
            }
        }
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            CurrentState = new[] { comboBox1.SelectedIndex, CurrentState[1] };
            ResourcesProvider.SetLineState(CurrentState);
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            CurrentState = new[] { comboBox1.SelectedIndex, comboBox2.SelectedIndex };
            ResourcesProvider.SetLineState(CurrentState);
        }

        private void btnSaveAccount_Click(object sender, EventArgs e)
        {
            user = textBox2.Text;
            passWord = textBox3.Text;
            textBox2.Text = user;
            textBox3.Text = passWord;
            ResourcesProvider.AddOrUpdateAccount(new[] { user, passWord });
            MessageBox.Show("Save Ok!");
        }
    }
    public class TextBoxWriter : System.IO.TextWriter
    {
        private TextBox tbox;
        delegate void VoidAction();
        private string searchKey = null;

        public TextBoxWriter(TextBox box)
        {
            tbox = box;
        }

        public override void Write(string value)
        {
            VoidAction action = delegate
            {
                tbox.AppendText(value);
            };
            try
            {
                tbox.BeginInvoke(action);
            }
            catch (Exception)
            {

            }

        }

        public override void WriteLine(string value)
        {
            VoidAction action = delegate
            {
                if (!string.IsNullOrEmpty(searchKey) && !value.Contains(searchKey)) return;
                if (tbox.Lines.Length > 200) tbox.Clear();
                tbox.AppendText(value + "\r\n");
                if (value.Contains("MessageBox|")) MessageBox.Show(value.Replace("MessageBox|", ""));
            };
            try
            {
                tbox.BeginInvoke(action);
            }
            catch (Exception)
            {


            }

        }

        public override System.Text.Encoding Encoding
        {
            get { return System.Text.Encoding.UTF8; }
        }

        public void SetSearchKey(string key)
        {
            searchKey = key;
        }
    }
}
