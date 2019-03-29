using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PureWebService.Models
{
    public class InvoiceHeader
    {
        public string enterby { get; set; }
        public string defaultapprover { get; set; }
        public decimal? totalcost { get; set; }
        public string currencycode { get; set; }
        public string description { get; set; }
        public string documentype { get; set; }
        public DateTime duedate { get; set; }
        public DateTime invoicedate { get; set; }
        public string paymentterms { get; set; }
        public string ponum { get; set; }
        public string orgid { get; set; }
        public string positeid { get; set; }
        public string siteid { get; set; }
        public string status { get; set; }
        public string vendor { get; set; }
        public string vendorinvoicenum { get; set; }

        public List<InvoiceLineItem> invoiceline { get; set; }
    }
}