using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PureWebService.Models
{
    public class ImageParam
    {
        public bool addinfo { get; set; }
        public string app { get; set; }
        public string description { get; set; }
        public bool copylinktowo { get; set; }
        public string doctype { get; set; }
        public string document { get; set; }
        public int ownerid { get; set; }
        public string ownertable { get; set; }
        public bool printthrulink { get; set; }
        public string urlname { get; set; }
        public string urltype { get; set; }
    }
}