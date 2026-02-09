using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace InstallationAPPNonUnify
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.IgnoreRoute("elmah.axd");
            routes.IgnoreRoute("ErrorLog.axd");
            routes.IgnoreRoute("AdminLogging/ErrorLog.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Redirect01",
                url: "{firstSection}/{secondSection}.aspx",
                defaults: new { controller = "Main", action = "Redirect" },
                namespaces: new[] { "InstallationAPPNonUnify.Controllers" }
            );

            routes.MapRoute(
                name: "Redirect02",
                url: "{firstSection}",
                defaults: new { controller = "Main", action = "Redirect" },
                namespaces: new[] { "InstallationAPPNonUnify.Controllers" }
            );

            routes.MapRoute(
                name: "Redirect03",
                url: "{secondSection}.aspx",
                defaults: new { controller = "Main", action = "Redirect" },
                namespaces: new[] { "InstallationAPPNonUnify.Controllers" }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Main", action = "Login", id = UrlParameter.Optional },
                namespaces : new[] { "InstallationAPPNonUnify.Controllers" }
            );
        }
    }
}