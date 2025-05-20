using System.Web;
using System.Web.Mvc;

namespace Template_DevExpress_By_MFM {
    public class FilterConfig {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters) {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
