using Newtonsoft.Json.Linq;
using InstallationAPPNonUnify.Areas.CMS.Models;
using InstallationAPPNonUnify.Areas.CMS.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace InstallationAPPNonUnify.Modules
{
    public static class BackendFile {
        public static JObject FileContent { get; set; }
        public static bool IsFileReady { get; set; }
        public static List<OutOfServerModel> NoInServiceSegment { get; set; }
        public static Dictionary<string, string> CompanyInfo { get; set; }
    }

    public class BackendInfo
    {
        public bool isTestMode { get; private set; }
        public bool IsEncryptPassword { get; private set; }
        
        private static string infoDir = "~/App_Data/";
        private static string cacheDir = infoDir + "cache/";
        private static string packageFileName = "BackendInfo.json";

        private readonly object fileLock = new object();

        public BackendInfo()
        {
            if (!BackendFile.IsFileReady || BackendFile.FileContent == null || BackendFile.FileContent.Count == 0)
            {
                UpdateFileContent();
            }
            EnvironmentSetting();
        }

        /// <summary>
        /// 從BackendInfo.json檔中重取，並更新 BackendFile 中的資料
        /// </summary>
        public void UpdateFileContent()
        {
            string packagePath = System.Web.Hosting.HostingEnvironment.MapPath(infoDir + packageFileName);

            BackendFile.IsFileReady = false;
            BackendFile.FileContent = new JObject();

            using (var fs = new FileStream(packagePath, FileMode.Open,FileAccess.Read,FileShare.Read))
            {
                using (var sr = new StreamReader(fs))
                {
                    var fileContent = JObject.Parse(sr.ReadToEnd().ToString());
                    if (fileContent != null && fileContent.Count > 0) {
                        BackendFile.FileContent = fileContent;
                        BackendFile.IsFileReady = true;
                        EnvironmentSetting();   //如果有需要在更新BackendInfo時需要重取的資料，請統一寫在這裡
                    }
                }
            }
        }

        /// <summary>
        /// 從 BackendFile中取出網站暫停服務時間(如果資料與設定不同時，請使用 UpdateFileContent() 重取設定 )
        /// </summary>
        public void UpdateWebsiteNoInServiceSegment()
        {
            BackendFile.NoInServiceSegment = new List<OutOfServerModel>();

            var result = new JArray();
            if (!BackendFile.IsFileReady) return;
            ReadToken<JArray>(BackendFile.FileContent, "OutOfServiceTime", ref result);

            //整理成List<OutOfServerModel>
            foreach (var data in result)
            {
                JObject setment = new JObject();
                try
                {
                    setment = JObject.Parse(data.ToString());
                }
                catch (Exception ex)
                {
                    var subject = "BackendFile.OutOfServiceTime syntax error: " + data.ToString();
                    Elmah.ErrorSignal.FromCurrentContext().Raise(new Exception(subject, ex));
                    continue;
                }
                JToken tempToken;
                setment.TryGetValue("Subject", out tempToken);
                var NoInServerSubject = string.IsNullOrEmpty(tempToken.ToString()) ? "" : tempToken.ToString();

                setment.TryGetValue("StartDate", out tempToken);
                var StartDate = tempToken.ToString();

                setment.TryGetValue("EndDate", out tempToken);
                var EndDate = tempToken.ToString();

                if (string.IsNullOrEmpty(StartDate) || string.IsNullOrEmpty(StartDate)) continue;

                setment.TryGetValue("StartTime", out tempToken);
                var StartTime = tempToken.ToString();
                if (!string.IsNullOrEmpty(StartTime)) StartDate = StartDate + " " + StartTime;

                setment.TryGetValue("EndTime", out tempToken);
                var EndTime = tempToken.ToString();
                if (!string.IsNullOrEmpty(EndTime)) EndDate = EndDate + " " + EndTime;

                DateTime StartDateTime;
                DateTime.TryParse(StartDate, out StartDateTime);

                DateTime EndDateTime;
                DateTime.TryParse(EndDate, out EndDateTime);

                OutOfServerModel oosm = new OutOfServerModel() { 
                    Subject= NoInServerSubject,
                    StartDate = StartDateTime,
                    EndDate = EndDateTime
                };
                BackendFile.NoInServiceSegment.Add(oosm);
            }
        }

        /// <summary>
        /// 重取public properties，如果有需要在更新BackendInfo時需要重取的資料，請統一寫在這裡
        /// </summary>
        private void EnvironmentSetting()
        {
            GetTestMode();                      //取得是否為測試模式
            UpdateWebsiteNoInServiceSegment();  //更新網站休息時間
            GetEncryptPasswordSetting();    //有沒有設定密碼加密
            BackendFile.CompanyInfo = ReadCompanyInfo();    //取得聯絡資訊
        }

        /// <summary>
        /// 從 BackendFile中設定是否為測試環境(如果資料與設定不同時，請使用 UpdateFileContent() 重取設定 )
        /// 目前測試環境的差異只在於會用TestModeSmtpSetting中設定的方式寄出信件
        /// </summary>
        private void GetTestMode()
        {
            JToken tempToken;
            if (!BackendFile.IsFileReady) return;
            BackendFile.FileContent.TryGetValue("TestMode", out tempToken);
            if (tempToken != null)
            {
                bool _testMode;
                bool.TryParse(tempToken.ToString(), out _testMode);
                isTestMode = _testMode;
            }
        }

        /// <summary>
        /// 從 BackendInfo 中取出 OtherSetting.PasswordEncryption，並成為 public property
        /// </summary>
        private void GetEncryptPasswordSetting()
        {
            bool setting;
            bool.TryParse(ReadOtherSetting("PasswordEncryption"), out setting);

            IsEncryptPassword = setting;
        }

        /// <summary>
        /// 從 BackendInfo 中的 OtherSetting 取出 MultipleCaseProduct
        /// </summary>
        /// <returns> true/false : true 同一Session中的Case Product要放在同一個Case下, false 一個case product 產生一筆 case
        /// </returns>
        public bool MultipleCaseProduct() {
            var value = ReadOtherSetting("MultipleCaseProduct");

            if (string.IsNullOrEmpty(value)) return false;  //沒有這個設定
            
            bool isMultipleCaseProduct = false;
            if (!bool.TryParse(value, out isMultipleCaseProduct)) return false;

            return isMultipleCaseProduct;
        }

        public Dictionary<string, string> ReadAsJObject(string catelog)
        {
            var result = new Dictionary<string, string>();
            if (!BackendFile.IsFileReady) return result;
            JObject content = new JObject();
            ReadToken<JObject>(BackendFile.FileContent, catelog, ref content);

            if (content.Count == 0) return result;
            result = content.Properties().ToDictionary(p => p.Name.ToString(), p => p.Value.ToString());

            return result;
        }

        public List<CountryModel> ReadCountries()
        {
            var result = new List<CountryModel>();
            if (!BackendFile.IsFileReady) return result;
            JArray content = new JArray();
            ReadToken<JArray>(BackendFile.FileContent, "Country", ref content);

            if (content == null || content.Count == 0) return result;

            foreach (var item in content) {
                CountryModel cm = new CountryModel();
                cm = item.ToObject<CountryModel>();
                result.Add(cm);
            }
            return new List<CountryModel>(result.OrderBy(item => item.Sequence));
        }

        public string GetRedirectCountryCode(string countryCode) {
            string RedirectCountryCode = string.Empty;

            var countries = ReadCountries();
            //如果Countries裡面有目前的countryCode，再找有沒有轉向的語系
            if (countries.Exists(item => item.LanguagePackage.ToLower().Equals(countryCode.ToLower())))
                RedirectCountryCode = countries.FirstOrDefault(item => item.LanguagePackage.ToLower().Equals(countryCode.ToLower())).RedirectLanguagePackage.ToLower();

            //如果為空，可能是某些瀏覽器的編碼不同，再檢查前N碼
            if (string.IsNullOrEmpty(RedirectCountryCode))
            {
                if (countries.Exists(item => item.LanguagePackage.Length>=countryCode.Length && item.LanguagePackage.ToLower().Substring(0, countryCode.Length).Equals(countryCode.ToLower())))
                    RedirectCountryCode = countries.FirstOrDefault(item => item.LanguagePackage.ToLower().Substring(0, countryCode.Length).Equals(countryCode.ToLower())).RedirectLanguagePackage.ToLower();
            }

            //如果還是為空，取-之前的碼別來比對，例如 fr-fr，可以用fr來比對
            if (string.IsNullOrEmpty(RedirectCountryCode))
            {
                if (countryCode.IndexOf("-") >= 0) {
                    var abb = countryCode.Substring(0, countryCode.IndexOf("-")).ToLower();
                    if (countries.Exists(item => item.LanguagePackage.ToLower().Substring(0, abb.Length).Equals(abb)))
                        RedirectCountryCode = countries.FirstOrDefault(item => item.LanguagePackage.ToLower().Substring(0, abb.Length).Equals(abb)).RedirectLanguagePackage.ToLower();
                }
            }

            return string.IsNullOrEmpty(RedirectCountryCode) ? countryCode.ToLower() : RedirectCountryCode.ToLower();
        }

        /// <summary>
        /// 提供幾個常用到的設定
        /// </summary>
        /// <returns></returns>
        public string GetSetting(string setting) {
            string result = string.Empty;
            switch (setting) { 
                case "Organization":
                    var CRMConnectionSetting = ReadCRMConnectionSetting();
                    if (CRMConnectionSetting.ContainsKey("CRMOrganizationName"))
                        result = CRMConnectionSetting["CRMOrganizationName"];
                    break;
            }

            return result;
        }

        public Dictionary<string, string> ReadCompanyInfo()
        {
            return ReadAsJObject("CompanyInfo");
        }

        public Dictionary<string, string> ReadOtherSetting()
        {
            return ReadAsJObject("OtherSetting");
        }

        public string ReadOtherSetting(string key)
        {
            var setting = ReadOtherSetting();
            if (setting.ContainsKey(key))
                return setting[key];
            else
                return string.Empty;
        }

        public Dictionary<string, string> ReadSmtpInfo(bool manual = false, string type = "SmtpSetting")
        {
            if (!manual)
            {
                if (isTestMode)
                    return ReadAsJObject("TestModeSmtpSetting");
                else
                    return ReadAsJObject("SmtpSetting");
            }
            else { 
                if (type.ToLower().Equals("smtpsetting"))
                    return ReadAsJObject("SmtpSetting");
                else
                    return ReadAsJObject("TestModeSmtpSetting");
            }
        }

        public Dictionary<string, string> ReadCRMConnectionSetting()
        {
            return ReadAsJObject("CRMConnectionSetting");
        }

        public Dictionary<string, string> ReadDBConnectionString()
        {
            return ReadAsJObject("DBConnectionString");
        }

        private void ReadToken<T>(JObject sourceObj, string category, ref T content) where T : JContainer
        {
            JToken tempToken;
            if (!BackendFile.IsFileReady) return;
            BackendFile.FileContent.TryGetValue(category, out tempToken);
            if (tempToken != null) {
                content = (T)JToken.Parse(tempToken.ToString());
            }
        }

        public bool UpdateBackendInfoFile(JArray updateContents, string modifier) {
            bool isUpdateSuccessfully = false;

            lock (fileLock) {
                //開始修改
                JObject newFileContent = new JObject(BackendFile.FileContent);

                foreach (var content in updateContents)
                {
                    JObject contentObj = content.ToObject<JObject>();
                    JToken tempToken;

                    //segment
                    if (!contentObj.TryGetValue("segment", out tempToken))
                        return isUpdateSuccessfully;
                    string segment = tempToken.ToString();

                    //newContent
                    if (!contentObj.TryGetValue("newContent", out tempToken))
                        return isUpdateSuccessfully;
                    JToken newContent = tempToken;

                    //這次修改的內容
                    newFileContent.Remove(segment);
                    newFileContent.Add(new JProperty(segment, newContent));
                }
                //加上修改者
                newFileContent.Remove("Modifier");
                newFileContent.Add(new JProperty("Modifier", modifier));

                //先備份
                var backupFileName = packageFileName + DateTime.Now.ToString("yyyyMMdd-HHmmss.fff");
                string packagePath = System.Web.Hosting.HostingEnvironment.MapPath(infoDir + packageFileName);
                string backupPath = System.Web.Hosting.HostingEnvironment.MapPath(cacheDir + backupFileName);
                File.Copy(packagePath, backupPath);

                //覆寫
                File.WriteAllText(packagePath, newFileContent.ToString(), Encoding.Unicode);
                isUpdateSuccessfully = true;
            }

            if (isUpdateSuccessfully)
            {
                UpdateFileContent();
            } 

            return isUpdateSuccessfully;
        }

        /// <summary>
        /// 更新 BackendInof.json 檔案中的內容
        /// </summary>
        public bool UpdateBackendInfoFile(string segment, JToken newContent, string modifier) {

            JArray contentArray = new JArray();
            JObject content = new JObject();
            content.Add(new JProperty("segment", segment));
            content.Add(new JProperty("newContent", newContent));
            contentArray.Add(content);

            return UpdateBackendInfoFile(contentArray, modifier);
        }

        public bool UpdateOutOfServiceDuration(string content,string modifier) {
            bool isUpdateSuccessfully = false;

            var newDuration = new JArray();
            if (!string.IsNullOrEmpty(content))
                newDuration = JArray.Parse(content);

            if (UpdateBackendInfoFile("OutOfServiceTime", newDuration, modifier))
                isUpdateSuccessfully = true;

            return isUpdateSuccessfully;
        }
    }
}