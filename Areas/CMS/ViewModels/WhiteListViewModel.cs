using Newtonsoft.Json.Linq;
using InstallationAPPNonUnify.Modules;
using InstallationAPPNonUnify.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.WebPages.Html;

namespace InstallationAPPNonUnify.Areas.CMS.ViewModels
{
    public class WhiteListViewModel : BaseViewModel
    {
        /// <summary>
        /// 已加入的白名單
        /// </summary>
        public List<WhilteListModel> WhiteList { get; set; }
        /// <summary>
        /// CRM上的名單
        /// </summary>
        public List<WhilteListModel> WholeList { get; set;}

        public WhiteListViewModel(string countryCode = "") : base(countryCode) {
            LoadWholeList();
            LoadWhiteList();
            
        }

        /// <summary>
        /// 取得目前設定檔中白名單的資料
        /// </summary>
        public void LoadWhiteList()
        {
            WhiteList = new List<WhilteListModel>();

            var list = InstallationAPPNonUnify.Modules.WhiteList.ApprovedList;
            if (list != null && list.Count > 0 && WholeList != null && WholeList.Count > 0)
            {
                foreach (var member in list)
                {
                    var newMember = WholeList.FirstOrDefault(item => item.UserGuid.ToLower().Equals(member.ToString().ToLower()));
                    if (newMember != null && !string.IsNullOrEmpty(newMember.UserId)) {
                        newMember.UserId = newMember.UserId.ToUpper();
                        WhiteList.Add(newMember);
                    }
                }
            }
            WhiteList = WhiteList.OrderBy(item => item.UserId).ToList();
        }

        /// <summary>
        /// 取得CRM系統中，所有合法的使用者名單
        /// </summary>
        public void LoadWholeList() {
            WholeList =  new WhiteListInfo().ReadWholeList().OrderBy(item => item.UserId).ToList();
        }
    }

    public class WhilteListModel {

        [Required]
        public string UserId { get; set; }
        public string UserGuid { get; set; }

        public WhilteListModel() { }

        public WhilteListModel(string userGuid, string userId)
        {
            UserGuid = userGuid;
            UserId = userId;
        }

    }
}