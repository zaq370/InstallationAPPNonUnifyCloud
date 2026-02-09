using Newtonsoft.Json.Linq;
using InstallationAPPNonUnify.Modules;
using InstallationAPPNonUnify.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InstallationAPPNonUnify.Areas.CMS.ViewModels
{
    public class OutOfServiceViewModel : BaseViewModel
    {
        public List<OutOfServerModel> Detail { get; set; }

        public OutOfServiceViewModel(string countryCode = "") : base(countryCode) {
            if (BackendFile.IsFileReady)
                Detail = new List<OutOfServerModel>(BackendFile.NoInServiceSegment).OrderByDescending(a => a.EndDate).ToList();
            else
                Detail = new List<OutOfServerModel>();
        }
    }

    public class OutOfServerModel {
        public string Subject { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}