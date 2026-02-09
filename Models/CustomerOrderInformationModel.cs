using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InstallationAPPNonUnify.Models
{
    public class CustomerOrderInformationModel
    {
        public string CustomerName { get; set; }
        public string Product { get; set; }
        public string FrameSerialNo { get; set; }
        public string ConsoleSerialNo { get; set; }
        public string InternalNo { get; set; }
        public DateTime? InstallDate { get; set; }
        public DateTime? PartsExpiryDate { get; set; }
        public string SalesOrderId { get; set; }
        public string AccountProductId { get; set; }
        public string SerialNo { get; set; }
        public string ProductId { get; set; }
        public string DrawingNo { get; set; }
        public string ModelNo { get; set; }
        public string ProductNumber { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public DateTime? LabourExpiryDate { get; set; }
        public bool CaseProductExists { get; set; }
        public string SalesOrderNumber { get; set; }
        public string RequestDeliveryBy { get; set; }
        public string ProductNo { get; set; }
        public string Productguid { get; set; }
        public string ExpectedQty { get; set; }
        public string InstalledQty { get; set; }
        public string ProductSN { get; set; }
        public string InstallMainIssue { get; set; }
        public string InstallMainIssueUpload { get; set; }
        public string InstallMainIssueNotes { get; set; }
        public string Status { get; set; } // Add Status property
        public DateTime RequestedDeliveryDate { get; set; } // Add RequestedDeliveryDate property       
    }
}