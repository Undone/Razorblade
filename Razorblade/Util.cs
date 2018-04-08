using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace Razorblade
{
    public static class Util
    {
        private static Random rand = new Random();
        private static RichTextBox richBox;
        private static TreeView treeView;
        private static ListView listView;

        public static void Init(RichTextBox box, TreeView tv)
        {
            richBox = box;
            treeView = tv;
        }

        public static void InitData(ListView lv)
        {
            listView = lv;
        }

        public static void CloseData()
        {
            listView = null;
        }

        public static string Concat(string[] arr, string spacing = ",")
        {
            string str = "";

            for (int i = 0; i < arr.Length; i++)
            {
                str += arr[i];

                if (i < arr.Length - 1)
                {
                    str += spacing;
                }
            }

            return str;
        }

        public static void AddDB(string name)
        {
            if (treeView.InvokeRequired)
            {
                treeView.BeginInvoke(new Action(() =>
                {
                    treeView.BeginUpdate();
                    TreeNode tn = treeView.Nodes.Add(name);
                    tn.Name = name;
                    tn.SelectedImageIndex = 0;
                    tn.ImageIndex = 0;
                    treeView.EndUpdate();
                }));
            }
            else
            {
                treeView.BeginUpdate();
                TreeNode tn = treeView.Nodes.Add(name);
                tn.Name = name;
                tn.SelectedImageIndex = 0;
                tn.ImageIndex = 0;
                treeView.EndUpdate();
            }
        }

        public static void AddTable(string db, string name)
        {
            if (treeView.InvokeRequired)
            {
                treeView.BeginInvoke(new Action(() =>
                {
                    treeView.BeginUpdate();
                    TreeNode tn = treeView.Nodes[db].Nodes.Add(name);
                    tn.Name = name;
                    tn.ImageIndex = 1;
                    tn.SelectedImageIndex = 1;
                    treeView.EndUpdate();
                }));
            }
            else
            {
                treeView.BeginUpdate();
                TreeNode tn = treeView.Nodes[db].Nodes.Add(name);
                tn.Name = name;
                tn.ImageIndex = 1;
                tn.SelectedImageIndex = 1;
                treeView.EndUpdate();
            }
        }

        public static void AddData(Row row)
        {
            if (listView != null)
            {
                if (listView.InvokeRequired)
                {
                    listView.BeginInvoke(new Action(() =>
                    {
                        listView.BeginUpdate();
                        listView.Items.Add(row.Data[0]).SubItems.AddRange(row.Data.Skip(1).ToArray());
                        listView.EndUpdate();
                    }));
                }
                else
                {
                    listView.BeginUpdate();
                    listView.Items.Add(row.Data[0]).SubItems.AddRange(row.Data.Skip(1).ToArray());
                    listView.EndUpdate();
                }
            }
        }

        public static void AddColumn(string db, string tab, string name)
        {
            if (treeView.InvokeRequired)
            {
                treeView.BeginInvoke(new Action(() =>
                {
                    treeView.BeginUpdate();
                    TreeNode tn = treeView.Nodes[db].Nodes[tab].Nodes.Add(name);
                    tn.Name = name;
                    tn.ImageIndex = 2;
                    tn.SelectedImageIndex = 2;
                    treeView.EndUpdate();
                }));
            }
            else
            {
                treeView.BeginUpdate();
                TreeNode tn = treeView.Nodes[db].Nodes[tab].Nodes.Add(name);
                tn.Name = name;
                tn.ImageIndex = 2;
                tn.SelectedImageIndex = 2;
                treeView.EndUpdate();
            }
        }

        public static string GetBetween(string input, string str1, string str2, int index = 0)
        {
            string[] rgx = Regex.Split(input, str1);

            if (rgx.Length >= index + 2)
            {
                string temp = Regex.Split(input, str1)[index + 1];
                return Regex.Split(temp, str2)[0];
            }
            else
            {
                return "";
            }
        }

        public static int GetBetweenCount(string input, string str1)
        {
            return Regex.Split(input, str1).Length;
        }

        public static string GetASCII(string str)
        {
            if (str == "")
                return "";

            string tmp = "";
            byte[] ascii = Encoding.ASCII.GetBytes(str);

            foreach(byte b in ascii)
            {
                tmp += b.ToString() + ",";
            }

            return tmp.Substring(0, tmp.Length - 1);
        }

        public static void Log(string str, Color? col = null)
        {
            str += Environment.NewLine;

            if (richBox.InvokeRequired)
            {
                richBox.BeginInvoke(new Action(() =>
                {
                    if (col != null)
                    {
                        richBox.SelectionStart = richBox.TextLength;
                        richBox.SelectionLength = 0;
                        richBox.SelectionColor = (Color)col;
                    }

                    richBox.AppendText(str);

                    if (col != null)
                    {
                        richBox.SelectionColor = richBox.ForeColor;
                    }
                }));
            }
            else
            {
                if (col != null)
                {
                    richBox.SelectionStart = richBox.TextLength;
                    richBox.SelectionLength = 0;
                    richBox.SelectionColor = (Color)col;
                }

                richBox.AppendText(str);

                if (col != null)
                {
                    richBox.SelectionColor = richBox.ForeColor;
                }
            }
        }
    }
}
