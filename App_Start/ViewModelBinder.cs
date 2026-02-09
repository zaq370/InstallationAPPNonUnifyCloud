using InstallationAPPNonUnify.Controllers;
using InstallationAPPNonUnify.ViewModels;
using System;
using System.Web.Mvc;

namespace InstallationAPPNonUnify
{
    public class ViewModelBinder : DefaultModelBinder
    {
        protected override object CreateModel(ControllerContext controllerContext, ModelBindingContext bindingContext, Type modelType)
        {
            //如果是繼承 BaseViewModel的，在bind model時，必須要傳入語系設定
            if (modelType == typeof(BaseViewModel) || modelType.IsSubclassOf(typeof(BaseViewModel))) {
                //如果Controller 是繼承 BaseController，在 BaseController 中已經有取好多語系
                var culture = string.Empty;
                if (controllerContext.Controller.GetType() == typeof(BaseController) ||
                    controllerContext.Controller.GetType().IsSubclassOf(typeof(BaseController))) {
                        culture = ((BaseController)controllerContext.Controller).CountryCode;
                }

                //取不到的走預設值
                if (culture == null || string.IsNullOrEmpty(culture)) culture = "en-US";

                //建立model
                Object[] args = { culture };
                return Activator.CreateInstance(modelType, args);
            }
            else
            {
                //走原生的建立model
                return base.CreateModel(controllerContext, bindingContext, modelType);
            }
        }
    }
}