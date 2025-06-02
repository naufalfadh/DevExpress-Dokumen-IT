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
using System.Numerics;

namespace Template_DevExpress_By_MFM.Controllers
{
    public class ManageMasterDokumenRequestController : ApiController
    {

        public GSDbContext GSDbContext { get; set; }
        private SessionLogin sessionLogin = (SessionLogin)System.Web.HttpContext.Current.Session["SHealth"];

        public ManageMasterDokumenRequestController()
        {

            GSDbContext = new GSDbContext(@"NAUFALF", "db_marketing_portal", "sa", "polman");
        }
        protected override void Dispose(bool disposing)
        {
            GSDbContext.Dispose();
        }

        [SessionCheck]
        [HttpGet]
        [Route("api/ManageMasterDokumenRequest/GetLastReferenceNumber")]
        public HttpResponseMessage GetLastReferenceNumber()
        {
            try
            {
                // Ambil kode lokasi dari session
                string userPlant = sessionLogin.userplant; // "K" atau "M"
                string siteCode = userPlant == "K" ? "KRW" : userPlant == "M" ? "SMG" : userPlant;

                string prefix = $"ERP-{siteCode}-";

                // Ambil nomor terakhir dari MasterDokumenRequest yang sesuai prefix
                var lastReference1 = GSDbContext.MasterDokumenRequest
                    .Where(x => x.dok_refnum.StartsWith(prefix))
                    .OrderByDescending(x => x.dok_refnum)
                    .Select(x => x.dok_refnum)
                    .FirstOrDefault();

                // Ambil nomor terakhir dari MasterDokumenRequestP3sp yang sesuai prefix
                var lastReference2 = GSDbContext.MasterDokumenRequestP3sp
                    .Where(x => x.dok_refnum.StartsWith(prefix))
                    .OrderByDescending(x => x.dok_refnum)
                    .Select(x => x.dok_refnum)
                    .FirstOrDefault();

                var lastReference3 = GSDbContext.MasterDokumenRequestPpi
                    .Where(x => x.dok_refnum.StartsWith(prefix))
                    .OrderByDescending(x => x.dok_refnum)
                    .Select(x => x.dok_refnum)
                    .FirstOrDefault();

                int lastRunNumber1 = 0;
                int lastRunNumber2 = 0;
                int lastRunNumber3 = 0;

                if (!string.IsNullOrEmpty(lastReference1))
                {
                    string[] parts1 = lastReference1.Split('-');
                    if (parts1.Length == 3 && int.TryParse(parts1[2], out int parsedNumber1))
                    {
                        lastRunNumber1 = parsedNumber1;
                    }
                }

                if (!string.IsNullOrEmpty(lastReference2))
                {
                    string[] parts2 = lastReference2.Split('-');
                    if (parts2.Length == 3 && int.TryParse(parts2[2], out int parsedNumber2))
                    {
                        lastRunNumber2 = parsedNumber2;
                    }
                }

                if (!string.IsNullOrEmpty(lastReference3))
                {
                    string[] parts2 = lastReference3.Split('-');
                    if (parts2.Length == 3 && int.TryParse(parts2[2], out int parsedNumber3))
                    {
                        lastRunNumber3 = parsedNumber3;
                    }
                }

                // Ambil angka terbesar dari kedua nomor terakhir
                int lastRunNumber = Math.Max(lastRunNumber1, Math.Max(lastRunNumber2, lastRunNumber3));

                int newRunNumber = lastRunNumber + 1;
                string newReference = $"{prefix}{newRunNumber.ToString("D3")}"; // Padding 3 digit

                return Request.CreateResponse(HttpStatusCode.OK, newReference);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
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

                IQueryable<MasterDokumenRequest> dataList;

                if (userRole == "admin" || userRole == "kadeptit")
                {
                    dataList = GSDbContext.MasterDokumenRequest
                        .Include("DetailDokumenRequests")
                        .Include("User")
                        .AsQueryable();

                    if (!string.IsNullOrEmpty(departmentFilter))
                    {
                        dataList = dataList.Where(d => d.dok_section == departmentFilter);
                    }
                }
                else if (userRole == "kadept")
                {
                    dataList = GSDbContext.MasterDokumenRequest
                        .Include("DetailDokumenRequests")
                        .Include("User")
                        .Where(d => d.dok_section == userDepartment)
                        .AsQueryable();
                }
                else
                {
                    dataList = GSDbContext.MasterDokumenRequest
                        .Include("DetailDokumenRequests")
                        .Include("User")
                        .Where(d => d.createBy == userFullName)
                        .AsQueryable();
                }

                // ✅ Proyeksikan hanya field yang dibutuhkan agar id_user tersedia di grid
                var projectedData = dataList.Select(d => new
                {
                    d.dok_id,
                    d.dok_refnum,
                    d.dok_plant,
                    d.dok_section,
                    d.dok_user_erp,
                    d.dok_erp,
                    d.dok_document,
                    d.dok_req_modul,
                    d.dok_reason,
                    d.dok_lampiran,
                    d.dok_ttd_user,
                    d.dok_ttd_kadept,
                    d.dok_ttd_kadeptit,
                    d.dok_approve_k,
                    d.modifDate_k,
                    d.dok_status,
                    d.createBy,
                    d.createDate,
                    d.modifBy,
                    d.modifDate,
                    d.dok_tgl_pembuatan,
                    d.dok_tgl_efektif,
                    d.dok_dilaksanakan,
                    d.dok_tgl_efektif_bast,
                    d.dok_dilaksanakan_bast,
                    d.dok_dilaksanakan_by,
                    d.dok_ttd_user_bast,
                    d.dok_ttd_kadeptit_bast,
                    d.dok_user_bast,
                    d.dok_kadeptit_bast,
                    d.dok_tgl_user_bast,
                    d.dok_tgl_kadeptit_bast,

                    id_user = d.id_user,                // ✅ ID user untuk dropdown
                    usr_nama = d.User.usr_nama,         // ✅ Display untuk SelectBox
                    usr_npk = d.User.usr_npk,           // ✅ Untuk diisi ke field NPK

                    DetailDokumenRequests = d.DetailDokumenRequests.Select(dt => new
                    {
                        dt.detail_id,
                        dt.dok_id,
                        dt.dok_menu,
                        dt.dok_id_menu,
                        dt.dok_access,
                        dt.dok_note
                    })
                });

                var loadResult = DataSourceLoader.Load(projectedData, loadOptions);
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    data = loadResult.data,
                    totalCount = loadResult.totalCount
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

            [SessionCheck]
            [HttpPost]
            public HttpResponseMessage Post([FromBody] MasterDokumenRequest requestData)
            {
                try
                {
                    // Pastikan session tidak null
                    if (sessionLogin == null)
                    {
                        return Request.CreateResponse(HttpStatusCode.Unauthorized, new { Message = "Session Expired" });
                    }

                    // Cek apakah data yang dikirim lengkap
                    if (string.IsNullOrEmpty(requestData.dok_refnum) ||
                        requestData.usr_npk == null ||
                        string.IsNullOrEmpty(requestData.usr_nama) ||
                        string.IsNullOrEmpty(requestData.dok_user_erp) ||
                        string.IsNullOrEmpty(requestData.dok_plant) ||
                        string.IsNullOrEmpty(requestData.dok_section) ||
                        string.IsNullOrEmpty(requestData.dok_req_modul) ||
                        string.IsNullOrEmpty(requestData.dok_document) ||
                        string.IsNullOrEmpty(requestData.dok_reason))
                {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new { Message = "Please complete all required fields!" });
                    }

                    using (var transaction = GSDbContext.Database.BeginTransaction())
                    {
                        try
                        {
                            // Ambil current user dari sessionLogin.npk
                            long? currentUserNpk = null;
                            if (long.TryParse(sessionLogin?.npk?.ToString(), out long parsedNpk))
                            {
                                currentUserNpk = parsedNpk;
                            }

                            var currentUser = GSDbContext.MasterUserForm.FirstOrDefault(u => u.usr_npk == currentUserNpk);

                            if (currentUser == null)
                            {
                                return Request.CreateResponse(HttpStatusCode.NotFound, new { Message = "User login not found in database!" });
                            }

                            var form = new MasterDokumenRequest
                            {
                                dok_refnum = requestData.dok_refnum ?? "",
                                usr_npk = requestData.usr_npk ?? 0,
                                usr_nama = requestData.usr_nama ?? "",
                                id_user = requestData.id_user,
                                dok_user_erp = requestData.dok_user_erp ?? "",
                                dok_plant = requestData.dok_plant ?? "",
                                dok_section = requestData.dok_section ?? "",
                                dok_req_modul = requestData.dok_req_modul ?? "",
                                dok_document = requestData.dok_document ?? "",
                                dok_reason = requestData.dok_reason ?? "",
                                dok_ttd_user = currentUser?.usr_img_ttd ?? "",
                                dok_lampiran = SaveBase64File(requestData.dok_lampiran, requestData.dok_refnum, $"Lampiran_{requestData.dok_refnum}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"),
                                dok_status = 0,
                                createDate = DateTime.UtcNow.AddHours(7),
                                createBy = sessionLogin?.fullname ?? "System"
                            };

                            System.Diagnostics.Debug.WriteLine($"✅ Data yang akan disimpan: {JsonConvert.SerializeObject(form)}");

                            System.Diagnostics.Debug.WriteLine("🔍 Cek Panjang Data Sebelum Simpan Master:");
                            foreach (var prop in form.GetType().GetProperties())
                            {
                                var value = prop.GetValue(form)?.ToString();
                                System.Diagnostics.Debug.WriteLine($"🔹 {prop.Name}: Panjang = {value?.Length}, Nilai = {value}");
                            }

                            // Simpan ke database
                            GSDbContext.MasterDokumenRequest.Add(form);
                            GSDbContext.SaveChanges();

                            System.Diagnostics.Debug.WriteLine($"🆕 Data Master tersimpan dengan dok_id: {form.dok_id}");

                            // Simpan detail jika ada
                            if (requestData.DetailDokumenRequests != null && requestData.DetailDokumenRequests.Any())
                            {
                                var detailList = requestData.DetailDokumenRequests
                                    .Select(detail => new DetailDokumenRequest
                                    {
                                        dok_id = form.dok_id,
                                        dok_menu = detail.dok_menu,
                                        dok_id_menu = detail.dok_id_menu,
                                        dok_access = detail.dok_access,
                                        dok_note = detail.dok_note ?? ""
                                    }).ToList();

                                System.Diagnostics.Debug.WriteLine("🔍 Cek Panjang Data Sebelum Simpan Detail:");
                                foreach (var detail in detailList)
                                {
                                    foreach (var prop in detail.GetType().GetProperties())
                                    {
                                        var value = prop.GetValue(detail)?.ToString();
                                        System.Diagnostics.Debug.WriteLine($"🔹 {prop.Name}: Panjang = {value?.Length}, Nilai = {value}");
                                    }
                                }

                                GSDbContext.DetailDokumenRequest.AddRange(detailList);
                                GSDbContext.SaveChanges();
                            }

                            transaction.Commit();

                            // 🔔 Kirim email ke semua Kadept dengan section yang sesuai
                            var kadeptUsers = GSDbContext.MasterUserForm
                                .Where(u => u.usr_role.ToLower().Contains("kadept") && u.usr_section == requestData.dok_section)
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

    Telah diajukan permintaan dokumen baru dengan rincian sebagai berikut:

    - Nomor Referensi: {requestData.dok_refnum}
    - Plant: {requestData.dok_plant}
    - Modul: {requestData.dok_req_modul}
    - Dokumen: {requestData.dok_document}
    - Alasan: {requestData.dok_reason}
    - Section: {requestData.dok_section}

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



            [SessionCheck]
            [HttpPut]
            [Route("api/ManageMasterDokumenRequest/Update")]
            public HttpResponseMessage Update(long dokId, [FromBody] MasterDokumenRequest requestData)
            {
                try
                {
                    if (sessionLogin == null)
                        return Request.CreateResponse(HttpStatusCode.Unauthorized, "Session expired.");

                    var existing = GSDbContext.MasterDokumenRequest.FirstOrDefault(d => d.dok_id == dokId);
                    if (existing == null)
                        return Request.CreateResponse(HttpStatusCode.NotFound, "Data tidak ditemukan.");

                    existing.usr_npk = requestData.usr_npk ?? 0;
                    existing.usr_nama = requestData.usr_nama ?? "";
                    existing.id_user = requestData.id_user;
                    existing.dok_user_erp = requestData.dok_user_erp ?? "";
                    existing.dok_plant = requestData.dok_plant ?? "";
                    existing.dok_section = requestData.dok_section ?? "";
                    existing.dok_req_modul = requestData.dok_req_modul ?? "";
                    existing.dok_document = requestData.dok_document ?? "";
                    existing.dok_reason = requestData.dok_reason ?? "";
                    existing.dok_ttd_user = requestData.dok_ttd_user ?? "";
                    existing.modifBy = sessionLogin.fullname;
                    existing.modifDate = DateTime.UtcNow.AddHours(7);

                    if (!string.IsNullOrEmpty(requestData.dok_lampiran))
                    {
                        existing.dok_lampiran = SaveBase64File(requestData.dok_lampiran, existing.dok_refnum, $"Lampiran_{existing.dok_refnum}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
                    }

                    // Update Detail
                    var existingDetails = GSDbContext.DetailDokumenRequest.Where(x => x.dok_id == dokId);
                    GSDbContext.DetailDokumenRequest.RemoveRange(existingDetails);
                    if (requestData.DetailDokumenRequests != null && requestData.DetailDokumenRequests.Any())
                    {
                        var newDetails = requestData.DetailDokumenRequests.Select(x => new DetailDokumenRequest
                        {
                            dok_id = dokId,
                            dok_menu = x.dok_menu,
                            dok_id_menu = x.dok_id_menu,
                            dok_access = x.dok_access,
                            dok_note = x.dok_note ?? ""
                        }).ToList();

                        GSDbContext.DetailDokumenRequest.AddRange(newDetails);
                    }

                    GSDbContext.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, new { Message = "Data berhasil diperbarui!" });
                }
                catch (Exception ex)
                {
                    return HandleException(ex);
                }
            }


        [SessionCheck]
        [HttpDelete]
        [Route("api/ManageMasterDokumenRequest/Delete/{id}")]
        public HttpResponseMessage Delete(long id)
        {
            try
            {
                var data = GSDbContext.MasterDokumenRequest.FirstOrDefault(d => d.dok_id == id);

                if (data == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, new { Message = "Data tidak ditemukan." });

                // Hapus detail dulu
                var detailList = GSDbContext.DetailDokumenRequest.Where(d => d.dok_id == id).ToList();
                GSDbContext.DetailDokumenRequest.RemoveRange(detailList);

                // Hapus master
                GSDbContext.MasterDokumenRequest.Remove(data);
                GSDbContext.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, new { Message = "Data berhasil dihapus." });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
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


        [SessionCheck]
        [HttpDelete]
        public HttpResponseMessage Delete(FormDataCollection form)
        {
            try
            {
                var key = Convert.ToInt64(form.Get("key"));
                var order = GSDbContext.MasterDokumenRequest.First(e => e.dok_id == key);

                GSDbContext.MasterDokumenRequest.Remove(order);
                GSDbContext.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK);

            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message.ToString());
            }

        }

        [SessionCheck]
        [HttpPut]
        [Route("api/ManageMasterDokumenRequest/UpdateStatus")]
        public HttpResponseMessage UpdateStatus(FormDataCollection form)
        {
            try
            {
                if (form == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "FormDataCollection is null");

                var keyValue = form.Get("key");
                var statusValue = form.Get("dok_status"); // bisa dipakai kalau mau update ke status dinamis

                if (string.IsNullOrEmpty(keyValue))
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Key tidak boleh kosong");

                var key = Convert.ToInt64(keyValue);
                var master = GSDbContext.MasterDokumenRequest.FirstOrDefault(e => e.dok_id == key);

                if (master == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Dokumen tidak ditemukan");

                if (master.dok_status == 2)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest,
                        "Dokumen sudah direject, silakan ajukan kembali.");
                }

                // Convert session NPK ke long
                long? currentUserNpk = null;
                if (long.TryParse(sessionLogin?.npk?.ToString(), out long parsedNpk))
                {
                    currentUserNpk = parsedNpk;
                }

                if (currentUserNpk == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "NPK user login tidak valid");

                // Ambil user yang sedang login
                var currentUser = GSDbContext.MasterUserForm.FirstOrDefault(u => u.usr_npk == currentUserNpk);

                if (currentUser == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, "User tidak ditemukan di database");

                // Update status & TTD kadept
                master.dok_status = 1;
                master.modifBy = sessionLogin.fullname;
                master.modifDate = DateTime.UtcNow.AddHours(7);
                master.dok_ttd_kadept = currentUser.usr_img_ttd ?? "";

                GSDbContext.SaveChanges();

                // Kirim email ke semua user dengan role 'kadeptit' (tanpa filter section)
                var kadeptItUsers = GSDbContext.MasterUserForm
                    .Where(u => u.usr_role.ToLower().Contains("kadeptit"))
                    .ToList();

                if (kadeptItUsers.Any())
                {
                    foreach (var user in kadeptItUsers)
                    {
                        if (!string.IsNullOrEmpty(user.usr_email))
                        {
                            string emailUser = user.usr_email;
                            LogToFile("Akan mengirim email ke: " + emailUser);

                            string emailMessage = $@"
Yth. {user.usr_nama},

Dokumen request dengan nomor referensi {master.dok_refnum} telah disetujui oleh {currentUser.usr_nama} sebagai {currentUser.usr_role}.

Silakan masuk ke sistem untuk melakukan proses lebih lanjut.

Link sistem: http://localhost:8000/Login
";

                            bool emailSent = SendEmailNotification(emailUser, emailMessage);

                            if (emailSent)
                                Console.WriteLine("✅ Email berhasil dikirim ke Kadept IT: " + emailUser);
                            else
                                Console.WriteLine("⚠️ Gagal mengirim email ke: " + emailUser);
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ Email kosong untuk user: {user.usr_nama}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ Tidak ditemukan user dengan role kadeptit.");
                }

                return Request.CreateResponse(HttpStatusCode.OK, new { Message = "Status berhasil diperbarui & TTD disimpan" });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }


        private void LogToFile(string message)
        {
            try
            {
                var logFilePath = HttpContext.Current.Server.MapPath("~/App_Data/EmailDebugLog.txt");
                System.IO.File.AppendAllText(logFilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n");
            }
            catch { /* ignore error logging */ }
        }



        [SessionCheck]
        [HttpPut]
        [Route("api/ManageMasterDokumenRequest/ApproveIt")]
        public HttpResponseMessage ApproveIt(FormDataCollection form)
        {
            try
            {
                if (form == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "FormDataCollection is null");

                var keyValue = form.Get("key");

                if (string.IsNullOrEmpty(keyValue))
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Key tidak boleh kosong");

                var key = Convert.ToInt64(keyValue);
                var master = GSDbContext.MasterDokumenRequest.FirstOrDefault(e => e.dok_id == key);

                if (master == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Dokumen tidak ditemukan");

                // ✅ Convert NPK dari session ke long?
                long? currentUserNpk = null;
                if (long.TryParse(sessionLogin?.npk?.ToString(), out long parsedNpk))
                {
                    currentUserNpk = parsedNpk;
                }

                if (currentUserNpk == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "NPK user login tidak valid");

                // ✅ Cari user dari tabel user
                var currentUser = GSDbContext.MasterUserForm.FirstOrDefault(u => u.usr_npk == currentUserNpk);

                if (currentUser == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, "User tidak ditemukan di database");

                // ✅ Update status dan tanda tangan IT
                master.dok_status = 3;
                master.dok_approve_k = sessionLogin.fullname;
                master.modifDate_k = DateTime.UtcNow.AddHours(7);
                master.dok_ttd_kadeptit = currentUser.usr_img_ttd ?? "";

                GSDbContext.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, new { Message = "Status berhasil diperbarui & TTD IT disimpan" });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }


        [SessionCheck]
        [HttpPut]
        [Route("api/ManageMasterDokumenRequest/RejectDokumen")]
        public HttpResponseMessage RejectDokumen(FormDataCollection form)
        {
            try
            {
                Console.WriteLine("🛠 Proses UpdateDokumen dimulai...");

                if (form == null)
                {
                    Console.WriteLine("⚠️ FormDataCollection is null");
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "FormDataCollection is null");
                }

                var keyValue = form.Get("dokId");
                if (string.IsNullOrEmpty(keyValue))
                {
                    Console.WriteLine("⚠️ dokId tidak boleh kosong");
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "dokId tidak boleh kosong");
                }

                var key = Convert.ToInt64(keyValue);
                var master = GSDbContext.MasterDokumenRequest.FirstOrDefault(e => e.dok_id == key);

                if (master == null)
                {
                    Console.WriteLine("⚠️ Dokumen tidak ditemukan");
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Dokumen tidak ditemukan");
                }

                Console.WriteLine($"✅ Dokumen ditemukan: dok_id = {key}, dok_status = {master.dok_status}");

                var alasanReject = form.Get("dok_reason_reject");
                if (!string.IsNullOrEmpty(alasanReject))
                {
                    master.dok_reason_reject = alasanReject;
                    Console.WriteLine($"📝 dok_reason_reject diperbarui: {alasanReject}");
                }


                master.dok_status = 2;

                // 🔄 Simpan perubahan
                GSDbContext.SaveChanges();
                Console.WriteLine("✅ Perubahan disimpan ke database.");



                return Request.CreateResponse(HttpStatusCode.OK, new { Message = "Dokumen berhasil diperbarui" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new
                {
                    Message = "Terjadi kesalahan saat memperbarui dokumen",
                    Error = ex.Message,
                    StackTrace = ex.StackTrace
                });
            }
        }
        [SessionCheck]
        [HttpPut]
        [Route("api/ManageMasterDokumenRequest/UpdateDokumen")]
        public HttpResponseMessage UpdateDokumen(FormDataCollection form)
        {
            try
            {
                Console.WriteLine("🛠 Proses UpdateDokumen dimulai...");

                if (form == null)
                {
                    Console.WriteLine("⚠️ FormDataCollection is null");
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "FormDataCollection is null");
                }

                var keyValue = form.Get("dokId");
                if (string.IsNullOrEmpty(keyValue))
                {
                    Console.WriteLine("⚠️ dokId tidak boleh kosong");
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "dokId tidak boleh kosong");
                }

                var key = Convert.ToInt64(keyValue);
                var master = GSDbContext.MasterDokumenRequest.FirstOrDefault(e => e.dok_id == key);

                if (master == null)
                {
                    Console.WriteLine("⚠️ Dokumen tidak ditemukan");
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Dokumen tidak ditemukan");
                }

                Console.WriteLine($"✅ Dokumen ditemukan: dok_id = {key}, dok_status = {master.dok_status}");

                if (master.dok_status == 2 || master.dok_status == 1 || master.dok_status == 0)
                {
                    Console.WriteLine("⛔ Permohonan belum disetujui oleh Kadept dan Kadept IT SM!");
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new
                    {
                        Success = false,
                        Message = "Permohonan belum disetujui oleh Kadept dan Kadept IT SM!"
                    });
                }

                // 🔹 Perbarui dok_tgl_pembuatan jika tersedia di form
                var tglPembuatanStr = form.Get("dok_tgl_pembuatan");
                if (!string.IsNullOrEmpty(tglPembuatanStr) && DateTime.TryParse(tglPembuatanStr, out DateTime tglPembuatan))
                {
                    master.dok_tgl_pembuatan = tglPembuatan;
                    Console.WriteLine($"📅 dok_tgl_pembuatan diperbarui: {tglPembuatan}");
                }

                // 🔹 Perbarui dok_tgl_efektif jika tersedia di form
                var tglEfektifStr = form.Get("dok_tgl_efektif");
                if (!string.IsNullOrEmpty(tglEfektifStr) && DateTime.TryParse(tglEfektifStr, out DateTime tglEfektif))
                {
                    master.dok_tgl_efektif = tglEfektif;
                    Console.WriteLine($"📅 dok_tgl_efektif diperbarui: {tglEfektif}");
                }

                // 🔹 Ambil tanda tangan user dari session dan simpan ke dok_dilaksanakan
                long? currentUserNpk = null;
                if (long.TryParse(sessionLogin?.npk?.ToString(), out long parsedNpk))
                {
                    currentUserNpk = parsedNpk;
                }

                if (currentUserNpk == null)
                {
                    Console.WriteLine("⚠️ NPK user login tidak valid");
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "NPK user login tidak valid");
                }

                var currentUser = GSDbContext.MasterUserForm.FirstOrDefault(u => u.usr_npk == currentUserNpk);

                if (currentUser == null)
                {
                    Console.WriteLine("⚠️ User tidak ditemukan di database");
                    return Request.CreateResponse(HttpStatusCode.NotFound, "User tidak ditemukan di database");
                }

                master.dok_dilaksanakan = currentUser.usr_img_ttd ?? "";
                Console.WriteLine($"🖊 dok_dilaksanakan diisi otomatis dari usr_img_ttd: {master.dok_dilaksanakan}");

                master.dok_dilaksanakan_by = sessionLogin?.fullname ?? "";
                Console.WriteLine($"👤 dok_dilaksanakan_by diisi: {master.dok_dilaksanakan_by}");

                master.dok_status = 4;

                // 🔄 Simpan perubahan
                GSDbContext.SaveChanges();
                Console.WriteLine("✅ Perubahan disimpan ke database.");

              

                return Request.CreateResponse(HttpStatusCode.OK, new { Message = "Dokumen berhasil diperbarui" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new
                {
                    Message = "Terjadi kesalahan saat memperbarui dokumen",
                    Error = ex.Message,
                    StackTrace = ex.StackTrace
                });
            }
        }


        [SessionCheck]
        [HttpPut]
        [Route("api/ManageMasterDokumenRequest/UpdateBast")]
        public HttpResponseMessage UpdateBast(FormDataCollection form)
        {
            try
            {
                Console.WriteLine("🛠 Proses UpdateBast dimulai...");

                if (form == null)
                {
                    Console.WriteLine("⚠️ FormDataCollection is null");
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "FormDataCollection is null");
                }

                var keyValue = form.Get("dokId");
                if (string.IsNullOrEmpty(keyValue))
                {
                    Console.WriteLine("⚠️ dokId tidak boleh kosong");
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "dokId tidak boleh kosong");
                }

                var key = Convert.ToInt64(keyValue);
                var master = GSDbContext.MasterDokumenRequest.FirstOrDefault(e => e.dok_id == key);

                if (master == null)
                {
                    Console.WriteLine("⚠️ Dokumen tidak ditemukan");
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Dokumen tidak ditemukan");
                }

                Console.WriteLine($"✅ Dokumen ditemukan: dok_id = {key}, dok_status = {master.dok_status}");

                if (master.dok_status == 3 || master.dok_status == 2 || master.dok_status == 1 || master.dok_status == 0)
                {
                    Console.WriteLine("⛔ Permohonan belum disetujui oleh Kadept dan Kadept IT SM!");
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new
                    {
                        Success = false,
                        Message = "Permohonan belum disetujui oleh Kadept, Kadept IT SM dan ADMIN!"
                    });
                }

                var tglEfektifbastStr = form.Get("dok_tgl_efektif_bast");
                if (!string.IsNullOrEmpty(tglEfektifbastStr) && DateTime.TryParse(tglEfektifbastStr, out DateTime tglEfektifbast))
                {
                    master.dok_tgl_efektif_bast = tglEfektifbast;
                    Console.WriteLine($"📅 dok_tgl_efektif diperbarui: {tglEfektifbast}");
                }


                master.dok_dilaksanakan_bast = "System Department IT";
                Console.WriteLine($"🖊 dok_dilaksanakan diisi otomatis dari usr_img_ttd: {master.dok_dilaksanakan}");


                master.dok_status = 5;

                // 🔄 Simpan perubahan
                GSDbContext.SaveChanges();
                Console.WriteLine("✅ Perubahan disimpan ke database.");

                // 🔹 Kirim email notifikasi jika sudah disetujui
                long dokCreateByNpk = 0;
                if (!string.IsNullOrEmpty(master.createBy))
                {
                    if (!long.TryParse(master.createBy, out dokCreateByNpk))
                    {
                        Console.WriteLine($"🔍 Mencari NPK berdasarkan nama: {master.createBy}");
                        var userData = GSDbContext.MasterUserForm.FirstOrDefault(u => u.usr_nama == master.createBy);

                        if (userData != null)
                        {
                            dokCreateByNpk = userData.usr_npk ?? 0;
                            Console.WriteLine($"🔄 Menggunakan NPK dari MasterUserForm: {dokCreateByNpk}");
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ User tidak ditemukan berdasarkan nama: {master.createBy}");
                            return Request.CreateResponse(HttpStatusCode.NotFound, "User tidak ditemukan berdasarkan createBy");
                        }
                    }
                }

                Console.WriteLine($"📩 Mencari email berdasarkan NPK: {dokCreateByNpk}");
                var user = GSDbContext.MasterUserForm.FirstOrDefault(u => u.usr_npk == dokCreateByNpk);

                if (user != null)
                {
                    string emailUser = user.usr_email;
                    Console.WriteLine($"✅ Email ditemukan: {emailUser}");

                    // 🔹 Tambahkan link ke dalam email
                    string projectLink = "http://localhost:8000/Login";
                    string emailMessage = $@"
    Yth. {user.usr_nama},

    Kami ingin menginformasikan bahwa pengajuan dokumen request dengan nomor referensi {master.dok_refnum} telah berhasil diajukan. Dokumen terkait dapat segera diproses lebih lanjut sesuai dengan prosedur yang berlaku.

    Berikut adalah rincian pengajuan dokumen request:

    - Nomor Referensi: {master.dok_refnum}
    - Tanggal Pengajuan: {master.dok_tgl_pembuatan:dd MMM yyyy}
    - Status: {master.dok_status}

    Masuk ke sistem untuk melakukan TTD Berita Acara Serah Terima Akses Data.

    Klik link berikut untuk mengakses proyek: {projectLink}
";

                    bool emailSent = SendEmailNotification(emailUser, emailMessage);

                    if (emailSent)
                    {
                        Console.WriteLine("✅ Email berhasil dikirim ke: " + emailUser);
                    }
                    else
                    {
                        Console.WriteLine("⚠️ Email gagal dikirim ke: " + emailUser);
                    }
                }
                else
                {
                    Console.WriteLine($"⚠️ User dengan NPK {dokCreateByNpk} tidak ditemukan di database.");
                }

                return Request.CreateResponse(HttpStatusCode.OK, new { Message = "Dokumen BAST berhasil dibuat" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new
                {
                    Message = "Terjadi kesalahan saat memperbarui dokumen",
                    Error = ex.Message,
                    StackTrace = ex.StackTrace
                });
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




        [SessionCheck]
        [HttpPut]
        [Route("api/ManageMasterDokumenRequest/ApproveBastUser")]
        public HttpResponseMessage ApproveBastUser(FormDataCollection form)
        {
            try
            {
                if (form == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "FormDataCollection is null");

                var keyValue = form.Get("key");
                var statusValue = form.Get("dok_status"); // bisa dipakai kalau mau update ke status dinamis

                if (string.IsNullOrEmpty(keyValue))
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Key tidak boleh kosong");

                var key = Convert.ToInt64(keyValue);
                var master = GSDbContext.MasterDokumenRequest.FirstOrDefault(e => e.dok_id == key);

                if (master == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Dokumen tidak ditemukan");

                if (master.dok_status == 2)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest,
                        "Dokumen sudah direject, silakan ajukan kembali.");
                }

                // ✅ Convert session NPK ke long?
                long? currentUserNpk = null;
                if (long.TryParse(sessionLogin?.npk?.ToString(), out long parsedNpk))
                {
                    currentUserNpk = parsedNpk;
                }

                if (currentUserNpk == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "NPK user login tidak valid");

                // ✅ Ambil user yang sedang login
                var currentUser = GSDbContext.MasterUserForm.FirstOrDefault(u => u.usr_npk == currentUserNpk);

                if (currentUser == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, "User tidak ditemukan di database");

                // ✅ Update status & TTD kadept
                master.dok_status = 6;
                master.dok_user_bast = sessionLogin.fullname;
                master.dok_tgl_user_bast = DateTime.UtcNow.AddHours(7);
                master.dok_ttd_user_bast = currentUser.usr_img_ttd ?? "";

                GSDbContext.SaveChanges();
                var kadeptItUsers = GSDbContext.MasterUserForm
                   .Where(u => u.usr_role.ToLower().Contains("kadeptit"))
                   .ToList();

                if (kadeptItUsers.Any())
                {
                    foreach (var user in kadeptItUsers)
                    {
                        if (!string.IsNullOrEmpty(user.usr_email))
                        {
                            string emailUser = user.usr_email;
                            LogToFile("Akan mengirim email ke: " + emailUser);

                            string emailMessage = $@"
Yth. {user.usr_nama},

Dokumen request dengan nomor referensi {master.dok_refnum} telah ditandatangani oleh {currentUser.usr_nama} 

Dokumen telah melakukan Pembuatan BERITA ACARA SERAH TERIMA DATA AKSES
Silakan masuk ke sistem untuk melakukan proses lebih lanjut.

Link sistem: http://localhost:8000/Login
";

                            bool emailSent = SendEmailNotification(emailUser, emailMessage);

                            if (emailSent)
                                Console.WriteLine("✅ Email berhasil dikirim ke Kadept IT: " + emailUser);
                            else
                                Console.WriteLine("⚠️ Gagal mengirim email ke: " + emailUser);
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ Email kosong untuk user: {user.usr_nama}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ Tidak ditemukan user dengan role kadeptit.");
                }

                return Request.CreateResponse(HttpStatusCode.OK, new { Message = "Status berhasil diperbarui & TTD disimpan" });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [SessionCheck]
        [HttpPut]
        [Route("api/ManageMasterDokumenRequest/ApproveBastKadeptIT")]
        public HttpResponseMessage ApproveBastKadeptIT(FormDataCollection form)
        {
            try
            {
                if (form == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "FormDataCollection is null");

                var keyValue = form.Get("key");
                var statusValue = form.Get("dok_status"); // bisa dipakai kalau mau update ke status dinamis

                if (string.IsNullOrEmpty(keyValue))
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Key tidak boleh kosong");

                var key = Convert.ToInt64(keyValue);
                var master = GSDbContext.MasterDokumenRequest.FirstOrDefault(e => e.dok_id == key);

                if (master == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Dokumen tidak ditemukan");

                if (master.dok_status == 2)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest,
                        "Dokumen sudah direject, silakan ajukan kembali.");
                }

                // ✅ Convert session NPK ke long?
                long? currentUserNpk = null;
                if (long.TryParse(sessionLogin?.npk?.ToString(), out long parsedNpk))
                {
                    currentUserNpk = parsedNpk;
                }

                if (currentUserNpk == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "NPK user login tidak valid");

                // ✅ Ambil user yang sedang login
                var currentUser = GSDbContext.MasterUserForm.FirstOrDefault(u => u.usr_npk == currentUserNpk);

                if (currentUser == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, "User tidak ditemukan di database");

                // ✅ Update status & TTD kadept
                master.dok_status = 7;
                master.dok_kadeptit_bast = sessionLogin.fullname;
                master.dok_tgl_kadeptit_bast = DateTime.UtcNow.AddHours(7);
                master.dok_ttd_kadeptit_bast = currentUser.usr_img_ttd ?? "";

                GSDbContext.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, new { Message = "Status berhasil diperbarui & TTD disimpan" });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
    }
}