using Newtonsoft.Json.Linq;
using InstallationAPPNonUnify.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace InstallationAPPNonUnify.Areas.CMS.ViewModels
{
    public class BasicSettingViewModel : BaseViewModel
    {
        [Required]
        [Display(Name = "Resource.CompanyName")]
        public string CompanyName { get; set; }

        [Required]
        [Display(Name = "Resource.TelephoneNumber")]
        public string Telphone { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Resource.ServiceMail")]
        public string ServiceEmail { get; set; }

        [Required]
        [Display(Name = "Resource.OfficalWebsite")]
        public string Website { get; set; }

        [Display(Name = "Resource.OfficalWebsite2")]
        public string Website2 { get; set; }

        [Display(Name = "Resource.ShortenLink")]
        public string ShortenLink { get; set; }

        public BasicSettingViewModel(string countryCode = "") : base(countryCode) { }

        public override bool Equals(object obj)
        {
            //return base.Equals(obj);
            return this.Equals(obj as BasicSettingViewModel);
        }

        public bool Equals(BasicSettingViewModel obj)
        {
            bool isEqual = true;

            if (!this.CompanyName.Equals(obj.CompanyName)) isEqual = false;
            if (!this.Telphone.Equals(obj.Telphone)) isEqual = false;
            if (!this.ServiceEmail.Equals(obj.ServiceEmail)) isEqual = false;
            if (!this.Website.Equals(obj.Website)) isEqual = false;
            if (!this.Website2.Equals(obj.Website2)) isEqual = false;
            if (!this.ShortenLink.Equals(obj.ShortenLink)) isEqual = false;

            return isEqual;
        }
    }

    public static class BasicSettingViewModelExtension{
        public static BasicSettingViewModel Transform(this BasicSettingViewModel bsvm, Dictionary<string, string> dic)
        {
            string temp = "";
            if (dic.TryGetValue("CompanyName", out temp)) bsvm.CompanyName = temp;
            if (dic.TryGetValue("Email", out temp)) bsvm.ServiceEmail = temp;
            if (dic.TryGetValue("Tel", out temp)) bsvm.Telphone = temp;
            if (dic.TryGetValue("WebSite", out temp)) bsvm.Website = temp;
            if (dic.TryGetValue("WebSite2", out temp)) bsvm.Website2 = temp;
            if (dic.TryGetValue("ShortenLink", out temp)) bsvm.ShortenLink = temp;
            return bsvm;
        }

        public static JObject ToJObject(this BasicSettingViewModel bsvm)
        {
            JObject result = new JObject();
            result.Add(new JProperty("CompanyName", bsvm.CompanyName));
            result.Add(new JProperty("Email", bsvm.ServiceEmail));
            result.Add(new JProperty("Tel", bsvm.Telphone));
            result.Add(new JProperty("WebSite", bsvm.Website));
            result.Add(new JProperty("WebSite2", bsvm.Website2));
            result.Add(new JProperty("ShortenLink", bsvm.ShortenLink));

            return result;
        }
    }
        
}