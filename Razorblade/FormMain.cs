using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace Razorblade
{
    public partial class FormMain : Form
    {
        public Random rand = new Random();
        private Target target;
        private Config config;

        public const int BUILD = 20150722;

        public FormMain()
        {
            InitializeComponent();

            FormClosing += Form1_FormClosing;
        }

        void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Text += " (build: " + BUILD.ToString() + ")";
            if (File.Exists("config.xml"))
            {
                try
                {
                    XmlSerializer ser = new XmlSerializer(typeof(Config));

                    using (FileStream fs = new FileStream("config.xml", FileMode.Open))
                    {
                        config = (Config)ser.Deserialize(fs);
                    }
                }
                catch(InvalidOperationException)
                {
                    if (MessageBox.Show("Configuration file is corrupted. Delete and create a new one?", "Error!", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == System.Windows.Forms.DialogResult.Yes)
                    {
                        File.Delete("config.xml");
                        config = new Config();
                    }
                }
            }
            else
            {
                config = new Config();
            }

            Util.Init(richTextBox1, treeView1);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!comboBox1.Items.Contains(comboBox1.Text))
            {
                comboBox1.Items.Add(comboBox1.Text);
            }

            treeView1.Nodes.Clear();
            richTextBox1.Clear();

            if (!comboBox1.Text.StartsWith("http"))
            {
                comboBox1.Text = "http://" + comboBox1.Text;
            }

            if (comboBox1.Text.Contains("=") && !comboBox1.Text.EndsWith("="))
            {
                comboBox1.Text = comboBox1.Text.Substring(0, comboBox1.Text.LastIndexOf('=') + 1);
            }

            target = new Target(comboBox1.Text, config);
            target.ScanStateChanged += target_ScanStateChanged;

            ThreadPool.QueueUserWorkItem(new WaitCallback(target.GetAll));
        }

        void target_ScanStateChanged(object sender, ScanStateEventArgs e)
        {
            if (e.Scanning)
            {
                if (button1.InvokeRequired)
                {
                    button1.BeginInvoke(new Action(() =>
                    {
                        button1.Text = "Scanning...";
                        button1.Enabled = false;
                    }));
                }
                else
                {
                    button1.Text = "Scanning...";
                    button1.Enabled = false;
                }
            }
            else
            {
                if (button1.InvokeRequired)
                {
                    button1.BeginInvoke(new Action(() =>
                    {
                        button1.Text = "Scan";
                        button1.Enabled = true;
                    }));
                }
                else
                {
                    button1.Text = "Scan";
                    button1.Enabled = true;
                }
            }
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            getTablesToolStripMenuItem.Visible = false;
            getColumnsToolStripMenuItem.Visible = false;
            getDataToolStripMenuItem.Visible = false;

            if (treeView1.SelectedNode != null)
            {
                if (treeView1.SelectedNode.Level == 0)
                {
                    getTablesToolStripMenuItem.Visible = true;
                }
                else if (treeView1.SelectedNode.Level == 1)
                {
                    getColumnsToolStripMenuItem.Visible = true;
                    getDataToolStripMenuItem.Visible = true;
                }
            }
        }

        private void getTablesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Action<object> doIt = new Action<object>((object obj) => { target.GetTables((Database)obj); });
            Thread sThread = new Thread(doIt.Invoke);
            
            sThread.Start(target.Databases.Single(x => x.Name == treeView1.SelectedNode.Name));
        }

        private void getColumnsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Action<object> doIt = new Action<object>((object obj) => { target.GetColumns((Table)obj); });
            Thread sThread = new Thread(doIt.Invoke);

            foreach(Database db in target.Databases)
            {
                foreach(Table tab in db.Tables)
                {
                    if (db.Name == treeView1.SelectedNode.Parent.Name && tab.Name == treeView1.SelectedNode.Name)
                    {
                        sThread.Start(tab);
                        return;
                    }
                }
            }
        }

        private void getDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Database db in target.Databases)
            {
                foreach (Table tab in db.Tables)
                {
                    if (db.Name == treeView1.SelectedNode.Parent.Name && tab.Name == treeView1.SelectedNode.Name)
                    {
                        FormData form = new FormData(tab);
                        form.Show();
                        return;
                    }
                }
            }
        }

        private void refreshDatabasesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            treeView1.Nodes.Clear();
            target.Databases.Clear();
            target.GetDatabases();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormSettings form = new FormSettings(config);
            form.Show();
        }

        private void ExportStructure(object obj)
        {
            if (!Directory.Exists(target.Host))
            {
                Directory.CreateDirectory(target.Host);
            }

            string path = target.Host + "\\structure.xml";

            using (StreamWriter w = new StreamWriter(path))
            {
                using (XmlTextWriter xml = new XmlTextWriter(w))
                {
                    xml.Formatting = Formatting.Indented;
                    xml.WriteStartDocument();
                    xml.WriteComment("Dump generated by Razorblade");
                    xml.WriteStartElement("Host");
                    xml.WriteAttributeString("name", target.Host);
                    xml.WriteAttributeString("url", target.URL);

                    foreach(Database db in target.Databases)
                    {
                        xml.WriteStartElement("Database");
                        xml.WriteAttributeString("name", db.Name);

                        foreach(Table tab in db.Tables)
                        {
                            xml.WriteStartElement("table");
                            xml.WriteAttributeString("name", tab.Name);

                            foreach(Column col in tab.Columns)
                            {
                                xml.WriteStartElement("column");
                                xml.WriteAttributeString("name", col.Name);
                                xml.WriteEndElement();
                            }

                            xml.WriteEndElement();
                        }

                        xml.WriteEndElement();
                    }

                    xml.WriteEndElement();
                }
            }

            MessageBox.Show("Database structure exported", "Success!", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void exportStructureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(ExportStructure));
        }
    }
}
