using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SNNetsisStokTakip.Models
{
    public class ModelStock
    {
        public string StockCode { get; set; }
        public double Amount { get; set; }
 
    }

    public class ModelStockDetailed  : ModelStock
    {
        public double Price { get; set; }

        public DateTime LastDate { get; set; }

    }
}
