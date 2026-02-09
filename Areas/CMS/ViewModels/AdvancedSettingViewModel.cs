using InstallationAPPNonUnify.Areas.CMS.Models;
using InstallationAPPNonUnify.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace InstallationAPPNonUnify.Areas.CMS.ViewModels
{
    public class AdvancedSettingViewModel : BaseViewModel
    {
        [Display(Name="Resource.TestMode")]
        public bool TestMode { get; set; }

        [Display(Name="Resource.CountryList")]
        public List<CountryModel> Country { get; set; }

        [Display(Name = "Resource.OtherSetting")]
        public OtherSettingModel OtherSetting { get; set; }

        [Display(Name = "Resource.SmtpSetting")]
        public SmtpSettingModel SmtpSetting { get; set; }

        [Display(Name = "Resource.TestModeSmtpSetting")]
        public SmtpSettingModel TestModeSmtpSetting { get; set; }

        [Display(Name = "Resource.CRMConnectionSetting")]
        public CRMConnectionSettingModel CRMConnectionSetting { get; set; }

        [Display(Name = "Resource.DBConnectionString")]
        public DBConnectionStringModel DBConnectionString { get; set; }

        public List<LanguageCultureModel> LanguageCulture { get; set; }

        public AdvancedSettingViewModel(string countryCode = "") : base(countryCode) { }
    }


}