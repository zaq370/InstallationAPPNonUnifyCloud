using MvcPaging;
using InstallationAPPNonUnify.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace InstallationAPPNonUnify.ViewModels
{
    public class CustomerOrderInformationViewModel: BaseViewModel
    {
        public string CustomerProductId { get; set; }
        public string SalesOrderNumber { get; set; }
        public string SalesOrderID { get; set; }
        public string RequestDeliveryBy { get; set; }
        public string NewInstallationId { get; set; }
        public string OrderBy { get; set; }
        public bool Asc { get; set; }
        public List<ViewCustomerOrderInformationModel> Detail { get; set; }
        public IPagedList<ViewCustomerOrderInformationModel> PagedDetail { get; set; }

        public CustomerOrderInformationViewModel(string countryCode = "") : base(countryCode) { }
    }
}