using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Web.Http;
using Template_DevExpress_By_MFM.Models;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Newtonsoft.Json;
using Template_DevExpress_By_MFM.Utils;
using OfficeOpenXml.FormulaParsing.Utilities;

namespace Template_DevExpress_By_MFM.Controllers
{
    public class ManageMasterUserFormController : ApiController
    {
        public GSDbContext GSDbContext { get; set; }
        public GSDbContext GSDbContextDev { get; set; }

        private SessionLogin sessionLogin = (SessionLogin)System.Web.HttpContext.Current.Session["SHealth"];

        public ManageMasterUserFormController()
        {
            GSDbContext = new GSDbContext(@"NAUFALF", "db_marketing_portal", "sa", "polman");
            GSDbContextDev = new GSDbContext(@"NAUFALF", "db_marketing_portal", "sa", "polman");
        }

        protected override void Dispose(bool disposing)
        {
            GSDbContext.Dispose();
        }

        [HttpGet]
        [Route("api/ManageMasterUserForm/GetUserById")]
        public HttpResponseMessage GetUserById(int id_user)
        {
            try
            {
                var user = GSDbContext.MasterUserForm.FirstOrDefault(u => u.id_user == id_user);

                if (user == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "User not found");
                }

                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    usr_nama = user.usr_nama,           
                    usr_plant = user.usr_plant,
                    usr_section = user.usr_section,
                    usr_npk = user.usr_npk
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpGet]
        [Route("api/ManageMasterUserForm/GetUserByName")]
        public HttpResponseMessage GetUserByName(string usr_nama)
        {
            try
            {
                var user = GSDbContext.MasterUserForm
                            .FirstOrDefault(u => u.usr_nama.ToLower() == usr_nama.ToLower());

                if (user == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "User not found");
                }

                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    id_user = user.id_user,
                    usr_nama = user.usr_nama,
                    usr_npk = user.usr_npk,
                    usr_section = user.usr_section,
                    usr_plant = user.usr_plant
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }



        [SessionCheck]
        [HttpGet]
        public HttpResponseMessage Get(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var dataList = GSDbContext.MasterUserForm.ToList();
                return Request.CreateResponse(DataSourceLoader.Load(dataList, loadOptions));
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message.ToString());
            }
        }

        [SessionCheck]
        [HttpPost]
        public HttpResponseMessage Post(FormDataCollection form)
        {
            try
            {
                var values = form.Get("values");
                var master = new MasterUserForm();

                JsonConvert.PopulateObject(values, master);

                Validate(master);
                if (!ModelState.IsValid)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState.GetFullErrorMessage());

                // Validasi NPK unik
                bool npkExists = GSDbContext.MasterUserForm.Any(u => u.usr_npk == master.usr_npk);
                if (npkExists)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.Conflict, "NPK sudah digunakan oleh user lain.");
                }

                master.usr_createDate = DateTime.UtcNow.AddHours(7);
                master.usr_createBy = sessionLogin.fullname;

                GSDbContext.MasterUserForm.Add(master);
                GSDbContext.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.Created);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message.ToString());
            }
        }

        [SessionCheck]
        [HttpPut]
        public HttpResponseMessage Put(FormDataCollection form)
        {
            try
            {
                var key = Convert.ToInt64(form.Get("key"));
                var values = form.Get("values");
                var master = GSDbContext.MasterUserForm.First(e => e.id_user == key);

                JsonConvert.PopulateObject(values, master);

                Validate(master);
                if (!ModelState.IsValid)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState.GetFullErrorMessage());

                // Validasi NPK unik (kecuali dirinya sendiri)
                bool npkExists = GSDbContext.MasterUserForm
                    .Any(u => u.usr_npk == master.usr_npk && u.id_user != key);
                if (npkExists)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.Conflict, "NPK sudah digunakan oleh user lain.");
                }

                master.usr_modifDate = DateTime.UtcNow.AddHours(7);
                master.usr_modifBy = sessionLogin.fullname;

                GSDbContext.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message.ToString());
            }
        }


        [SessionCheck]
        [HttpDelete]
        public HttpResponseMessage Delete(FormDataCollection form)
        {
            try
            {
                var key = Convert.ToInt64(form.Get("key"));
                var order = GSDbContext.MasterUserForm.First(e => e.id_user == key);

                GSDbContext.MasterUserForm.Remove(order);
                GSDbContext.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK);

            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message.ToString());
            }

        }


    }
}