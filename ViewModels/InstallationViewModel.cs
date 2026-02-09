using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace InstallationAPPNonUnify.ViewModels
{
    public class InstallationViewModel : BaseViewModel
    {
       [Required(ErrorMessage = "Resource.ProductNameRequired")]
        [Display(Name = "Resource.RegisteredProductName")]
        public string RegisteredProductName { get; set; }

        [Required(ErrorMessage = "Resource.SerialNumberRequired")]
        [Display(Name = "Resource.SerialNumber")]
        public string SerialNumber { get; set; }

        [Required(ErrorMessage = "Resource.FaultInfoRequired")]
        [Display(Name = "Resource.FaultInformation")]
        public string FaultInformation { get; set; }

        [Required(ErrorMessage = "Resource.FieldRequired")]
        [Display(Name = "Resource.IsOutOfOrder")]
        public bool IsOutOfOrder { get; set; }

        [Required(ErrorMessage = "Resource.ReportedByRequired")]
        [Display(Name = "Resource.ReportBy")]
        public string ReportBy { get; set; }

        [Display(Name = "Resource.RepairAttachment")]
        public string InstallerSignatureimg { get; set; }

        public string ClubManagerSignatureimg { get; set; }

        public string OtherData { get; set; }
        public string CountryCode2 { get; set; }
        public string AccountProductId { get; set; }
        public bool DuplicateAlert { get; set; }
        public string Installer { get; set; }
        public string ClubManager { get; set; }
        public string ClubManagerEmail { get; set; }
        public string Productguid { get; set; }
        public string SignatureDate { get; set; }
        public InstallationViewModel(string CountryCode = "")
            : base(CountryCode)
        {
        }
    }
}