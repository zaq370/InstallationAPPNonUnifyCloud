using InstallationAPPNonUnify.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace InstallationAPPNonUnify.Areas.CMS.ViewModels
{
    public class SimulateSettingViewModel : BaseViewModel
    {
        [Display(Name = "resource.CurrentLoginUser")]
        public string CurrentLoginUser { get; set; }

        [Required]
        [Display(Name = "resource.NewLoginUser")]
        public string NewLoginUser { get; set; }

        public SimulateSettingViewModel(string countryCode = "") : base(countryCode) { }
    }
}