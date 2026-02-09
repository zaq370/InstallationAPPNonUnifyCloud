using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;

namespace InstallationAPPNonUnify.Modules
{
    public class FormsAuthenticationTicketReader
    {
        public bool isValidTicket { get; private set; }
        public bool isAdministrator { get; private set; }
        public bool isSuperUser { get; private set; }
        public string Role { get; private set; }
        public string CrmAccountId { get; private set; }
        public string CrmCustomerTypeCode { get; private set; }
        public string CrmUserName { get; private set; }
        public string CRMUserId { get; private set; }
        public string SystemUserId { get; private set; }
        public string TransactionCurrencyId { get; private set; }
        public string AdminstratorId { get; private set; }

        public FormsAuthenticationTicketReader(HttpCookie authCookie, string type = "regular")
        {
            if (authCookie == null || String.IsNullOrEmpty(authCookie.Value))
            {
                isValidTicket = false;  //不正確的資料
            }
            else {
                init(authCookie, type);
            }
                
        }

        private void init(HttpCookie AuthCookie, string type)
        {
            isValidTicket = true;

            FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(AuthCookie.Value);
            CRMUserId = ticket.Name;
            var _userData = ticket.UserData;
            try
            {
                JObject userData = JObject.Parse(_userData);
                JToken tempToken;

                userData.TryGetValue("Role", out tempToken);
                if (tempToken != null) Role = tempToken.ToString();
                if (Role.Equals(Enum.GetName(typeof(InstallationAPPNonUnify.Modules.Roles), Roles.Administrator))) 
                    isAdministrator = true;

                if (Role.Equals(Enum.GetName(typeof(InstallationAPPNonUnify.Modules.Roles), Roles.SuperUser))) {
                    isAdministrator = true;
                    isSuperUser = true;
                }

                if (isAdministrator || isSuperUser)
                {
                    userData.TryGetValue("AdministratorId", out tempToken);
                    if (tempToken != null) AdminstratorId = tempToken.ToString();
                }

                userData.TryGetValue("CrmAccountId", out tempToken);
                if (tempToken != null) CrmAccountId = tempToken.ToString();

                userData.TryGetValue("CrmCustomerTypeCode", out tempToken);
                if (tempToken != null) CrmCustomerTypeCode = tempToken.ToString();

                userData.TryGetValue("CrmUserName", out tempToken);
                if (tempToken != null) CrmUserName = tempToken.ToString();

                userData.TryGetValue("SystemUserId", out tempToken);
                if (tempToken != null) SystemUserId = tempToken.ToString();

                userData.TryGetValue("TransactionCurrencyId", out tempToken);
                if (tempToken != null) TransactionCurrencyId = tempToken.ToString();
            }
            catch {
                isValidTicket = false;
            }
            
            if (type.ToLower().Equals("regular"))
            {
                //如果沒有Regular User 身份，也是無效的憑証
                if (string.IsNullOrEmpty(CrmAccountId))
                    isValidTicket = false;
            }

            //如果身份是 TempRegular，也是無效的憑証 
            if (Role == null || (Role.Equals(Enum.GetName(typeof(InstallationAPPNonUnify.Modules.Roles), Roles.TempRegularUser))))
            {
                isValidTicket = false;
            }
        }
    }

    public class NewFormAuthentication{

        public FormsAuthenticationTicket Ticket { get; set; }
        public string EncryptedTicket { get; set; }
        public bool IsTicketReady { get; set; }

        public NewFormAuthentication(Authorization auth, string role = "regular", string administratorId = "")
        {
            try
            {
                //其它資訊
                JObject OtherData = new JObject();
                string UserId = string.Empty;

                switch (role.ToLower())
                {
                    case "regular":
                    case "tempregular":
                        OtherData.Add(new JProperty("Role", Enum.GetName(typeof(InstallationAPPNonUnify.Modules.Roles), auth.Role)));  //角色
                        OtherData.Add(new JProperty("CrmAccountId", auth.AccountId));
                        OtherData.Add(new JProperty("CrmCustomerTypeCode", auth.CustomerTypeCode));
                        OtherData.Add(new JProperty("CrmUserName", auth.UserName));
                        OtherData.Add(new JProperty("SystemUserId", auth.SystemUserId));
                        OtherData.Add(new JProperty("TransactionCurrencyId", auth.TransactionCurrencyId));
                        UserId = auth.UserId;
                        break;
                    case "administrator":
                    case "superuser":
                        //模擬身份
                        if (!string.IsNullOrWhiteSpace(administratorId))
                        {
                            OtherData.Add(new JProperty("Role", role));  //角色
                            OtherData.Add(new JProperty("AdministratorId", administratorId));   //管理者帳號
                            OtherData.Add(new JProperty("CrmAccountId", auth.AccountId));
                            OtherData.Add(new JProperty("CrmCustomerTypeCode", auth.CustomerTypeCode));
                            OtherData.Add(new JProperty("CrmUserName", auth.UserName));
                            OtherData.Add(new JProperty("SystemUserId", auth.SystemUserId));
                            OtherData.Add(new JProperty("TransactionCurrencyId", auth.TransactionCurrencyId));
                            UserId = auth.UserId;
                        }
                        else {
                            OtherData.Add(new JProperty("Role", Enum.GetName(typeof(InstallationAPPNonUnify.Modules.Roles), auth.Role)));  //角色
                            OtherData.Add(new JProperty("AdministratorId", auth.AdministratorId));  //管理者帳號
                            UserId = auth.AdministratorId;
                        }
                        break;
                }
                
                //add FormsAuthenticationTicket
                Ticket = new FormsAuthenticationTicket(
                    version: 1,
                    name: UserId,
                    issueDate: DateTime.Now,
                    expiration: DateTime.Now.AddMinutes(30),
                    isPersistent: true,
                    userData: OtherData.ToString(),
                    cookiePath: FormsAuthentication.FormsCookiePath
                    );
                EncryptedTicket = FormsAuthentication.Encrypt(Ticket);
                IsTicketReady = true;
            } catch (Exception ex) {
                throw new Exception("Generate FormsAuthenticationTicket Error", ex);
            }
        }
    }
}