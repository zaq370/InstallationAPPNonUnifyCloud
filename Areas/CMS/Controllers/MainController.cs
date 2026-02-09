using Newtonsoft.Json.Linq;
using InstallationAPPNonUnify.Areas.CMS.Models;
using InstallationAPPNonUnify.Areas.CMS.ViewModels;
using InstallationAPPNonUnify.Controllers;
using InstallationAPPNonUnify.Modules;
using InstallationAPPNonUnify.Modules.Filters;
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Threading;

namespace InstallationAPPNonUnify.Areas.CMS.Controllers
{
    [Authorize]
    public class MainController : BaseController
    {
        [AllowAnonymous]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
        public ActionResult Login() {
            GetViewBag("Login");
            LoginViewModel lvm = new LoginViewModel(CountryCode);

            //如果已經有權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName],"cms");
            if (fatr.isValidTicket)
            {
                if (fatr.isAdministrator)
                    return RedirectToAction("BasicSetting", "Main", new { area = "CMS" });
                else
                    return RedirectToAction("Logout", "Main");
            } 

            return View(lvm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        [AntiForgeryErrorHandler(ExceptionType = typeof(HttpAntiForgeryException), View = "Login", Controller = "Main", Area = "CMS", ErrorMessage = "Session Timeout")]
        public ActionResult Login(LoginViewModel lvm) {

            GetViewBag("Login");
            Authorization auth = new Authorization(lvm.UserId, lvm.UserPassword, "admin");
            if ((!auth.IsValidUser) || (!auth.IsPasswordCorrect) || (!auth.IsAdminstrator))
            {
                LanguagePackage lp = new LanguagePackage(CountryCode);
                if (!auth.IsValidUser) ModelState.AddModelError("UserId", lp.getContentWithNoPrefix("InvalidUserId"));
                if (!auth.IsPasswordCorrect) ModelState.AddModelError("UserPassword", lp.getContentWithNoPrefix("IncorrectPassword"));
                if (!auth.IsAdminstrator) ModelState.AddModelError("UserId", lp.getContentWithNoPrefix("NonAdministrator"));

                return View(lvm);
            }

            //產生FormAuthentication
            NewFormAuthentication authcation = new NewFormAuthentication(auth, Enum.GetName(typeof(InstallationAPPNonUnify.Modules.Roles), auth.Role));
            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, authcation.EncryptedTicket);
            Response.Cookies.Add(cookie);

            return RedirectToAction("BasicSetting");
        }

        [AllowAnonymous]
        public ActionResult Logout()
        {
            LogoutProcess();
            return RedirectToAction("Login");
        }

        [NonAction]
        public void LogoutProcess()
        {
            FormsAuthentication.SignOut();

            //清除所有的 session
            Session.RemoveAll();

            //建立一個同名的 Cookie 來覆蓋原本的 Cookie
            HttpCookie clearCookie = new HttpCookie(FormsAuthentication.FormsCookieName, "");
            clearCookie.Expires = DateTime.Now.AddYears(-1);
            Response.Cookies.Add(clearCookie);

            //建立 ASP.NET 的 Session Cookie 同樣是為了覆蓋
            HttpCookie aspDotNetCookie = new HttpCookie("ASP.NET_SessionId", "");
            aspDotNetCookie.Expires = DateTime.Now.AddYears(-1);
            Response.Cookies.Add(aspDotNetCookie);
        }

        public ActionResult BasicSetting()
        {
            GetViewBag("BasicSetting");

            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isAdministrator) return RedirectToAction("Logout", "Main");
            ViewBag.IsSuperUser = fatr.isSuperUser ? true : false;

            BasicSettingViewModel bsvm = new BasicSettingViewModel(CountryCode);
            new BackendInfoManagement().ReadCompanyInfo(bsvm);
            Session["OriginalModel"] = bsvm;
            return View(bsvm);
        }

        [HttpPost]
        public ActionResult BasicSetting(BasicSettingViewModel bsvm)
        {
            GetViewBag("BasicSetting");

            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isAdministrator) return RedirectToAction("Logout", "Main");
            ViewBag.IsSuperUser = fatr.isSuperUser ? true : false;

            //判斷資料有沒有變動，如果沒有就不更新
            var OriginalModel = (BasicSettingViewModel)Session["OriginalModel"];
            if (OriginalModel.Equals(bsvm)) {
                bsvm.AlertMessage = ViewBag.UpdateFileUnchanged;
                return View(bsvm);
            } 

            //儲存資料
            new BackendInfoManagement().UpdateCompanyInfo(bsvm, fatr.AdminstratorId);
            Session["OriginalModel"] = bsvm;
            return View(bsvm);
        }

        public ActionResult SimulateSetting() {

            GetViewBag("SimulateSetting");

            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isAdministrator) return RedirectToAction("Logout", "Main");
            ViewBag.IsSuperUser = fatr.isSuperUser ? true : false;

            SimulateSettingViewModel ssvm = new SimulateSettingViewModel(CountryCode);

            //如果 ticket 上的 AdministatorId 與 CrmUserId 不同時，代表使用別人身份登入
            if (!fatr.AdminstratorId.Equals(fatr.CRMUserId)) ssvm.CurrentLoginUser = fatr.CRMUserId;
            
            return View(ssvm);
        }

        [HttpPost]
        public ActionResult SimulateSetting(SimulateSettingViewModel ssvm)
        {
            GetViewBag("SimulateSetting");

            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isAdministrator) return RedirectToAction("Logout", "Main");
            ViewBag.IsSuperUser = fatr.isSuperUser ? true : false;
            
            //使用user身份登入
            //check auth
            Authorization auth = new Authorization(ssvm.NewLoginUser, "");
            if ((!auth.IsValidUser))
            {
                LanguagePackage lp = new LanguagePackage(CountryCode);
                if (!auth.IsValidUser) ModelState.AddModelError("NewLoginUser", lp.getContentWithNoPrefix("InvalidUserId"));
                return View(ssvm);
            }

            //取出原本的角色及管理者id
            FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value);
            var _userData = ticket.UserData;
            JObject userData = JObject.Parse(_userData);
            JToken tempToken;
            userData.TryGetValue("Role", out tempToken);
            var role = tempToken.ToString();  //角色
            userData.TryGetValue("AdministratorId", out tempToken);
            var administratorId = tempToken.ToString();  //管理者Id

            //產生FormAuthentication
            NewFormAuthentication authcation = new NewFormAuthentication(auth, role, administratorId);
            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, authcation.EncryptedTicket);
            Response.Cookies.Add(cookie);

            //清除CaseInfo的Cookie
            var CaseInfoCookie = new HttpCookie("CaseInfo");
            CaseInfoCookie.Expires = DateTime.Now.AddYears(-1);
            Response.Cookies.Add(CaseInfoCookie);

            ssvm.CurrentLoginUser = auth.UserId;
            ssvm.NewLoginUser = string.Empty;
            ModelState.Clear();

            return View(ssvm);
        }

        public ActionResult WhiteList() {

            GetViewBag("WhiteList");

            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isAdministrator) return RedirectToAction("Logout", "Main");
            ViewBag.IsSuperUser = fatr.isSuperUser ? true : false;

            var wlvm = new WhiteListViewModel(CountryCode);

            return View(wlvm);
        }

        [HttpPost]
        public ActionResult WhiteList(FormCollection fc)
        {
            GetViewBag("WhiteList");

            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isAdministrator) return RedirectToAction("Logout", "Main");
            ViewBag.IsSuperUser = fatr.isSuperUser ? true : false;

            bool isSuccessed = false;
            try
            {
                var newList = fc["NewList"];
                var listArray = new JArray();
                if (!string.IsNullOrEmpty(newList) && !string.IsNullOrWhiteSpace(newList))
                    listArray = JArray.Parse(newList);

                WhiteListInfo wli = new WhiteListInfo();
                if (wli.UpdateWhiteListFile(listArray, fatr.AdminstratorId))
                    isSuccessed = true;
            } catch  {}

            WhiteListViewModel wlvm = new WhiteListViewModel(CountryCode);
            if (isSuccessed)
                wlvm.AlertMessage = ViewBag.UpdateFileSuccessed;
            else
                wlvm.AlertMessage = ViewBag.UpdateFileFailed;

            return View(wlvm);
        }

        public ActionResult OutOfServiceSetting() {
            GetViewBag("OutOfServiceSetting");

            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isAdministrator) return RedirectToAction("Logout", "Main");
            ViewBag.IsSuperUser = fatr.isSuperUser ? true : false;

            OutOfServiceViewModel osvm = new OutOfServiceViewModel(CountryCode);

            return View(osvm);
        }

        [HttpPost]
        public ActionResult OutOfServiceSetting(FormCollection fc)
        {
            GetViewBag("OutOfServiceSetting");

            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isAdministrator) return RedirectToAction("Logout", "Main");
            ViewBag.IsSuperUser = fatr.isSuperUser ? true : false;

            bool isSuccessed = false;
            try
            {
                var Duration = fc["NewList"];
                BackendInfo bf = new BackendInfo();
                if (bf.UpdateOutOfServiceDuration(Duration, fatr.AdminstratorId))
                    isSuccessed = true;
            }
            catch { }

            OutOfServiceViewModel osvm = new OutOfServiceViewModel(CountryCode);
            if (isSuccessed)
                osvm.AlertMessage = ViewBag.UpdateFileSuccessed;
            else
                osvm.AlertMessage = ViewBag.UpdateFileFailed;

            return View(osvm);
        }

        public ActionResult PickLanguageSet() {

            GetViewBag("PickLanguageSet");

            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isAdministrator) return RedirectToAction("Logout", "Main");
            ViewBag.IsSuperUser = fatr.isSuperUser ? true : false;

            PickLanguageSetViewModel psvm = new PickLanguageSetViewModel();
            psvm.init();

            //如果有傳來訊息
            if (TempData.ContainsKey("AlertMessage"))
                psvm.AlertMessage = TempData["AlertMessage"].ToString();

            //如果就只有一個語系，就不需要選擇
            if (psvm.Countries.Count == 1) {
                TempData["PickedCountry1"] = psvm.PickedCountry1;
                return RedirectToAction("LanguageSetting");
            }

            return View(psvm);
        }

        [HttpPost]
        public ActionResult PickLanguageSet(PickLanguageSetViewModel psvm) {

            GetViewBag("PickLanguageSet");

            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isAdministrator) return RedirectToAction("Logout", "Main");
            ViewBag.IsSuperUser = fatr.isSuperUser ? true : false;

            if (psvm.PickedCountry1.Equals(psvm.PickedCountry2))
                psvm.PickedCountry2 = null;

            TempData["PickedCountry1"] = psvm.PickedCountry1;
            TempData["PickedCountry2"] = psvm.PickedCountry2;

            return RedirectToAction("LanguageSetting");
        }

        public ActionResult LanguageSetting() {
            GetViewBag("LanguageSetting");

            if (!TempData.ContainsKey("PickedCountry1"))
                return RedirectToAction("PickLanguageSet");

            string PickedCountry1 = string.Empty, PickedCountry2 = string.Empty;

            PickedCountry1 = TempData["PickedCountry1"].ToString();

            var tempObject = TempData["PickedCountry2"];
            if (tempObject != null)
                PickedCountry2 = TempData["PickedCountry2"].ToString();

            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isAdministrator) return RedirectToAction("Logout", "Main");
            ViewBag.IsSuperUser = fatr.isSuperUser ? true : false;

            LanguageSettingViewModel lsvm = new LanguageSettingViewModel(CountryCode);
            lsvm.init(PickedCountry1, PickedCountry2);

            return View(lsvm);
        }

        [HttpPost]
        public ActionResult LanguageSetting(FormCollection fc)
        {
            GetViewBag("LanguageSetting");

            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isAdministrator) return RedirectToAction("Logout", "Main");
            ViewBag.IsSuperUser = fatr.isSuperUser ? true : false;

            var NewList = fc["NewList"];
            //更新檔案
            bool isSuccessed = true;
            LanguagePackage lp = new LanguagePackage();
            if (!lp.UpdateLanguagePackageFile(NewList, fatr.AdminstratorId))
            {
                isSuccessed = false;
            }

            var PickLanguage = JArray.Parse(fc["PickeLanguage"]).ToList();
            string language1 = string.Empty, language2 = string.Empty;
            language1 = PickLanguage.ElementAt(0).ToString();
            if (PickLanguage.Count > 1)
                language2 = PickLanguage.ElementAt(1).ToString();

            LanguageSettingViewModel lsvm = new LanguageSettingViewModel(CountryCode);
            lsvm.init(language1, language2);

            if (isSuccessed)
                lsvm.AlertMessage = ViewBag.UpdateFileSuccessed;
            else
                lsvm.AlertMessage = ViewBag.UpdateFileFailed;
            
            return View(lsvm);
        }

        [HttpPost]
        public ActionResult ImportLanguagePackage(HttpPostedFileBase ImportFile)
        {
            GetViewBag("ImportLanguagePackage");

            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isAdministrator) return RedirectToAction("Logout", "Main");
            ViewBag.IsSuperUser = fatr.isSuperUser ? true : false;

            ImportExportLanguagePack importLanguagePack = new ImportExportLanguagePack();
            var result = importLanguagePack.import(ImportFile, fatr.AdminstratorId, fatr.isSuperUser);

            if (!result.Item1)
            {
                TempData["AlertMessage"] = ViewBag.ImportFileFailed + " " + result.Item2;
            }
            else {
                TempData["AlertMessage"] = ViewBag.ImportFileSuccessed;
            }

            return RedirectToAction("PickLanguageSet");
        }

        public ActionResult ExportLanguagePackage() {
            ImportExportLanguagePack exportLanguagePack = new ImportExportLanguagePack();
            var result = exportLanguagePack.Export();

            if (result.Item1) {
                //取得檔案名稱
                string filename = System.IO.Path.GetFileName(result.Item2);

                //讀成串流
                Stream iStream = new FileStream(result.Item2, FileMode.Open, FileAccess.Read, FileShare.Read);

                //回傳出檔案
                return File(iStream, "application/unknown", filename);
            }
            else
            {
                GetViewBag("ImportLanguagePackage");
                TempData["AlertMessage"] = ViewBag.ImportFileFailed + " " + result.Item2;
                return RedirectToAction("PickLanguageSet");
            }
        }

        private bool GetAdvancedSettingViewModel(AdvancedSettingViewModel asvm, bool getLanguageCulture = false)
        {
            bool isSuccess = true;

            try {
                if (!new AdvancedSetting().GetViewModel(asvm, getLanguageCulture))
                {
                    Elmah.ErrorSignal.FromCurrentContext().Raise(new Exception("Advanced Setting Loading Failed"));
                    isSuccess = false;
                }
            } catch(Exception ex) {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                isSuccess = false;
            }

            return isSuccess;
        }

        //進階設定
        public ActionResult AdvancedSetting() {

            GetViewBag("AdvancedSetting");

            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isAdministrator) return RedirectToAction("Logout", "Main");

            //只有Super User允許進入
            if (!fatr.isSuperUser) {
                if (Request.UrlReferrer != null)
                    return Redirect(Request.UrlReferrer.ToString());
                else
                    return RedirectToAction("BasicSetting");
            } 
            
            ViewBag.IsSuperUser = fatr.isSuperUser ? true : false;

            //取得ViewModel的資料
            var asvm = new AdvancedSettingViewModel(CountryCode);
            if (!GetAdvancedSettingViewModel(asvm))
            {
                return View("Error");
            }

            //有指定partial view
            if (TempData.Keys.Contains("OriginalAction")) {
                ViewBag.Action = TempData["OriginalAction"];
                switch (TempData["OriginalAction"].ToString()) { 
                    case "Smtp":
                        GetViewBag("_Smtp");
                        break;
                    case "ConsoleCabinet":
                        GetViewBag("_ConsoleCabinet");
                        break;
                    case "Country":
                        GetViewBag("_Country");
                        GetAdvancedSettingViewModel(asvm, true);
                        break;
                    case "CRMConnection":
                        GetViewBag("_CRMConnection");
                        break;
                    case "DBConnection":
                        GetViewBag("_DBConnection");
                        break;
                    case "OtherSetting":
                        GetViewBag("_OtherSetting");
                        break;
                }
            }

            //有回傳訊息
            if (TempData.Keys.Contains("AlertMessage")) {
                asvm.AlertMessage = TempData["AlertMessage"].ToString();
            }

            return View(asvm);
        }

        //Ajax call function
        [AllowAnonymous]
        public ActionResult ConsoleCabinet()
        {
            GetViewBag("_ConsoleCabinet");

            //只允許ajax call
            if (!Request.IsAjaxRequest())
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);
            }

            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isSuperUser) return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);

            //取得ViewModel的資料
            AdvancedSettingViewModel asvm = new AdvancedSettingViewModel(CountryCode);
            var isSuccessed = GetAdvancedSettingViewModel(asvm);
            if (isSuccessed)
                return PartialView("_ConsoleCabinet", asvm);
            else
                return PartialView("_Error");
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult UpdateConsleCabinet(string Segment, string NewValue)
        {
            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isAdministrator) return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);

            //要回傳的資訊
            JObject returnResult = new JObject();

            //更新資料
            BackendInfoManagement bim = new BackendInfoManagement();
            var result = bim.UpdateContent(Segment, NewValue, fatr.AdminstratorId, CountryCode);
            returnResult.Add(new JProperty("IsSuccessed", result.Item1));
            returnResult.Add(new JProperty("AlertMessage", result.Item2));

            //回傳頁面 //這個頁面是特殊處理
            //取得ViewModel的資料
            AdvancedSettingViewModel asvm = new AdvancedSettingViewModel(CountryCode);
            var isSuccessed = GetAdvancedSettingViewModel(asvm);
            if (isSuccessed)
            {
                //取得同一個Segment的值
                var AfterValue = asvm.GetType().GetProperty(Segment).GetValue(asvm,null);
                returnResult.Add(new JProperty("NewContent", AfterValue.ToString()));
            }
            else 
            {
                returnResult.Add(new JProperty("NewContent", ""));
            }
            return this.Content(returnResult.ToString(), "application/json");
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult UpdateLanguagePack()
        {
            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isAdministrator) return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);

            //要回傳的資訊
            JObject returnResult = new JObject();

            //更新
            LanguagePackage lp = new LanguagePackage(CountryCode);
            lp.LoadLanguagePackage();
            if (LPFile.IsResourceReady)
                returnResult.Add(new JProperty("AlertMessage",lp.getContentWithNoPrefix("ExecuteSuccessfully")));
            else
                returnResult.Add(new JProperty("AlertMessage",lp.getContentWithNoPrefix("FailToExecute")));

            return this.Content(returnResult.ToString(), "application/json");
        }

        /// <summary>
        /// 匯入 語言檔的 Json 檔
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public ActionResult ImportUpdateLanguagePack()
        {
            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isAdministrator) return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);

            return ProcessLanguagePack(Request.Files, fatr.AdminstratorId, CountryCode, false);
        }

        /// <summary>
        /// 匯入 語言檔的 Json 檔(只更新原本json檔不存在的內容)
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public ActionResult ImportUpdateNewLanguagePack()
        {
            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isAdministrator) return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);

            return ProcessLanguagePack(Request.Files, fatr.AdminstratorId, CountryCode, true);
        }

        /// <summary>
        /// 實際更新 json 檔
        /// </summary>
        /// <returns></returns>
        [NonAction]
        public ActionResult ProcessLanguagePack(HttpFileCollectionBase files, string modifier, string countryCode, bool onlyNew = false)
        {
            //要回傳的資訊
            JObject returnResult = new JObject();

            //更新
            ImportExportLanguagePack ielp = new ImportExportLanguagePack();
            var result = ielp.ImportAndApplyLanguageFile(Request.Files, modifier, countryCode, onlyNew);

            returnResult.Add("AlertMessage",result.Item2);

            return this.Content(returnResult.ToString(), "application/json");
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult UpdateBackendInfo()
        {
            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isAdministrator) return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);

            //要回傳的資訊
            JObject returnResult = new JObject();
            LanguagePackage lp = new LanguagePackage(CountryCode);

            //更新
            BackendInfo bi = new BackendInfo();
            bi.UpdateFileContent();
            if (BackendFile.IsFileReady)
                returnResult.Add(new JProperty("AlertMessage", lp.getContentWithNoPrefix("ExecuteSuccessfully")));
            else
                returnResult.Add(new JProperty("AlertMessage", lp.getContentWithNoPrefix("FailToExecute")));

            return this.Content(returnResult.ToString(), "application/json");
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult ImportUpdateBackendInfo()
        {
            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isAdministrator) return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);

            //要回傳的資訊
            JObject returnResult = new JObject();

            //更新
            BackendInfoManagement bim = new BackendInfoManagement();
            var result = bim.ImportAndApplyBackendInfoFile(Request.Files, fatr.AdminstratorId, CountryCode);

            returnResult.Add("AlertMessage", result.Item2);

            return this.Content(returnResult.ToString(), "application/json");
        }

        //Ajax call function
        [AllowAnonymous]
        public ActionResult Smtp() {
            GetViewBag("_Smtp");

            //只允許ajax call
            if (!Request.IsAjaxRequest())
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);
            }

            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isSuperUser) return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);

            //取得ViewModel的資料
            AdvancedSettingViewModel asvm = new AdvancedSettingViewModel(CountryCode);
            var isSuccessed = GetAdvancedSettingViewModel(asvm);
            if (isSuccessed)
                return PartialView("_Smtp", asvm);
            else
                return PartialView("_Error");
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult UpdateSmtp(AdvancedSettingViewModel asvm) {

            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isAdministrator) return RedirectToAction("Logout", "Main");

            //更新資料
            BackendInfoManagement bim = new BackendInfoManagement();
            var result = bim.UpdateSmtpInfo(asvm, fatr.AdminstratorId);

            //回傳頁面
            TempData["OriginalAction"] = "Smtp";
            TempData["AlertMessage"] = result.Item2;
            return RedirectToAction("AdvancedSetting");
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult SendTestMail(string type, string ip, string port, string id, string password, string adminSender, string adminReceiver)
        {
            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isSuperUser) return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);

            //要回傳的資訊
            JObject returnResult = new JObject();
            LanguagePackage lp = new LanguagePackage(CountryCode);

            //寄送mail
            SmtpSettingModel ssm = new SmtpSettingModel() { 
                Ip = ip,
                Port = port,
                Id = id,
                Password = password,
                AdminSender = adminSender,
                Admin = adminReceiver
            };

            Email email = new Email(CountryCode);
            if (email.SendTestMail(ssm,type))
                returnResult.Add(new JProperty("AlertMessage", lp.getContentWithNoPrefix("ExecuteSuccessfully")));
            else
                returnResult.Add(new JProperty("AlertMessage", lp.getContentWithNoPrefix("FailToExecute")));

            return this.Content(returnResult.ToString(), "application/json");
        }

        [AllowAnonymous]
        public ActionResult Country()
        {
            GetViewBag("_Country");

            //只允許ajax call
            if (!Request.IsAjaxRequest())
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);
            }

            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isSuperUser) return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);

            //取得ViewModel的資料
            AdvancedSettingViewModel asvm = new AdvancedSettingViewModel(CountryCode);
            var isSuccessed = GetAdvancedSettingViewModel(asvm,true);

            if (isSuccessed)
                return PartialView("_Country", asvm);
            else
                return PartialView("_Error");
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult UpdateCountry(string AddNewCountry, string RemoveCountry, string UpdateContent)
        {
            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isAdministrator) return RedirectToAction("Logout", "Main");

            //更新資料
            Tuple<bool, string> result = new Tuple<bool,string>(false,"");
            BackendInfoManagement bim = new BackendInfoManagement();

            //新增
            if (!string.IsNullOrEmpty(AddNewCountry))
                result = bim.InsertNewCountry(AddNewCountry, fatr.AdminstratorId, CountryCode);

            //移除
            if (!string.IsNullOrEmpty(RemoveCountry))
                result = bim.RemoveCountry(RemoveCountry, fatr.AdminstratorId, CountryCode);

            //修改
            if (!string.IsNullOrEmpty(UpdateContent))
                result = bim.UpdateCountries(UpdateContent, fatr.AdminstratorId, CountryCode);

            ////回傳頁面
            TempData["OriginalAction"] = "Country";
            TempData["AlertMessage"] = result.Item2;
            return RedirectToAction("AdvancedSetting");
        }

        [AllowAnonymous]
        public ActionResult CRMConnection()
        {
            GetViewBag("_CRMConnection");

            //只允許ajax call
            if (!Request.IsAjaxRequest())
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);
            }

            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isSuperUser) return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);

            //取得ViewModel的資料
            AdvancedSettingViewModel asvm = new AdvancedSettingViewModel(CountryCode);
            var isSuccessed = GetAdvancedSettingViewModel(asvm);

            if (isSuccessed)
                return PartialView("_CRMConnection", asvm);
            else
                return PartialView("_Error");
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult UpdateCRMConnection(AdvancedSettingViewModel asvm)
        {
            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isAdministrator) return RedirectToAction("Logout", "Main");

            //更新資料
            BackendInfoManagement bim = new BackendInfoManagement();
            var result = bim.UpdateCRMConnection(asvm, fatr.AdminstratorId);

            //回傳頁面
            TempData["OriginalAction"] = "CRMConnection";
            TempData["AlertMessage"] = result.Item2;
            return RedirectToAction("AdvancedSetting");
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult TestCRMConnection(string CRMDecryptUrl, string CRMOrganizationName, string CRMServiceUser, string CRMServicePassword, string CRMDomain, string CRMUrl, string CRMServiceUrl)
        {
            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isSuperUser) return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);

            //要回傳的資訊
            JObject returnResult = new JObject();
            LanguagePackage lp = new LanguagePackage(CountryCode);

            //測試連線
            CRMConnectionSettingModel csm = new CRMConnectionSettingModel() {
                CRMOrganizationName = CRMOrganizationName,
                CRMServiceUser = CRMServiceUser,
                CRMServicePassword = CRMServicePassword,
                CRMDomain = CRMDomain,
                CRMServiceUrl = CRMServiceUrl,
                CRMUrl = CRMUrl,
                CRMDecryptUrl = CRMDecryptUrl
            };

            CRMServiceConnection csc = new CRMServiceConnection();
            if (csc.TestCrmServiceClient(csm))
                returnResult.Add(new JProperty("AlertMessage", lp.getContentWithNoPrefix("ExecuteSuccessfully")));
            else
                returnResult.Add(new JProperty("AlertMessage", lp.getContentWithNoPrefix("FailToExecute")));

            return this.Content(returnResult.ToString(), "application/json");
        } 

        [AllowAnonymous]
        public ActionResult DBConnection()
        {
            GetViewBag("_DBConnection");

            //只允許ajax call
            if (!Request.IsAjaxRequest())
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);
            }

            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isSuperUser) return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);

            //取得ViewModel的資料
            AdvancedSettingViewModel asvm = new AdvancedSettingViewModel(CountryCode);
            var isSuccessed = GetAdvancedSettingViewModel(asvm);

            if (isSuccessed)
                return PartialView("_DBConnection", asvm);
            else
                return PartialView("_Error");
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult UpdateDBConnection(AdvancedSettingViewModel asvm)
        {
            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isAdministrator) return RedirectToAction("Logout", "Main");

            //更新資料
            BackendInfoManagement bim = new BackendInfoManagement();
            var result = bim.UpdateDBConnection(asvm, fatr.AdminstratorId);

            //回傳頁面
            TempData["OriginalAction"] = "DBConnection";
            TempData["AlertMessage"] = result.Item2;
            return RedirectToAction("AdvancedSetting");
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult TestDBConnection(string Server, string InitialCatalog, string PersistSecurityInfo, string UserId, string Password, string TimeOut)
        {
            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isSuperUser) return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);

            //要回傳的資訊
            JObject returnResult = new JObject();
            LanguagePackage lp = new LanguagePackage(CountryCode);

            //測試連線
            DBConnectionStringModel csm = new DBConnectionStringModel()
            {
                Server = Server,
                InitialCatalog = InitialCatalog,
                PersistSecurityInfo = PersistSecurityInfo,
                UserId = UserId,
                Password = Password,
                TimeOut = TimeOut
            };

            DBConnetion dc = new DBConnetion(false);
            if (dc.TestConnection(csm))
                returnResult.Add(new JProperty("AlertMessage", lp.getContentWithNoPrefix("ExecuteSuccessfully")));
            else
                returnResult.Add(new JProperty("AlertMessage", lp.getContentWithNoPrefix("FailToExecute")));

            return this.Content(returnResult.ToString(), "application/json");
        }

        [AllowAnonymous]
        public ActionResult OtherSetting()
        {
            GetViewBag("_OtherSetting");

            //只允許ajax call
            if (!Request.IsAjaxRequest())
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);
            }

            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isSuperUser) return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);

            //取得ViewModel的資料
            AdvancedSettingViewModel asvm = new AdvancedSettingViewModel(CountryCode);
            var isSuccessed = GetAdvancedSettingViewModel(asvm);

            if (isSuccessed)
                return PartialView("_OtherSetting", asvm);
            else
                return PartialView("_Error");
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult UpdateOtherSetting(string Segment, string NewValue)
        {
            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isAdministrator) return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);

            //要回傳的資訊
            JObject returnResult = new JObject();

            //更新資料
            BackendInfoManagement bim = new BackendInfoManagement();
            var result = bim.UpdateOtherSetting(Segment, NewValue, fatr.AdminstratorId, CountryCode);
            returnResult.Add(new JProperty("IsSuccessed", result.Item1));
            returnResult.Add(new JProperty("AlertMessage", result.Item2));

            //回傳頁面 //這個頁面是特殊處理
            //取得ViewModel的資料
            AdvancedSettingViewModel asvm = new AdvancedSettingViewModel(CountryCode);
            var isSuccessed = GetAdvancedSettingViewModel(asvm);
            if (isSuccessed)
            {
                //取得同一個Segment的值
                var AfterValue = asvm.OtherSetting.GetType().GetProperty(Segment).GetValue(asvm.OtherSetting, null);
                //var AfterValue = asvm.GetType().GetProperty(Segment).GetValue(asvm, null);
                returnResult.Add(new JProperty("NewContent", AfterValue.ToString()));
            }
            else
            {
                returnResult.Add(new JProperty("NewContent", ""));
            }

            return this.Content(returnResult.ToString(), "application/json");
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult CorrectPassword()
        {
            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isAdministrator) return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);

            //要回傳的資訊
            JObject returnResult = new JObject();

            //更新資料
            LanguagePackage lp = new LanguagePackage(CountryCode);
            BackendInfoManagement bim = new BackendInfoManagement();
            bim.CorrectPassword();
            returnResult.Add(new JProperty("AlertMessage", lp.getContentWithNoPrefix("UpdatePasswordEncryptionInProgress")));

            return this.Content(returnResult.ToString(), "application/json");
        } 

        
        [HttpGet]
        [AllowAnonymous]
        public ActionResult SendInvitation()
        {
            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName], "cms");
            if (!fatr.isValidTicket || !fatr.isAdministrator) return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);

            //要回傳的資訊
            JObject returnResult = new JObject();

            //更新資料
            LanguagePackage lp = new LanguagePackage(CountryCode);
            BackendInfoManagement bim = new BackendInfoManagement();
            ResetPassword rp = new ResetPassword();
            string resetUrl = rp.GetResetPasswordUrl(this.Request);
            string loginUrl = rp.GetLoginUrl(this.Request);
            Thread sendInvitation = new Thread(() => bim.BatchSendingInvitation(resetUrl, loginUrl, CountryCode));
            sendInvitation.Start();
            returnResult.Add(new JProperty("AlertMessage", lp.getContentWithNoPrefix("SendInvitationInProgress")));

            return this.Content(returnResult.ToString(), "application/json");
        } 

        private void GetViewBag(string viewName) {
            LanguagePackage lp = new LanguagePackage(CountryCode);

            GetActionTitles();
            //common viewbag
            ViewBag.Title = lp.getContentWithNoPrefix("CMSTilte");
            ViewBag.UpdateFileSuccessed = lp.getContentWithNoPrefix("UpdateFileSuccessed");
            ViewBag.UpdateFileFailed = lp.getContentWithNoPrefix("UpdateFileFailed");
            ViewBag.Apply = lp.getContentWithNoPrefix("Apply");
            ViewBag.Remove = lp.getContentWithNoPrefix("Remove");
            ViewBag.Add = lp.getContentWithNoPrefix("Add");
            ViewBag.ShortenLink = lp.getContentWithNoPrefix("ShortenLink");

            switch (viewName) { 
                case "Login":
                    ViewBag.Submit = lp.getContentWithNoPrefix("Login");
                    ViewBag.PageTitle = lp.getContentWithNoPrefix("CMTPageTilte");
                    break;
                case "BasicSetting":
                    ViewBag.Description = lp.getContentWithNoPrefix("BasicSettingDesc");
                    ViewBag.OpenUrl = lp.getContentWithNoPrefix("OpenUrl");
                    ViewBag.UpdateFileUnchanged = lp.getContentWithNoPrefix("UpdateFileUnchanged");
                    break;
                case "SimulateSetting":
                    ViewBag.Description = lp.getContentWithNoPrefix("SimulateSettingDesc");
                    ViewBag.StartSimulating = lp.getContentWithNoPrefix("StartSimulating");
                    ViewBag.EndSimulating = lp.getContentWithNoPrefix("EndSimulating");
                    break;
                case "WhiteList":
                    ViewBag.Description = lp.getContentWithNoPrefix("WhiteListSettingDesc");
                    ViewBag.CRMUserId = lp.getContentWithNoPrefix("CRMUserId");
                    ViewBag.SearchForId = lp.getContentWithNoPrefix("SearchForId");
                    break;
                case "OutOfServiceSetting":
                    ViewBag.Description = lp.getContentWithNoPrefix("OutOfServiceSettingDesc");
                    ViewBag.StartTime = lp.getContentWithNoPrefix("StartTime");
                    ViewBag.EndTime = lp.getContentWithNoPrefix("EndTime");
                    ViewBag.DescriptionField = lp.getContentWithNoPrefix("Description");
                    ViewBag.EndTimeError = lp.getContentWithNoPrefix("EndTimeError");
                    break;
                case "LanguageSetting":
                    ViewBag.Description = lp.getContentWithNoPrefix("LanguageSettingDesc");
                    ViewBag.Search = lp.getContentWithNoPrefix("Search");
                    ViewBag.Undo = lp.getContentWithNoPrefix("Undo");
                    break;
                case "PickLanguageSet":
                    ViewBag.Description = lp.getContentWithNoPrefix("PickLanguageSetDesc");
                    ViewBag.StartModify = lp.getContentWithNoPrefix("StartModify");
                    ViewBag.ExportLanguagePack = lp.getContentWithNoPrefix("ExportLanguagePack");
                    ViewBag.ImportLanguagePack = lp.getContentWithNoPrefix("ImportLanguagePack");
                    break;
                case "ImportLanguagePackage":
                    ViewBag.Description = lp.getContentWithNoPrefix("PickLanguageSetDesc");
                    ViewBag.ImportFileSuccessed = lp.getContentWithNoPrefix("ImportFileSuccessed");
                    ViewBag.ImportFileFailed = lp.getContentWithNoPrefix("ImportFileFailed");
                    break;
                case "AdvancedSetting":
                case "_ConsoleCabinet":
                    ViewBag.TestMode = lp.getContentWithNoPrefix("TestMode");
                    ViewBag.TestModeDescription = lp.getContentWithNoPrefix("TestModeDescription");
                    ViewBag.RenewLanguageDesc1 = lp.getContentWithNoPrefix("RenewLanguageDesc1");
                    ViewBag.RenewLanguageDesc2 = lp.getContentWithNoPrefix("RenewLanguageDesc2");
                    ViewBag.RenewLanguageDesc3 = lp.getContentWithNoPrefix("RenewLanguageDesc3");
                    ViewBag.ImportAndApplyLanguagePack = lp.getContentWithNoPrefix("ImportAndApplyLanguagePack");
                    ViewBag.ImportNewAndApplyLanguagePack = lp.getContentWithNoPrefix("ImportNewAndApplyLanguagePack");
                    ViewBag.RenewLanguagePack = lp.getContentWithNoPrefix("RenewLanguagePack");
                    ViewBag.RenewBackendInfoDesc1 = lp.getContentWithNoPrefix("RenewBackendInfoDesc1");
                    ViewBag.RenewBackendInfoDesc2 = lp.getContentWithNoPrefix("RenewBackendInfoDesc2");
                    ViewBag.RenewBackendInfoDesc3 = lp.getContentWithNoPrefix("RenewBackendInfoDesc3");
                    ViewBag.RenewBackendInfo = lp.getContentWithNoPrefix("RenewBackendInfo");
                    ViewBag.ImportAndApplyBackendInfo = lp.getContentWithNoPrefix("ImportAndApplyBackendInfo");
                    ViewBag.ImportNewAndApplyBackendInfo = lp.getContentWithNoPrefix("ImportNewAndApplyBackendInfo");
                    ViewBag.Organization = lp.getContentWithNoPrefix("Organization");
                    ViewBag.OrganizationDescription = lp.getContentWithNoPrefix("OrganizationDescription");
                    ViewBag.AdminLogDescription1 = lp.getContentWithNoPrefix("AdminLogDescription1");
                    ViewBag.AdminLogDescription2 = lp.getContentWithNoPrefix("AdminLogDescription2");
                    ViewBag.OpenAdminLog = lp.getContentWithNoPrefix("OpenAdminLog");
                    ViewBag.ClearAdminLog = lp.getContentWithNoPrefix("ClearAdminLog");
                    break;
                case "_Smtp":
                    ViewBag.SmtpDesc = "SMTP Setting";
                    ViewBag.TestModeSmtpDesc = "TestMode Mail Setting(SSL)";
                    ViewBag.Apply = lp.getContentWithNoPrefix("Apply");
                    ViewBag.SendTestMail = lp.getContentWithNoPrefix("SendTestMail");
                    ViewBag.AdminSender = lp.getContentWithNoPrefix("AdminSender");
                    ViewBag.AdminReceiver = lp.getContentWithNoPrefix("AdminReceiver");
                    break;
                case "_Country":
                    ViewBag.Sequence = lp.getContentWithNoPrefix("Sequence");
                    ViewBag.CountryName = lp.getContentWithNoPrefix("CountryName");
                    ViewBag.LanguagePackage = lp.getContentWithNoPrefix("LanguagePackage");
                    ViewBag.RedirectLanguagePackage = lp.getContentWithNoPrefix("RedirectLanguagePackage");
                    ViewBag.Upper = lp.getContentWithNoPrefix("Upper");
                    ViewBag.Lower = lp.getContentWithNoPrefix("Lower");
                    break;
                case "_CRMConnection":
                    ViewBag.CRMConnectionDescription = lp.getContentWithNoPrefix("CRMConnectionDescription");
                    ViewBag.OpenUrl = lp.getContentWithNoPrefix("OpenUrl");
                    ViewBag.TestConnection = lp.getContentWithNoPrefix("TestConnection");
                    break;
                case "_DBConnection":
                    ViewBag.DBConnectionDescription = lp.getContentWithNoPrefix("DBConnectionDescription");
                    ViewBag.TestConnection = lp.getContentWithNoPrefix("TestConnection");
                    break;
                case "_OtherSetting":
                    ViewBag.MultipleCaseProduct = lp.getContentWithNoPrefix("MultipleCaseProduct");
                    ViewBag.PasswordEncryption = lp.getContentWithNoPrefix("PasswordEncryption");
                    ViewBag.PasswordEncryptionDesc = lp.getContentWithNoPrefix("PasswordEncryptionDesc");
                    ViewBag.MultipleCaseProductDesc = lp.getContentWithNoPrefix("MultipleCaseProductDesc");
                    ViewBag.CorrectPassword = lp.getContentWithNoPrefix("CorrectPassword");
                    ViewBag.SendActivateAccountInvitationDesc = lp.getContentWithNoPrefix("SendActivateAccountInvitationDesc");
                    ViewBag.SendActivateAccountInvitation = lp.getContentWithNoPrefix("SendActivateAccountInvitation");
                    ViewBag.SendInvitation = lp.getContentWithNoPrefix("SendInvitation");
                    break;
                default:
                    break;

            }
        }

        //取得layout中按鈕的說明
        private void GetActionTitles()
        {
            LanguagePackage lp = new LanguagePackage(CountryCode);
            ViewBag.BasicSetting = lp.getContentWithNoPrefix("BasicSetting");
            ViewBag.LanguageSetting = lp.getContentWithNoPrefix("LanguageSetting");
            ViewBag.WhiteListSetting = lp.getContentWithNoPrefix("WhiteListSetting");
            ViewBag.SimulationSetting = lp.getContentWithNoPrefix("SimulationSetting");
            ViewBag.OutOfServiceSetting = lp.getContentWithNoPrefix("OutOfServiceSetting");
            ViewBag.AdvancedSetting = lp.getContentWithNoPrefix("AdvancedSetting");
            ViewBag.ActionLogOut = lp.getContentWithNoPrefix("ActionLogOut");
            ViewBag.Alert = lp.getContentWithNoPrefix("Alert");
        }
    }
}
