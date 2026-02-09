using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InstallationAPPNonUnify.Models
{
    public class CaseHistoryModel
    {
        public string CaseName { get; set; }
        public string CustomerName { get; set; }
        public DateTime? CallReceivedDate { get; set; }
        public string ProductName { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string Status { get; set; }
        public string CustomerProductName { get; set; }
        public string ProductSerialNo { get; set; }
        public string ConsoleSerialNo { get; set; }
        public string GeneralDefectCodeName { get; set; }
        public string ActualDefectCodeName { get; set; }
        public string ProblemDescription { get; set; }
        public string ProductNumber { get; set; }
        public string InternalNo { get; set; }
        public string InternalNo2 { get; set; }
        public string ReportedBy { get; set; }
        public string SalesOrderNumber { get; set; }
        public string RequestDeliveryBy { get; set; }
        public string SalesOrderID { get; set; }
        public string NewInstallationId { get; set; }
    }
}