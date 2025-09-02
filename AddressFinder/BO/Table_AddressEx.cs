using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddressFinder.BO
{
    public class Table_AddressEx
    {
        public int nID { get; set; }
        public decimal dbLon { get; set; }
        public decimal dbLat { get; set; }
        public string strAddress { get; set; }
    }
}
