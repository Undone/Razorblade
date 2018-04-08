using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Razorblade
{
    public class Database
    {
        public string Name;
        public Target Target;
        public List<Table> Tables = new List<Table>();
    }

    public class Table
    {
        public string Name;
        public Database Database;
        public List<Column> Columns = new List<Column>();
        public List<Row> Rows = new List<Row>();
    }

    public class Column
    {
        public string Name;
        public Table Table;
    }

    public class Row
    {
        public Table Table;
        public List<string> Data = new List<string>();
    }
}
