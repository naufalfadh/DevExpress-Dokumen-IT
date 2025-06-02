using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Template_DevExpress_By_MFM.Models;
using Template_DevExpress_By_MFM.Utils;
using PagedList;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Data.OleDb;
using System.Data;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using System.Web.Routing;

namespace Template_DevExpress_By_MFM.Controllers
{
    public class ManageController : Controller
    {
        private SessionLogin sessionLogin = (SessionLogin)System.Web.HttpContext.Current.Session["SHealth"];
        public GSDbContext GSDbContext { get; set; }

        public ManageController()
        {
            if (sessionLogin != null)
            {

                GSDbContext = new GSDbContext("NAUFALF", "db_marketing_portal", "sa", "polman");
            }
            else
            {
                RedirectToAction("Index", "Login");
            }

        }
        protected override void Dispose(bool disposing)
        {
            if (sessionLogin != null)
            {
                GSDbContext.Dispose();
            }
            else
            {
                RedirectToAction("Index", "Login");
            }
        }

        protected override void OnException(ExceptionContext filterContext)
        {
            //Do your logging
            // and redirect / return error view
            filterContext.ExceptionHandled = true;
            // If the exception occured in an ajax call. Send a json response back
            // (you need to parse this and display to user as needed at client side)
            if (filterContext.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                filterContext.Result = new JsonResult
                {
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                    Data = new { Error = true, Message = filterContext.Exception.Message }
                };
                filterContext.HttpContext.Response.StatusCode = 500; // Set as needed
            }
            else
            {
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary { { "controller", "Login" }, { "action", "Index" } });
                //Assuming the view exists in the "~/Views/Shared" folder
            }
        }

        // AREA MANAGE USER ACCOUNT
        [SessionCheck]
        public ActionResult ManageMasterUser()
        {
            return View();
        }

        [SessionCheck]
        public ActionResult AddMasterDokumenRequest()
        {
            return View();
        }

        // AREA MANAGE USER ACCOUNT FORM
        [SessionCheck]
        public ActionResult ManageMasterUserForm()
        {
            return View();
        }

        [SessionCheck]
        public ActionResult ManageMasterDokumenRequest()
        {
            return View();
        }

        public ActionResult ManageMasterDokumenRequestP3sp()
        {
            return View();
        }

        public ActionResult ManageMasterDokumenRequestPpi()
        {
            return View();
        }

        // AREA MANAGE CUSTOMER
        public ActionResult AddDokumenRequest()
        {
            return View();
        }

      

        public static Attachment GetAttachment(DataTable dataTable, string sheetnameTarget, string targetFilename)
        {
            MemoryStream outputStream = new MemoryStream();
            using (ExcelPackage package = new ExcelPackage(outputStream))
            {
                ExcelWorksheet facilityWorksheet = package.Workbook.Worksheets.Add(sheetnameTarget);
                facilityWorksheet.Cells.LoadFromDataTable(dataTable, true);

                var i = dataTable.Rows.Count;

                // PROTECT COLUMN FOR NOT EDITABLE
                facilityWorksheet.Protection.IsProtected = true;
                facilityWorksheet.Column(9).Style.Locked = false;
                facilityWorksheet.Cells[1, 9].Style.Locked = true;

                #region FORMATING EXCEL
                using (var range = facilityWorksheet.Cells[1, 1, 1, 9])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    //range.Style.Font.Color.SetColor(Color.White);
                }

                using (var range = facilityWorksheet.Cells[2, 12, 3, 12])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    //range.Style.Font.Color.SetColor(Color.White);
                }

                using (var range = facilityWorksheet.Cells[1, 10, 1, 10])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    //range.Style.Font.Color.SetColor(Color.White);
                }
                #endregion


                // FORMAT COLOR FOR INPUT DISABLED
                for (var p = 1; p <= 10; p++)
                {
                    if (p != 9)
                    {
                        using (var range = facilityWorksheet.Cells[2, p, i + 1, 10])
                        {
                            range.Style.Font.Bold = true;
                            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(Color.Silver);
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            //range.Style.Font.Color.SetColor(Color.White);
                        }
                    }
                }

                // FORMAT COLOR FOR INPUT ENABLE
                using (var range = facilityWorksheet.Cells[2, 9, i + 1, 9])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.Orange);
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    //range.Style.Font.Color.SetColor(Color.White);
                }


                if (i > 0)
                {
                    using (var range = facilityWorksheet.Cells[2, 1, i + 1, 10])
                    {
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    }
                }

                facilityWorksheet.Cells.AutoFitColumns(0);

                package.Save();
            }

            outputStream.Position = 0;
            Attachment attachment = new Attachment(outputStream, targetFilename, "application/vnd.ms-excel");

            return attachment;
        }

        private static DataTable ReadExcelFile(string sheetName, string path)
        {

            using (OleDbConnection conn = new OleDbConnection())
            {
                DataTable dt = new DataTable();
                string Import_FileName = path;
                string fileExtension = Path.GetExtension(Import_FileName);
                if (fileExtension == ".xls")
                    conn.ConnectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + Import_FileName + ";" + "Extended Properties='Excel 8.0;HDR=YES;'";
                if (fileExtension == ".xlsx")
                    conn.ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + Import_FileName + ";" + "Extended Properties='Excel 12.0 Xml;HDR=YES;'";
                using (OleDbCommand comm = new OleDbCommand())
                {
                    comm.CommandText = "Select * from [" + sheetName + "$]";
                    comm.Connection = conn;
                    using (OleDbDataAdapter da = new OleDbDataAdapter())
                    {
                        da.SelectCommand = comm;
                        da.Fill(dt);
                        conn.Close();
                        return dt;
                    }
                }
            }
        }
    }
}