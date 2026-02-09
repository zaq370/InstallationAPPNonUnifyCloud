using MvcPaging;
using InstallationAPPNonUnify.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace InstallationAPPNonUnify.ViewModels
{
    public class RepairRequestViewModel : BaseViewModel
    {
        public string OrderBy { get; set; }
        public bool Asc { get; set; }
        public string AccountProductId { get; set; }
        public string SalesOrderId { get; set; }
        public List<RepairRequestDetailModel> Detail { get; set; }
        public IPagedList<RepairRequestDetailModel> PagedDetail { get; set; }


        public RepairRequestViewModel(string countryCode="") : base(countryCode) {
        }
    }
}