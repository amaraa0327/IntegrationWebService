using PureWebService.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace PureWebService.Helpers
{
    public class PostInvoiceHelper : IDisposable
    {
        SqlConnection conn;
        SqlCommand cmd;

        public PostInvoiceHelper()
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["OGAPS"].ConnectionString);
            conn.Open();
            cmd = conn.CreateCommand();
        }

        public void Dispose()
        {
            cmd.Dispose();
            conn.Close();
            conn.Dispose();
        }

        public InvoiceHeader RetrieveInvoiceFromOGAPS(string pJobID, string pDefaultApprover, string pEnterby)
        {
            InvoiceHeader invoice = new InvoiceHeader();
            List<InvoiceLineItem> listOfInvoiceLine = new List<InvoiceLineItem>();
            List<InvoiceCost> listOfInvoiceCost = new List<InvoiceCost>();

            #region [Select InvoiceHeader from OGAPS]
            cmd.CommandText = @"
                SELECT 
                  'KOFAX' as enterby, 
                  invh.Currency currencycode, 
                  poh.Description podesc, 
                  invh.InvoiceType documenttype, 
                  SYSDATETIME() as duedate,
                  invh.InvoiceDate invoicedate, 
                  invh.PurchaseOrderNumber ponum, 
                  'KAISER' orgid,
                  invh.PaymentTerms paymentterms,
                  poh.SiteID as positeid,
                  CASE SUBSTRING(invh.SupplierID, 1, CHARINDEX('-', invh.SupplierID)-1) 
	                WHEN '142' THEN 'BELLWOOD' 
	                WHEN '81' THEN 'ALEXCO'
	                WHEN '143' THEN 'CHANDLER'
	                WHEN '83' THEN 'FLORENCE'
	                WHEN '86' THEN 'JACKSON'
	                WHEN '144' THEN 'KALAMAZOO'
	                WHEN '141' THEN 'LA'
	                WHEN '145' THEN 'SHERMAN'
	                WHEN '146' THEN 'NEWARK'
	                WHEN '121' THEN 'RICHLAND'
	                WHEN '167' THEN 'LONDON'
	                WHEN '82' THEN 'CORPORATE'
	                WHEN '147' THEN 'TRENTWOOD'
	                ELSE ''
                  END AS siteid,
                  'ENTERED' as [status], 
                  invh.SupplierID vendor, 
                  invh.InvoiceNumber vendorinvoicenum,
                  invh.totalamount totalcost
                FROM [OGAPS].[dbo].[InvoiceHeader] invh 
                LEFT JOIN [OGAPS].[dbo].[POHeader] poh
                ON invh.PurchaseOrderNumber = poh.PONumber
                WHERE invh.JobID = @JOBID
            ";

            cmd.Parameters.Clear();
            cmd.Parameters.Add(new SqlParameter("@JOBID", pJobID));
            SqlDataReader dreader = cmd.ExecuteReader();
            while (dreader.Read())
            {
                invoice.defaultapprover = pDefaultApprover;
                invoice.enterby = pEnterby;
                invoice.currencycode = dreader.GetValue(1).ToString();
                invoice.documentype = dreader.GetValue(3).ToString();
                invoice.duedate = string.IsNullOrEmpty(dreader.GetValue(4).ToString()) ? DateTime.Now : DateTime.Parse(dreader.GetValue(4).ToString());
                invoice.invoicedate = string.IsNullOrEmpty(dreader.GetValue(5).ToString()) ? DateTime.Now : DateTime.Parse(dreader.GetValue(5).ToString());
                invoice.ponum = dreader.GetValue(6).ToString();
                invoice.orgid = dreader.GetValue(7).ToString();
                invoice.paymentterms = dreader.GetValue(8).ToString();
                invoice.positeid = dreader.GetValue(9).ToString();
                invoice.siteid = dreader.GetValue(10).ToString();
                invoice.status = dreader.GetValue(11).ToString();
                invoice.vendor = dreader.GetValue(12).ToString();
                invoice.vendorinvoicenum = dreader.GetValue(13).ToString();
                invoice.totalcost = string.IsNullOrEmpty(dreader.GetValue(14).ToString()) ? null : (decimal?)decimal.Parse(dreader.GetValue(14).ToString());
            }
            #endregion

            #region [Select InvoiceLine from OGAPS]
            if (invoice.documentype.Equals("Invoice"))
            {
                #region [when doctype is direct pay]
                cmd.CommandText = @"
                SELECT 
	                invl.Description [description], 
	                invl.LineNumber invoicelinenum, 
	                invl.Quantity invoiceqty, 
	                invl.UnitOfMeasure invoiceunit, 

	                'SERVICE' linetype, 
	                '' polinenum, 
	                '' ponum, 
	                '' porevisionnum,
	                '' positeid, 
	                invl.TotalPrice proratecost,
	                0 prorateservice, 
	                0 tax1, 
	                '' tax1code, 
	                0 tax2, 
	                '' tax2code, 
	                0 taxexempt, 
	                invl.UnitPrice unitcost,

	                1 invoicecost_costlinenum,
	                glp.GLCode invoicecost_gldebitacct,
	                invl.TotalPrice invoicecost_linecost,
	                'KAISER' invoicecost_orgid,
	                '' invoicecost_positeid, 
	                1 invoicecost_quantity,
	                CASE SUBSTRING(invh.SupplierID, 1, CHARINDEX('-', invh.SupplierID)-1) 
		                WHEN '142' THEN 'BELLWOOD'
		                WHEN '81' THEN 'ALEXCO'
		                WHEN '143' THEN 'CHANDLER'
		                WHEN '83' THEN 'FLORENCE'
		                WHEN '86' THEN 'JACKSON'
		                WHEN '144' THEN 'KALAMAZOO'
		                WHEN '141' THEN 'LA'
		                WHEN '145' THEN 'SHERMAN'
		                WHEN '146' THEN 'NEWARK'
		                WHEN '121' THEN 'RICHLAND'
		                WHEN '167' THEN 'LONDON'
		                WHEN '82' THEN 'CORPORATE'
		                WHEN '147' THEN 'TRENTWOOD'
		                ELSE ''
	                END AS invoicecost_tositeid,
	                invl.UnitPrice invoicecost_unitcost

                FROM [OGAPS].[dbo].[InvoiceLineItems] invl
	                LEFT JOIN [OGAPS].[dbo].[InvoiceHeader] invh
	                ON invl.ERPInvoiceID = invh.ERPInvoiceID
	                LEFT JOIN [OGAPS].[dbo].GLPostings glp 
	                ON invh.JobID = glp.JOBID
                WHERE invh.JobID = @JOBID
                ";
                #endregion
            }
            else
            {
                #region [when doctype is PO Invoice]
                cmd.CommandText = @"
                SELECT 
	                pol.Description [description], 
	                pol.LineNumber invoicelinenum, 
	                invl.Quantity invoiceqty, 
	                ISNULL(NULLIF(pol.UoQ, ''), 'EACH') invoiceunit, 
	                pol.LineType linetype, 
	                pol.LineNumber polinenum, 
	                poh.PONumber ponum, 
	                poh.RevisionNum porevisionnum,
	                CASE SUBSTRING(poh.SupplierID, 1, CHARINDEX('-', poh.SupplierID)-1) 
		                WHEN '142' THEN 'BELLWOOD'
		                WHEN '81' THEN 'ALEXCO'
		                WHEN '143' THEN 'CHANDLER'
		                WHEN '83' THEN 'FLORENCE'
		                WHEN '86' THEN 'JACKSON'
		                WHEN '144' THEN 'KALAMAZOO'
		                WHEN '141' THEN 'LA'
		                WHEN '145' THEN 'SHERMAN'
		                WHEN '146' THEN 'NEWARK'
		                WHEN '121' THEN 'RICHLAND'
		                WHEN '167' THEN 'LONDON'
		                WHEN '82' THEN 'CORPORATE'
		                WHEN '147' THEN 'TRENTWOOD'
		                ELSE ''
	                END AS positeid, 
	                CASE pol.prorateService 
		                WHEN 0 THEN 0
		                ELSE invl.TotalPrice
	                END AS proratecost,
	                pol.ProRateService prorateservice, 
	                pol.Tax1 tax1, 
	                pol.Tax1Code tax1code, 
	                pol.Tax2 tax2, 
	                pol.Tax2Code tax2code, 
	                pol.TAXExempt taxexempt, 
	                invl.UnitPrice unitcost,

	                1 invoicecost_costlinenum,
	                pol.GLCode invoicecost_gldebitacct,
	                invl.TotalPrice invoicecost_linecost,
	                'KAISER' invoicecost_orgid,
	                CASE SUBSTRING(poh.SupplierID, 1, CHARINDEX('-', poh.SupplierID)-1) 
		                WHEN '142' THEN 'BELLWOOD'
		                WHEN '81' THEN 'ALEXCO'
		                WHEN '143' THEN 'CHANDLER'
		                WHEN '83' THEN 'FLORENCE'
		                WHEN '86' THEN 'JACKSON'
		                WHEN '144' THEN 'KALAMAZOO'
		                WHEN '141' THEN 'LA'
		                WHEN '145' THEN 'SHERMAN'
		                WHEN '146' THEN 'NEWARK'
		                WHEN '121' THEN 'RICHLAND'
		                WHEN '167' THEN 'LONDON'
		                WHEN '82' THEN 'CORPORATE'
		                WHEN '147' THEN 'TRENTWOOD'
		                ELSE ''
	                END AS invoicecost_positeid, 
	                invl.Quantity invoicecost_quantity,
	                CASE SUBSTRING(invh.SupplierID, 1, CHARINDEX('-', invh.SupplierID)-1) 
	                    WHEN '142' THEN 'BELLWOOD'
	                    WHEN '81' THEN 'ALEXCO'
	                    WHEN '143' THEN 'CHANDLER'
	                    WHEN '83' THEN 'FLORENCE'
	                    WHEN '86' THEN 'JACKSON'
	                    WHEN '144' THEN 'KALAMAZOO'
	                    WHEN '141' THEN 'LA'
	                    WHEN '145' THEN 'SHERMAN'
	                    WHEN '146' THEN 'NEWARK'
	                    WHEN '121' THEN 'RICHLAND'
	                    WHEN '167' THEN 'LONDON'
	                    WHEN '82' THEN 'CORPORATE'
	                    WHEN '147' THEN 'TRENTWOOD'
	                    ELSE ''
                    END AS invoicecost_tositeid,
	                invl.UnitPrice invoicecost_unitcost

                FROM [OGAPS].[dbo].[POLineItems] pol 
	                LEFT JOIN [OGAPS].[dbo].[POHeader] poh
	                ON pol.PONumber = poh.PONumber
	                LEFT JOIN [OGAPS].[dbo].[InvoiceHeader] invh
	                ON poh.PONumber = invh.PurchaseOrderNumber
					LEFT JOIN [OGAPS].[dbo].[InvoiceLineItems] invl
					ON pol.LineNumber = invl.POLineNumber 
                        AND invl.PurchaseOrderNumber = pol.PONumber 
                        AND invl.ERPInvoiceID = invh.ERPInvoiceID
                WHERE invh.JobID = @JOBID 
                    AND EXISTS(SELECT * FROM InvoiceLineItems invl WHERE pol.LineNumber = invl.POLineNumber 
																		AND invl.PurchaseOrderNumber = pol.PONumber
																		AND invl.ERPInvoiceID = invh.ERPInvoiceID)

                UNION ALL

                SELECT 
	                glp.TransactionDesc [description],
	                0 invoicelinenum,
	                1 invoiceqty,
	                'EACH' invoiceunit,
	                'SERVICE' linetype,
	                null polinenum,
	                poh.PONumber ponum,
	                poh.RevisionNum porevisionnum,
	                CASE SUBSTRING(poh.SupplierID, 1, CHARINDEX('-', poh.SupplierID)-1) 
		                WHEN '142' THEN 'BELLWOOD'
		                WHEN '81' THEN 'ALEXCO'
		                WHEN '143' THEN 'CHANDLER'
		                WHEN '83' THEN 'FLORENCE'
		                WHEN '86' THEN 'JACKSON'
		                WHEN '144' THEN 'KALAMAZOO'
		                WHEN '141' THEN 'LA'
		                WHEN '145' THEN 'SHERMAN'
		                WHEN '146' THEN 'NEWARK'
		                WHEN '121' THEN 'RICHLAND'
		                WHEN '167' THEN 'LONDON'
		                WHEN '82' THEN 'CORPORATE'
		                WHEN '147' THEN 'TRENTWOOD'
		                ELSE ''
	                END AS positeid, 
	                0 proratecost,
	                0 prorateservice, 
	                CASE glp.TransactionDesc
						WHEN 'TAX ACCRUAL' THEN glp.GLAmount
						ELSE 0
					END tax1, 
	                '' tax1code, 
	                0 tax2, 
	                '' tax2code, 
	                0 taxexempt, 
	                CASE glp.TransactionDesc
						WHEN 'TAX ACCRUAL' THEN 0
						ELSE glp.GLAmount
					END unitcost,

	                1 invoicecost_costlinenum,
	                glp.GLCode invoicecost_gldebitacct,
	                CASE glp.TransactionDesc
						WHEN 'TAX ACCRUAL' THEN 0
						ELSE glp.GLAmount
					END invoicecost_linecost,
	                'KAISER' invoicecost_orgid,
	                CASE SUBSTRING(poh.SupplierID, 1, CHARINDEX('-', poh.SupplierID)-1) 
		                WHEN '142' THEN 'BELLWOOD'
		                WHEN '81' THEN 'ALEXCO'
		                WHEN '143' THEN 'CHANDLER'
		                WHEN '83' THEN 'FLORENCE'
		                WHEN '86' THEN 'JACKSON'
		                WHEN '144' THEN 'KALAMAZOO'
		                WHEN '141' THEN 'LA'
		                WHEN '145' THEN 'SHERMAN'
		                WHEN '146' THEN 'NEWARK'
		                WHEN '121' THEN 'RICHLAND'
		                WHEN '167' THEN 'LONDON'
		                WHEN '82' THEN 'CORPORATE'
		                WHEN '147' THEN 'TRENTWOOD'
		                ELSE ''
	                END AS invoicecost_positeid, 
	                1 invoicecost_quantity,
	                CASE SUBSTRING(invh.SupplierID, 1, CHARINDEX('-', invh.SupplierID)-1) 
		                WHEN '142' THEN 'BELLWOOD'
		                WHEN '81' THEN 'ALEXCO'
		                WHEN '143' THEN 'CHANDLER'
		                WHEN '83' THEN 'FLORENCE'
		                WHEN '86' THEN 'JACKSON'
		                WHEN '144' THEN 'KALAMAZOO'
		                WHEN '141' THEN 'LA'
		                WHEN '145' THEN 'SHERMAN'
		                WHEN '146' THEN 'NEWARK'
		                WHEN '121' THEN 'RICHLAND'
		                WHEN '167' THEN 'LONDON'
		                WHEN '82' THEN 'CORPORATE'
		                WHEN '147' THEN 'TRENTWOOD'
		                ELSE ''
	                END AS invoicecost_tositeid,
	                CASE glp.TransactionDesc
						WHEN 'TAX ACCRUAL' THEN 0
						ELSE glp.GLAmount
					END invoicecost_unitcost

                FROM [OGAPS].[dbo].GLPostings glp 
	                LEFT JOIN [OGAPS].[dbo].InvoiceHeader invh ON glp.JOBID = invh.JobID
	                LEFT JOIN [OGAPS].[dbo].POHeader poh ON invh.PurchaseOrderNumber = poh.PONumber
                WHERE glp.JOBID = @JOBID
                ";
                #endregion
            }
            dreader.Close();
            dreader = cmd.ExecuteReader();
            while (dreader.Read())
            {
                InvoiceLineItem currentLine = new InvoiceLineItem();
                InvoiceCost currentLineCost = new InvoiceCost();

                currentLine.description = dreader.GetValue(0).ToString();
                currentLine.invoicelinenum = string.IsNullOrEmpty(dreader.GetValue(1).ToString()) ? null : (int?)int.Parse(dreader.GetValue(1).ToString());
                currentLine.invoiceqty = string.IsNullOrEmpty(dreader.GetValue(2).ToString()) ? null : (decimal?)decimal.Parse(dreader.GetValue(2).ToString());
                currentLine.invoiceunit = dreader.GetValue(3).ToString();
                currentLine.linetype = dreader.GetValue(4).ToString();
                currentLine.polinenum = string.IsNullOrEmpty(dreader.GetValue(5).ToString()) ? null : (int?)int.Parse(dreader.GetValue(5).ToString());
                currentLine.ponum = dreader.GetValue(6).ToString();
                currentLine.porevisionnum = string.IsNullOrEmpty(dreader.GetValue(7).ToString()) ? null : (int?)int.Parse(dreader.GetValue(7).ToString());
                currentLine.positeid = dreader.GetValue(8).ToString();
                currentLine.tax1 = string.IsNullOrEmpty(dreader.GetValue(11).ToString()) ? null : (decimal?)decimal.Parse(dreader.GetValue(11).ToString());
                currentLine.tax1code = dreader.GetValue(12).ToString();
                currentLine.tax2 = string.IsNullOrEmpty(dreader.GetValue(13).ToString()) ? null : (decimal?)decimal.Parse(dreader.GetValue(13).ToString());
                currentLine.tax2code = dreader.GetValue(14).ToString();
                currentLine.taxexempt = dreader.GetValue(15).ToString() == "0" ? false : true;
                currentLine.unitcost = string.IsNullOrEmpty(dreader.GetValue(16).ToString()) ? null : (decimal?)decimal.Parse(dreader.GetValue(16).ToString());

                currentLineCost.costlinenum = string.IsNullOrEmpty(dreader.GetValue(17).ToString()) ? null : (int?)int.Parse(dreader.GetValue(17).ToString());
                currentLineCost.gldebitacct = dreader.GetValue(18).ToString();
                currentLineCost.linecost = string.IsNullOrEmpty(dreader.GetValue(19).ToString()) ? null : (decimal?)decimal.Parse(dreader.GetValue(19).ToString());
                currentLineCost.orgid = dreader.GetValue(20).ToString();
                currentLineCost.positeid = dreader.GetValue(21).ToString();
                currentLineCost.quantity = string.IsNullOrEmpty(dreader.GetValue(22).ToString()) ? null : (decimal?)decimal.Parse(dreader.GetValue(22).ToString());
                currentLineCost.tositeid = dreader.GetValue(23).ToString();
                currentLineCost.unitcost = string.IsNullOrEmpty(dreader.GetValue(24).ToString()) ? null : (decimal?)decimal.Parse(dreader.GetValue(24).ToString());

                currentLine.invoicecost = new List<InvoiceCost>();
                currentLine.invoicecost.Add(currentLineCost);

                listOfInvoiceLine.Add(currentLine);
            }
            dreader.Close();
            #endregion

            int? maxLineNum = listOfInvoiceLine.Select(line => line.invoicelinenum).Max();
            foreach (InvoiceLineItem lineitem in listOfInvoiceLine.Where(line => line.invoicelinenum == 0)) {
                maxLineNum += 1;
                lineitem.invoicelinenum = maxLineNum;
            }

            invoice.invoiceline = listOfInvoiceLine;

            return invoice;
        }
    }
}