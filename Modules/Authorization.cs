using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.ServiceModel.Description;

namespace InstallationAPPNonUnify.Modules
{
    public enum Roles
    {
        Administrator,
        RegularUser,
        SuperUser,
        TempRegularUser
    }

    public class Authorization
    {
        public Roles Role { get; private set; }
        /// <summary>
        /// Crm Account GUID
        /// </summary>
        public string AccountId { get; private set; }

        /// <summary>
        /// Crm customer portal id
        /// </summary>
        public string UserId { get; private set; }

        /// <summary>
        /// Crm customer portal password
        /// </summary>
        private string UserPassword { get;  set; }

        /// <summary>
        /// Crm Account name
        /// </summary>
        public string UserName {get;private set;}

        /// <summary>
        /// Customer Type Code, like home or commercial
        /// </summary>
        public string CustomerTypeCode { get; private set; }

        /// <summary>
        /// System User id from SystemUser
        /// </summary>
        public string SystemUserId { get; set; }

        public string AdministratorId { get; set; }

        public string TransactionCurrencyId { get; set; }
        public bool IsValidUser { get; private set; }
        public bool IsPasswordCorrect { get; private set; }
        public bool IsAdminstrator { get; set; }

        public Authorization(string userId, string password, string role = "")
        {
            IsValidUser = false;
            IsPasswordCorrect = false;
            IsAdminstrator = false;

            switch (role.ToLower()) { 
                case "admin":
                    AdminstratorAuth(userId,password);
                    break;
                case "tempregular":
                    PortalUserAuth(userId, password, Roles.TempRegularUser);
                    break;
                default:
                    PortalUserAuth(userId, password, Roles.RegularUser);
                    break;
            }
        }

        public void PortalUserAuth(string userId, string password, Roles roles)
        {
            Role = roles;

            var dbConnection = new DBConnetion().GetConnection();
            if (!dbConnection.Item1)
            {
                PortalUserAuthViaCrmService(userId, password);  //使用 CRMServiceConnection 速度較慢
                return;
            }

            try
            {
                string queryString =
                "select top 1 name, new_portalaccount, new_portalpassword, accountid, new_portalpasswordencrypted, "
                + "(select strm.value from stringmap strm "
                + "  where strm.attributename = 'CustomerTypeCode' and strm.objecttypecode='1' "
                + "    and langid='1033' and attributevalue = acc.customertypecode) as customertypecodename "
                + " from account acc Where statecode='0' ";

                if (Role.Equals(Roles.TempRegularUser))
                {
                    queryString = queryString + " and accountid =@UserId ";
                }
                else
                {
                    queryString = queryString + " and new_portalaccount = @UserId ";
                }

                SqlCommand command = new SqlCommand(queryString, dbConnection.Item2);

                command.Parameters.AddWithValue("@UserId", userId);
                var portalpasswordencrypted = "false";

                using (SqlDataReader sdr = command.ExecuteReader())
                {
                    while (sdr.Read())
                    {
                        UserName = sdr.GetValue(sdr.GetOrdinal("name")).ToString();
                        UserId = sdr.GetValue(sdr.GetOrdinal("new_portalaccount")).ToString();
                        UserPassword = sdr.GetValue(sdr.GetOrdinal("new_portalpassword")).ToString();
                        AccountId = sdr.GetValue(sdr.GetOrdinal("accountid")).ToString();
                        CustomerTypeCode = sdr.GetValue(sdr.GetOrdinal("customertypecodename")).ToString();
                        portalpasswordencrypted = sdr.GetValue(sdr.GetOrdinal("new_portalpasswordencrypted")).ToString();
                        IsValidUser = true;
                        break;
                    }
                }

                if (portalpasswordencrypted.ToLower().Equals("true"))
                    UserPassword = PasswordEncryption.Decrypt(UserPassword);

                if (UserPassword == null || password == null || !password.Equals(UserPassword))
                {
                    IsPasswordCorrect = false;
                }
                else
                {
                    IsPasswordCorrect = true;
                }

                //transaction currency
                queryString = "select top 1 TransactionCurrencyId from TransactionCurrency";
                command = new SqlCommand(queryString, dbConnection.Item2);
                using (SqlDataReader sdr = command.ExecuteReader())
                {
                    while (sdr.Read())
                    {
                        TransactionCurrencyId = sdr.GetValue(sdr.GetOrdinal("TransactionCurrencyId")).ToString();
                        break;
                    }
                }

                //ownerId
                var domainUser = new BackendInfo().ReadOtherSetting("CRMOwner");
                if (!string.IsNullOrEmpty(domainUser))
                {
                    queryString = "select SystemUserId from SystemUser where DomainName=@DomainUser";
                    command = new SqlCommand(queryString, dbConnection.Item2);
                    command.Parameters.AddWithValue("@DomainUser", domainUser);
                    using (SqlDataReader sdr = command.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            SystemUserId = sdr.GetValue(sdr.GetOrdinal("SystemUserId")).ToString();
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("authenticate user error", ex);
            }
            finally {
                dbConnection.Item2.Dispose();
            }
        }

        public void PortalUserAuthViaCrmService(string userId, string password)
        {
            //string errorMsg = string.Empty;
            //CRMServiceConnection crmConnection = new CRMServiceConnection();
            //var ServiceContext = crmConnection.CrmServiceContext();

            //取user
            var queryPortal = from a in CRMService.ServiceContext.CreateQuery("account")
                              join b in CRMService.ServiceContext.CreateQuery("stringmap")
                                on ((string)a["customertypecode"]) equals ((string)b["attributevalue"]) into c
                              from d in c.DefaultIfEmpty()
                              where ((string)a["new_portalaccount"]).Equals(userId) && ((int)a["statecode"]) == 0
                              where ((string)d["attributename"]).Equals("CustomerTypeCode")
                                && ((int)d["objecttypecode"] == 1) && ((int)d["langid"] == 1033)
                              select new
                              {
                                  account = a,
                                  stringMap = d
                              };

            var userData = queryPortal.FirstOrDefault();
            if (userData == null)
            {
                return;
            }

            var tempValue = string.Empty;
            userData.account.TryGetAttributeValue<string>("new_portalpassword", out tempValue);
            UserPassword = tempValue == null ? string.Empty : tempValue;
            IsValidUser = true;

            if (!UserPassword.Equals(password)) return;

            tempValue = string.Empty;
            userData.account.TryGetAttributeValue<string>("new_portalaccount", out tempValue);
            UserId = tempValue == null ? string.Empty : tempValue;

            Guid tempGuid;
            userData.account.TryGetAttributeValue("accountid", out tempGuid);
            AccountId = tempGuid.Equals(Guid.Empty) ? string.Empty : tempGuid.ToString();

            tempValue = string.Empty;
            userData.account.TryGetAttributeValue<string>("name", out tempValue);
            UserName = tempValue == null ? string.Empty : tempValue;

            tempValue = string.Empty;
            userData.stringMap.TryGetAttributeValue<string>("value", out tempValue);
            CustomerTypeCode = tempValue == null ? string.Empty : tempValue;
        }

        public void AdminstratorAuth(string Id, string Password)
        {
            //以防萬一，埋一組預設的帳號密碼是不需檢驗的，但只限於緊急使用，因為帳號非CRM帳號
            if (Id.Equals("MatrixAdmin") && Password.Equals("szCAx5Xt")) {
                IsValidUser = true;
                IsPasswordCorrect = true;
                IsAdminstrator = true;
                Role = Roles.SuperUser;
                AdministratorId = new Guid().ToString();
                UserId = new Guid().ToString();
                return;
            }

            try {
                //組出新ID
                var newId = Id.Substring((Id.IndexOf("\\") + 1));
                var newDomain = Id.Substring(0, Id.IndexOf("\\"));
                var newDomainId = newId + "@" + newDomain;

                CRMServiceConnection crmConnection = new CRMServiceConnection();
                var orgService = crmConnection.WhoAmI(newDomainId, Password);
                IOrganizationService crmdefservice = (IOrganizationService)orgService;

                OrganizationRequest request = new OrganizationRequest() { RequestName = "WhoAmI" };
                OrganizationResponse res = new OrganizationResponse() { ResponseName = "WhoAmI" };

                res = crmdefservice.Execute(request);
            } catch
            {
                return;
            }
            IsValidUser = true;
            IsPasswordCorrect = true;

            //取GUID
            var userGuid = string.Empty;
            var dbConnection = new DBConnetion().GetConnection();
            if (!dbConnection.Item1)
            {
                throw new Exception("DB Connection Failed!");
            }

            string queryString =
                " SELECT systemuserid, domainname FROM systemuserbase where domainname = @UserId " +
                "    And isdisabled = 0";

            SqlCommand command = new SqlCommand(queryString, dbConnection.Item2);
            command.Parameters.AddWithValue("@UserId", Id);

            using (SqlDataReader sdr = command.ExecuteReader())
            {
                while (sdr.Read())
                {
                    userGuid = sdr.GetValue(sdr.GetOrdinal("systemuserid")).ToString();
                    break;
                }
            }

            //取administrator 權限
            var isAdministrator = false;

            if (!string.IsNullOrEmpty(userGuid) && !string.IsNullOrWhiteSpace(userGuid)) {
                //尋找這個ID有沒有system administrator 的權限
                queryString =
                    " SELECT systemuser.DomainName "
                    + "   FROM systemuserroles, role,systemuser "
                    + "  where lower(role.name) = 'system administrator' "
                    + "    and role.roleid = systemuserroles.roleid "
                    + "    and systemuser.systemuserid = systemuserroles.systemuserid "
                    + "    and systemuser.systemuserid = @userGuid ";

                command = new SqlCommand(queryString, dbConnection.Item2);
                command.Parameters.AddWithValue("@userGuid", userGuid);

                using (SqlDataReader sdr = command.ExecuteReader())
                {
                    while (sdr.Read())
                    {
                        isAdministrator = true;
                        break;
                    }
                }
            }

            //去除網域後的id
            var realId = Id.IndexOf("\\") < 0 ? Id : Id.Substring(Id.LastIndexOf("\\") + 1);

            if (isAdministrator)
            {
                Role = Roles.SuperUser;
                IsAdminstrator = true;
                AdministratorId = Id;
                UserId = Id;
            }
            else {
                //GUID 存不存在白名單
                if (new WhiteListInfo().IsUserInList(userGuid))
                {
                    IsAdminstrator = true;
                    Role = Roles.Administrator;
                    UserId = Id;
                    AdministratorId = Id;
                }
            }
        }
    }
}