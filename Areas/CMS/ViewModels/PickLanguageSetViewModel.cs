using InstallationAPPNonUnify.Modules;
using InstallationAPPNonUnify.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace InstallationAPPNonUnify.Areas.CMS.ViewModels
{
    public class PickLanguageSetViewModel : BaseViewModel
    {
        public Dictionary<string, string> Countries { get; set; }

        [Required]
        [Display(Name="resource.PickupContry1")]
        public string PickedCountry1 { get; set; }

        [Display(Name = "resource.PickupContry2")]
        public string PickedCountry2 { get; set; }

        public static int PickedNumber = 2;

        public PickLanguageSetViewModel(string countryCode = "") : base(countryCode) {
        }

        public void init()
        {
            //取後台設定資料中 國家的設定
            BackendInfo bi = new BackendInfo();
            var counties = bi.ReadCountries();

            Countries = new Dictionary<string, string>();   //國家別

            //裝載
            bool isLanguageOneSet = false, isLanguageTwoSet = false;
            for (var i = 0; i < counties.Count; i++) {
                //如果國家指定的語系非自身的，就不加入Countries(例如Brazil 指定用 Spain的語系，那只需要出現西班牙語系)
                if (counties[i].LanguagePackage.Equals(counties[i].RedirectLanguagePackage)) {
                    Countries.Add(counties[i].LanguagePackage, counties[i].CountryName);
                    if (!isLanguageOneSet) {
                        PickedCountry1 = counties[i].LanguagePackage;
                        isLanguageOneSet = true;
                        continue;
                    }
                    if (!isLanguageTwoSet)
                    {
                        PickedCountry2 = counties[i].LanguagePackage;
                        isLanguageTwoSet = true;
                        continue;
                    }  
                }
            }
        }
    }
}