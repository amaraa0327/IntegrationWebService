using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PureWebService.Models
{
    public class POInvoice
    {
        public string vendor { get; set; }
        public string siteid { get; set; }
        public string vendorinvoicenum { get; set; }
        public string invoicenum { get; set; }
    }
}