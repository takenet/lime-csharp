using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Lime.Web
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.LowercaseUrls = true;

            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");            

            routes.MapRoute(
                name: "Default",
                url: "{action}",
                defaults: new 
                { 
                    controller = "Home", 
                    action = "Index"
                }
            );
        }
    }
}
