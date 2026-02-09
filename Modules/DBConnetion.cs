using InstallationAPPNonUnify.Areas.CMS.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace InstallationAPPNonUnify.Modules
{
    public class DBConnetion
    {
        private string connectionString;
        public SqlConnection Connection { get; set; }
        public bool IsConnectionCreated { get; private set; }

        public DBConnetion(bool initial = true)
        {
            if (initial)
            {
                InitConnection();
            }
            else {
                IsConnectionCreated = false;
            }
        }

        public void InitConnection() {

            IsConnectionCreated = false;

            CreateConnectionString();
            Connection = new SqlConnection(connectionString);

            if (Connection.State == System.Data.ConnectionState.Closed)
                Connection.Open();

            if (Connection.State != System.Data.ConnectionState.Open)
                IsConnectionCreated = false;

            IsConnectionCreated = true;
        }

        private void CreateConnectionString()
        {
            BackendInfo bi = new BackendInfo();

            //dbConnetion info
            var crmConnectionSetting = bi.ReadDBConnectionString();
            string Server = string.Empty;
            crmConnectionSetting.TryGetValue("Server", out Server);

            string InitialCatalog = string.Empty;
            crmConnectionSetting.TryGetValue("InitialCatalog", out InitialCatalog);

            string PersistSecurityInfo = string.Empty;
            crmConnectionSetting.TryGetValue("PersistSecurityInfo", out PersistSecurityInfo);

            string UserId = string.Empty;
            crmConnectionSetting.TryGetValue("UserId", out UserId);

            string Password = string.Empty;
            crmConnectionSetting.TryGetValue("Password", out Password);

            string TimeOut = string.Empty;
            crmConnectionSetting.TryGetValue("TimeOut", out TimeOut);

            if (string.IsNullOrEmpty(Server) || string.IsNullOrEmpty(InitialCatalog) || string.IsNullOrEmpty(UserId)
                || string.IsNullOrEmpty(Password)) {
                    return;
            }

            bool _persistSecurityInfo;
            _persistSecurityInfo = bool.TryParse(PersistSecurityInfo, out _persistSecurityInfo) ? _persistSecurityInfo : true;

            int _timeOut;
            _timeOut = int.TryParse(TimeOut, out _timeOut) ? _timeOut : 3000;

            connectionString = string.Format("Server={0};Initial Catalog={1};Persist Security Info={2};User ID={3};Password={4};Connect Timeout={5}", 
                Server, InitialCatalog,_persistSecurityInfo.ToString(),UserId,Password,_timeOut.ToString());

        }

        public Tuple<bool, SqlConnection> GetConnection()
        {
            bool isConnected = false;

            if (Connection.State == System.Data.ConnectionState.Closed)
                Connection.Open();

            if (Connection.State != System.Data.ConnectionState.Open)
                isConnected = true;

            if (!isConnected) InitConnection();

            //如果還是失敗，就回傳異常
            if (!IsConnectionCreated) {
                Elmah.ErrorSignal.FromCurrentContext().Raise(new Exception("DB Connection Failed"));
                return new Tuple<bool, SqlConnection>(false, new SqlConnection());
            }

            return new Tuple<bool, SqlConnection>(true, Connection);
        }

        public bool TestConnection(DBConnectionStringModel dsm) {
            bool isSuccessed = true;

            //db connection string

            bool _persistSecurityInfo;
            _persistSecurityInfo = bool.TryParse(dsm.PersistSecurityInfo, out _persistSecurityInfo) ? _persistSecurityInfo : true;

            int _timeOut;
            _timeOut = int.TryParse(dsm.TimeOut, out _timeOut) ? _timeOut : 3000;

            string localConnectionString = string.Format("Server={0};Initial Catalog={1};Persist Security Info={2};User ID={3};Password={4};Connect Timeout={5}",
                dsm.Server, dsm.InitialCatalog, _persistSecurityInfo.ToString(), dsm.UserId, dsm.Password, _timeOut.ToString());

            var connection = new SqlConnection(localConnectionString);

            try
            {
                if (connection.State == System.Data.ConnectionState.Closed)
                    connection.Open();

                if (connection.State != System.Data.ConnectionState.Open)
                    isSuccessed = false;
            }
            catch (Exception ex)
            {
                isSuccessed = false;
            }
            finally {
                connection.Dispose();   //測試完關閉
            }

            return isSuccessed;
        }
    }
}