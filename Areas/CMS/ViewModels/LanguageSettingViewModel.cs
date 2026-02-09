using Newtonsoft.Json.Linq;
using InstallationAPPNonUnify.Modules;
using InstallationAPPNonUnify.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InstallationAPPNonUnify.Areas.CMS.ViewModels
{
    public class LanguageSettingViewModel : BaseViewModel
    {
        public MultiLanguageModel Pack1 { get; set; }
        public MultiLanguageModel Pack2 { get; set; }
        public List<string> Ids { get; set; }
        public Dictionary<string, string> Countries { get; set; }
        public Dictionary<string, string> SelectCountries { get; set; }
        public LanguageSettingViewModel(string countryCode = "") : base(countryCode) {
            SelectCountries = new Dictionary<string, string>();
            Countries = new Dictionary<string, string>();
            Ids = new List<string>();
        }

        /// <summary>
        /// 載入資料
        /// </summary>
        public void init(string country1, string country2) {

            //取多語言包
            LanguagePackage lp = new LanguagePackage();
            var resources = lp.LoadLanguageResources();

            ReadCountries();    //取設定的語言別
            SelectCountries = new Dictionary<string, string>();

            //裝載
            if (!string.IsNullOrEmpty(country1) && Countries.ContainsKey(country1))
            {
                Pack1 = new MultiLanguageModel(resources, country1, Countries[country1]);
                SelectCountries.Add(country1, Countries[country1]);
            }

            if (!string.IsNullOrEmpty(country2) && Countries.ContainsKey(country2))
            {
                Pack2 = new MultiLanguageModel(resources, country2, Countries[country2]);
                SelectCountries.Add(country2, Countries[country2]);
            }

            //將所有field key找出.
            Ids = new List<string>();
            if (Pack1 != null)
                Pack1.Content.ForEach(item => Ids.Add(item.FieldId));

            if (Pack2 != null)
                Pack2.Content.ForEach(item => Ids.Add(item.FieldId));

            Ids = Ids.Distinct().ToList<string>();
        }

        public void ReadCountries()
        {
            //取後台設定資料中 國家的設定
            BackendInfo bi = new BackendInfo();
            var counties = bi.ReadCountries();

            Countries = new Dictionary<string, string>();   //國家別

            //裝載
            for (var i = 0; i < counties.Count; i++) 
                Countries.Add(counties[i].LanguagePackage, counties[i].CountryName);
        }
    }

    public class MultiLanguageModel
    {
        public string CountryCode { get; set; }
        public string CountryCodeDesc { get; set; }
        public List<MultiLanguageContentModel> Content { get; set; }

        public MultiLanguageModel(JObject resources, string countryCode, string countryName) {

            CountryCode = countryCode;
            CountryCodeDesc = countryName;

            Content = new List<MultiLanguageContentModel>();

            foreach (var property in resources.Properties())
            {
                MultiLanguageContentModel mcm = new MultiLanguageContentModel();
                mcm.FieldId = property.Name;

                //顯示名稱
                var packageObj = JObject.Parse(property.Value.ToString()).Value<JObject>("Package");
                JToken tempJToken;
                packageObj.TryGetValue(CountryCode.ToLower(), out tempJToken);
                if (tempJToken == null || string.IsNullOrEmpty(tempJToken.ToString()))
                    packageObj.TryGetValue(countryCode, out tempJToken);
                mcm.FieldDesc = tempJToken == null ? "" : tempJToken.ToString();

                //類型
                var type = JObject.Parse(property.Value.ToString()).Value<string>("Type");
                mcm.Type = type;

                //加到明細中
                Content.Add(mcm);
            }
        }
    }

    public class MultiLanguageContentModel
    {
        public string FieldId { get; set; }
        public string FieldDesc { get; set; }
        public string Type { get; set; }
    }
}