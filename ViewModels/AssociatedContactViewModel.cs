using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace InstallationAPPNonUnify.ViewModels
{
    public class AssociatedContactViewModel : BaseViewModel
    {
        [Display(Name = "Resource.Forename")]
        public string Forename { get; set; }

        [Display(Name = "Resource.Surname")]
        public string Surname { get; set; }

        public AssociatedContactViewModel(string countryCode = "") : base(countryCode) { }

    }
}