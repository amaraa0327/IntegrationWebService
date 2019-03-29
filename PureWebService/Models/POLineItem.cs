using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PureWebService.Models
{
    public class POLineItem
    {
        public string itemnum { get; set; }
        public int polinenum { get; set; }
        //public int polineid { get; set; }
        public string description { get; set; }
        public decimal unitcost { get; set; }
        public decimal orderqty { get; set; }
        public decimal receivedqty { get; set; }
        public decimal linecost { get; set; }
        public decimal loadedcost { get; set; }
        public bool chargestore { get; set; }
        public bool taxexempt { get; set; }
        public bool prorateservice { get; set; }
        public bool receiptscomplete { get; set; }
        public string linetype { get; set; }
        public string gldebitacct { get; set; }
        public decimal tax1 { get; set; }
        public decimal tax2 { get; set; }
        public bool consignment { get; set; }
        public string storeloc { get; set; }
        public string orderunit { get; set; }
        public string catalogcode { get; set; }
        public bool receiptreqd { get; set; }
        public string tax1code { get; set; }
        public string tax2code { get; set; }

        public List<POReceipt> matrectrans { get; set; }

        public List<POReceipt> servrectrans { get; set; }
    }
}