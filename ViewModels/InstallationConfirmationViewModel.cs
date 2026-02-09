using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace InstallationAPPNonUnify.ViewModels
{
    public class InstallationConfirmationViewModel : BaseViewModel
    {
        [Required(ErrorMessage = "Resource.SalesOrderNumber")]
        [Display(Name = "Resource.SalesOrderNumber")]
        public string SalesOrderNumber { get; set; }

        [Required(ErrorMessage = "Resource.SalesOrderID")]
        [Display(Name = "Resource.SalesOrderID")]
        public string SalesOrderID { get; set; }

        [Required(ErrorMessage = "Resource.installationId")]
        [Display(Name = "Resource.installationId")]
        public string InstallationID { get; set; }

        [Required(ErrorMessage = "Resource.CustomerName")]
        [Display(Name = "Resource.CustomerName")]
        public string CustomerName { get; set; }
        public string OrderNumber { get; set; }
        public string InstallationDate { get; set; }  // 安裝日期
        public string CustomerEmail { get; set; }     // 客戶電子郵件
        public string InstallationTeamName { get; set; }  // 安裝團隊名稱
        public string TransportTeamName { get; set; }     // 運輸團隊名稱
        public InstallationConfirmationViewModel(string CountryCode = "")
            : base(CountryCode)
        {
        }

    }

    public class SalesOrder
    {
        public string CustomerIdName { get; set; }  // 客戶名稱
        public string CustomerId { get; set; }      // 客戶 ID
        public string SalesOrderID { get; set; }      // 訂單ID
        public string OrderNumber { get; set; }      // 訂單No
    }

    public class Account
    {
        public string EmailAddress1 { get; set; }   // 客戶 Email
    }

    public class Installation
    {
        public string NewInstallationTeam { get; set; }  // 安裝團隊
        public string NewTransportTeam { get; set; }     // 運輸團隊
        public string NewInstallationId { get; set; }     //ID
        public string NewName { get; set; }     //NewName
        public DateTime CreatedOn { get; set; }     //CreatedOn
        public string ClubManagerName { get; set; }     //ClubManagerName
    }

    public class InstallationDetail
    {
        public string InstallationId { get; set; }     //ID
        public string InstallationDetailId { get; set; }     //ID
    }
}