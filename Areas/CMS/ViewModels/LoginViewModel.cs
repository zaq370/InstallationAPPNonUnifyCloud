using InstallationAPPNonUnify.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace InstallationAPPNonUnify.Areas.CMS.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        [Required]
        [Display(Name = "Resource.UserId")]
        public string UserId { get; set; }

        [Required]
        [Display(Name = "Resource.UserPassword")]
        public string UserPassword { get; set; }

        public LoginViewModel(string countryCode = "") : base(countryCode) { }
    }
}