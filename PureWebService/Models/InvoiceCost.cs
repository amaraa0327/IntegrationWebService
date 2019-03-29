using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PureWebService.Models
{
    public class InvoiceCost
    {
        public int? costlinenum { get; set; }
        public decimal? linecost { get; set; }
        public string orgid { get; set; }
        public string gldebitacct { get; set; }
        public string positeid { get; set; }
        public decimal? quantity { get; set; }
        public string tositeid { get; set; }
        public decimal? unitcost { get; set; }
    }
}