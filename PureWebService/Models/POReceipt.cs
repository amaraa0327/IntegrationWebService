using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PureWebService.Models
{
    public class POReceipt
    {
        public string issuetype { get; set; }
        public decimal currencylinecost { get; set; }
        public decimal currencyunitcost { get; set; }
        public decimal loadedcost { get; set; }
        public string status_description { get; set; }
        public string packingslipnum { get; set; }
        public string status { get; set; }
        public decimal tax1 { get; set; }
        public decimal tax2 { get; set; }
        public bool consignment { get; set; }
        public decimal quantity { get; set; }

        public DateTime? actualdate { get; set; }
        //public decimal unitcost { get; set; }
        public int matrectransid { get; set; }
    }
}