﻿using System;
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

                if (userRole == "admin" || userRole == "kadeptit" || userRole == "kadiv")
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

        [SessionCheck]
        [HttpPut]
        [Route("api/ManageMasterDokumenRequestPpi/{dok_id}")]
        public HttpResponseMessage Update(long dok_id, [FromBody] MasterDokumenRequestPpi requestData)
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

                // Validasi field penting
                if (string.IsNullOrEmpty(requestData.dok_refnum) ||
                    string.IsNullOrEmpty(requestData.usr_nama) ||
                    string.IsNullOrEmpty(requestData.dok_user_erp) ||
                    string.IsNullOrEmpty(requestData.dok_plant) ||
                    string.IsNullOrEmpty(requestData.dok_section) ||
                    string.IsNullOrEmpty(requestData.dok_tingkat) ||
                    string.IsNullOrEmpty(requestData.dok_jenis_pekerjaan) ||
                    string.IsNullOrEmpty(requestData.dok_superuser) ||
                    string.IsNullOrEmpty(requestData.dok_jenis_superuser) ||
                    string.IsNullOrEmpty(requestData.dok_judul_request) ||
                    string.IsNullOrEmpty(requestData.dok_document) ||
                    string.IsNullOrEmpty(requestData.dok_reason) ||
                    string.IsNullOrEmpty(requestData.dok_spesifikasi))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new { Message = "Beberapa data master tidak valid atau kosong!" });
                }

                using (var transaction = GSDbContext.Database.BeginTransaction())
                {
                    try
                    {
                        var existingData = GSDbContext.MasterDokumenRequestPpi.FirstOrDefault(d => d.dok_id == dok_id);
                        if (existingData == null)
                        {
                            return Request.CreateResponse(HttpStatusCode.NotFound, new { Message = "Data tidak ditemukan!" });
                        }

                        // Update field
                        existingData.dok_refnum = requestData.dok_refnum;
                        existingData.usr_npk = requestData.usr_npk ?? existingData.usr_npk;
                        existingData.usr_nama = requestData.usr_nama;
                        existingData.dok_user_erp = requestData.dok_user_erp;
                        existingData.dok_plant = requestData.dok_plant;
                        existingData.dok_section = requestData.dok_section;
                        existingData.dok_tingkat = requestData.dok_tingkat;
                        existingData.dok_jenis_pekerjaan = requestData.dok_jenis_pekerjaan;
                        existingData.dok_superuser = requestData.dok_superuser;
                        existingData.dok_jenis_superuser = requestData.dok_jenis_superuser;
                        existingData.dok_judul_request = requestData.dok_judul_request;
                        existingData.dok_document = requestData.dok_document;
                        existingData.dok_reason = requestData.dok_reason;
                        existingData.dok_spesifikasi = requestData.dok_spesifikasi;

                        if (!string.IsNullOrEmpty(requestData.dok_lampiran))
                        {
                            existingData.dok_lampiran = SaveBase64File(requestData.dok_lampiran, existingData.dok_refnum, $"Lampiran_{existingData.dok_refnum}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
                        }

                        existingData.dok_ttd_user = requestData.dok_ttd_user ?? existingData.dok_ttd_user;

                        existingData.modifDate = DateTime.UtcNow.AddHours(7);
                        existingData.modifBy = sessionLogin?.fullname ?? "System";

                        GSDbContext.SaveChanges();

                        transaction.Commit();

                        return Request.CreateResponse(HttpStatusCode.OK, new { Message = "Data berhasil diperbarui!" });
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
        [HttpDelete]
        [Route("api/ManageMasterDokumenRequestPpi/{dok_id}")]
        public HttpResponseMessage Delete(long dok_id)
        {
            try
            {
                if (sessionLogin == null)
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, new { Message = "Session expired" });

                var entity = GSDbContext.MasterDokumenRequestPpi.FirstOrDefault(d => d.dok_id == dok_id);
                if (entity == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, new { Message = "Data tidak ditemukan" });

                GSDbContext.MasterDokumenRequestPpi.Remove(entity);
                GSDbContext.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, new { Message = "Data berhasil dihapus" });
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }


        [SessionCheck]
        [HttpPut]
        [Route("api/ManageMasterDokumenRequestPpi/UpdateStatus")]
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
                var master = GSDbContext.MasterDokumenRequestPpi.FirstOrDefault(e => e.dok_id == key);

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

                // --- Tambah pengiriman email ke semua user role kadeptit (tanpa filter section) ---
                var kadeptitUsers = GSDbContext.MasterUserForm
                        .Where(u => u.usr_role != null && u.usr_role.Equals("kadeptit", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                if (kadeptitUsers != null && kadeptitUsers.Any())
                {
                    foreach (var kadeptit in kadeptitUsers)
                    {
                        if (!string.IsNullOrEmpty(kadeptit.usr_email))
                        {
                            string emailKadeptit = kadeptit.usr_email;
                            string emailMessage = $@"
Yth. {kadeptit.usr_nama},

Status dokumen dengan nomor referensi {master.dok_refnum} telah disetujui oleh {sessionLogin.fullname}.

Silakan cek sistem untuk informasi lebih lanjut.

Link sistem: http://localhost:8000/Login
";

                            bool emailSent = SendEmailNotification(emailKadeptit, emailMessage);

                            if (emailSent)
                            {
                                Console.WriteLine("✅ Email berhasil dikirim ke KadeptIT: " + emailKadeptit);
                            }
                            else
                            {
                                Console.WriteLine("⚠️ Gagal mengirim email ke: " + emailKadeptit);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ Email KadeptIT kosong untuk user: {kadeptit.usr_nama}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ Tidak ditemukan user dengan role KadeptIT");
                }
                // --- End pengiriman email ---

                return Request.CreateResponse(HttpStatusCode.OK, new { Message = "Status berhasil diperbarui & TTD disimpan" });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [SessionCheck]
        [HttpPut]
        [Route("api/ManageMasterDokumenRequestPpi/RejectDokumen")]
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

                var keyValue = form.Get("dok_id");
                if (string.IsNullOrEmpty(keyValue))
                {
                    Console.WriteLine("⚠️ dok_id tidak boleh kosong");
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "dok_id tidak boleh kosong");
                }

                var key = Convert.ToInt64(keyValue);
                var master = GSDbContext.MasterDokumenRequestPpi.FirstOrDefault(e => e.dok_id == key);

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
        [Route("api/ManageMasterDokumenRequestPpi/ApproveIt")]
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
                var master = GSDbContext.MasterDokumenRequestPpi.FirstOrDefault(e => e.dok_id == key);

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
                master.dok_status = 12;
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
        [Route("api/ManageMasterDokumenRequestPpi/ApproveKadiv")]
        public HttpResponseMessage ApproveKadiv(FormDataCollection form)
        {
            try
            {
                if (form == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "FormDataCollection is null");

                var keyValue = form.Get("key");

                if (string.IsNullOrEmpty(keyValue))
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Key tidak boleh kosong");

                var key = Convert.ToInt64(keyValue);
                var master = GSDbContext.MasterDokumenRequestPpi.FirstOrDefault(e => e.dok_id == key);

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
                master.dok_approve_kadiv = sessionLogin.fullname;
                master.modifDate_kadiv = DateTime.UtcNow.AddHours(7);
                master.dok_ttd_kadivit = currentUser.usr_img_ttd ?? "";

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
        [Route("api/ManageMasterDokumenRequestPip/UpdateDokumen")]
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

                var keyValue = form.Get("dok_id");
                if (string.IsNullOrEmpty(keyValue))
                {
                    Console.WriteLine("⚠️ dok_id tidak boleh kosong");
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "dok_id tidak boleh kosong");
                }

                var key = Convert.ToInt64(keyValue);
                var master = GSDbContext.MasterDokumenRequestPpi.FirstOrDefault(e => e.dok_id == key);

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
                var tglPenerimaanStr = form.Get("dok_tgl_penerimaan");
                if (!string.IsNullOrEmpty(tglPenerimaanStr) && DateTime.TryParse(tglPenerimaanStr, out DateTime tglPenerimaan))
                {
                    master.dok_tgl_penerimaan = tglPenerimaan;
                    Console.WriteLine($"📅 dok_tgl_penerimaan diperbarui: {tglPenerimaan}");
                }

                // 🔹 Perbarui dok_tgl_dibutuhkan jika tersedia di form
                var tglDibutuhkanStr = form.Get("dok_tgl_dibutuhkan");
                if (!string.IsNullOrEmpty(tglDibutuhkanStr) && DateTime.TryParse(tglDibutuhkanStr, out DateTime tglDibutuhkan))
                {
                    master.dok_tgl_dibutuhkan = tglDibutuhkan;
                    Console.WriteLine($"📅 dok_tgl_dibutuhkan diperbarui: {tglDibutuhkan}");
                }

                var tglPekerjaanStr = form.Get("dok_tgl_pekerjaan");
                if (!string.IsNullOrEmpty(tglPekerjaanStr) && DateTime.TryParse(tglPekerjaanStr, out DateTime tglPekerjaan))
                {
                    master.dok_tgl_pekerjaan = tglPekerjaan;
                    Console.WriteLine($"📅 dok_tgl_pekerjaan diperbarui: {tglPekerjaan}");
                }

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


                master.dok_dilaksanakan_oleh = sessionLogin?.fullname ?? "";
                Console.WriteLine($"👤 dok_dilaksanakan_by diisi: {master.dok_dilaksanakan_oleh}");

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
        [Route("api/ManageMasterDokumenRequestPpi/UpdateBast")]
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

                var keyValue = form.Get("dok_id");
                if (string.IsNullOrEmpty(keyValue))
                {
                    Console.WriteLine("⚠️ dok_id tidak boleh kosong");
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "dok_id tidak boleh kosong");
                }

                var key = Convert.ToInt64(keyValue);
                var master = GSDbContext.MasterDokumenRequestPpi.FirstOrDefault(e => e.dok_id == key);

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
                Console.WriteLine($"🖊 dok_dilaksanakan diisi otomatis dari usr_img_ttd: {master.dok_dilaksanakan_bast}");


                master.dok_status = 5;

                // 🔄 Simpan perubahan
                GSDbContext.SaveChanges();

                var users = GSDbContext.MasterUserForm
                       .Where(u => u.usr_role != null && u.usr_role.Equals("user", StringComparison.OrdinalIgnoreCase))
                       .ToList();

                if (users != null && users.Any())
                {
                    foreach (var user in users)
                    {
                        if (!string.IsNullOrEmpty(user.usr_email))
                        {
                            string emailUser = user.usr_email;
                            string emailMessage = $@"
Yth. {user.usr_nama},

Status dokumen dengan nomor referensi {master.dok_refnum} telah dilaukan test oleh {sessionLogin.fullname}.

Silakan cek sistem untuk informasi lebih lanjut.

Link sistem: http://localhost:8000/Login
";

                            bool emailSent = SendEmailNotification(emailUser, emailMessage);


                        }
                        else
                        {
                            Console.WriteLine($"⚠️ Email  kosong untuk user: {user.usr_nama}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ Tidak ditemukan user dengan role KadeptIT");
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

        [SessionCheck]
        [HttpPut]
        [Route("api/ManageMasterDokumenRequestPpi/ApproveBastUser")]
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
                var master = GSDbContext.MasterDokumenRequestPpi.FirstOrDefault(e => e.dok_id == key);

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
                master.dok_ttd_user_bast = currentUser.usr_img_ttd ?? "";
                master.dok_tgl_bast_user = DateTime.UtcNow.AddHours(7);

                GSDbContext.SaveChanges();

                var kadeptitUsers = GSDbContext.MasterUserForm
                       .Where(u => u.usr_role != null && u.usr_role.Equals("kadeptit", StringComparison.OrdinalIgnoreCase))
                       .ToList();

                if (kadeptitUsers != null && kadeptitUsers.Any())
                {
                    foreach (var kadeptit in kadeptitUsers)
                    {
                        if (!string.IsNullOrEmpty(kadeptit.usr_email))
                        {
                            string emailKadeptit = kadeptit.usr_email;
                            string emailMessage = $@"
Yth. {kadeptit.usr_nama},

Status dokumen dengan nomor referensi {master.dok_refnum} telah disetujui BAST oleh {sessionLogin.fullname}.

Silakan cek sistem untuk informasi lebih lanjut.

Link sistem: http://localhost:8000/Login
";

                            bool emailSent = SendEmailNotification(emailKadeptit, emailMessage);

                            if (emailSent)
                            {
                                Console.WriteLine("✅ Email berhasil dikirim ke KadeptIT: " + emailKadeptit);
                            }
                            else
                            {
                                Console.WriteLine("⚠️ Gagal mengirim email ke: " + emailKadeptit);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ Email KadeptIT kosong untuk user: {kadeptit.usr_nama}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ Tidak ditemukan user dengan role KadeptIT");
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
        [Route("api/ManageMasterDokumenRequestPpi/ApproveBastKadeptIT")]
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
                var master = GSDbContext.MasterDokumenRequestPpi.FirstOrDefault(e => e.dok_id == key);

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
                master.dok_status = 13;
                master.dok_ttd_kadeptit_bast = currentUser.usr_img_ttd ?? "";
                master.dok_tgl_bast_kadeptit = DateTime.UtcNow.AddHours(7);

                GSDbContext.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, new { Message = "Status berhasil diperbarui & TTD disimpan" });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }


        [SessionCheck]
        [HttpPut]
        [Route("api/ManageMasterDokumenRequestPpi/ApproveBastKadept")]
        public HttpResponseMessage ApproveBastKadept(FormDataCollection form)
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
                var master = GSDbContext.MasterDokumenRequestPpi.FirstOrDefault(e => e.dok_id == key);

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
                master.dok_ttd_kadept_bast = currentUser.usr_img_ttd ?? "";
                master.dok_tgl_bast_kadept = DateTime.UtcNow.AddHours(7);

                GSDbContext.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, new { Message = "Status berhasil diperbarui & TTD disimpan" });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
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