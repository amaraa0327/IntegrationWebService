using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PureWebService.Models
{
    public class InvoiceLineItem
    {
        public string description { get; set; }
        public int? invoicelinenum { get; set; }
        public decimal? invoiceqty { get; set; }
        public string invoiceunit { get; set; }
        public string linetype { get; set; }
        public int? polinenum { get; set; }
        public string ponum { get; set; }
        public int? porevisionnum { get; set; }
        public string positeid { get; set; }
        public decimal? tax1 { get; set; }
        public string tax1code { get; set; }
        public decimal? tax2 { get; set; }
        public string tax2code { get; set; }
        public bool taxexempt { get; set; }
        public decimal? unitcost { get; set; }
        public List<InvoiceCost> invoicecost { get; set; }
    }
}