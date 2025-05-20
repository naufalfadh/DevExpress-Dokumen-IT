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

namespace Template_DevExpress_By_MFM.Controllers
{
    public class ManageMasterUserController : ApiController
    {
        public GSDbContext GSDbContext { get; set; }
        public GSDbContext GSDbContextDev { get; set; }

        private SessionLogin sessionLogin = (SessionLogin)System.Web.HttpContext.Current.Session["SHealth"];

        public ManageMasterUserController()
        {
            GSDbContext = new GSDbContext(@"JUANSWA", "db_marketing_portal", "sa", "polman");
            GSDbContextDev = new GSDbContext(@"JUANSWA", "db_marketing_portal", "sa", "polman");
        }

        protected override void Dispose(bool disposing)
        {
            GSDbContext.Dispose();
        }

        [SessionCheck]
        [HttpGet]
        public HttpResponseMessage Get(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var dataList = GSDbContext.MasterUser.ToList();
                return Request.CreateResponse(DataSourceLoader.Load(dataList, loadOptions));
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message.ToString());
            }
        }

        //[SessionCheck]
        //[HttpGet]
        //public HttpResponseMessage Get(DataSourceLoadOptions loadOptions, string CustID)
        //{
        //    try
        //    {
        //        if (master.user_role.Equals("customer") || master.user_role == "customer")
        //        {
        //            string sSQLSelect = "select customer_id from tlkp_cust where customer_id = '" + master.ref_id_cust + "'";

        //            var checkLastIDCust = GSDbContext.Database.SqlQuery<GetLastCustID>(sSQLSelect).SingleOrDefault();
        //            if (checkLastIDCust != null)
        //            {
        //                if (!string.IsNullOrEmpty(checkLastIDCust.cust_id))
        //                {
        //                    noIDCust = "CUST" + checkLastIDCust.cust_id.ToString().PadLeft(3, '0');
        //                    master.user_npk = noIDCust;
        //                }
        //            }
        //        }

        //        return Request.CreateResponse(HttpStatusCode.Created, noIDLastOrder);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message.ToString());
        //    }
        //}

        [SessionCheck]
        [HttpPost]
        public HttpResponseMessage Post(FormDataCollection form)
        {
            try
            {
                var values = form.Get("values");
                var master = new MasterUser();
                var noIDCust = "";

                JsonConvert.PopulateObject(values, master);

                Validate(master);
                if (!ModelState.IsValid)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState.GetFullErrorMessage());

                if (!string.IsNullOrEmpty(values) && values.Contains("user_pass"))
                    master.user_pass = Helper.EncodePassword(master.user_pass, "bangcakrek");

                master.user_createDate = DateTime.UtcNow.AddHours(7);
                master.user_createBy = sessionLogin.fullname;

                //if (master.user_role.Equals("customer") || master.user_role == "customer")
                //{
                //    string sSQLSelect = "select customer_id as cust_id from tlkp_cust where customer_id = '" + master.ref_id_cust + "'";

                //    var checkLastIDCust = GSDbContextDev.Database.SqlQuery<GetLastCustID>(sSQLSelect).SingleOrDefault();
                //    if (checkLastIDCust != null)
                //    {
                //        if (!string.IsNullOrEmpty(checkLastIDCust.cust_id.ToString()))
                //        {
                //            noIDCust = "CUST" + checkLastIDCust.cust_id.ToString().PadLeft(3, '0');
                //            master.user_npk = noIDCust;
                //        }
                //    }
                //}

                GSDbContext.MasterUser.Add(master);
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
                var master = GSDbContext.MasterUser.First(e => e.id_user == key);

                JsonConvert.PopulateObject(values, master);

                Validate(master);
                if (!ModelState.IsValid)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState.GetFullErrorMessage());

                if (!string.IsNullOrEmpty(values) && values.Contains("user_pass"))
                    master.user_pass = Helper.EncodePassword(master.user_pass, "bangcakrek");
                master.user_modifDate = DateTime.UtcNow.AddHours(7);
                master.user_modifBy = sessionLogin.fullname;
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
                var order = GSDbContext.MasterUser.First(e => e.id_user == key);

                GSDbContext.MasterUser.Remove(order);
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