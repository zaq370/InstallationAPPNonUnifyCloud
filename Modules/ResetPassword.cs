using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace InstallationAPPNonUnify.Modules
{
    public class ResetPassword
    {

        public string GetResetPasswordUrl(HttpRequestBase rcBase)
        {
            return new UrlHelper(rcBase.RequestContext).Action("ChangePasswordByUrl", "Main", new { Area = ""}, rcBase.Url.Scheme);
        }

        public string GetLoginUrl(HttpRequestBase rcBase) 
        {
            return new UrlHelper(rcBase.RequestContext).Action("Login", "Main", new { Area = ""}, rcBase.Url.Scheme);
        }

        public bool ResetUserPassword(ResetPasswordModel rpm, HttpRequestBase rcBase, string countryCode, bool isActivate = false) {
            rpm.Script = GetResetPasswordUrl(rcBase);
            rpm.Website = GetLoginUrl(rcBase);
            return ResetUserPassword(rpm, countryCode, isActivate);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="accountid">帳號 id</param>
        /// <param name="email">要寄出信的email</param>
        /// <param name="rcBase">http的 Request 物件(以取得網址) </param>
        /// <returns></returns>
        public bool ResetUserPassword(ResetPasswordModel rpm, string countryCode, bool isActivate = false)
        {
            bool isSuccessed = true;

            //產生一組密碼
            rpm.Token = Guid.NewGuid().ToString();

            //寫到ResetPassword.json
            ResetPasswordList rpl = new ResetPasswordList();
            isSuccessed = rpl.AddToList(rpm, isActivate);

            if (isSuccessed) {
                //組進網址
                rpm.Script = rpm.Script + "?accountId=" + rpm.AccountId + "&password=" + rpm.Token;

                //寄信給User
                if (!string.IsNullOrEmpty(rpm.CountryCode)) countryCode = rpm.CountryCode;

                Email email = new Email(countryCode);
                if (isActivate)
                {
                    isSuccessed = email.SendActivateAccountMail(rpm);
                }
                else {
                    isSuccessed = email.SendChangePasswordMail(rpm);
                }
            }

            return isSuccessed;
        }

        /// <summary>
        /// 依照User Id (portal account) 取得Email及 Account Id，會回傳Tuple，item1:true(有這個帳號)/false(沒有這個帳號)，
        /// item2:帳號連結到的Email，空白代表找不到可使用的Email，item3:accountId
        /// </summary>
        /// <param name="account">portal account</param>
        /// <returns></returns>
        public ResetPasswordModel GetResetAccountEmail(string account, bool isGuid = false)
        {
            var organization = new BackendInfo().GetSetting("Organization").ToLower();
            var new_language = string.Empty;    //部份國家有設定account language

            var dbConnection = new DBConnetion().GetConnection();
            if (!dbConnection.Item1)
            {
                throw new Exception("Get DBConnection Error!");
            }

            string queryString =
                "  select top 1 accountid, emailaddress1,name, emailaddress2, "
                + " (select strm.value from stringmap strm "
                + "  where strm.attributename = 'CustomerTypeCode' and strm.objecttypecode='1' "
                + "    and langid='1033' and attributevalue = acc.customertypecode) as customertypecodename , "
                + " (select emailaddress1 from Contact where contactId = acc.primarycontactid) as primatyContactEmail "
                + " ,new_portalaccount";

            switch (organization) { 
                case "jhtnl":
                case "jhtch":
                case "jhtdk":
                    queryString = queryString + " ,new_language ";
                    break;
            }

           queryString = queryString + " from account acc where statecode='0' ";

            if (isGuid)
            {
                queryString = queryString + " and accountid = @UserId";
            }
            else {
                queryString = queryString + " and new_portalaccount = @UserId";
            }
            SqlCommand command = new SqlCommand(queryString, dbConnection.Item2);
            command.Parameters.AddWithValue("@UserId", account);
            ResetPasswordModel rpm = new ResetPasswordModel();
            using (SqlDataReader sdr = command.ExecuteReader())
            {
                while (sdr.Read())
                {
                    var emailAddress1 = sdr.GetValue(sdr.GetOrdinal("emailaddress1")).ToString();
                    var emailAddress2 = sdr.GetValue(sdr.GetOrdinal("emailaddress2")).ToString();
                    var primaryContactEmail = sdr.GetValue(sdr.GetOrdinal("primatyContactEmail")).ToString();
                    

                    switch (organization)
                    { 
                        case "jhtnl":
                        case "jhtdk":
                            rpm.Email = (string.IsNullOrWhiteSpace(emailAddress2)) ? emailAddress1 : emailAddress2;
                            new_language = sdr.GetValue(sdr.GetOrdinal("new_language")).ToString();
                            break;
                        case "jhtch":
                            rpm.Email = (string.IsNullOrWhiteSpace(emailAddress1)) ? primaryContactEmail : emailAddress1;
                            new_language = sdr.GetValue(sdr.GetOrdinal("new_language")).ToString();
                            break;
                        default:
                            rpm.Email = (string.IsNullOrWhiteSpace(emailAddress1)) ? primaryContactEmail : emailAddress1;
                            break;
                    }
                    rpm.AccountId = sdr.GetValue(sdr.GetOrdinal("accountid")).ToString();
                    rpm.AccountName = sdr.GetValue(sdr.GetOrdinal("name")).ToString();
                    rpm.PortalAccount = sdr.GetValue(sdr.GetOrdinal("new_portalaccount")).ToString();
                }
            }

            dbConnection.Item2.Dispose();

            if (string.IsNullOrEmpty(rpm.AccountId) || string.IsNullOrWhiteSpace(rpm.AccountId)) rpm.IsValid = false;
            if (string.IsNullOrEmpty(rpm.Email) || string.IsNullOrWhiteSpace(rpm.Email)) rpm.IsEmailCompleted = false;

            //有設定UserLanguage的國家，比對出countryCode
            switch (organization)
            {
                case "jhtnl":
                    switch (new_language) { 
                        case "1":
                            rpm.CountryCode = "en";
                            break;
                        case "2":
                            rpm.CountryCode = "nl";
                            break;
                        case "3":
                            rpm.CountryCode = "fr";
                            break;
                        default:
                            rpm.CountryCode = "en";
                            break;
                    }
                    break;
                case "jhtdk":
                    switch (new_language)
                    {
                        case "1":
                            rpm.CountryCode = "en";
                            break;
                        case "2":
                            rpm.CountryCode = "nl";
                            break;
                        case "3":
                            rpm.CountryCode = "fr";
                            break;
                        case "5":
                            rpm.CountryCode = "da";
                            break;
                        default:
                            rpm.CountryCode = "en";
                            break;
                    }
                    break;
                case "jhtch":
                    switch (new_language) { 
                        case "100000001":
                            rpm.CountryCode = "nl";
                            break;
                        case "100000002":
                            rpm.CountryCode = "en";
                            break;
                        case "100000003":
                            rpm.CountryCode = "fr";
                            break;
                        default:
                            rpm.CountryCode = "en";
                            break;
                    }
                    break;
            }
            return rpm;
        }

        public Tuple<bool,string> CheckResetPasswordList(string accountId, string password, string countryCode) {
            var message = string.Empty;

            LanguagePackage lp = new LanguagePackage(countryCode);

            //取得
            ResetPasswordList rpl = new ResetPasswordList();
            var member = rpl.GetListMember(accountId);

            //不存在
            if (member == null) {
                message = lp.getContentWithNoPrefix("DataNotExist");
                return new Tuple<bool, string>(false, message);
            }

            //token不同
            JToken tempToken;
            member.TryGetValue("Token", out tempToken);
            if (tempToken == null || !tempToken.ToString().Equals(password)) {
                message = lp.getContentWithNoPrefix("DataNotExist");
                return new Tuple<bool, string>(false, message);
            }
                
            //時間超過30分鐘
            member.TryGetValue("ExpireTime", out tempToken);
            if (tempToken == null || long.Parse(tempToken.ToString()) < DateTime.UtcNow.Ticks) {
                message = lp.getContentWithNoPrefix("DataExpired");
                return new Tuple<bool, string>(false, message);
            }

            return new Tuple<bool, string>(true, "");
        }
    }

    public class ResetPasswordModel
    {
        public string AccountId { get; set; }
        public string AccountName { get; set; }
        public string Email { get; set; }
        public string Token { get; set; }
        public bool IsValid { get; set; }
        public bool IsEmailCompleted { get; set; }
        public string Script { get; set; }
        public string Website { get; set; }
        public string PortalAccount { get; set; }
        public string CountryCode { get; set; }

        public ResetPasswordModel() {
            IsValid = true;
            IsEmailCompleted = true;
        }
    }

    public class ResetPasswordList
    {

        public static JArray List { get; set; }
        public static bool IsListReady { get; set; }

        private readonly object fileLock = new object();
        private readonly object listLock = new object();

        private static string infoDir = "~/App_Data/";
        private static string cacheDir = infoDir + "cache/";
        private static string packageFileName = "ResetPassword.json";
        private static int timeout = 30;    //token 只存放30分鐘
        private static int timeoutForActivate = 60*24*2;    //token 2天

        public ResetPasswordList()
        {
            if (!IsListReady)
            {
                ReloadList();
            }
        }

        private void ReloadList()
        {
            string packagePath = System.Web.Hosting.HostingEnvironment.MapPath(infoDir + packageFileName);

            IsListReady = false;

            using (var fs = new FileStream(packagePath, FileMode.OpenOrCreate))
            {
                using (var sr = new StreamReader(fs))
                {
                    try
                    {
                        List = JArray.Parse(sr.ReadToEnd().ToString());
                        IsListReady = true;
                    }
                    catch (Exception ex)
                    {
                        List = new JArray();
                        IsListReady = false;
                        Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                    }
                }
            }
        }

        public bool AddToList(ResetPasswordModel newMember, bool isActivate = false)
        {
            if (!IsListReady) return false;

            //修改
            lock (listLock)
            {
                //如果有同樣的AccountId，先刪除再新增
                var newList = new JArray(List);
                newList.Where(i => i.Value<string>("AccountId").Equals(newMember.AccountId)).ToList().ForEach(i => i.Remove());

                //加到JArray
                DateTime expiredTime = DateTime.UtcNow.AddMinutes(timeout);  //過期時間

                if (isActivate)
                    expiredTime = DateTime.UtcNow.AddMinutes(timeoutForActivate);

                JObject memberObj = new JObject();
                memberObj.Add(new JProperty("AccountId", newMember.AccountId));
                memberObj.Add(new JProperty("Token", newMember.Token));
                memberObj.Add(new JProperty("ExpireTime", expiredTime.Ticks));
                newList.Add(memberObj);

                //回寫
                return WriteBackFile(newList);
            }
        }

        public bool RemoveFromList(ResetPasswordModel newMember)
        {
            if (!IsListReady) return false;

            //修改
            lock (listLock)
            {
                //移除同樣的AccountId
                var newList = new JArray(List);
                newList.Where(i => i.Value<string>("AccountId").Equals(newMember.AccountId)).ToList().ForEach(i => i.Remove());

                //回寫
                return WriteBackFile(newList);
            }
        }

        public JObject GetListMember(string AccountId)
        {
            if (!IsListReady) return new JObject();

            //取得Object
            var member = List.FirstOrDefault(i => i.Value<string>("AccountId").Equals(AccountId));

            //轉成
            return member == null ? new JObject() : JObject.Parse(member.ToString());
        }

        public bool WriteBackFile(JArray newContent)
        {
            bool isUpdateSuccessfully = false;

            //修改
            lock (fileLock)
            {
                string packagePath = System.Web.Hosting.HostingEnvironment.MapPath(infoDir + packageFileName);

                ////先備份 (不備份)
                //var backupFileName = packageFileName + DateTime.Now.ToString("yyyyMMdd-HHmmss.fff");
                //string backupPath = System.Web.Hosting.HostingEnvironment.MapPath(cacheDir + backupFileName);
                //File.Copy(packagePath, backupPath);

                //過濾已經超過30分鐘的Token
                JArray contentArray = new JArray();
                foreach (var content in newContent) {

                    JToken tempToken;
                    JObject member = JObject.Parse(content.ToString());

                    //時間沒超過30分鐘
                    member.TryGetValue("ExpireTime", out tempToken);
                    if (tempToken != null && long.Parse(tempToken.ToString()) >= DateTime.UtcNow.Ticks)
                    {
                        contentArray.Add(member);
                    }
                }

                try { 
                    //覆寫
                    File.WriteAllText(packagePath, contentArray.ToString(), Encoding.Unicode);
                    isUpdateSuccessfully = true;
                } catch(Exception ex) {
                    Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                }

                if (isUpdateSuccessfully)
                {
                    List = new JArray(newContent);
                }
            }
            
            return isUpdateSuccessfully;
        }
    }
}