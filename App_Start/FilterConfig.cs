using InstallationAPPNonUnify.Modules.Filters;
using System.Web;
using System.Web.Mvc;

namespace InstallationAPPNonUnify
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new GlobalExceptionHandlerAttribute());
            filters.Add(new HandleErrorAttribute());
        }
    }
}