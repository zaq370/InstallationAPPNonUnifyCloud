using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace InstallationAPPNonUnify.Models
{
    public class ViewCustomerOrderInformationModel
    {
      
        [Display(Name = "resource.CustomerName")]
        public string CustomerName { get; set; }

        [Display(Name = "resource.SalesOrderNumber")]
        public string SalesOrderNumber { get; set; }
        [Display(Name = "resource.SalesOrderID")]
        public string SalesOrderID { get; set; }

        [Display(Name = "resource.RequestDeliveryBy")]
        public string RequestDeliveryBy { get; set; }

        [Display(Name = "resource.CustomerProductNo")]
        public string CustomerProductNo { get; set; }

        [Display(Name = "resource.CallReceivedDate")]
        public DateTime? CallReceivedDate { get; set; }

        [Display(Name = "resource.CaseStatus")]
        public string CaseStatus { get; set; }

        [Display(Name = "resource.FrameSerialNo")]
        public string FrameSerialNo { get; set; }

        [Display(Name = "resource.ConsoleSerialNo")]
        public string ConsoleSerialNo { get; set; }

        [Display(Name = "resource.InternalNo")]
        public string InternalNo { get; set; }

        [Display(Name = "resource.FaultDescription")]
        public string FaultDescription { get; set; }
        public string NewInstallationId { get; set; }
        public ViewCustomerOrderInformationModel() { }

        public ViewCustomerOrderInformationModel(CaseHistoryModel chm) {
            CustomerName = chm.CustomerName;
            CustomerProductNo = chm.CaseName;
            CallReceivedDate = chm.CallReceivedDate;
            CaseStatus = chm.Status;
            FrameSerialNo = chm.ProductSerialNo;
            ConsoleSerialNo = chm.ConsoleSerialNo;
            InternalNo = chm.InternalNo2;
            FaultDescription = chm.ProblemDescription;
            RequestDeliveryBy = chm.RequestDeliveryBy;
            SalesOrderNumber = chm.SalesOrderNumber;
            SalesOrderID = chm.SalesOrderID;
            NewInstallationId = chm.NewInstallationId;
        }

    }
}