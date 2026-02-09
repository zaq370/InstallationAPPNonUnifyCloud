using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace InstallationAPPNonUnify.ViewModels
{
    public class EmailUsViewModel : BaseViewModel
    {
        [Display(Name = "Resource.Company")]
        [Required(ErrorMessage = "Resource.CompanyRequired")]
        public string Company { get; set; }

        [Display(Name = "Resource.FullName")]
        [Required(ErrorMessage = "Resource.FullNameRequired")]
        public string FullName { get; set; }

        [Display(Name = "Resource.TelephoneNumber")]
        [Required(ErrorMessage = "Resource.TelephoneNumberRequired")]
        public string TelephoneNumber { get; set; }

        [Display(Name = "Resource.MobileNumber")]
        public string MobileNumber { get; set; }

        [Display(Name = "Resource.EmailAddress")]
        [RegularExpression(@"^(([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)(\s*(;|,)\s*|\s*$))*$",
            ErrorMessage = "Resource.InvalidEmailAddress")
        ]
        [Required(ErrorMessage = "Resource.EmailAddressRequired")]
        public string EmailAddress { get; set; }

        [Display(Name = "Resource.Address")]
        public string Address { get; set; }

        [Display(Name = "Resource.PostCode")]
        [Required(ErrorMessage = "Resource.PostCodeRequired")]
        public string PostCode { get; set; }

        [Display(Name = "Resource.ProductInEmailUs")]
        public string Product { get; set; }

        [Display(Name = "Resource.Model")]
        public string Model { get; set; }

        [Display(Name = "Resource.SerialNumber")]
        public string SerialNumber { get; set; }

        [Display(Name = "Resource.RequestContext")]
        public string RequestContext { get; set; }

        public EmailUsViewModel(string countryCode="") : base(countryCode) { }

        public EmailUsViewModel(EmailUsViewModel euvm, string countryCode = "") : base(countryCode) {
            this.Company = euvm.Company;
            this.FullName = euvm.FullName;
            this.TelephoneNumber = euvm.TelephoneNumber;
            this.MobileNumber = euvm.MobileNumber;
            this.EmailAddress = euvm.EmailAddress;
            this.Address = euvm.Address;
            this.PostCode = euvm.PostCode;
            this.Product = euvm.Product;
            this.Model = euvm.Model;
            this.SerialNumber = euvm.SerialNumber;
            this.RequestContext = euvm.RequestContext;
        }
    }
}