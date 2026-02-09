using InstallationAPPNonUnify.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace InstallationAPPNonUnify.Models
{
    public class RepairRequestDetailModel
    {
        public string CustomerName { get; set; }

        public string CaseProductNo { get; set; }

        public DateTime? ReportedDate { get; set; }

        public string ProductName { get; set; }

        public string InternalNo { get; set; }

        public string FrameSerialNo { get; set; }

        public string ConsoleSerialNo { get; set; }

        public string FaultDescription { get; set; }

        public string Status { get; set; }

        public string ReportedBy { get; set; }


        public RepairRequestDetailModel(CaseHistoryModel chm)
        {
            this.CustomerName = chm.CustomerName;
            this.CaseProductNo = chm.CaseName;
            this.ReportedDate = chm.CallReceivedDate;
            this.ProductName = chm.ProductName;
            this.InternalNo = chm.InternalNo2;
            this.FrameSerialNo = chm.ProductSerialNo;
            this.ConsoleSerialNo = chm.ConsoleSerialNo;
            this.FaultDescription = chm.ProblemDescription;
            this.ReportedBy = chm.ReportedBy;
            this.Status = chm.Status;
        }
    }
}