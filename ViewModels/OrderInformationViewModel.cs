using MvcPaging;
using InstallationAPPNonUnify.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace InstallationAPPNonUnify.ViewModels
{
    public class OrderInformationViewModel : BaseViewModel
    {
        [Required(ErrorMessage = "Resource.SalesOrderNumber")]
        [Display(Name = "Resource.SalesOrderNumber")]
        public string SalesOrderNumber { get; set; }

        [Required(ErrorMessage = "Resource.SalesOrderID")]
        [Display(Name = "Resource.SalesOrderID")]
        public string SalesOrderID { get; set; }

        [Required(ErrorMessage = "Resource.CustomerName")]
        [Display(Name = "Resource.CustomerName")]
        public string CustomerName { get; set; }

        [Display(Name = "resource.Product")]
        public string ProductName { get; set; }
        [Display(Name = "resource.ProductSN")]
        public string ProductSerialNo { get; set; }
        [Display(Name = "resource.InternalNo")]
        public string InternalNo { get; set; }
        [Display(Name = "resource.ProductNo")]
        public string ProductNo { get; set; }
        public bool Search { get; set; }
        public string OrderBy { get; set; }
        public bool Asc { get; set; }
        public string Productguid { get; set; }
        public List<CustomerOrderInformationModel> ProductDetail { get; set; }
        public List<CustomerOrderInformationModel> ProductDetail2 { get; set; }
        public IPagedList<CustomerOrderInformationModel> PagedProductDetail { get; set; }

        public OrderInformationViewModel(string countryCode = "") : base(countryCode) { }
    }
}