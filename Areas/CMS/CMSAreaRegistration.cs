using System.Web.Mvc;

namespace InstallationAPPNonUnify.Areas.CMS
{
    public class CMSAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get
            {
                return "CMS";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                "CMS_default1",
                "CMS/{controller}/{action}/{id}",
                new { controll = "Main",action = "Login", id = UrlParameter.Optional }
            );
        }
    }
}
