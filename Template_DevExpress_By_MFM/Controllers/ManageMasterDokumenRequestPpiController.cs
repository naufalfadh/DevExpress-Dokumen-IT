using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Web.Http;
using Template_DevExpress_By_MFM.Models;
using Template_DevExpress_By_MFM.Utils;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Data.ResponseModel;
using DevExtreme.AspNet.Mvc;
using Newtonsoft.Json;
using Template_DevExpress_By_MFM.Controllers;
using System.Threading.Tasks;
using System.Globalization;
using System.Web;
using System.IO;
using System.Net.Mail;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Numeric;
using System.Diagnostics;
using System.Data.Entity;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;

namespace Template_DevExpress_By_MFM.Controllers
{
    public class ManageMasterDokumenRequestPpiController : ApiController
    {

        public GSDbContext GSDbContext { get; set; }
        private SessionLogin sessionLogin = (SessionLogin)System.Web.HttpContext.Current.Session["SHealth"];

        public ManageMasterDokumenRequestPpiController()
        {

            GSDbContext = new GSDbContext(@"NAUFALF", "db_marketing_portal", "sa", "polman");
        }
        protected override void Dispose(bool disposing)
        {
            GSDbContext.Dispose();
        }

        private string SaveBase64File(string base64Data, string refNumber, string customFileName)
        {
            if (string.IsNullOrWhiteSpace(base64Data))
                return null;

            try
            {
                // Hilangkan prefix base64 jika ada
                if (base64Data.Contains(","))
                {
                    base64Data = base64Data.Split(',')[1];
                }

                // Ubah base64 menjadi byte array
                byte[] fileBytes = Convert.FromBase64String(base64Data);

                // Tentukan folder simpan
                string folderPath = HttpContext.Current.Server.MapPath("~/Content/Uploads/");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                // Format nama file: Lampiran_{refNumber}_{yyyyMMdd_HHmmss}.pdf
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string sanitizedRef = string.IsNullOrEmpty(refNumber) ? "NODATA" : refNumber.Replace(" ", "_").Replace(":", "-");
                string fileName = $"Lampiran_{sanitizedRef}_{timestamp}.pdf";

                string fullPath = Path.Combine(folderPath, fileName);

                // Simpan file
                File.WriteAllBytes(fullPath, fileBytes);

                // Kembalikan nama file (atau path relatif jika perlu)
                return fileName;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Gagal menyimpan file: {ex.Message}");
                return null;
            }
        }

        private HttpResponseMessage HandleException(Exception ex)
        {
            var errorDetails = "";
            var innerException = ex;

            while (innerException != null)
            {
                errorDetails += $"Exception: {innerException.Message}\nStackTrace: {innerException.StackTrace}\n\n";
                innerException = innerException.InnerException;
            }

            return Request.CreateResponse(HttpStatusCode.InternalServerError, new
            {
                Message = "Server Error",
                Error = ex.Message,
                DetailedError = errorDetails
            });
        }

        [HttpGet]
        public HttpResponseMessage Get(DataSourceLoadOptions loadOptions, string departmentFilter = null)
        {
            try
            {
                var sessionLogin = (SessionLogin)System.Web.HttpContext.Current.Session["SHealth"];

                if (sessionLogin == null || string.IsNullOrEmpty(sessionLogin.fullname))
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "User tidak terautentikasi.");
                }

                string userFullName = sessionLogin.fullname;
                string userRole = sessionLogin.userrole;
                string userDepartment = sessionLogin.userdepartment;

                IQueryable<MasterDokumenRequestPpi> dataList;

                if (userRole == "admin" || userRole == "kadeptit" || userRole == "businessanalyst")
                {
                    dataList = GSDbContext.MasterDokumenRequestPpi.AsQueryable();

                    if (!string.IsNullOrEmpty(departmentFilter))
                    {
                        dataList = dataList.Where(d => d.dok_section == departmentFilter);
                    }
                }
                else if (userRole == "kadept")
                {
                    dataList = GSDbContext.MasterDokumenRequestPpi
                        .Where(d => d.dok_section == userDepartment)
                        .AsQueryable();

                    if (!string.IsNullOrEmpty(departmentFilter))
                    {
                        dataList = dataList.Where(d => d.dok_section == departmentFilter);
                    }
                }
                else
                {
                    dataList = GSDbContext.MasterDokumenRequestPpi
                        .Where(d => d.createBy == userFullName)
                        .AsQueryable();
                }

                var loadResult = DataSourceLoader.Load(dataList, loadOptions);
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    data = loadResult.data,
                    totalCount = loadResult.totalCount
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message.ToString());
            }
        }

        [SessionCheck]
        [HttpPost]
        public HttpResponseMessage Post([FromBody] MasterDokumenRequestPpi requestData)
        {
            try
            {
                if (sessionLogin == null)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, new { Message = "Session Expired" });
                }

                if (requestData == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new { Message = "Data tidak boleh kosong!" });
                }

                if (requestData.usr_npk == null || requestData.usr_npk == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new { Message = "NPK harus berupa angka yang valid!" });
                }

                System.Diagnostics.Debug.WriteLine($"📌 Payload Diterima (MasterDokumenRequestPpi): {JsonConvert.SerializeObject(requestData)}");

                // Cek validitas data request
                if (string.IsNullOrEmpty(requestData.dok_refnum) ||
                    requestData.usr_npk == null ||
                    string.IsNullOrEmpty(requestData.usr_nama) ||
                    string.IsNullOrEmpty(requestData.dok_user_erp) ||
                    string.IsNullOrEmpty(requestData.dok_plant) ||
                    string.IsNullOrEmpty(requestData.dok_section) ||
                    string.IsNullOrEmpty(requestData.dok_tingkat) ||
                    string.IsNullOrEmpty(requestData.dok_jenis_pekerjaan) ||
                    string.IsNullOrEmpty(requestData.dok_superuser) ||
                    string.IsNullOrEmpty(requestData.dok_judul_request) ||
                    string.IsNullOrEmpty(requestData.dok_document) ||
                    string.IsNullOrEmpty(requestData.dok_reason) ||
                    string.IsNullOrEmpty(requestData.dok_spesifikasi))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new { Message = "Beberapa data master tidak valid atau kosong!" });
                }

                // Mengatur nilai dok_document secara manual
                requestData.dok_document = "Permintaan Pekerjaan Infor";

                using (var transaction = GSDbContext.Database.BeginTransaction())
                {
                    try
                    {
                        // Ambil data user dari session NPK
                        long? currentUserNpk = null;
                        if (long.TryParse(sessionLogin?.npk?.ToString(), out long parsedNpk))
                        {
                            currentUserNpk = parsedNpk;
                        }

                        var currentUser = GSDbContext.MasterUserForm.FirstOrDefault(u => u.usr_npk == currentUserNpk);

                        if (currentUser == null)
                        {
                            return Request.CreateResponse(HttpStatusCode.NotFound, new { Message = "User login tidak ditemukan di database!" });
                        }

                        var form = new MasterDokumenRequestPpi
                        {
                            dok_refnum = requestData.dok_refnum ?? "",
                            usr_npk = requestData.usr_npk ?? 0,
                            usr_nama = requestData.usr_nama ?? "",
                            dok_user_erp = requestData.dok_user_erp ?? "",
                            dok_plant = requestData.dok_plant ?? "",
                            dok_section = requestData.dok_section ?? "",
                            dok_tingkat = requestData.dok_tingkat ?? "",
                            dok_jenis_pekerjaan = requestData.dok_jenis_pekerjaan ?? "",
                            dok_superuser = requestData.dok_superuser ?? "",
                            dok_judul_request = requestData.dok_judul_request ?? "",
                            dok_jenis_superuser = requestData.dok_jenis_superuser ?? "",
                            dok_document = requestData.dok_document ?? "",
                            dok_reason = requestData.dok_reason ?? "",
                            dok_spesifikasi = requestData.dok_spesifikasi ?? "",
                            dok_lampiran = SaveBase64File(requestData.dok_lampiran, requestData.dok_refnum, $"Lampiran_{requestData.dok_refnum}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"),
                            dok_ttd_user = currentUser?.usr_img_ttd ?? "",
                            dok_status = 0,
                            createDate = DateTime.UtcNow.AddHours(7),
                            createBy = sessionLogin?.fullname ?? "System"
                        };

                        foreach (var prop in form.GetType().GetProperties())
                        {
                            var value = prop.GetValue(form)?.ToString();
                            System.Diagnostics.Debug.WriteLine($"🔹 {prop.Name}: Panjang = {value?.Length}, Nilai = {value}");
                        }

                        GSDbContext.MasterDokumenRequestPpi.Add(form);
                        GSDbContext.SaveChanges();

                        var kadeptUsers = GSDbContext.MasterUserForm
                        .Where(u => u.usr_role != null && u.usr_role.Equals("kadept", StringComparison.OrdinalIgnoreCase) && u.usr_section == requestData.dok_section)
                        .ToList();

                        if (kadeptUsers != null && kadeptUsers.Any())
                        {
                            foreach (var kadept in kadeptUsers)
                            {
                                if (!string.IsNullOrEmpty(kadept.usr_email))
                                {
                                    string emailKadept = kadept.usr_email;
                                    string emailMessage = $@"
Yth. {kadept.usr_nama},

Telah diajukan permintaan pembuatan dan perubahan sistem ERP baru dengan rincian sebagai berikut:

- Nomor Referensi: {requestData.dok_refnum}
- Plant: {requestData.dok_plant}
- Tingkat: {requestData.dok_tingkat}
- Jenis Perkerjaan: {requestData.dok_jenis_pekerjaan}
- Jenis Superuser: {requestData.dok_jenis_superuser}
- Judul Request: {requestData.dok_judul_request}
- Dokumen: {requestData.dok_document}

Silakan masuk ke sistem untuk melakukan proses approval.

Link sistem: http://localhost:8000/Login
";

                                    bool emailSent = SendEmailNotification(emailKadept, emailMessage);

                                    if (emailSent)
                                    {
                                        Console.WriteLine("✅ Email berhasil dikirim ke Kadept: " + emailKadept);
                                    }
                                    else
                                    {
                                        Console.WriteLine("⚠️ Gagal mengirim email ke: " + emailKadept);
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"⚠️ Email Kadept kosong untuk user: {kadept.usr_nama}");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ Tidak ditemukan user dengan role Kadept di section: {requestData.dok_section}");
                        }
                        // --- End tambahan kirim email ---

                        transaction.Commit();
                        return Request.CreateResponse(HttpStatusCode.Created, new { Message = "Data berhasil disimpan!" });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return HandleException(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        private static bool SendEmailNotification(string recipientEmail, string message)
        {
            try
            {
                Console.WriteLine("📨 Mengirim email ke: " + recipientEmail);

                using (var client = new SmtpClient("smtp.gmail.com", 587))
                {
                    client.Credentials = new NetworkCredential("opalrohman11@gmail.com", "ghzb xdku sunz hmtn");
                    client.EnableSsl = true;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress("opalrohman11@gmail.com"),
                        Subject = "Pemberitahuan Persetujuan Dokumen",
                        Body = message,
                        IsBodyHtml = false
                    };

                    mailMessage.To.Add(recipientEmail);
                    client.Send(mailMessage);
                }

                Console.WriteLine("✅ Email sukses dikirim ke: " + recipientEmail);
                return true; // Email berhasil dikirim
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Gagal mengirim email: " + ex.Message);
                return false; // Email gagal dikirim
            }
        }
    }
}