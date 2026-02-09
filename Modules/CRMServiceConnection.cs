using Elmah;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Tooling.Connector;
using Newtonsoft.Json.Linq;
using InstallationAPPNonUnify.Areas.CMS.Models;
using System;
using System.Net;
using System.ServiceModel.Description;
using System.Text;

namespace InstallationAPPNonUnify.Modules
{
    public static class CRMService
    {
        public static OrganizationServiceProxy Service { get; set; }
        public static OrganizationServiceContext ServiceContext { get; set; }
        public static bool IsInService { get; set; }
    }

    public class CRMManipulate {

        public CRMManipulate() {
            if (!CRMService.IsInService)
            {
                new CRMServiceConnection().UpdateConnectionSetting();
            }
        }

        public bool IsInService() {
            return CRMService.IsInService;
        }

        public OrganizationServiceProxy GetService() {
            if (!CRMService.IsInService)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(new Exception("CRM Connection Failed"));
            }
            return CRMService.Service;
        }

        public Guid Create(Entity newEntity)
        {
            var newGuid = Guid.Empty;
            if (!CRMService.IsInService)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(new Exception("CRM Connection Failed"));
            }
            else {
                try
                {
                    newGuid = CRMService.Service.Create(newEntity);
                }
                catch {
                    try
                    {
                        //先假設是 Connection failure，再重連跟重新新增
                        new CRMServiceConnection().UpdateConnectionSetting();
                        if (CRMService.IsInService) {
                            newGuid = CRMService.Service.Create(newEntity);
                        }
                        else 
                        {
                            Elmah.ErrorSignal.FromCurrentContext().Raise(new Exception("CRM Connection Failed"));
                        }
                    }
                    catch(Exception ex) {
                        Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                    }
                }
            }
            return newGuid;
        }
    }

    public class CRMServiceConnection
    {
        private string CRMServiceUser { get; set; }
        private string CRMServicePassword { get; set; }
        private string CRMDomain { get; set; }
        private string CRMOrganizationName { get; set; }
        private string CRMServiceUrl { get; set; }
        private string CRMUrl { get; set; }
        private string CRMDecryptUrl { get; set; }
        private BackendInfo BackendInfo { get; set; }

        public CRMServiceConnection() {

            BackendInfo = new BackendInfo();
            string _tempStr = string.Empty;

            //company info
            var crmConnectionSetting = BackendInfo.ReadCRMConnectionSetting();
            crmConnectionSetting.TryGetValue("CRMOrganizationName", out _tempStr);
            CRMOrganizationName = _tempStr;
            crmConnectionSetting.TryGetValue("CRMServiceUser", out _tempStr);
            CRMServiceUser = _tempStr;
            crmConnectionSetting.TryGetValue("CRMServicePassword", out _tempStr);
            CRMServicePassword = _tempStr;
            crmConnectionSetting.TryGetValue("CRMDomain", out _tempStr);
            CRMDomain = _tempStr;
            crmConnectionSetting.TryGetValue("CRMUrl", out _tempStr);
            CRMUrl = _tempStr;
            crmConnectionSetting.TryGetValue("CRMServiceUrl", out _tempStr);
            CRMServiceUrl = _tempStr;
            crmConnectionSetting.TryGetValue("CRMDecryptUrl", out _tempStr);
            CRMDecryptUrl = _tempStr;
        }

        public void UpdateConnectionSetting()
        {
            //更新Service
            InitService();
            UpdateCrmServiceClient();
        }

        private void InitService() {
            if (CRMService.IsInService) {
                CRMService.Service.Dispose();
                CRMService.ServiceContext.Dispose();
            }

            CRMService.IsInService = false;
            CRMService.Service = null;
            CRMService.ServiceContext = null;
        }

        private void UpdateCrmServiceClientByConfigFactory()
        {
            bool isSuccessed = true;
            OrganizationServiceProxy orgService = null;

            try
            {
                // Set up the CRM Service.
                string user = CRMServiceUser;
                string pass = CRMServicePassword;
                string domain = CRMDomain;
                Uri organizationUriIFD = new Uri(CRMServiceUrl);
                ClientCredentials credentials = new ClientCredentials();
                credentials.UserName.UserName = domain + @"\" + user;
                credentials.UserName.Password = pass;
                IServiceConfiguration<IOrganizationService> config = ServiceConfigurationFactory.CreateConfiguration<IOrganizationService>(organizationUriIFD);
                orgService = new OrganizationServiceProxy(config, credentials);
                //下行是代表要不會做檢查,一般只用在late binding，有加入就不做檢查...也就是防呆
                orgService.ServiceConfiguration.CurrentServiceEndpoint.Behaviors.Add(new ProxyTypesBehavior());
                //IOrganizationService _service = (IOrganizationService)orgService;
            }
            catch (Exception ex) {
                isSuccessed = false;
                Elmah.ErrorSignal.FromCurrentContext().Raise(CRMServiceConnectionEx(ex, "CrmServiceClient"));
            }

            if (isSuccessed)
            {
                CRMService.IsInService = true;
                CRMService.Service = orgService;
                UpdateCrmServiceContext();
            }
        }

        private void UpdateCrmServiceClient()
        {
            bool isSuccessed = true;
            OrganizationServiceProxy orgService = null;

            try
            {
                // Set up the CRM Service.
                if (BackendInfo.isTestMode)
                {
                    string user = CRMServiceUser;
                    string pass = CRMServicePassword;
                    string domain = CRMDomain;
                    string ConnectStr = "AuthType=IFD; Url=" + CRMUrl + "; Domain=" + domain + "; Username=" + user + "@" + domain + "; Password=" + pass;
                    CrmServiceClient crmconn = new CrmServiceClient(ConnectStr);
                    if (crmconn.IsReady)
                    {
                        orgService = crmconn.OrganizationServiceProxy;
                    }
                    else
                    {
                        isSuccessed = false;
                        throw CRMServiceConnectionEx(new Exception(), "CrmServiceClient");
                    }
                }
                else {
                    WebClient client = new WebClient();
                    client.Encoding = Encoding.UTF8;
                    client.Headers.Add(HttpRequestHeader.ContentType, "text/plain");

                    // aesDecryptBase64為解密程式
                    string connstr = client.DownloadString(CRMDecryptUrl);
                    //string connstr = "W6JWY0QAsYqNyomUMkgKIjmxVbgFojPxypxtLnOfjkwELjwWoyaNlXz5G6Ub/IMSINAU7tMijmvYTqk04Z7BtT6c65KEwQDKPyKsr7ArABTk3K8ldp/h9qkNh2ji/BKRNugRL8l2ZjKjhDylS1E9+jlkRpE8Luf3YHexWASfAzva3OTECLoJlAcFvTb3xPNe";
                    CrmServiceClient crmconn = new CrmServiceClient(Security.aesDecryptBase64(connstr));
                    if (crmconn.IsReady)
                    {
                        orgService = crmconn.OrganizationServiceProxy;
                    }
                    else
                    {
                        isSuccessed = false;
                        throw CRMServiceConnectionEx(new Exception(), "CrmServiceClient");
                    }
                }
            }
            catch (Exception ex)
            {
                isSuccessed = false;
                Elmah.ErrorLog.GetDefault(null).Log(new Error(ex));
            }

            if (isSuccessed) {
                CRMService.IsInService = true;
                CRMService.Service = orgService;
                UpdateCrmServiceContext();
            }
        }

        private void  UpdateCrmServiceContext()
        {
            var service = CRMService.Service;
            var crmservice = (IOrganizationService)service;
            CRMService.ServiceContext = new OrganizationServiceContext(crmservice);
        }

        private Exception CRMServiceConnectionEx(Exception innerEx, string identify = "none")
        {
            
            JObject extraData = new JObject();
            extraData.Add(new JProperty("CRMOrganizationName", CRMOrganizationName));
            extraData.Add(new JProperty("CRMServiceUser", CRMServiceUser));
            extraData.Add(new JProperty("CRMServicePassword", CRMServicePassword));
            extraData.Add(new JProperty("CRMDomain", CRMDomain));
            extraData.Add(new JProperty("CRMUrl", CRMUrl));
            extraData.Add(new JProperty("CRMServiceUrl", CRMServiceUrl));
            extraData.Add(new JProperty("CRMDecryptUrl", CRMDecryptUrl));
            Exception CrmEx = new Exception(identify+":"+extraData.ToString(), innerEx);

            return CrmEx;
        }

        private Exception CRMServiceConnectionExForTest(Exception innerEx, CRMConnectionSettingModel csm, string identify = "none")
        {

            JObject extraData = new JObject();
            extraData.Add(new JProperty("CRMOrganizationName", csm.CRMOrganizationName));
            extraData.Add(new JProperty("CRMServiceUser", csm.CRMServiceUser));
            extraData.Add(new JProperty("CRMServicePassword", csm.CRMServicePassword));
            extraData.Add(new JProperty("CRMDomain", csm.CRMDomain));
            extraData.Add(new JProperty("CRMUrl", csm.CRMUrl));
            extraData.Add(new JProperty("CRMServiceUrl", csm.CRMServiceUrl));
            extraData.Add(new JProperty("CRMDecryptUrl", csm.CRMDecryptUrl));
            Exception CrmEx = new Exception(identify + ":" + extraData.ToString(), innerEx);

            return CrmEx;
        }

        public bool TestCrmServiceClient(CRMConnectionSettingModel csm)
        {
            bool isSuccessed = true;
            OrganizationServiceProxy orgService = null;

            BackendInfo bi = new BackendInfo();

            try
            {
                // Set up the CRM Service.
                if (bi.isTestMode)
                {
                    string user = csm.CRMServiceUser;
                    string pass = csm.CRMServicePassword;
                    string domain = csm.CRMDomain;
                    string crmUrl = csm.CRMUrl;
                    string ConnectStr = "AuthType=IFD; Url=" + crmUrl + "; Domain=" + domain + "; Username=" + user + "@" + domain + "; Password=" + pass;
                    CrmServiceClient crmconn = new CrmServiceClient(ConnectStr);
                    if (crmconn.IsReady)
                    {
                        orgService = crmconn.OrganizationServiceProxy;
                    }
                    else
                    {
                        isSuccessed = false;
                        throw CRMServiceConnectionExForTest(new Exception(), csm, "(Ignorable)CrmServiceClientTest");
                    }
                }
                else {
                    WebClient client = new WebClient();
                    client.Encoding = Encoding.UTF8;
                    client.Headers.Add(HttpRequestHeader.ContentType, "text/plain");
                    string connstr = client.DownloadString(CRMDecryptUrl);
                    //string connstr = "W6JWY0QAsYqNyomUMkgKIjmxVbgFojPxypxtLnOfjkwELjwWoyaNlXz5G6Ub/IMSINAU7tMijmvYTqk04Z7BtT6c65KEwQDKPyKsr7ArABTk3K8ldp/h9qkNh2ji/BKRNugRL8l2ZjKjhDylS1E9+jlkRpE8Luf3YHexWASfAzva3OTECLoJlAcFvTb3xPNe";
                    // aesDecryptBase64為解密程式
                    CrmServiceClient crmconn = new CrmServiceClient(Security.aesDecryptBase64(connstr));
                    if (crmconn.IsReady)
                    {
                        orgService = crmconn.OrganizationServiceProxy;
                    }
                    else
                    {
                        isSuccessed = false;
                        throw CRMServiceConnectionExForTest(new Exception(), csm, "(Ignorable)CrmServiceClientTestAPIVersion");
                    }
                }
            }
            catch (Exception ex)
            {
                isSuccessed = false;
                Elmah.ErrorLog.GetDefault(null).Log(new Error(ex));
            }

            return isSuccessed;
        }

        public OrganizationServiceProxy WhoAmI(string _id, string _password)
        {
            bool isSuccessed = true;
            OrganizationServiceProxy orgService = null;

            try
            {
                // Set up the CRM Service.
                if (BackendInfo.isTestMode)
                {
                    string user = CRMServiceUser;
                    string pass = CRMServicePassword;
                    string domain = CRMDomain;
                    string ConnectStr = "AuthType=IFD; Url=" + CRMUrl + "; Domain=" + domain + "; Username=" + _id + "; Password=" + _password;
                    CrmServiceClient crmconn = new CrmServiceClient(ConnectStr);
                    if (crmconn.IsReady)
                    {
                        orgService = crmconn.OrganizationServiceProxy;
                    }
                    else
                    {
                        isSuccessed = false;
                        throw CRMServiceConnectionEx(new Exception(), "CrmServiceClient");
                    }
                }
                else
                {
                    WebClient client = new WebClient();
                    client.Encoding = Encoding.UTF8;
                    client.Headers.Add(HttpRequestHeader.ContentType, "text/plain");
                    string connstr = client.DownloadString(CRMDecryptUrl);
                    //string connstr = "W6JWY0QAsYqNyomUMkgKIjmxVbgFojPxypxtLnOfjkwELjwWoyaNlXz5G6Ub/IMSINAU7tMijmvYTqk04Z7BtT6c65KEwQDKPyKsr7ArABTk3K8ldp/h9qkNh2ji/BKRNugRL8l2ZjKjhDylS1E9+jlkRpE8Luf3YHexWASfAzva3OTECLoJlAcFvTb3xPNe";

                    // aesDecryptBase64為解密程式
                    var connectioString = Security.aesDecryptBase64(connstr);

                    //取代掉id跟password
                    //先取出Id
                    var tempIndex = connectioString.IndexOf("=", connectioString.ToLower().IndexOf("username"))+1;
                    var tempEndIndex = connectioString.IndexOf(";", connectioString.ToLower().IndexOf("username"));
                    var originalId = connectioString.Substring(tempIndex, tempEndIndex - tempIndex);
                    //replace
                    connectioString = connectioString.Replace(originalId, _id);

                    //取出密碼
                    tempIndex = connectioString.IndexOf("=", connectioString.ToLower().IndexOf("password"))+1;
                    tempEndIndex = connectioString.IndexOf(";", connectioString.ToLower().IndexOf("password"));
                    var originalPassword = connectioString.Substring(tempIndex, tempEndIndex - tempIndex);
                    //replace
                    connectioString = connectioString.Replace(originalPassword, _password);

                    CrmServiceClient crmconn = new CrmServiceClient(connectioString);
                    if (crmconn.IsReady)
                    {
                        orgService = crmconn.OrganizationServiceProxy;
                    }
                    else
                    {
                        isSuccessed = false;
                        throw CRMServiceConnectionEx(new Exception(), "CrmServiceClient");
                    }
                }
            }
            catch (Exception ex)
            {
                isSuccessed = false;
                Elmah.ErrorLog.GetDefault(null).Log(new Error(ex));
            }
            return orgService;
        }
    }
}
