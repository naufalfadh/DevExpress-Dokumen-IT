using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Optimization;

namespace Template_DevExpress_By_MFM {

    public class BundleConfig {

        public static void RegisterBundles(BundleCollection bundles) {

            var scriptBundle = new ScriptBundle("~/Scripts/bundle");
            var styleBundle = new StyleBundle("~/Content/bundle");

            // jQuery
            scriptBundle
                .Include("~/Scripts/jquery-3.5.1.js");

            // Bootstrap
            scriptBundle
                .Include("~/Scripts/bootstrap.js");

            // Bootstrap
            styleBundle
                .Include("~/Content/bootstrap.css");

            // Custom site styles
            styleBundle
                .Include("~/Content/Site.css");

            bundles.Add(scriptBundle);
            bundles.Add(styleBundle);

#if !DEBUG
            BundleTable.EnableOptimizations = true;
#else
            BundleTable.EnableOptimizations = true;
#endif
        }
    }
}