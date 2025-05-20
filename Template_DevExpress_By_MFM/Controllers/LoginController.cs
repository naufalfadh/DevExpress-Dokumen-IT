using System;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using System.Web.Mvc;
using Template_DevExpress_By_MFM.Models;

namespace Template_DevExpress_By_MFM.Controllers
{
    public class LoginController : Controller
    {
        public GSDbContext GSDbContext { get; set; }
        public GSDbContext GSDbContextDev { get; set; }

        public LoginController()
        {
            GSDbContext = new GSDbContext("NAUFALF", "db_marketing_portal", "sa", "polman");
            GSDbContextDev = new GSDbContext("NAUFALF", "db_marketing_portal", "sa", "polman");
        }

        protected override void Dispose(bool disposing)
        {
            GSDbContext.Dispose();
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult PostLogin(string username, string userpass, string userplant, string userdepartment)
        {
            bool hasil = false;
            var hasilCode = 0;

            try
            {
                if (username == "admin" && userpass == "213020")
                {
                    hasil = true;
                    hasilCode = 200;

                    SessionLogin session = new SessionLogin();
                    session.npk = "010830";
                    session.fullname = "superadmin";
                    session.userrole = "superadmin";
                    session.userplant = userplant;
                    session.userdepartment = userdepartment;
                    session.login_date = DateTime.UtcNow.AddHours(7);
                    Session["SHealth"] = session;

                    return Json(new { status = hasil, status_code = hasilCode }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    long npkLong;
                    if (!long.TryParse(username, out npkLong))
                    {
                        return Json(new { status = false, status_code = 400, message = "NPK harus berupa angka." }, JsonRequestBehavior.AllowGet);
                    }

                    var checkData = GSDbContext.MasterUserForm
                        .Where(p => p.usr_npk == npkLong && p.usr_pass == userpass)
                        .SingleOrDefault();

                    if (checkData != null)
                    {
                        hasil = true;
                        hasilCode = 200;

                        SessionLogin session = new SessionLogin();
                        session.npk = checkData.usr_npk.ToString(); // Convert ke string agar kompatibel dengan session
                        session.fullname = checkData.usr_nama;
                        session.userrole = checkData.usr_role;
                        session.userplant = checkData.usr_plant;
                        session.userdepartment = checkData.usr_section;
                        session.login_date = DateTime.UtcNow.AddHours(7);
                        Session["SHealth"] = session;
                    }
                    else
                    {
                        hasilCode = 404;
                        hasil = false;
                    }
                }
            }
            catch (Exception ex)
            {
                hasilCode = 500;
                hasil = false;
                Console.WriteLine(ex.Message.ToString());
            }

            return Json(new { status = hasil, status_code = hasilCode }, JsonRequestBehavior.AllowGet);
        }



        public ActionResult Logout()
        {
            Session["SHealth"] = null;
            return RedirectToAction("", "Login");
        }



        // FUNCTION LAST LOGIN
        public bool SaveHistoryLogin(string program, string username, string reason, int status_login, string ip_source)
        {
            Boolean bResult = false;
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                   | SecurityProtocolType.Tls11
                   | SecurityProtocolType.Tls12
                   | SecurityProtocolType.Ssl3;

            System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return true; };

            var token = GenerateToken();

            var clientID = ReadFile(5, "C:/tex.txt");
            var clientSecret = ReadFile(6, "C:/tex.txt");


            if (!string.IsNullOrEmpty(token))
                bResult = true;

            if (bResult)
            {
                string url_api = "https://gs-api.gs.astra.co.id/api/log/last_login";
                HttpWebRequest myReq = (HttpWebRequest)WebRequest.Create(url_api);
                myReq.Method = "POST";
                myReq.ContentType = "application/x-www-form-urlencoded";
                myReq.Headers.Add("Authorization", ("Bearer " + token));
                myReq.Headers.Add("clientid", clientID);
                myReq.Headers.Add("clientsecret", clientSecret);
                string myData = "program=" + HttpUtility.UrlEncode(program) + "&username=" + HttpUtility.UrlEncode(username) + "&reason=" + HttpUtility.UrlEncode(reason) + "&status_login=" + HttpUtility.UrlEncode(status_login.ToString()) + "&ip_source=" + HttpUtility.UrlEncode(ip_source);

                string responseFromServer = "";
                try
                {
                    myReq.ContentLength = myData.Length;
                    using (var dataStream = myReq.GetRequestStream())
                    {
                        dataStream.Write(System.Text.Encoding.UTF8.GetBytes(myData), 0, myData.Length);
                    }
                    using (WebResponse response = myReq.GetResponse())
                    {
                        using (Stream stream = response.GetResponseStream())
                        {
                            StreamReader reader = new StreamReader(stream);
                            responseFromServer = reader.ReadToEnd();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message.ToString());
                    throw;
                }

                if (responseFromServer != null)
                {
                    var result = Newtonsoft.Json.JsonConvert.DeserializeObject(responseFromServer, typeof(JsonApi_Result)) as JsonApi_Result;
                    if (result != null)
                    {
                        if (result.meta[0].code == 200 && result.meta[0].status == "success")
                        {
                            bResult = true;
                        }
                        else
                        {
                            bResult = false;
                        }
                    }
                    else
                    {
                        bResult = false;
                    }
                }
            }

            return bResult;
        }

        public string GenerateToken()
        {
            var sToken = "";

            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                   | SecurityProtocolType.Tls11
                   | SecurityProtocolType.Tls12
                   | SecurityProtocolType.Ssl3;

            var user = ReadFile(0, "C:/tex.txt");
            var pass = ReadFile(1, "C:/tex.txt");
            var grant = ReadFile(2, "C:/tex.txt");

            System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return true; };

            string url_api = "https://gs-api.gs.astra.co.id/generate-token";
            HttpWebRequest myReq = (HttpWebRequest)WebRequest.Create(url_api);
            myReq.Method = "POST";
            myReq.ContentType = "application/x-www-form-urlencoded";
            string myData = "username=" + HttpUtility.UrlEncode(user) + "&password=" + HttpUtility.UrlEncode(pass) + "&grant_type=" + HttpUtility.UrlEncode(grant);

            string responseFromServer = "";
            try
            {
                myReq.ContentLength = myData.Length;
                using (var dataStream = myReq.GetRequestStream())
                {
                    dataStream.Write(System.Text.Encoding.UTF8.GetBytes(myData), 0, myData.Length);
                }
                using (WebResponse response = myReq.GetResponse())
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(stream);
                        responseFromServer = reader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
                throw;
            }

            if (responseFromServer != null)
            {
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject(responseFromServer, typeof(APIModel)) as APIModel;
                if (result != null)
                    if (!string.IsNullOrEmpty(result.access_token))
                        sToken = result.access_token;
            }

            return sToken;
        }

        public string ReadFile(int urutan, string locdir)
        {
            var sResult = "";
            try
            {
                using (var sr = new StreamReader(locdir))
                {
                    var text = sr.ReadToEnd();
                    var sVar = text.Split(';');
                    sResult = sVar[urutan];
                    sr.Dispose();
                    sr.Close();
                }
            }
            catch (IOException e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }

            return sResult;
        }
    }
}
