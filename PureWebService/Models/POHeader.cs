using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PureWebService.Models
{
    public class POHeader
    {
        public string ponum { get; set; }
        public string vendor { get; set; }
        public string purchaseagent { get; set; }
        public string currencycode { get; set; }
        public decimal totaltax1 { get; set; }
        public decimal totaltax2 { get; set; }
        public decimal totalcost { get; set; }
        public string fob_description { get; set; }
        public string siteid { get; set; }
        public string shipto { get; set; }
        public string shipvia { get; set; }
        public string shipvia_description { get; set; }
        public string freightterms_description { get; set; }
        public string status { get; set; }
        public string paymentterms { get; set; }
        public string description { get; set; }
        public string receipts { get; set; }

        public string revisionnum { get; set; }

        public List<POLineItem> poline { get; set; }
        public List<Company> companies { get; set; }
    }
}