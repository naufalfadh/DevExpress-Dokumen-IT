//using Template_DevExpress_By_MFM.Models;
//using Template_DevExpress_By_MFM.Utils;
//using System;
//using System.Collections.Generic;
//using System.DirectoryServices;
//using System.Linq;
//using System.Security.Claims;
//using System.Threading.Tasks;
//using System.Web;
//using System.Web.Mvc;

//namespace Template_DevExpress_By_MFM.Controllers
//{
//    public class ChangePasswordController : Controller
//    {
//        public GSDbContext GSDbContext { get; set; }
//        private SessionLogin sessionLogin = (SessionLogin)System.Web.HttpContext.Current.Session["SHealth"];

//        public ChangePasswordController()
//        {

//            GSDbContext = new GSDbContext("JUANSWA", "db_marketing_portal", "sa", "polman");
//        }

//        protected override void Dispose(bool disposing)
//        {
//            GSDbContext.Dispose();
//        }

//        [SessionCheck]
//        public ActionResult Index()
//        {
//            return View();
//        }

//        [SessionCheck]
//        public ActionResult Index2()
//        {
//            return View();
//        }

//        [SessionCheck]
//        public ActionResult PostChangePasswordCustomer(string userpass, string userpass_retype, bool cust)
//        {
//            bool hasil = false;
//            var hasilCode = 0;

//            if (userpass != null && userpass_retype != null && cust == true)
//            {
//                try
//                {
//                    var checkData = GSDbContext.MasterUser.Where(p => p.user_nama == sessionLogin.fullname && p.user_npk == sessionLogin.npk).SingleOrDefault();
//                    checkData.user_pass = Template_DevExpress_By_MFM.Utils.Helper.EncodePassword(userpass_retype, "bangcakrek");
//                    checkData.user_modifDate = DateTime.UtcNow.AddHours(7);
//                    checkData.user_modifBy = sessionLogin.fullname;
//                    GSDbContext.SaveChanges();
//                    hasil = true;
//                    hasilCode = 200;
//                }
//                catch (Exception ex)
//                {
//                    hasilCode = 500;
//                    hasil = false;
//                }       
//            }

//            return Json(new { status = hasil, status_code = hasilCode }, JsonRequestBehavior.AllowGet);
//            //return RedirectToAction("", "Login");
//        }

//        [SessionCheck]
//        public ActionResult PostChangePassword(string userpass, string userpass_retype)
//        {
//            bool hasil = false;
//            var hasilCode = 0;

//            if (userpass != null && userpass_retype != null)
//            {
//                try
//                {
//                    var checkData = GSDbContext.MasterUser.Where(p => p.user_nama == sessionLogin.fullname && p.user_npk == sessionLogin.npk).SingleOrDefault();
//                    checkData.user_pass = Template_DevExpress_By_MFM.Utils.Helper.EncodePassword(userpass_retype, "bangcakrek");
//                    checkData.user_modifDate = DateTime.UtcNow.AddHours(7);
//                    checkData.user_modifBy = sessionLogin.fullname;
//                    GSDbContext.SaveChanges();
//                    hasil = true;
//                    hasilCode = 200;
//                }
//                catch (Exception ex)
//                {
//                    hasilCode = 500;
//                    hasil = false;
//                }       
//                return Json(new { status = hasil, status_code = hasilCode }, JsonRequestBehavior.AllowGet);
//            }

//            return RedirectToAction("", "Login");
//        }


//        [SessionCheck]
//        public ActionResult PostLogin(string username, string userpass, string usertype, string userplant, bool remember)
//        {
//            bool hasil = false;
//            var hasilCode = 0;
//            if (!string.IsNullOrEmpty(usertype) && usertype == "GS")
//            {
//                var initLDAPPath = "dc=gs, dc=astra, dc=co, dc=id";
//                var initLDAPServer = "10.19.48.7";
//                var initShortDomainName = "gs";
//                var DomainAndUsername = "";
//                var strCommu = "LDAP://" + initLDAPServer + "/" + initLDAPPath;
//                DomainAndUsername = initShortDomainName + @"\" + username;

//                var entry = new DirectoryEntry(strCommu, DomainAndUsername, userpass);
//                try
//                {
//                    var search = new DirectorySearcher(entry);
//                    SearchResult result;
//                    search.Filter = "(SAMAccountName=" + username + ")";
//                    search.PropertiesToLoad.Add("cn");
//                    result = search.FindOne();

//                    if (result != null)
//                    {
//                        var passEncrypt = Template_DevExpress_By_MFM.Utils.Helper.EncodePassword(userpass, "bangcakrek");
//                        var checkData = GSDbContext.MasterUser.Where(p => p.user_nama == username).FirstOrDefault();
//                        if (checkData != null)
//                        {
//                            if (checkData.user_status == 1)
//                            {
//                                var role = checkData.user_role;
//                                hasil = true;
//                                hasilCode = 200;

//                                SessionLogin session = new SessionLogin();
//                                session.npk = checkData.user_npk;
//                                session.fullname = checkData.user_nama;
//                                session.userrole = checkData.user_role;
//                                session.userplant = checkData.shift_plant;
//                                session.login_date = DateTime.UtcNow.AddHours(7);
//                                Session["SHealth"] = session;
//                                return Json(new { status = hasil, status_code = hasilCode }, JsonRequestBehavior.AllowGet);
//                            }
//                            else
//                            {
//                                hasilCode = 403;
//                                hasil = false;
//                            }

//                        }
//                        else
//                        {
//                            MasterUser masterUser = new MasterUser();
//                            masterUser.user_nama = username;
//                            masterUser.user_pass = passEncrypt;
//                            masterUser.user_status = 0;
//                            masterUser.user_createBy = username;
//                            masterUser.user_createDate = DateTime.UtcNow.AddHours(7);
//                            masterUser.shift_plant = userplant;
//                            masterUser.user_role = "";

//                            GSDbContext.MasterUser.Add(masterUser);
//                            GSDbContext.SaveChanges();
//                            hasilCode = 403;
//                            hasil = false;
//                        }

//                    }
//                    else
//                    {
//                        hasilCode = 404;
//                        hasil = false;
//                    }
//                }
//                catch (Exception ex)
//                {
//                    hasilCode = 500;
//                    hasil = false;
//                    Console.WriteLine(ex.Message.ToString());
//                }
//            }
//            else if (!string.IsNullOrEmpty(usertype) && usertype == "Local")
//            {
//                try
//                {
//                    if (username == "admin" && userpass == "123456")
//                    {
//                        hasil = true;
//                        hasilCode = 200;

//                        SessionLogin session = new SessionLogin();
//                        session.npk = "010830";
//                        session.fullname = "superadmin";
//                        session.userrole = "superadmin";
//                        session.userplant = userplant;
//                        session.login_date = DateTime.UtcNow.AddHours(7);
//                        Session["SHealth"] = session;
//                        return Json(new { status = hasil, status_code = hasilCode }, JsonRequestBehavior.AllowGet);
//                    }
//                    else
//                    {
//                        var passEncrypt = Template_DevExpress_By_MFM.Utils.Helper.EncodePassword(userpass, "bangcakrek");

//                        var checkData = GSDbContext.MasterUser.Where(p => p.user_nama == username && p.user_pass == passEncrypt).SingleOrDefault();

//                        if (checkData != null)
//                        {
//                            hasil = true;
//                            hasilCode = 200;

//                            SessionLogin session = new SessionLogin();
//                            session.npk = checkData.user_npk;
//                            session.fullname = checkData.user_nama;
//                            session.userrole = checkData.user_role;
//                            session.userplant = checkData.shift_plant;
//                            session.login_date = DateTime.UtcNow.AddHours(7);
//                            Session["SHealth"] = session;
//                        }
//                        else
//                        {
//                            hasilCode = 404;
//                            hasil = false;
//                        }
//                    }
//                }
//                catch (Exception ex)
//                {
//                    hasilCode = 500;
//                    hasil = false;
//                    Console.WriteLine(ex.Message.ToString());
//                }
//            }

//            return Json(new { status = hasil, status_code = hasilCode }, JsonRequestBehavior.AllowGet);
//        }

//    }
//}
