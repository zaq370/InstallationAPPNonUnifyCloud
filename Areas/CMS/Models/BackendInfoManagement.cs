using Elmah;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json.Linq;
using InstallationAPPNonUnify.Areas.CMS.ViewModels;
using InstallationAPPNonUnify.Modules;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Web;

namespace InstallationAPPNonUnify.Areas.CMS.Models
{
    public class BackendInfoManagement
    {
        private object PasswordLocker = new Object(); // 用於獨佔鎖定的物件

        public void  ReadCompanyInfo(BasicSettingViewModel bsvm) {
            var backendInfo = new BackendInfo();
            if (!BackendFile.IsFileReady) backendInfo.UpdateFileContent();
            if (BackendFile.IsFileReady)
            {
                var CompanyInfo = backendInfo.ReadCompanyInfo();
                bsvm.Transform(CompanyInfo);
            }
        }

        public bool UpdateCompanyInfo(BasicSettingViewModel bsvm, string modifier)
        {
            var lp = new LanguagePackage(bsvm.CountryCode);
            var backendInfo = new BackendInfo();

            if (!backendInfo.UpdateBackendInfoFile("CompanyInfo", bsvm.ToJObject(), modifier))
            {
                bsvm.AlertMessage = lp.getContentWithNoPrefix("UpdateFileFailed");  //失敗
                return false;
            }
            else
            {
                bsvm.AlertMessage = lp.getContentWithNoPrefix("UpdateFileSuccessed");   //成功
                return true;
            }
        }

        public Tuple<bool,string> UpdateSmtpInfo(AdvancedSettingViewModel asvm, string modifier) {

            //smtp
            JObject Smtp = new JObject();
            Smtp.Add(new JProperty("segment", "SmtpSetting"));
            Smtp.Add(new JProperty("newContent", JObject.FromObject(asvm.SmtpSetting)));

            //test mode smtp
            JObject TestModeSmtp = new JObject();
            TestModeSmtp.Add(new JProperty("segment", "TestModeSmtpSetting"));
            TestModeSmtp.Add(new JProperty("newContent", JObject.FromObject(asvm.TestModeSmtpSetting)));

            //jarray
            JArray newContent = new JArray();
            newContent.Add(Smtp);
            newContent.Add(TestModeSmtp);

            //execute
            var lp = new LanguagePackage(asvm.CountryCode);
            var backendInfo = new BackendInfo();
            if (!backendInfo.UpdateBackendInfoFile(newContent, modifier))
            {
                return new Tuple<bool, string>(false, lp.getContentWithNoPrefix("UpdateFileFailed"));   //失敗
            }
            else
            {
                return new Tuple<bool, string>(true, lp.getContentWithNoPrefix("UpdateFileSuccessed"));   //成功
            }
        }

        public Tuple<bool, string> UpdateContent(string Segment, string NewValue, string Modifier, string CountryCode="en-us") {
            var isSuccessed = true;

            //execute
            var backendInfo = new BackendInfo();
            if (!backendInfo.UpdateBackendInfoFile(Segment,new JValue(NewValue), Modifier))
                isSuccessed = false;

            var lp = new LanguagePackage(CountryCode);

            //return
            if (isSuccessed)
                return new Tuple<bool, string>(true, lp.getContentWithNoPrefix("UpdateFileSuccessed"));
            else
                return new Tuple<bool, string>(false, lp.getContentWithNoPrefix("UpdateFileFailed"));
        }

        public Tuple<bool,string> ImportAndApplyBackendInfoFile(HttpFileCollectionBase files, string modifier, string countryCode)
        {
            Stream req = files[0].InputStream;
            var content = new StreamReader(req).ReadToEnd();

            //確認匯入的資料正常
            LanguagePackage lp = new LanguagePackage(countryCode);
            JObject contentObj = new JObject();
            try
            {
                contentObj = JObject.Parse(content);
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string>(false, lp.getContentWithNoPrefix("InvalidContent"));
            }

            //確認該有的資料都在
            JArray contentArray = new JArray();
            JToken tempToken;
            foreach (var property in BackendFile.FileContent.Properties()) {
                //確定新匯入的資料也有這些property
                contentObj.TryGetValue(property.Name, out tempToken);
                if (property.Name.ToLower() != "modifier") {
                    if (tempToken == null || string.IsNullOrEmpty(tempToken.ToString()) || string.IsNullOrWhiteSpace(tempToken.ToString()))
                        return new Tuple<bool, string>(false, property.Name + " " + lp.getContentWithNoPrefix("InvalidContent"));

                    //新增到Array
                    JObject tempContent = new JObject();
                    tempContent.Add(new JProperty("segment", property.Name));
                    tempContent.Add(new JProperty("newContent", tempToken));
                    contentArray.Add(tempContent);
                }
                //移除
                contentObj.Remove(property.Name);
            }

            //找出新增加的property
            foreach (var newProperty in contentObj.Properties()) {
                //新增到Array
                JObject tempContent = new JObject();
                tempContent.Add(new JProperty("segment", newProperty.Name));
                tempContent.Add(new JProperty("newContent", newProperty.Value));
                contentArray.Add(tempContent);
            }

            //更新至檔案
            var backendInfo = new BackendInfo();
            if (backendInfo.UpdateBackendInfoFile(contentArray, modifier))
                return new Tuple<bool, string>(true, lp.getContentWithNoPrefix("ExecuteSuccessfully"));
            else
                return new Tuple<bool, string>(true, lp.getContentWithNoPrefix("FailToExecute"));
        }

        public Tuple<bool,string> InsertNewCountry(string newContent, string modifier, string countryCode) {

            LanguagePackage lp = new LanguagePackage(countryCode);

            JObject newContentObj;
            try
            {
                CountryModel cm = new CountryModel();

                //確認匯入的資料正常
                newContentObj = JObject.Parse(newContent);

                JToken tempToken;

                newContentObj.TryGetValue("CountryName", out tempToken);
                if (tempToken == null || String.IsNullOrEmpty(tempToken.ToString()))
                    return new Tuple<bool, string>(true, lp.getContentWithNoPrefix("FailToExecute"));
                cm.CountryName = tempToken.ToString();

                newContentObj.TryGetValue("LanguagePackage", out tempToken);
                if (tempToken == null || String.IsNullOrEmpty(tempToken.ToString()))
                    return new Tuple<bool, string>(true, lp.getContentWithNoPrefix("FailToExecute"));
                cm.LanguagePackage = tempToken.ToString();

                newContentObj.TryGetValue("RedirectLanguagePackage", out tempToken);
                if (tempToken == null || String.IsNullOrEmpty(tempToken.ToString()))
                    return new Tuple<bool, string>(true, lp.getContentWithNoPrefix("FailToExecute"));
                cm.RedirectLanguagePackage = tempToken.ToString();

                //更新回檔案
                var backendInfo = new BackendInfo();
                var countryList = backendInfo.ReadCountries().OrderBy(item => item.Sequence);  //先取目前的清單
                var newCountry = new JArray();
                int index = 0;
                foreach(var country in countryList){
                    var countryObj = JObject.FromObject(country);
                    countryObj.Remove("Sequence");
                    countryObj.Add(new JProperty("Sequence", index));
                    newCountry.Add(countryObj);
                    index++;
                }
                cm.Sequence = index;
                var newCountryObj = JObject.FromObject(cm);
                newCountry.Add(newCountryObj);

                newCountry = ReArrangeLanguagePack(newCountry);

                if (backendInfo.UpdateBackendInfoFile("Country", newCountry, modifier))
                    return new Tuple<bool, string>(true, lp.getContentWithNoPrefix("ExecuteSuccessfully"));
                else
                    return new Tuple<bool, string>(true, lp.getContentWithNoPrefix("FailToExecute"));
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return new Tuple<bool, string>(true, lp.getContentWithNoPrefix("FailToExecute"));
            }
        }

        public Tuple<bool, string> RemoveCountry(string removeContent, string modifier, string countryCode)
        {
            var isSuccess = false;

            LanguagePackage lp = new LanguagePackage(countryCode);
            try
            {
                //確認匯入的資料正常
                var newContentObj = JObject.Parse(removeContent);

                JToken tempToken;

                newContentObj.TryGetValue("LanguagePackage", out tempToken);
                if (tempToken == null || String.IsNullOrEmpty(tempToken.ToString()))
                    return new Tuple<bool, string>(true, lp.getContentWithNoPrefix("FailToExecute"));

                var backendInfo = new BackendInfo();
                var countryList = backendInfo.ReadCountries().OrderBy(item => item.Sequence);  //先取目前的清單
                var newCountry = new JArray();
                int index = 0;
                foreach (var country in countryList)
                {
                    //如果是移除的item
                    if (tempToken.ToString().Trim().Equals(country.LanguagePackage))
                        continue;

                    //如果目前導向的Language Pack 是要移除的item
                    if (tempToken.ToString().Equals(country.RedirectLanguagePackage))
                        country.RedirectLanguagePackage = country.LanguagePackage;

                    var countryObj = JObject.FromObject(country);
                    countryObj.Remove("Sequence");
                    countryObj.Add(new JProperty("Sequence", index));
                    newCountry.Add(countryObj);
                    index++;
                }

                newCountry = ReArrangeLanguagePack(newCountry);

                if (backendInfo.UpdateBackendInfoFile("Country", newCountry, modifier))
                {
                    isSuccess = true;
                    //將LanguagePack.json中該語系的資料移除
                    lp.RemoveCountryCode(tempToken.ToString(), modifier);
                }

                if (isSuccess)
                    return new Tuple<bool, string>(true, lp.getContentWithNoPrefix("ExecuteSuccessfully"));
                else
                    return new Tuple<bool, string>(true, lp.getContentWithNoPrefix("FailToExecute"));
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return new Tuple<bool, string>(true, lp.getContentWithNoPrefix("FailToExecute"));
            }
        }

        public Tuple<bool, string> UpdateCountries(string updateContent, string modifier, string countryCode)
        {
            LanguagePackage lp = new LanguagePackage(countryCode);
            JArray newContentArray = new JArray();
            try
            {
                var backendInfo = new BackendInfo();
                var countryList = backendInfo.ReadCountries().OrderBy(item => item.Sequence).ToList();  //取目前的清單

                var contentArray = JArray.Parse(updateContent);
                var index = 0;
                foreach (var country in contentArray) {
                    CountryModel cm = JObject.Parse(country.ToString()).ToObject<CountryModel>(); //確定資料有對到
                    cm.Sequence = index;
                    newContentArray.Add(JObject.FromObject(cm));
                    countryList.RemoveAll(item => item.LanguagePackage.ToLower().Equals(cm.LanguagePackage.ToLower()));
                    index++;
                }

                //如果countryList還有資料
                foreach (var country in countryList) {
                    country.Sequence = index;
                    newContentArray.Add(JObject.FromObject(country));
                    index++;
                }

                newContentArray = ReArrangeLanguagePack(newContentArray);

                //新增回BackendInfo
                if (backendInfo.UpdateBackendInfoFile("Country", newContentArray, modifier))
                    return new Tuple<bool, string>(true, lp.getContentWithNoPrefix("ExecuteSuccessfully"));
                else
                    return new Tuple<bool, string>(true, lp.getContentWithNoPrefix("FailToExecute"));
            }
            catch (Exception ex) {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return new Tuple<bool, string>(true, lp.getContentWithNoPrefix("FailToExecute"));
            }
        }

        public JArray ReArrangeLanguagePack(JArray newContentArray)
        {
            JArray returnArray = new JArray();

            try {
                List<CountryModel> countreis = newContentArray.ToObject<List<CountryModel>>();

                //如果有語系指向了已不存在系統的語系，預設回英文en-us
                foreach (var country in countreis)
                {
                    var redirectCountry = countreis.FirstOrDefault(item => item.LanguagePackage.ToLower().Equals(country.RedirectLanguagePackage.ToLower()));
                    if (redirectCountry == null || redirectCountry.LanguagePackage == null)
                        country.RedirectLanguagePackage = "en-US";

                    returnArray.Add(JObject.FromObject(country));
                }
            } catch(Exception ex) {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
            }

            return returnArray;
        }

        public Tuple<bool, string> UpdateCRMConnection(AdvancedSettingViewModel asvm, string modifier)
        {
            //execute
            var lp = new LanguagePackage(asvm.CountryCode);
            var backendInfo = new BackendInfo();

            if (!backendInfo.UpdateBackendInfoFile("CRMConnectionSetting", JObject.FromObject(asvm.CRMConnectionSetting), modifier))
            {
                return new Tuple<bool, string>(true, lp.getContentWithNoPrefix("FailToExecute"));
            }
            else {
                //重新建立連線
                CRMServiceConnection crm = new CRMServiceConnection();
                crm.UpdateConnectionSetting();
                if (!CRMService.IsInService)
                    return new Tuple<bool, string>(true, lp.getContentWithNoPrefix("FailToExecute"));
                else
                    return new Tuple<bool, string>(true, lp.getContentWithNoPrefix("ExecuteSuccessfully"));
            }
        }

        public Tuple<bool, string> UpdateDBConnection(AdvancedSettingViewModel asvm, string modifier)
        {
            //execute
            var lp = new LanguagePackage(asvm.CountryCode);
            var backendInfo = new BackendInfo();

            if (!backendInfo.UpdateBackendInfoFile("DBConnectionString", JObject.FromObject(asvm.DBConnectionString), modifier))
            {
                return new Tuple<bool, string>(true, lp.getContentWithNoPrefix("FailToExecute"));
            }
            else
            {
                //重新建立連線
                DBConnetion db = new DBConnetion();
                db.InitConnection();
                if (!db.IsConnectionCreated)
                    return new Tuple<bool, string>(true, lp.getContentWithNoPrefix("FailToExecute"));
                else
                    return new Tuple<bool, string>(true, lp.getContentWithNoPrefix("ExecuteSuccessfully"));
            }
        }

        public void CorrectPassword() {
            var backendInfo = new BackendInfo();

            //取出OtherData
            var setting = backendInfo.ReadOtherSetting();
            if (setting.ContainsKey("PasswordEncryption")) {
                Thread updatePassword = new Thread(() => ChangePasswordEncryptType(setting["PasswordEncryption"]));
                updatePassword.Start();
            }
        }

        public void BatchSendingInvitation(string resetUrl, string loginUrl, string countryCode)
        {
            var isSuccess = true;
            var dbConnection = new DBConnetion().GetConnection();
            var emailEmpty = new List<string>();
            var sendError = new List<string>();
            BackendInfo bi = new BackendInfo();

            if (!dbConnection.Item1)
            {
                Elmah.ErrorLog.GetDefault(null).Log(new Error(new Exception("BatchSendingInvitationError: DB Connection Failed!")));
            }

            try
            {
                string queryString =
                  "  select accountid, emailaddress1,name "
                + "   ,(select strm.value from stringmap strm "
                + "     where strm.attributename = 'CustomerTypeCode' and strm.objecttypecode='1' "
                + "      and langid='1033' and attributevalue = acc.customertypecode) as customertypecodename "
                + "   ,(select emailaddress1 from Contact where contactId = acc.primarycontactid) as primatyContactEmail "
                + "   ,new_portalaccount"
                + "  from account acc "
                + " where statecode='0' "
                + "   and new_portalaccount is not null and (new_portalpassword is null or new_portalpassword = '')";

                SqlCommand command = new SqlCommand(queryString, dbConnection.Item2);
                ResetPasswordModel rpm = new ResetPasswordModel();

                using (SqlDataReader sdr = command.ExecuteReader())
                {
                    while (sdr.Read())
                    {
                        rpm = new ResetPasswordModel();
                        rpm.Script = resetUrl;
                        rpm.Website = loginUrl;
                        rpm.AccountId = sdr.GetValue(sdr.GetOrdinal("accountid")).ToString();
                        rpm.Email = sdr.GetValue(sdr.GetOrdinal("emailaddress1")).ToString();
                        rpm.AccountName = sdr.GetValue(sdr.GetOrdinal("name")).ToString();
                        var primaryContactEmail = sdr.GetValue(sdr.GetOrdinal("primatyContactEmail")).ToString();
                        rpm.Email = (string.IsNullOrWhiteSpace(rpm.Email)) ? primaryContactEmail : rpm.Email;
                        rpm.PortalAccount = sdr.GetValue(sdr.GetOrdinal("new_portalaccount")).ToString();

                        if (string.IsNullOrEmpty(rpm.Email) || string.IsNullOrWhiteSpace(rpm.Email)) {
                            isSuccess = false;
                            emailEmpty.Add("Account Name:" + rpm.AccountName + "\r\n");
                            isSuccess = false;
                            continue;
                        }

                        //寄出信件
                        //產生token並寄出信件
                        ResetPassword rp = new ResetPassword();
                        if (!rp.ResetUserPassword(rpm, countryCode, true))
                        {
                            //出現異常，例如回寫資料錯誤或信件寄不出去
                            sendError.Add("Account Name:"+rpm.AccountName+"\r\n");
                            isSuccess = false;
                        }
                    }
                }
            }
            catch (Exception ex) {
                Elmah.ErrorLog.GetDefault(null).Log(new Error(ex));
                isSuccess = false;
            }
            finally
            {
                if (isSuccess)
                {
                    Elmah.ErrorLog.GetDefault(null).Log(new Error(new Exception("Send invitation all successed")));
                }
                else
                {
                    if (emailEmpty.Count > 0){
                        Elmah.ErrorLog.GetDefault(null).Log(new Error()
                        {
                            Detail = string.Join(";", emailEmpty),
                            Message = "Email Empty List"
                        });
                    }
                    if (sendError.Count > 0)
                    {
                        Elmah.ErrorLog.GetDefault(null).Log(new Error()
                        {
                            Detail = string.Join(";", sendError),
                            Message = "Email Send Error List"
                        });
                    }
                }
            }
        }

        public Tuple<bool, string> UpdateOtherSetting(string Segment, string NewValue, string Modifier, string CountryCode = "en-us")
        {
            var backendInfo = new BackendInfo();
            var lp = new LanguagePackage(CountryCode);

            //取出OtherData
            var setting = backendInfo.ReadOtherSetting();

            //如果要更新的值與現在的值相同，就不更新
            if (!setting.ContainsKey(Segment))
                return new Tuple<bool, string>(false, lp.getContentWithNoPrefix("NothingHasBeenChanged"));

            var value = setting[Segment];
            if (value.ToLower().Equals(NewValue.ToLower())) {
                return new Tuple<bool, string>(false, lp.getContentWithNoPrefix("NothingHasBeenChanged"));
            }

            //如果要做其它處理
            switch (Segment) {
                case "MultipleCaseProduct":
                    break;
                case "PasswordEncryption":
                    //更新所有密碼
                    Thread updatePassword = new Thread(() => ChangePasswordEncryptType(NewValue));
                    updatePassword.Start();
                    break;
            }

            if (SetupOtherSettingNewValue(Segment, NewValue, Modifier))
            {
                if (Segment.Equals("PasswordEncryption"))
                {
                    return new Tuple<bool, string>(true, lp.getContentWithNoPrefix("UpdatePasswordEncryptionInProgress"));
                }
                else {
                    return new Tuple<bool, string>(true, lp.getContentWithNoPrefix("UpdateFileSuccessed"));
                }
            } else {
                return new Tuple<bool, string>(false, lp.getContentWithNoPrefix("UpdateFileFailed"));
            }
        }

        private bool SetupOtherSettingNewValue(string Segment, string NewValue, string Modifier)
        {
            var isSuccessed = true;

            var backendInfo = new BackendInfo();

            //取出OtherData
            var setting = backendInfo.ReadOtherSetting();

            var newSetting = new JObject();
            foreach (var item in setting)
            {
                if (item.Key.ToLower().Equals(Segment.ToLower()))
                {
                    newSetting.Add(new JProperty(Segment, NewValue));
                }
                else
                {
                    newSetting.Add(new JProperty(item.Key, item.Value));
                }
            }

            //execute
            if (!backendInfo.UpdateBackendInfoFile("OtherSetting", newSetting, Modifier))
                isSuccessed = false;

            return isSuccessed;
        }

        /// <summary>
        /// 整批更新 CRM 上的密碼為 加密 或 不加密 
        /// </summary>
        /// <param name="setting"></param>
        /// <returns></returns>
        public void ChangePasswordEncryptType(string newValue)
        {
            lock (PasswordLocker)
            {
                var batchSize = 500;
                var isSuccess = true;
                var errorList = new List<string>();

                var dbConnection = new DBConnetion().GetConnection();

                if (!dbConnection.Item1)
                {
                    Elmah.ErrorLog.GetDefault(null).Log(new Error(new Exception("ChangePasswordEncryptTypeError:DB Connection Failed!")));
                }

                CRMManipulate cmm = new CRMManipulate();
                if (!cmm.IsInService())
                {
                    Elmah.ErrorLog.GetDefault(null).Log(new Error(new Exception("ChangePasswordEncryptTypeError:CRM Connection Failed!")));
                }

                //建立 transaction
                ExecuteMultipleRequest multipleReq = new ExecuteMultipleRequest()
                {
                    Settings = new ExecuteMultipleSettings()
                    {
                        ContinueOnError = true,
                        ReturnResponses = true
                    },
                    Requests = new OrganizationRequestCollection()
                };

                try
                {
                    //撈出每個有設定Portal Password 的資料
                    string queryString =
                    "select name, new_portalaccount, new_portalpassword, accountid, new_portalpasswordencrypted, "
                    + "(select strm.value from stringmap strm "
                    + "  where strm.attributename = 'CustomerTypeCode' and strm.objecttypecode='1' "
                    + "    and langid='1033' and attributevalue = acc.customertypecode) as customertypecodename "
                    + " from account acc Where statecode='0' "
                    + "  and new_portalpassword is not null ";
                    SqlCommand command = new SqlCommand(queryString, dbConnection.Item2);
                    var index = 0;

                    using (SqlDataReader sdr = command.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            var userPassword = sdr.GetValue(sdr.GetOrdinal("new_portalpassword")).ToString();
                            var accountId = sdr.GetValue(sdr.GetOrdinal("accountid")).ToString();
                            var portalpasswordencrypted = sdr.GetValue(sdr.GetOrdinal("new_portalpasswordencrypted")).ToString();

                            var newPassword = string.Empty;
                            var newportalpasswordencrypted = true;

                            if (newValue.ToLower().Equals("true"))
                            {
                                if (portalpasswordencrypted.ToString().ToLower().Equals("true")) continue;
                                newPassword = PasswordEncryption.Encrypt(userPassword);
                                newportalpasswordencrypted = true;
                            }
                            else
                            {
                                if (portalpasswordencrypted.ToString().ToLower().Equals("false")) continue;
                                newPassword = PasswordEncryption.Decrypt(userPassword);
                                newportalpasswordencrypted = false;
                            }
                            if (newPassword.Equals(userPassword)) continue; //沒轉換成功

                            var accountEntity = cmm.GetService().Retrieve("account", new Guid(accountId), new ColumnSet("new_portalpassword", "new_portalpasswordencrypted"));
                            accountEntity["new_portalpassword"] = newPassword;
                            accountEntity["new_portalpasswordencrypted"] = newportalpasswordencrypted;
                            UpdateRequest req = new UpdateRequest();
                            req.Target = accountEntity;
                            multipleReq.Requests.Add(req);
                            index++;

                            if (index == 1)
                                Elmah.ErrorLog.GetDefault(null).Log(new Error(new Exception("Password transfomed starting")));

                            if (index % batchSize == 0)
                            {
                                ExecuteMultipleResponse responses = (ExecuteMultipleResponse)cmm.GetService().Execute(multipleReq);
                                if (responses.IsFaulted)
                                {
                                    isSuccess = false;
                                    foreach (var response in responses.Responses)
                                    {
                                        if (response.Fault != null)
                                        {
                                            errorList.Add(response.Fault.ToString());
                                        }
                                    }
                                }
                                multipleReq.Requests.Clear();
                                Elmah.ErrorLog.GetDefault(null).Log(new Error(new Exception(index + " records of password has been upated")));
                            }
                        }

                        //可能有不滿batchsize的在這邊送出
                        if (multipleReq.Requests.Count > 0)
                        {
                            ExecuteMultipleResponse responses = (ExecuteMultipleResponse)cmm.GetService().Execute(multipleReq);
                            if (responses.IsFaulted)
                            {
                                isSuccess = false;
                                foreach (var response in responses.Responses)
                                {
                                    if (response.Fault != null)
                                    {
                                        errorList.Add(response.Fault.ToString());
                                    }
                                }
                            }
                            Elmah.ErrorLog.GetDefault(null).Log(new Error(new Exception(index + " records of password has been upated")));
                        }
                    }
                }
                catch (Exception ex)
                {
                    isSuccess = false;
                    Elmah.ErrorLog.GetDefault(null).Log(new Error(ex));
                }
                finally
                {
                    if (isSuccess)
                    {
                        Elmah.ErrorLog.GetDefault(null).Log(new Error(new Exception("Password transfomed all successed")));
                    }
                    else
                    {
                        Elmah.ErrorLog.GetDefault(null).Log(new Error(new Exception("Password transfomed failed")));
                        Elmah.ErrorLog.GetDefault(null).Log(new Error()
                        {
                            Detail = errorList.ToString(),
                            Message = "Error List"
                        });
                    }
                }
            }
        }
    }
}