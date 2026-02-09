using MvcPaging;
using InstallationAPPNonUnify.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace InstallationAPPNonUnify.ViewModels
{
    public class CombinedInstalledProductAndIssueViewModel
    {
        public InstalledProductsViewModel InstalledProducts { get; set; }
        public ReportIssueVieModel ReportIssue { get; set; }
    }
}