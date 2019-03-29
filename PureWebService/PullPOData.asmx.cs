using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Services;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using PureWebService.Models;
using PureWebService.Helpers;
using System.Text;
using System.IO;

namespace PureWebService
{
    /// <summary>
    /// Summary description for WebService1
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class PullPOData : System.Web.Services.WebService
    {
        POHeader poheader;
        //check invoice summary from Maximo
        List<POInvoice> poinvoices;
        //for post invoice to Maximo
        InvoiceHeader invoice;

        [WebMethod]
        public string PullAndSaveToOGAPS(string ponum)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    string url = ConfigurationManager.AppSettings.Get("webService:URL") + "maximo/oslc/os/KA_MXPO_REC?lean=1&oslc.select=*&oslc.where=ponum=" + "\"" + ponum + "\" and status in [\"APPR\",\"CLOSE\",\"INPRG\",\"WAPPR\"]";
                    client.BaseAddress = new Uri(url);

                    client.DefaultRequestHeaders.Add("MAXAUTH", "Q09MX1NWQ19Lb2ZheF9PQ1JJbWFnZXM6SzA1QFgxbnQzNnQxb24=");

                    HttpResponseMessage response = client.GetAsync("").Result;
                    this.LogoutMaximo();
                    client.Dispose();

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = response.Content.ReadAsStringAsync();
                        jsonString.Wait();

                        JToken root = JObject.Parse(jsonString.Result)["member"];

                        if (root.Count() == 0) return "not found";

                        poheader = JsonConvert.DeserializeObject<POHeader>(root[0].ToString());

                        using (PullPOHelper pohelper = new PullPOHelper(poheader))
                        {
                            return pohelper.StoreToDB();
                        }
                    }
                    else
                    {
                        return response.StatusCode + " : " + response.ReasonPhrase;
                    }
                }
            }
            catch (Exception ex)
            {
                return "failed :" + ex.Message;
            }
        }

        [WebMethod]
        public string CheckInvoiceNumber(string pVendor, string pVendorInvoiceNumber, string pSiteName)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    string url = ConfigurationManager.AppSettings.Get("webService:URL") + "maximo/oslc/os/KA_MXINVOICE?lean=1&oslc.select=invoicenum,vendor,vendorinvoicenum,siteid&oslc.where=vendor=\"" + pVendor + "\" and VENDORINVOICENUM=\"" + pVendorInvoiceNumber + "\" and siteid=\"" + pSiteName + "\"";
                    client.BaseAddress = new Uri(url);

                    client.DefaultRequestHeaders.Add("MAXAUTH", "Q09MX1NWQ19Lb2ZheF9PQ1JJbWFnZXM6SzA1QFgxbnQzNnQxb24=");

                    HttpResponseMessage response = client.GetAsync("").Result;
                    this.LogoutMaximo();
                    client.Dispose();

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = response.Content.ReadAsStringAsync();
                        jsonString.Wait();

                        JToken root = JObject.Parse(jsonString.Result)["member"];

                        if (root.Count() == 0) return "";

                        poinvoices = JsonConvert.DeserializeObject<List<POInvoice>>(root.ToString());

                        List<POInvoice> matchedinvoices = poinvoices.Where<POInvoice>(invoice => (invoice.siteid == pSiteName && invoice.vendor == pVendor && invoice.vendorinvoicenum == pVendorInvoiceNumber)).ToList<POInvoice>();

                        if (matchedinvoices.Count > 0) return "The Invoice already exists in Maximo!";
                        else return "";
                    }
                    else
                    {
                        return response.StatusCode + " : " + response.ReasonPhrase;
                    }
                }
            }
            catch (Exception ex)
            {
                return "failed :" + ex.Message;
            }
        }

        [WebMethod]
        public string PostInvoiceToMaximo(string pJobID, string pDefaultApprover, string pEnterby)
        {
            try
            {
                using (PostInvoiceHelper invhelper = new PostInvoiceHelper())
                {
                    invoice = invhelper.RetrieveInvoiceFromOGAPS(pJobID, pDefaultApprover, pEnterby);
                }

                using (var client = new HttpClient())
                {
                    string url = ConfigurationManager.AppSettings.Get("webService:URL") + "maximo/oslc/os/KA_MXINVOICE?lean=1";
                    client.BaseAddress = new Uri(url);

                    client.DefaultRequestHeaders.Add("MAXAUTH", "Q09MX1NWQ19Lb2ZheF9PQ1JJbWFnZXM6SzA1QFgxbnQzNnQxb24=");
                    client.DefaultRequestHeaders.Add("cache-control", "no-cache");
                    client.DefaultRequestHeaders.Add("properties", "invoiceid");

                    string jsonInvoice = JsonConvert.SerializeObject(invoice);
                    jsonInvoice = jsonInvoice.Replace(@"\n", "\n");

                    //----------Write File----------
                    try
                    {
                        string path = @"C:\MaximoLogs\" + pJobID + ".txt";
                        using (StreamWriter sw = new StreamWriter(path, true))
                        {
                            sw.WriteLine(jsonInvoice);
                        }
                    }
                    catch (Exception ex) { }
                    //----------Write File----------

                    StringContent content = new StringContent(jsonInvoice, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = client.PostAsync(client.BaseAddress, content).Result;
                    this.LogoutMaximo();
                    client.Dispose();

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = response.Content.ReadAsStringAsync();
                        jsonString.Wait();

                        JToken root = JObject.Parse(jsonString.Result)["invoiceid"];

                        if (!string.IsNullOrEmpty(root.ToString())) return "invoiceid|" + root.ToString();
                        else
                        {
                            string[] responseresult = JsonConvert.DeserializeObject<string[]>(JObject.Parse(jsonString.Result)["Error"].ToString());
                            return responseresult[2];
                        }
                    }
                    else
                    {
                        var jsonString = response.Content.ReadAsStringAsync();
                        JToken root = JObject.Parse(jsonString.Result)["Error"];
                        return root["message"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                return "failed :" + ex.Message;
            }
        }

        [WebMethod]
        public string PostInvoiceImageToMaximo(string pMaximoInvoiceID, string pImageURL)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    int maximoInvID = string.IsNullOrEmpty(pMaximoInvoiceID) ? 0 : int.Parse(pMaximoInvoiceID);
                    string url = ConfigurationManager.AppSettings.Get("webService:URL") + "maximo/oslc/os/KA_DOCLINKS/?lean=1";
                    client.BaseAddress = new Uri(url);

                    client.DefaultRequestHeaders.Add("MAXAUTH", "Q09MX1NWQ19Lb2ZheF9PQ1JJbWFnZXM6SzA1QFgxbnQzNnQxb24=");
                    client.DefaultRequestHeaders.Add("cache-control", "no-cache");

                    string jsonImageParam = JsonConvert.SerializeObject(new ImageParam()
                    {
                        addinfo = true,
                        app = "INVOICE",
                        description = "INVOICE IMAGE",
                        copylinktowo = false,
                        doctype = "ATTACHMENTS",
                        document = "INV",
                        ownerid = maximoInvID,
                        ownertable = "INVOICE",
                        printthrulink = true,
                        urlname = pImageURL,
                        urltype = "URL"
                    });

                    StringContent content = new StringContent(jsonImageParam, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = client.PostAsync(client.BaseAddress, content).Result;
                    this.LogoutMaximo();
                    client.Dispose();

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = response.Content.ReadAsStringAsync();
                        jsonString.Wait();
                        return jsonString.Result.ToString();
                    }
                    else
                    {
                        var jsonString = response.Content.ReadAsStringAsync();
                        JToken root = JObject.Parse(jsonString.Result)["Error"];
                        return root["message"].ToString();
                    }
                    //return jsonImageParam;
                }
            }
            catch (Exception ex)
            {
                return "failed :" + ex.Message;
            }
        }

        private void LogoutMaximo()
        {
            using (var clientLogout = new HttpClient())
            {
                clientLogout.BaseAddress = new Uri(ConfigurationManager.AppSettings.Get("webService:URL") + "maximo/oslc/logout");
                clientLogout.DefaultRequestHeaders.Clear();
                clientLogout.DefaultRequestHeaders.Add("MAXAUTH", "Q09MX1NWQ19Lb2ZheF9PQ1JJbWFnZXM6SzA1QFgxbnQzNnQxb24=");
                HttpResponseMessage responselogout = clientLogout.GetAsync("").Result;
                if (responselogout.IsSuccessStatusCode)
                { }
                clientLogout.Dispose();
            }
        }
    }
}
