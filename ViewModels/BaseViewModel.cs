using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using InstallationAPPNonUnify.Modules;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InstallationAPPNonUnify.ViewModels
{
    public class BaseViewModel
    {
        public Dictionary<string, string> FieldDispalyName { get; private set; }
        public string Message { get; set; }
        public string CountryCode {get; private set;}
        public string Layout { get; set; }
        public string AlertMessage { get; set; }
        public int Page { get; set; }
        public string Organization { get; private set; }
        public bool IsTestMode { get; private set; }
        /// <summary>
        /// ture/false:需不需顯示interal no
        /// </summary>
        public bool ShowInternalNo { get; private set; }
        /// <summary>
        /// 日期顯示格式，預設為 "dd/MM/yyyy"
        /// </summary>
        public string DateFormat { get; private set; }

        public BaseViewModel(string countryCode = "")
        {
            if (string.IsNullOrEmpty(countryCode)) countryCode = "en-US";

            CountryCode = countryCode;
            Init();
            Page = 0;
        }
        public BaseViewModel(BaseViewModel bv)
        {
            CountryCode = bv.CountryCode;
            Init();
            Page = 0;
        }
        private void Init()
        {
            var languagePackage = new LanguagePackage(CountryCode);
            FieldDispalyName = languagePackage.GetObjectDisplayName(this.GetType().GetProperties());
            
            //Message = languagePackage.LoadMessagePackage();
            Message = JsonConvert.SerializeObject(languagePackage.LoadMessagePackage()).Replace("\\'","\'");

            //organization
            BackendInfo bi = new BackendInfo();
            Organization = bi.GetSetting("Organization");
            IsTestMode = bi.isTestMode;

            if (!Organization.Equals("JHTDK")) {
                ShowInternalNo = true;
            }

            switch (Organization) {
                case "JHTDK":
                case "JHTNL":
                case "JHTES":
                    DateFormat = "dd-MM-yyyy";
                    break;
                default:
                    DateFormat = "dd/MM/yyyy";
                    break;
            }
        }
    }
}