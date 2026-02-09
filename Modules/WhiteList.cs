using Newtonsoft.Json.Linq;
using InstallationAPPNonUnify.Areas.CMS.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;

namespace InstallationAPPNonUnify.Modules
{
    public static class WhiteList
    {
        public static JArray ApprovedList { get; set; }
        public static bool IsListReady { get; set; }
    }

    public class WhiteListInfo {

        private static string infoDir = "~/App_Data/";
        private static string cacheDir = infoDir + "cache/";
        private static string packageFileName = "WhiteList.json";

        private readonly object fileLock = new object();

        public WhiteListInfo() {
            if (!WhiteList.IsListReady) UpdateList();
        }

        public void UpdateList() {

            string packagePath = System.Web.Hosting.HostingEnvironment.MapPath(infoDir + packageFileName);

            WhiteList.IsListReady = false;

            using (var fs = new FileStream(packagePath, FileMode.OpenOrCreate))
            {
                using (var sr = new StreamReader(fs))
                {
                    var fileContent = new JArray();
                    try {
                        var tempObj = JObject.Parse(sr.ReadToEnd().ToString());
                        //取content
                        //var content = tempObj.Value<JObject>("content");
                        //fileContent = JArray.Parse(sr.ReadToEnd().ToString());
                        fileContent = tempObj.Value<JArray>("List");
                    } catch{

                    }
                    WhiteList.ApprovedList = fileContent;
                    WhiteList.IsListReady = true;
                }
            }
        }

        public bool IsUserInList(string id) {
            return WhiteList.ApprovedList.Any(item => item.ToString().ToLower().Equals(id.ToLower()));
        }

        public string ReadUserId(string userGuid) {

            var userId = string.Empty;

            var dbConnection = new DBConnetion().GetConnection();
            if (!dbConnection.Item1)
            {
                throw new Exception("DB Connection Failed!");
            }

            string queryString =
                " SELECT systemuserid, domainname FROM systemuserbase where systemuserid = @UserGuid " + 
                "    And isdisabled = 0";

            SqlCommand command = new SqlCommand(queryString, dbConnection.Item2);
            command.Parameters.AddWithValue("@UserGuid", userGuid);

            using (SqlDataReader sdr = command.ExecuteReader())
            {
                while (sdr.Read())
                {
                    userId = sdr.GetValue(sdr.GetOrdinal("domainname")).ToString();
                    break;
                }
            }
            return userId;
        }

        public List<WhilteListModel> ReadWholeList()
        {
            var wholeList = new List<WhilteListModel>();
            var dbConnection = new DBConnetion().GetConnection();
            if (!dbConnection.Item1)
            {
                throw new Exception("DB Connection Failed!");
            }

            string queryString =
                " SELECT systemuserid, domainname FROM systemuserbase where isdisabled = 0";

            SqlCommand command = new SqlCommand(queryString, dbConnection.Item2);

            using (SqlDataReader sdr = command.ExecuteReader())
            {
                while (sdr.Read())
                {
                    var userId = sdr.GetValue(sdr.GetOrdinal("domainname")).ToString().ToUpper();
                    var userGuid = sdr.GetValue(sdr.GetOrdinal("systemuserid")).ToString();
                    wholeList.Add(new WhilteListModel(userGuid, userId));
                }
            }

            return wholeList;
        }

        /// <summary>
        /// 更新 BackendInof.json 檔案中的內容
        /// </summary>
        public bool UpdateWhiteListFile(JArray newContent, string modifier)
        {
            bool isUpdateSuccessfully = false;

            //修改
            lock (fileLock)
            {
                var newFileContent = new JObject();

                //這次修改的內容
                newFileContent.Add(new JProperty("List", newContent));

                //加上修改者
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
                UpdateList();
            }
            return isUpdateSuccessfully;
        }
    }
}