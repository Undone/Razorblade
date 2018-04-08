using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.IO;

namespace Razorblade
{
    public partial class FormSettings : Form
    {
        private Config Config;

        public FormSettings(Config cfg)
        {
            InitializeComponent();
            Config = cfg;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Config.Scan_Columns_Max = Convert.ToInt32(numericUpDown1.Value);
            Config.Scan_Proxy = textBox1.Text;
            Config.Scan_UserAgent = textBox2.Text;
            Config.Scan_Proxy_Enabled = checkBox1.Checked;
            Config.Print_Debug = checkBox2.Checked;
            Config.Scan_Tables_Auto = checkBox3.Checked;

            XmlSerializer ser = new XmlSerializer(typeof(Config));

            using(StreamWriter wr = new StreamWriter("config.xml", false))
            {
                ser.Serialize(wr, Config);
            }

            this.Close();
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            numericUpDown1.Value = Config.Scan_Columns_Max;
            textBox1.Text = Config.Scan_Proxy;
            textBox2.Text = Config.Scan_UserAgent;
            checkBox1.Checked = Config.Scan_Proxy_Enabled;
            checkBox2.Checked = Config.Print_Debug;
            checkBox3.Checked = Config.Scan_Tables_Auto;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            textBox1.Enabled = checkBox1.Checked;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
