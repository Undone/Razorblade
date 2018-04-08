using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Razorblade
{
    public class Config
    {
        public bool         Print_Debug             = false;

        public string       Scan_Proxy              = "http://localhost:8118";
        public bool         Scan_Proxy_Enabled      = false;
        public int          Scan_Columns_Max        = 20;
        public bool         Scan_Tables_Auto        = true;
        public string       Scan_UserAgent          = "Mozilla/5.0 (Windows NT 6.2; WOW64; rv:27.0) Gecko/20100101 Firefox/27.0";
    }
}
