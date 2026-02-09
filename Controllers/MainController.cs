using Newtonsoft.Json.Linq;
using InstallationAPPNonUnify.Modules;
using InstallationAPPNonUnify.Modules.Filters;
using InstallationAPPNonUnify.ViewModels;
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI;

namespace InstallationAPPNonUnify.Controllers
{
    [OutOfServiceAttribute(AllowRole = new string[] { "administrator", "superuser" })]
    public class MainController : BaseController
    {
        protected string Layout = "_Layout_Unauth.cshtml";
        protected string Title = "Johnson Health Tech Mobile";

        public ActionResult Redirect() {
            return RedirectToAction("Login");
        }

        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
        public ActionResult Login(string ReturnUrl, string message)
        {
            GetViewBag("Login");    //取得相關訊息

            LoginViewModel lvm = new LoginViewModel(CountryCode);

            //有送來訊息
            if (TempData.ContainsKey("alertMessage")) lvm.AlertMessage = TempData["alertMessage"].ToString();

            //如果有message，以message為主
            if (!string.IsNullOrEmpty(message) || !string.IsNullOrWhiteSpace(message)) {
                lvm.AlertMessage = message;
            }

            //在測試環境下，跳出選擇的語言
            lvm.TestModeMessage = lvm.IsTestMode ? lvm.CountryCode : "";

            //如果已經有權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName]);
            if (fatr.isValidTicket) {
                if (fatr.Role.ToLower().Equals("regularuser"))
                    return RedirectToAction("OrderInformation", "Order");
            }

            ViewBag.ReturnUrl = ReturnUrl;

            lvm.TestUserLanguage = UserLanguage;

            return View(lvm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AntiForgeryErrorHandler(ExceptionType = typeof(HttpAntiForgeryException), View = "Login", Controller = "Main", ErrorMessage = "Session Timeout")]
        public ActionResult Login(LoginViewModel lvm, string ReturnUrl)
        {
            GetViewBag("Login");    //取得相關訊息

            if (!ModelState.IsValid) return View(lvm);

            //Clear cookie first
            LogoutProcess();

            //check auth
            Authorization auth = new Authorization(lvm.UserName, lvm.UserPassword);
            if ((!auth.IsValidUser) || (!auth.IsPasswordCorrect))
            {
                LanguagePackage lp = new LanguagePackage(CountryCode);
                if (!auth.IsValidUser) ModelState.AddModelError("UserName", lp.getContentWithNoPrefix("InvalidUserId"));
                if (!auth.IsPasswordCorrect) ModelState.AddModelError("UserPassword", lp.getContentWithNoPrefix("IncorrectPassword"));

                return View(lvm);
            }

            //產生FormAuthentication
            NewFormAuthentication authcation = new NewFormAuthentication(auth);
            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, authcation.EncryptedTicket);
            Response.Cookies.Add(cookie);

            if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
                return Redirect(ReturnUrl);
            else
                return RedirectToAction("OrderInformation", "Order");
        }

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

            //清除CaseInfo的Cookie
            var cookie = new HttpCookie("CaseInfo");
            cookie.Expires = DateTime.Now.AddYears(-1);
            Response.Cookies.Add(cookie);
        }

        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
        public ActionResult EmailUs()
        {
            GetViewBag("EmailUs");    //取得相關訊息

            EmailUsViewModel evm = new EmailUsViewModel(CountryCode);

            //有送來訊息
            if (TempData.ContainsKey("alertMessage")) evm.AlertMessage = TempData["alertMessage"].ToString();

            return View("EmailUs", evm);
        }

        [HttpPost]
        public ActionResult EmailUs(EmailUsViewModel euvm)
        {
            LanguagePackage lp = new LanguagePackage(CountryCode);

            Email mail = new Email(CountryCode);
            mail.SendRequest(euvm);

            if (mail.IsSuccessed)
            {
                LoginViewModel lvm = new LoginViewModel(CountryCode);
                TempData["alertMessage"] = lp.getContentWithNoPrefix("FeedbackSuccessed");
                return RedirectToAction("Login");
            }
            else
            {
                GetViewBag("EmailUs");    //取得相關訊息
                EmailUsViewModel evm = new EmailUsViewModel(CountryCode);
                evm.AlertMessage = lp.getContentWithNoPrefix("FeedbackFailed");
                return View(evm);
            }
        }

        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
        public ActionResult ForgetPassword(string message = "") {
            GetViewBag("ForgetPassword");    //取得相關訊息

            ForegetPasswordViewModel fpvm = new ForegetPasswordViewModel(CountryCode);

            //有送來訊息
            if (TempData.ContainsKey("alertMessage")) fpvm.AlertMessage = TempData["alertMessage"].ToString();

            //如果有message，以message為主
            if (!string.IsNullOrEmpty(message) || !string.IsNullOrWhiteSpace(message))
            {
                //fpvm.AlertMessage = message;
                TempData["alertMessage"] = message;
                return RedirectToAction("ForgetPassword");
            }

            return View(fpvm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AntiForgeryErrorHandler(ExceptionType = typeof(HttpAntiForgeryException), View = "ForgetPassword", Controller = "Main", ErrorMessage = "Session Timeout")]
        public ActionResult ForgetPassword(ForegetPasswordViewModel fpvm)
        {
            GetViewBag("ForgetPassword");    //取得相關訊息

            if (fpvm == null)
                return RedirectToAction("ForgetPassword");

            //get email
            ResetPassword rp = new ResetPassword();
            ResetPasswordModel rpm = rp.GetResetAccountEmail(fpvm.UserName, false);
            if (!rpm.IsValid)
            {
                TempData["Type"] = "AccountNotFound";   //找不到帳號
            }
            else 
            {
                if (!rpm.IsEmailCompleted)
                {
                    //帳號沒有維護Email，或主要聯絡人上也沒有維護Email
                    TempData["Type"] = "IncompleteInformation";
                }
                else {
                    //產生token並寄出信件
                    if (!rp.ResetUserPassword(rpm, this.Request, CountryCode))
                    {
                        //出現異常，例如回寫資料錯誤或信件寄不出去，回到ForgetPassword畫面，並出現訊息
                        TempData["alertMessage"] = ViewBag.ErrorOccrus;
                        return RedirectToAction("ForgetPassword");
                    }
                    TempData["Type"] = "ResetPasswordSuccessfully";
                    TempData["Email"] = rpm.Email;
                }
            }
            return RedirectToAction("Messages");
        }

        public ActionResult Messages()
        {
            if (!TempData.Keys.Contains("Type")) return RedirectToAction("Login");

            GetViewBag("Messages");    //取得相關訊息
            var type = TempData["Type"].ToString();
            LanguagePackage lp = new LanguagePackage(CountryCode);

            switch (type) {
                case "ResetPasswordSuccessfully":
                    ViewBag.CurrentPage = type;
                    ViewBag.BodyTitle = lp.getContentWithNoPrefix("ResetPasswordSuccessfully");
                    ViewBag.Email = TempData["Email"].ToString();
                    ViewBag.ResetPasswordSuccessfullyDesc1 = lp.getContentWithNoPrefix("ResetPasswordSuccessfullyDesc1");
                    ViewBag.ResetPasswordSuccessfullyDesc2 = lp.getContentWithNoPrefix("ResetPasswordSuccessfullyDesc2");
                    ViewBag.ResetPasswordSuccessfullyDesc3 = lp.getContentWithNoPrefix("AccountNotFoundDesc2");
                    break;
                case "AccountNotFound":
                    ViewBag.CurrentPage = "AccountNotFound";
                    ViewBag.BodyTitle = lp.getContentWithNoPrefix("AccountNotFound");
                    ViewBag.AccountNotFoundDesc1 = lp.getContentWithNoPrefix("AccountNotFoundDesc1");
                    ViewBag.AccountNotFoundDesc2 = lp.getContentWithNoPrefix("AccountNotFoundDesc2");
                    ViewBag.AccountNotFoundDesc3 = lp.getContentWithNoPrefix("AccountNotFoundDesc3");
                    break;
                case "IncompleteInformation":
                    ViewBag.CurrentPage = "IncompleteInformation";
                    ViewBag.BodyTitle = lp.getContentWithNoPrefix("IncompleteInformation");
                    ViewBag.IncompleteInformationDesc = lp.getContentWithNoPrefix("IncompleteInformationDesc");
                    ViewBag.AccountNotFoundDesc2 = lp.getContentWithNoPrefix("AccountNotFoundDesc2");
                    break;
                default:
                    return RedirectToAction("Login");
            }
            return View();
        }

        public ActionResult ChangePasswordByUrl(string accountId, string password) {

            LogoutProcess();    //先清掉所有session

            //檢查身份，合法的身份直接轉到 ChangePassword
            ResetPassword rp = new ResetPassword();
            var result = rp.CheckResetPasswordList(accountId, password, CountryCode);

            if (result.Item1)
            {
                //驗証正確，給一個暫時的權限，轉到Change Password
                Authorization auth = new Authorization(accountId, "", "tempRegular");

                //產生FormAuthentication
                NewFormAuthentication authcation = new NewFormAuthentication(auth, "tempRegular");
                var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, authcation.EncryptedTicket);
                Response.Cookies.Add(cookie);

                return RedirectToAction("ChangePassword", "Order");
            }
            return RedirectToAction("ForgetPassword", new { message = result.Item2 });
        }

        public ActionResult ChangePasswordDone() {

            LogoutProcess();    //先清掉所有session
            LanguagePackage lp = new LanguagePackage(CountryCode);
            return RedirectToAction("Login", new { message = lp.getContentWithNoPrefix("UpdatePasswordDone")});
        }

        [HttpGet]
        public ActionResult ActivateAccount(string AccountId)
        {
            GetViewBag("ActivateAccount");    //取得相關訊息
            LanguagePackage lp = new LanguagePackage(CountryCode);

            if (string.IsNullOrEmpty(AccountId) || string.IsNullOrWhiteSpace(AccountId))
            {
                ViewBag.AlertMessage = lp.getContentWithNoPrefix("MissingParameter");
                return View();
            }

            //get email
            ResetPassword rp = new ResetPassword();
            ResetPasswordModel rpm = rp.GetResetAccountEmail(AccountId, true);
            if (!rpm.IsValid)
            {
                ViewBag.AlertMessage = lp.getContentWithNoPrefix("FailToExecute") 
                    + " " + lp.getContentWithNoPrefix("AccountNotFound");   //找不到帳號
            }
            else
            {
                if (!rpm.IsEmailCompleted)
                {
                    //帳號沒有維護Email，或主要聯絡人上也沒有維護Email
                    ViewBag.AlertMessage = lp.getContentWithNoPrefix("FailToExecute")
                        + " " + lp.getContentWithNoPrefix("IncompleteInformation");
                }
                else
                {
                    //產生token並寄出信件
                    if (!rp.ResetUserPassword(rpm, this.Request, CountryCode, true))
                    {
                        //出現異常，例如回寫資料錯誤或信件寄不出去
                        ViewBag.AlertMessage = lp.getContentWithNoPrefix("FeedbackFailed");
                    }
                    else {
                        ViewBag.AlertMessage = lp.getContentWithNoPrefix("ExecuteSuccessfully")
                            + " " + lp.getContentWithNoPrefix("ActivatePasswordSuccessfully")
                            + " : " + rpm.Email;
                    }
                }
            }
            return View();
        }

        //取得頁面使用訊息
        private void GetViewBag(string viewName)
        {
            LanguagePackage lp = new LanguagePackage(CountryCode);
            switch (viewName)
            {
                case "Login":
                    ViewBag.Title = Title;
                    ViewBag.Layout = Layout;

                    //取訊息
                    ViewBag.WelcomeString = lp.getContentWithNoPrefix("WelcomeUnauth"); //Welcome to...
                    ViewBag.ServiceName = lp.getContentWithNoPrefix("CaseService");     //Matrix Installation APP
                    ViewBag.Signin = lp.getContentWithNoPrefix("Signin");               //Signin
                    ViewBag.UserIdPlaceholder = lp.getContentWithNoPrefix("PleaseInputUserId");
                    ViewBag.PasswordPlaceholder = lp.getContentWithNoPrefix("PleaseInputPassword");
                    ViewBag.FooterAnnounce = lp.getContentWithNoPrefix("FooterAnnounce");
                    ViewBag.FooterServiceAnnounce = lp.getContentWithNoPrefix("FooterServiceAnnounce");
                    ViewBag.ForgetPassword = lp.getContentWithNoPrefix("ForgetPassword");
                    ViewBag.Statement = lp.getContentWithNoPrefix("Statement");
                    break;

                case "EmailUs":
                    ViewBag.Title = Title;
                    ViewBag.Layout = "_Layout_Unauth2.cshtml";

                    //取訊息
                    ViewBag.FooterAnnounce = lp.getContentWithNoPrefix("FooterAnnounce");
                    ViewBag.PageTitle = lp.getContentWithNoPrefix("ContactUs");
                    ViewBag.YourDetails = lp.getContentWithNoPrefix("YourDetails");
                    ViewBag.MachineDetails = lp.getContentWithNoPrefix("MachineDetails");
                    ViewBag.SendQuery = lp.getContentWithNoPrefix("SendQuery");
                    ViewBag.Statement = lp.getContentWithNoPrefix("Statement");
                    break;
                case "ForgetPassword":
                    ViewBag.Title = Title;
                    ViewBag.Layout = "_Layout_Unauth2.cshtml";

                    //取訊息
                    ViewBag.ForgetYourPassword = lp.getContentWithNoPrefix("ForgetYourPassword");
                    ViewBag.ForgetPasswordDesc = lp.getContentWithNoPrefix("ForgetPasswordDesc");
                    ViewBag.ResetPassword = lp.getContentWithNoPrefix("ResetPassword");
                    ViewBag.ErrorOccrus = lp.getContentWithNoPrefix("ErrorOccrus");
                    break;
                case "ResetPassword":
                case "Messages":
                    ViewBag.Title = Title;
                    ViewBag.Layout = "_Layout_Unauth2.cshtml";

                    //取訊息
                    ViewBag.ErrorOccrus = lp.getContentWithNoPrefix("ErrorOccrus");
                    break;
            }
        }
    }
}
