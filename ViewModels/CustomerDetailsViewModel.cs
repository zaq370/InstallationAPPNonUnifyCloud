using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace InstallationAPP2024.ViewModels 
{
    public class CustomerDetailsViewModel : BaseViewModel
    {
        [Display(Name = "Resource.Company")]
        public string Company{ get; set;}

        [Display(Name = "Resource.PrimaryContactForename")]
        public string PrimaryContactForename { get; set; }

        [Display(Name = "Resource.PrimaryContactSurname")]
        public string PrimaryContactSurname { get; set; }

        [Display(Name = "Resource.AssociatedContacts")]
        public List<AssociatedContactViewModel> AssociatedContacts { get; set; }

        public CustomerDetailsViewModel(string countryCode = "") : base(countryCode) { }

        public CustomerDetailsViewModel(CustomerDetailsViewModel cdvm, string countryCode = "") : base(countryCode) {
            this.Company = cdvm.Company;
            this.PrimaryContactForename = cdvm.PrimaryContactForename;
            this.PrimaryContactSurname = cdvm.PrimaryContactSurname;
            this.AssociatedContacts = new List<AssociatedContactViewModel>(cdvm.AssociatedContacts);
        }
    }
}