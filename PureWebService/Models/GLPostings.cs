using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PureWebService.Models
{
    public class GLPostings
    {
        public string ERPInvoiceID { get; set; }
        public string GLCode { get; set; }
        public string GLDesc { get; set; }
        public decimal GLAmount { get; set; }
        public string PostingPeriod { get; set; }
        public string PostingYear { get; set; }
        public DateTime TransactionDate { get; set; }
        public string TransactionDesc { get; set; }
        public string InvoiceType { get; set; }
    }
}