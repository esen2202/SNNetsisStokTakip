using Microsoft.Office.Interop.Excel;
using SNNetsisStokTakip.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SNNetsisStokTakip.Classes
{
    public static class ConnectionManagement
    {
        public static ModelConnStr ConnStr { get; set; }

        public static SQLOperations SqlOperations { get; set; }

        public static DataView StocksTable { get; set; }
    }
}
