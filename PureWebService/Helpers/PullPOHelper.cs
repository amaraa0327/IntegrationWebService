using PureWebService.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace PureWebService.Helpers
{
    public class PullPOHelper : IDisposable
    {
        POHeader poheader;
        SqlConnection conn;
        SqlCommand cmd;
        SqlTransaction tr;

        public PullPOHelper(POHeader pPOHeader)
        {
            this.poheader = pPOHeader;
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["OGAPS"].ConnectionString);
            conn.Open();
            cmd = conn.CreateCommand();
        }

        public void Dispose()
        {
            poheader = null;
            tr.Dispose();
            cmd.Dispose();
            conn.Close();
            conn.Dispose();
        }

        public string StoreToDB()
        {
            string resultofquery;
            tr = conn.BeginTransaction("SetPurchaseOrder");
            cmd.Transaction = tr;
            #region [Delete operations]
            #region  [Delete Command With POHeader]
            resultofquery = this.DeletePO("POHeader");
            if (!resultofquery.Equals("succeeded")) return resultofquery;
            resultofquery = this.DeletePO("POLineItems");
            if (!resultofquery.Equals("succeeded")) return resultofquery;
            resultofquery = this.DeletePO("POReceipts");
            if (!resultofquery.Equals("succeeded")) return resultofquery;
            #endregion
            #endregion

            #region [Insert operations]
            resultofquery = this.InsertIntoPOHeader();
            if (!resultofquery.Equals("succeeded")) return resultofquery;
            if (poheader.poline != null)
                foreach (POLineItem poline in poheader.poline)
                {
                    resultofquery = this.InsertIntoPOLineItems(poline);
                    if (!resultofquery.Equals("succeeded")) return resultofquery;

                    if (poline.matrectrans != null)
                        foreach (POReceipt poreceipt in poline.matrectrans)
                        {
                            resultofquery = this.InsertIntoPOReceipts(poline, poreceipt);
                            if (!resultofquery.Equals("succeeded")) return resultofquery;
                        }
                    if (poline.servrectrans != null)
                        foreach (POReceipt poreceipt in poline.servrectrans)
                        {
                            resultofquery = this.InsertIntoPOReceipts(poline, poreceipt);
                            if (!resultofquery.Equals("succeeded")) return resultofquery;
                        }
                }

            #endregion
            tr.Commit();

            return "succeeded";

        }

        private string DeletePO(string pTableName)
        {
            try
            {
                cmd.CommandText = "DELETE FROM [dbo].[" + pTableName + "] WHERE [PONumber] = @PONUMBER";
                cmd.Parameters.Clear();
                cmd.Parameters.Add(new SqlParameter("@PONUMBER", poheader.ponum));
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                tr.Rollback();
                return "deletion failed (Delete" + pTableName + "): " + ex.Message;
            }
            return "succeeded";
        }

        private string InsertIntoPOHeader()
        {
            try
            {
                cmd.CommandText = @"
                    INSERT INTO [dbo].[POHeader]
                    (
                        PONumber, 
                        BusinessUnitCode,
                        BusinessUnitName,

                        SupplierID, SupplierName, Description, TotalAmount, Status, 
                        PurchaseAgent, CurrencyCode, TotalTax1, TotalTax2, 
                        TotalCost, FOBDescription, SiteID, ShipTo, 
                        ShipViaDescription, FreightTermDescription, ShipVia, Receipts, RevisionNum

                    ) VALUES (
                        @PONUMBER, 
                        @BUSINESSUNITCODE,
                        @BUSINESSUNITNAME,

                        @SUPPLIERID, @SUPPLIERNAME, @DESCRIPTION, @TOTALAMOUNT, @STATUS, 
                        @PURCHASEAGENT, @CURRENCYCODE, @TOTALTAX1, @TOTALTAX2, 
                        @TOTALCOST, @FOBDESCRIPTION, @SITEID, @SHIPTO, 
                        @SHIPVIADESCRIPTION, @FREIGHTTERMDESCRIPTION, @SHIPVIA, @RECEIPTS, @REVISIONNUM

                    )";
                List<Company> companies = poheader.companies.Where(company => company.company == poheader.vendor).ToList();
                String vendorName = "";
                if (companies.Count > 0)
                {
                    vendorName = companies.Select(company => company.name).ToList()[0];
                }
                cmd.Parameters.Clear();
                cmd.Parameters.Add(new SqlParameter("@PONUMBER", poheader.ponum == null ? "" : poheader.ponum));
                cmd.Parameters.Add(new SqlParameter("@BUSINESSUNITCODE", "2022"));
                cmd.Parameters.Add(new SqlParameter("@BUSINESSUNITNAME", "Kaiser"));
                cmd.Parameters.Add(new SqlParameter("@SUPPLIERID", poheader.vendor == null ? "" : poheader.vendor));
                cmd.Parameters.Add(new SqlParameter("@SUPPLIERNAME", vendorName));
                cmd.Parameters.Add(new SqlParameter("@DESCRIPTION", poheader.description == null ? "" : poheader.description));
                cmd.Parameters.Add(new SqlParameter("@TOTALAMOUNT", poheader.totalcost == null ? 0 : poheader.totalcost));
                cmd.Parameters.Add(new SqlParameter("@STATUS", poheader.status == null ? "" : poheader.status));
                cmd.Parameters.Add(new SqlParameter("@PURCHASEAGENT", poheader.purchaseagent == null ? "" : poheader.purchaseagent));
                cmd.Parameters.Add(new SqlParameter("@CURRENCYCODE", poheader.currencycode == null ? "" : poheader.currencycode));
                cmd.Parameters.Add(new SqlParameter("@TOTALTAX1", poheader.totaltax1 == null ? 0 : poheader.totaltax1));
                cmd.Parameters.Add(new SqlParameter("@TOTALTAX2", poheader.totaltax2 == null ? 0 : poheader.totaltax2));
                cmd.Parameters.Add(new SqlParameter("@TOTALCOST", poheader.totalcost == null ? 0 : poheader.totalcost));
                cmd.Parameters.Add(new SqlParameter("@FOBDESCRIPTION", poheader.fob_description == null ? "" : poheader.fob_description));
                cmd.Parameters.Add(new SqlParameter("@SITEID", poheader.siteid == null ? "" : poheader.siteid));
                cmd.Parameters.Add(new SqlParameter("@SHIPTO", poheader.shipto == null ? "" : poheader.shipto));
                cmd.Parameters.Add(new SqlParameter("@SHIPVIADESCRIPTION", poheader.shipvia_description == null ? "" : poheader.shipvia_description));
                cmd.Parameters.Add(new SqlParameter("@FREIGHTTERMDESCRIPTION", poheader.freightterms_description == null ? "" : poheader.freightterms_description));
                cmd.Parameters.Add(new SqlParameter("@SHIPVIA", poheader.shipvia == null ? "" : poheader.shipvia));
                cmd.Parameters.Add(new SqlParameter("@RECEIPTS", poheader.receipts == null ? "" : poheader.receipts));
                cmd.Parameters.Add(new SqlParameter("@REVISIONNUM", poheader.revisionnum == null ? "" : poheader.revisionnum));
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                tr.Rollback();
                return "insertion failed (InsertIntoPOHeader): " + ex.Message;
            }
            return "succeeded";
        }

        private string InsertIntoPOLineItems(POLineItem pPOLine)
        {
            decimal tax1 = 0, tax2 = 0;
            try
            {
                #region [Get taxrate from OGAPS]
                if (pPOLine.tax1code != null)
                {
                    using (SqlConnection sqlconn = new SqlConnection(ConfigurationManager.ConnectionStrings["OGAPS"].ConnectionString))
                    {
                        decimal taxrate = 0;
                        sqlconn.Open();
                        SqlCommand sqlcmd = sqlconn.CreateCommand();
                        sqlcmd.CommandText = @"SELECT TaxRate FROM Kaiser_TaxCodes WHERE TaxCode = @TAXCODE AND TaxType = 'Tax1Code'";
                        sqlcmd.Parameters.Clear();
                        sqlcmd.Parameters.Add(new SqlParameter("@TAXCODE", pPOLine.tax1code));
                        SqlDataReader dreader = sqlcmd.ExecuteReader();
                        while (dreader.Read())
                        {
                            taxrate = (decimal)dreader.GetValue(0);
                            break;
                        }
                        tax1 = pPOLine.linecost * taxrate;
                    }
                }
                if (pPOLine.tax2code != null && poheader.siteid != "LONDON")
                {
                    using (SqlConnection sqlconn = new SqlConnection(ConfigurationManager.ConnectionStrings["OGAPS"].ConnectionString))
                    {
                        decimal taxrate = 0;
                        sqlconn.Open();
                        SqlCommand sqlcmd = sqlconn.CreateCommand();
                        sqlcmd.CommandText = @"SELECT TaxRate FROM Kaiser_TaxCodes WHERE TaxCode = @TAXCODE AND TaxType = 'Tax2Code'";
                        sqlcmd.Parameters.Clear();
                        sqlcmd.Parameters.Add(new SqlParameter("@TAXCODE", pPOLine.tax2code));
                        SqlDataReader dreader = sqlcmd.ExecuteReader();
                        while (dreader.Read())
                        {
                            taxrate = (decimal)dreader.GetValue(0);
                            break;
                        }
                        tax2 = pPOLine.linecost * taxrate;
                    }
                }
                #endregion

                cmd.CommandText = @"
                    INSERT INTO [dbo].[POLineItems]
                    (
                        BusinessUnitCode, 
                        BusinessUnitName, 
                        PONumber, 
                        LineId,

                        LineNumber, PartNumber, UoQ, QuantityOrdered, QuantityReceived, QuantityRemaining, 
                        UnitPrice, Description, LineTotal, SupplierID, LineType, Status, 
                        GLCode, LoadedCost, ChargeStore, TAXExempt, ItemNum, ProRateService, 
                        ReceiptsComplete, Tax1, Tax2, Consignment, StoreLoc, CatalogCode, ReceiptReqd, Tax1Code, Tax2Code

                    ) VALUES (
                        @BUSINESSUNITCODE, 
                        @BUSINESSUNITNAME, 
                        @PONUMBER, 
                        @LINEID,

                        @LINENUMBER, @PARTNUMBER, @UOQ, @QUANTITYORDERED, @QUANTITYRECEIVED, @QUANTITYREMAINING, 
                        @UNITPRICE, @DESCRIPTION, @LINETOTAL, @SUPPLIERID, @LINETYPE, @STATUS, 
                        @GLCODE, @LOADEDCOST, @CHARGESTORE, @TAXEXEMPT, @ITEMNUM, @PRORATESERVICE, 
                        @RECEIPTSCOMPLETE, @TAX1, @TAX2, @CONSIGNMENT, @STORELOC, @CATALOGCODE, @RECEIPTREQD, @TAX1CODE, @TAX2CODE

                    )";
                cmd.Parameters.Clear();
                cmd.Parameters.Add(new SqlParameter("@BUSINESSUNITCODE", "2022"));
                cmd.Parameters.Add(new SqlParameter("@BUSINESSUNITNAME", "Kaiser"));
                cmd.Parameters.Add(new SqlParameter("@PONUMBER", poheader.ponum == null ? "" : poheader.ponum));
                cmd.Parameters.Add(new SqlParameter("@LINEID", pPOLine.polinenum == null ? 0 : pPOLine.polinenum));

                cmd.Parameters.Add(new SqlParameter("@LINENUMBER", pPOLine.polinenum == null ? 0 : pPOLine.polinenum));
                cmd.Parameters.Add(new SqlParameter("@PARTNUMBER", pPOLine.itemnum == null ? "" : pPOLine.itemnum));
                cmd.Parameters.Add(new SqlParameter("@UOQ", pPOLine.orderunit == null ? "" : pPOLine.orderunit));
                cmd.Parameters.Add(new SqlParameter("@QUANTITYORDERED", pPOLine.orderqty == null ? 0 : pPOLine.orderqty));
                cmd.Parameters.Add(new SqlParameter("@QUANTITYRECEIVED", pPOLine.orderqty == null ? 0 : pPOLine.orderqty));
                cmd.Parameters.Add(new SqlParameter("@QUANTITYREMAINING", pPOLine.orderqty == null ? 0 : pPOLine.orderqty));
                cmd.Parameters.Add(new SqlParameter("@UNITPRICE", pPOLine.unitcost == null ? 0 : pPOLine.unitcost));
                cmd.Parameters.Add(new SqlParameter("@DESCRIPTION", pPOLine.description == null ? "" : pPOLine.description));
                cmd.Parameters.Add(new SqlParameter("@LINETOTAL", pPOLine.linecost == null ? 0 : pPOLine.linecost));
                cmd.Parameters.Add(new SqlParameter("@SUPPLIERID", poheader.vendor == null ? "" : poheader.vendor));
                cmd.Parameters.Add(new SqlParameter("@LINETYPE", pPOLine.linetype == null ? "" : pPOLine.linetype));
                cmd.Parameters.Add(new SqlParameter("@STATUS", ""));
                cmd.Parameters.Add(new SqlParameter("@GLCODE", pPOLine.gldebitacct == null ? "" : pPOLine.gldebitacct));
                cmd.Parameters.Add(new SqlParameter("@LOADEDCOST", pPOLine.loadedcost == null ? 0 : pPOLine.loadedcost));
                cmd.Parameters.Add(new SqlParameter("@CHARGESTORE", pPOLine.chargestore == null ? false : pPOLine.chargestore));
                cmd.Parameters.Add(new SqlParameter("@TAXEXEMPT", pPOLine.taxexempt == null ? false : pPOLine.taxexempt));
                cmd.Parameters.Add(new SqlParameter("@ITEMNUM", pPOLine.itemnum == null ? "" : pPOLine.itemnum));
                cmd.Parameters.Add(new SqlParameter("@PRORATESERVICE", pPOLine.prorateservice == null ? false : pPOLine.prorateservice));
                cmd.Parameters.Add(new SqlParameter("@RECEIPTSCOMPLETE", pPOLine.receiptscomplete == null ? false : pPOLine.receiptscomplete));
                cmd.Parameters.Add(new SqlParameter("@TAX1", pPOLine.tax1code == null ? pPOLine.tax1 : tax1));
                cmd.Parameters.Add(new SqlParameter("@TAX2", pPOLine.tax2code == null ? pPOLine.tax2 : tax2));
                cmd.Parameters.Add(new SqlParameter("@CONSIGNMENT", pPOLine.consignment == null ? false : pPOLine.consignment));
                cmd.Parameters.Add(new SqlParameter("@STORELOC", pPOLine.storeloc == null ? "" : pPOLine.storeloc));
                cmd.Parameters.Add(new SqlParameter("@CATALOGCODE", pPOLine.catalogcode == null ? "" : pPOLine.catalogcode));
                cmd.Parameters.Add(new SqlParameter("@RECEIPTREQD", pPOLine.receiptreqd == null ? false : pPOLine.receiptreqd));
                cmd.Parameters.Add(new SqlParameter("@TAX1CODE", pPOLine.tax1code == null ? "" : pPOLine.tax1code));
                cmd.Parameters.Add(new SqlParameter("@TAX2CODE", pPOLine.tax2code == null ? "" : pPOLine.tax2code));
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                tr.Rollback();
                return "insertion failed (InsertIntoPOLineItems): " + ex.Message;
            }
            return "succeeded";
        }

        private string InsertIntoPOReceipts(POLineItem pPOLine, POReceipt pPOReceipt)
        {
            try
            {
                cmd.CommandText = @"
                    INSERT INTO [dbo].[POReceipts]
                    (
                        BusinessUnitCode, 
                        BusinessUnit, 
                        PONumber, 

                        ReceiptNumber, DeliveryDate, ItemId, QuantityOrdered, QuantityReceived, QuantityRemaining, 
                        UnitOfMeasure, UnitPrice, TotalPrice, VendorID, Description, POLineNumber, 
                        IssueType, CurrencyLineCost, CurrencyUnitCost, LoadedCost, StatusDescription, 
                        PackingSlipNum, Status, Tax1, Tax2, Consignment, MatRecTransID

                    ) VALUES (
                        @BUSINESSUNITCODE, 
                        @BUSINESSUNITNAME, 
                        @PONUMBER, 

                        @RECEIPTNUMBER, @DELIVERYDATE, @ITEMID, @QUANTITYORDERED, @QUANTITYRECEIVED, @QUANTITYREMAINING, 
                        @UNITOFMEASURE, @UNITPRICE, @TOTALPRICE, @VENDORID, @DESCRIPTION, @POLINENUMBER, 
                        @ISSUETYPE, @CURRENCYLINECOST, @CURRENCYUNITCOST, @LOADEDCOST, @STATUSDESCRIPTION, 
                        @PACKINGSLIPNUM, @STATUS, @TAX1, @TAX2, @CONSIGNMENT, @MATRECTRANSID

                    )";
                cmd.Parameters.Clear();
                cmd.Parameters.Add(new SqlParameter("@BUSINESSUNITCODE", "2022"));
                cmd.Parameters.Add(new SqlParameter("@BUSINESSUNITNAME", "Kaiser"));
                cmd.Parameters.Add(new SqlParameter("@PONUMBER", poheader.ponum == null ? "" : poheader.ponum));
                cmd.Parameters.Add(new SqlParameter("@RECEIPTNUMBER", ""));
                cmd.Parameters.Add(new SqlParameter("@DELIVERYDATE", (object)pPOReceipt.actualdate ?? DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@ITEMID", pPOLine.itemnum == null ? "" : pPOLine.itemnum));
                cmd.Parameters.Add(new SqlParameter("@QUANTITYORDERED", pPOReceipt.quantity == null ? 0 : pPOReceipt.quantity));
                cmd.Parameters.Add(new SqlParameter("@QUANTITYRECEIVED", pPOReceipt.quantity == null ? 0 : pPOReceipt.quantity));
                cmd.Parameters.Add(new SqlParameter("@QUANTITYREMAINING", pPOReceipt.quantity == null ? 0 : pPOReceipt.quantity));
                cmd.Parameters.Add(new SqlParameter("@UNITOFMEASURE", pPOLine.orderunit == null ? "" : pPOLine.orderunit));
                cmd.Parameters.Add(new SqlParameter("@UNITPRICE", pPOLine.unitcost == null ? 0 : pPOLine.unitcost));
                cmd.Parameters.Add(new SqlParameter("@TOTALPRICE", pPOReceipt.loadedcost == null ? 0 : pPOReceipt.loadedcost));
                cmd.Parameters.Add(new SqlParameter("@VENDORID", poheader.vendor == null ? "" : poheader.vendor));
                cmd.Parameters.Add(new SqlParameter("@DESCRIPTION", pPOReceipt.status_description == null ? "" : pPOReceipt.status_description));
                cmd.Parameters.Add(new SqlParameter("@POLINENUMBER", pPOLine.polinenum == null ? 0 : pPOLine.polinenum));
                cmd.Parameters.Add(new SqlParameter("@ISSUETYPE", pPOReceipt.issuetype == null ? "" : pPOReceipt.issuetype));
                cmd.Parameters.Add(new SqlParameter("@CURRENCYLINECOST", pPOReceipt.currencylinecost == null ? 0 : pPOReceipt.currencylinecost));
                cmd.Parameters.Add(new SqlParameter("@CURRENCYUNITCOST", pPOReceipt.currencyunitcost == null ? 0 : pPOReceipt.currencyunitcost));
                cmd.Parameters.Add(new SqlParameter("@LOADEDCOST", pPOReceipt.loadedcost == null ? 0 : pPOReceipt.loadedcost));
                cmd.Parameters.Add(new SqlParameter("@STATUSDESCRIPTION", pPOReceipt.status_description == null ? "" : pPOReceipt.status_description));
                cmd.Parameters.Add(new SqlParameter("@PACKINGSLIPNUM", pPOReceipt.packingslipnum == null ? "" : pPOReceipt.packingslipnum));
                cmd.Parameters.Add(new SqlParameter("@STATUS", pPOReceipt.status == null ? "" : pPOReceipt.status));
                cmd.Parameters.Add(new SqlParameter("@TAX1", pPOReceipt.tax1 == null ? 0 : pPOReceipt.tax1));
                cmd.Parameters.Add(new SqlParameter("@TAX2", pPOReceipt.tax2 == null ? 0 : pPOReceipt.tax2));
                cmd.Parameters.Add(new SqlParameter("@CONSIGNMENT", pPOReceipt.consignment == null ? false : pPOReceipt.consignment));
                cmd.Parameters.Add(new SqlParameter("@MATRECTRANSID", pPOReceipt.matrectransid == null ? 0 : pPOReceipt.matrectransid));
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                tr.Rollback();
                return "insertion failed (InsertIntoPOReceipts): " + ex.Message;
            }
            return "succeeded";
        }
    }
}