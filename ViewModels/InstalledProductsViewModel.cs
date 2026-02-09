using MvcPaging;
using InstallationAPPNonUnify.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace InstallationAPPNonUnify.ViewModels
{
    public class InstalledProductsViewModel : BaseViewModel
    {
        [Required(ErrorMessage = "Resource.SalesOrderNumber")]
        [Display(Name = "Resource.SalesOrderNumber")]
        public string SalesOrderNumber { get; set; }

        [Required(ErrorMessage = "Resource.SalesOrderID")]
        [Display(Name = "Resource.SalesOrderID")]
        public string SalesOrderID { get; set; }

        [Required(ErrorMessage = "Resource.CustomerName")]
        [Display(Name = "Resource.CustomerName")]
        public string CustomerName { get; set; } // 客戶名稱

        [Display(Name = "resource.ProductNo")]
        public string ProductNo { get; set; }
        public string Productguid { get; set; }
        public DateTime RequestDate { get; set; } // 請求日期

        public bool Search { get; set; }
        public string OrderBy { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public bool Asc { get; set; }

      // 設為靜態屬性
        public static int DefaultPageSizeIn { get; private set; }

        // 靜態建構函式
        //static InstalledProductsViewModel()
        //{
        //    DefaultPageSizeIn = 10; // 預設分頁大小設為 10
        //}

        //public int GetDefaultPageSize()
        //{
        //    return DefaultPageSizeIn;
        //}

        public List<CustomerOrderInformationModel> ProductDetail { get; set; }
        public List<CustomerOrderInformationModel> ProductDetail2 { get; set; }
        public List<CustomerOrderInformationModel> ProductDetail3 { get; set; }
        public List<CustomerOrderInformationModel> ProductDetailNonSN { get; set; }
        public IPagedList<CustomerOrderInformationModel> PagedProductDetail { get; set; }
        public IPagedList<CustomerOrderInformationModel> PagedProductDetail2 { get; set; }
        // Default Constructor
        public InstalledProductsViewModel()
            : base()
        {
            // 初始化 ProductDetail 和 ProductDetail2 為空列表，避免 NullReferenceException
            ProductDetail = new List<CustomerOrderInformationModel>();
            ProductDetail2 = new List<CustomerOrderInformationModel>();
            ProductDetail3 = new List<CustomerOrderInformationModel>();
            ProductDetailNonSN = new List<CustomerOrderInformationModel>();
        }

        // Constructor with CountryCode
        public InstalledProductsViewModel(string countryCode = "")
            : base(countryCode)
        {
            ProductDetail = new List<CustomerOrderInformationModel>();
            ProductDetail2 = new List<CustomerOrderInformationModel>();
            ProductDetail3 = new List<CustomerOrderInformationModel>();
            ProductDetailNonSN = new List<CustomerOrderInformationModel>();
        }


        public class ReportIssueVieModel
        {
            public string SalesOrderId { get; set; }
            public string DeliveredAmount { get; set; }
            public string IssueType { get; set; }
            public string Notes { get; set; }
            public DateTime? ReportTime { get; set; }
            public string ImagePath { get; set; }
        }
    }

}