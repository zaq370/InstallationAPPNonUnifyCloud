using MvcPaging;
using InstallationAPP2024.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace InstallationAPP2024.ViewModels
{
    public class CustomerProductViewModel : BaseViewModel
    {
        [Display(Name = "resource.CustomerName")]
        public string CustomerNameCriteria { get; set; }

        [Display(Name = "resource.SalesOrderNumber")]
        public string SalesOrderNumberCriteria { get; set; }

        [Display(Name = "resource.Product")]
        public string ProductCriteria { get; set; }

        [Display(Name = "resource.ProductSN")]
        public string ProuductSNCriteria { get; set; }

        [Display(Name="resource.InternalNo")]
        public string InternalNoCriteria { get; set; }

        public string AccountProductId { get; set; }

        public string OrderBy { get; set; }

        public bool OrderByAsc { get; set; }

        public List<CustomerProductDetailModel> Detail { get; set; }
        public IPagedList<CustomerProductDetailModel> PagedDetail { get; set; }

        public CustomerProductViewModel(string countryCode = "") : base(countryCode) {
            OrderByAsc = true;
        }
    }
}