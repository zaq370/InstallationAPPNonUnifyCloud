using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace InstallationAPPNonUnify.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        [Display(Name="Resource.UserName")]
        [Required(ErrorMessage = "Resource.FieldRequired")]
        public string UserName { get; set; }

        [Display(Name = "Resource.UserPassword")]
        [Required(ErrorMessage = "Resource.FieldRequired")]
        public string UserPassword { get; set; }

        public string TestModeMessage { get; set; }

        public LoginViewModel(string countryCode="") : base(countryCode) {}

        public string[] TestUserLanguage { get; set; }
    }
}