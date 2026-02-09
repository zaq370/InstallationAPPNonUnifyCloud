using MvcPaging;
using InstallationAPPNonUnify.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace InstallationAPPNonUnify.ViewModels
{

    public class ReportIssueVieModel : BaseViewModel
    {
        public string SalesOrderId  { get; set; }
        public string ProductName { get; set; }
        public string ProductId { get; set; }
        public string TitleName { get; set; }
        public string DeliveredAmount  { get; set; }
        public string RequestDeliveryBy { get; set; }
        public string IssueType  { get; set; }
        public string Notes  { get; set; }
        public DateTime? ReportTime  { get; set; }
        public string ImageBase64 { get; set; }
        public ReportIssueVieModel(string CountryCode = "")
            : base(CountryCode)
        {
        }
        public string CountryCode2 { get; set; }
    }
}