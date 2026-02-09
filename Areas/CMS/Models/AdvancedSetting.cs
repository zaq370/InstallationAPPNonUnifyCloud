using Newtonsoft.Json.Linq;
using InstallationAPPNonUnify.Areas.CMS.ViewModels;
using InstallationAPPNonUnify.Modules;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace InstallationAPPNonUnify.Areas.CMS.Models
{
    public class AdvancedSetting
    {
        public bool GetViewModel(AdvancedSettingViewModel asvm, bool getLanguageCulture = false)
        {
            bool isDataReady = true;

            //重取資料
            if (!BackendFile.IsFileReady) {
                BackendInfo backendInfo = new BackendInfo();
                backendInfo.UpdateFileContent();
            }

            //不影響原先的運作
            var fileContent = new JObject(BackendFile.FileContent);
            asvm.TestMode = bool.Parse(fileContent.GetValue("TestMode").ToString());
            asvm.Country = fileContent.GetValue("Country").ToObject<List<CountryModel>>();
            asvm.OtherSetting = fileContent.GetValue("OtherSetting").ToObject<OtherSettingModel>();
            asvm.SmtpSetting = fileContent.GetValue("SmtpSetting").ToObject<SmtpSettingModel>();
            asvm.TestModeSmtpSetting = fileContent.GetValue("TestModeSmtpSetting").ToObject<SmtpSettingModel>();
            asvm.CRMConnectionSetting = fileContent.GetValue("CRMConnectionSetting").ToObject<CRMConnectionSettingModel>();
            asvm.DBConnectionString = fileContent.GetValue("DBConnectionString").ToObject<DBConnectionStringModel>();
            asvm.LanguageCulture = new List<LanguageCultureModel>();

            asvm.Country = asvm.Country.OrderBy(m => m.Sequence).ToList<CountryModel>(); ;

            //要取所有語系
            if (getLanguageCulture) { 
                LanguageCulture lc = new LanguageCulture();

                if (lc.IsReadSuccessed)
                {
                    foreach (var culture in lc.Content) {
                        JObject cultureObj = JObject.Parse(culture.ToString());

                        LanguageCultureModel lcm = new LanguageCultureModel() { 
                            LangCultureName = culture.Value<string>("LangCultureName"),
                            DisplayName = culture.Value<string>("DisplayName")
                        };

                        //如果這個國家別已經在asvm.Country清單中，就不加入
                        if (!asvm.Country.Exists(item => item.LanguagePackage.ToLower().Equals(lcm.LangCultureName.ToLower())))
                            asvm.LanguageCulture.Add(lcm);
                    }
                }
            }
            return isDataReady;
        }

    }

    public class CountryModel
    {
        [Display(Name = "resource.Sequence")]
        public int Sequence { get; set; }
        [Display(Name = "resource.CountryName")]
        public string CountryName { get; set; }
        [Display(Name = "resource.LanguagePackage")]
        public string LanguagePackage { get; set; }
        [Display(Name = "resource.RedirectLanguagePackage")]
        public string RedirectLanguagePackage { get; set; }
    }

    public class OtherSettingModel
    {
        public string CRMOwner { get; set; }
        public string PasswordEncryption { get; set; }
        public string MultipleCaseProduct { get; set; }
    }

    public class SmtpSettingModel
    {
        [Required]
        public string Ip { get; set; }
        [Required]
        public string Port { get; set; }
        public string Id { get; set; }
        public string Password { get; set; }

        [EmailAddress(ErrorMessage = "Resource.InvalidEmailAddress")]
        [Required]
        public string AdminSender { get; set; }

        [Required]
        public string Admin { get; set; }

        public string Pic { get; set; }
    }

    public class CRMConnectionSettingModel
    {
        [Required]
        public string CRMOrganizationName { get; set; }
        [Required]
        public string CRMServiceUser { get; set; }
        [Required]
        public string CRMServicePassword { get; set; }
        [Required]
        public string CRMDomain { get; set; }
        [Required]
        public string CRMUrl { get; set; }
        [Required]
        public string CRMServiceUrl { get; set; }
        [Required]
        public string CRMDecryptUrl { get; set; }
    }

    public class DBConnectionStringModel
    {
        public string Server { get; set; }
        public string InitialCatalog { get; set; }
        public string PersistSecurityInfo { get; set; }
        public string UserId { get; set; }
        public string Password { get; set; }
        public string TimeOut { get; set; }
    }

    public class LanguageCultureModel
    {
        public string LangCultureName { get; set; }
        public string DisplayName { get; set; }
    }
}