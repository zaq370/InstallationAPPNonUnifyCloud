using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace InstallationAPPNonUnify.ViewModels
{
    public class ForegetPasswordViewModel : BaseViewModel
    {
        [Display(Name = "Resource.UserName")]
        [Required(ErrorMessage = "Resource.FieldRequired")]
        public string UserName { get; set; }

        public ForegetPasswordViewModel(string countryCode = "") : base(countryCode) { }
    }
}