using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace InstallationAPPNonUnify.ViewModels
{
    public class ChangePasswordViewModel : BaseViewModel 
    {
        [Display(Name = "Resource.UserId")]
        public string UserId { get; set; }

        [Required(ErrorMessage = "Resource.FieldRequired")]
        [Display(Name="Resource.NewPassword")]
        [StringLength(30)]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Resource.FieldRequired")]
        [Display(Name = "Resource.NewPasswordConfirm")]
        [StringLength(30)]
        public string NewPasswordConfirm { get; set; }

        public ChangePasswordViewModel(string countryCode = "") : base(countryCode) { }

    }
}