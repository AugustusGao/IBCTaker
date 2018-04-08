using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using log4net;
using QIC.Sport.Inplay.Collector.Core;
using QIC.Sport.Odds.Collector.Core;
using QIC.Sport.Odds.Collector.Ibc;

namespace QIC.Sport.Odds.Collector.Service
{
    public partial class Form1 : Form
    {
        private TextBoxWriter tw;
        private IReptile reptile;
        private IbcWorkManager workManager;
        private ILog logger = LogManager.GetLogger(typeof(Form1));
        public Form1()
        {
            InitializeComponent();
            tw = new TextBoxWriter(this.textBox1);
        }

        private void btnInit_Click(object sender, EventArgs e)
        {
            Console.SetOut(tw); 
            Task.Run(() =>
            {
                reptile = Reptile.Instance();

                Console.WriteLine("Init WorkManager");
                workManager = new IbcWorkManager();
                workManager.Init();

                reptile.Init(workManager);
            });
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                reptile.Start();
                Console.WriteLine("Start Ok.");
            });
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (reptile != null)
                reptile.Stop();
            System.Environment.Exit(System.Environment.ExitCode);
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
