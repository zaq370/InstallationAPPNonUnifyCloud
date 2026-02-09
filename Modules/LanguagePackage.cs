using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;

namespace InstallationAPPNonUnify.Modules
{
    public static class LPFile{
        //public static JObject Resource { get; set; }
        //public static JObject Message { get; set; }
        public static Dictionary<string, JObject> Resources { get; set; }
        public static Dictionary<string, JObject> Messages { get; set; }
        public static bool IsResourceReady { get; set; }
    }

    public class LanguagePackage
    {
        public string CountryCode { get; private set; }
        public string Version { get; private set; }

        private static string PackageDir = "~/App_Data/";
        private static string CacheDir = PackageDir + "cache/";
        private static string PackageFileName = "LanguagePackage.json";
        private static string PackagePath = System.Web.HttpContext.Current.Server.MapPath(PackageDir + PackageFileName);
        private const string DefaultCountryCode = "en-us";

        private readonly object fileLock = new object();

        public LanguagePackage(string countryCode = DefaultCountryCode)
        {
            CountryCodeMapping(countryCode);
            
            if (!LPFile.IsResourceReady || LPFile.Resources == null || LPFile.Resources.Count == 0 
                || LPFile.Messages == null || LPFile.Messages.Count == 0)
            {
                LoadLanguagePackage();
            }
        }

        /// <summary>
        /// 從檔案取出多語系的資料(未處理)
        /// </summary>
        /// <returns></returns>
        public JObject LoadLanguageResources()
        {
            var resources = new JObject();

            using (var fs = new FileStream(PackagePath, FileMode.Open, FileAccess.Read))
            {
                using (var sr = new StreamReader(fs))
                {
                    var tempObj = JObject.Parse(sr.ReadToEnd().ToString());
                    resources = tempObj.Value<JObject>("Content");
                }
            }
            return resources;
        }

        /// <summary>
        /// 將多語系的資料轉成 LPFile 中的資料，供外顯多語系使用
        /// </summary>
        public void LoadLanguagePackage()
        {
            LPFile.Resources = new Dictionary<string,JObject>();
            LPFile.Messages = new Dictionary<string, JObject>();
            LPFile.IsResourceReady = false;

            var resources = LoadLanguageResources();    //取得所有語言資料
            //var countries = new BackendInfo().ReadCountries(); //取目前子公司有使用的語系

            //foreach (var country in countries)
            //{
                var countryCode = "en-us";

                //if (LPFile.Resources.ContainsKey(countryCode)) continue; //如果已經取過該語系的資料，就不再取

                var content = new JObject();
                var message = new JObject();

                foreach (var property in resources.Properties())
                {
                    var packageObj = JObject.Parse(property.Value.ToString()).Value<JObject>("Package");
                    JToken tempJToken;

                    packageObj.TryGetValue(countryCode, out tempJToken);

                    //如果取的東西是空值，取英文的資料
                    if (tempJToken == null || string.IsNullOrEmpty(tempJToken.ToString()))
                        packageObj.TryGetValue(DefaultCountryCode, out tempJToken);
                    content.Add(new JProperty(property.Name, (tempJToken == null ? "" : tempJToken.ToString())));

                    //如果是message
                    var type = JObject.Parse(property.Value.ToString()).Value<string>("Type");
                    if (type.ToLower().Equals("message"))
                        message.Add(new JProperty(property.Name, (tempJToken == null ? "" : tempJToken.ToString())));
                }
                LPFile.Resources.Add(countryCode.ToLower(), content);
                LPFile.Messages.Add(countryCode.ToLower(),message);
            //}

            //只取訊息的資料
            //resources.Properties().Where(item =>
            //{
            //    var typeObj = JObject.Parse(item.Value.ToString()).Value<string>("Type");
            //    return (typeObj.ToLower() != "message");
            //}).ToList().ForEach(attr => attr.Remove());

            LPFile.IsResourceReady = true;
        }

        private void CountryCodeMapping(string countryCode)
        {
            CountryCode = new BackendInfo().GetRedirectCountryCode(countryCode);
            if (string.IsNullOrEmpty(CountryCode) || string.IsNullOrWhiteSpace(CountryCode))
                CountryCode = DefaultCountryCode;
        }

        /// <summary>
        /// 只取得類型Type 為 Message的資料
        /// </summary>
        public JObject LoadMessagePackage()
        {
            if (LPFile.Messages.ContainsKey(CountryCode))
                return LPFile.Messages[CountryCode];
            else
                return LPFile.Messages[DefaultCountryCode];
        }

        public JObject LoadAllLanguagePackage()
        {
            if (LPFile.Resources.ContainsKey(CountryCode))
                return LPFile.Resources[CountryCode];
            else
                return LPFile.Resources[DefaultCountryCode];
        }

        protected string CreateTempLanguagePackage()
        {
            var tempFileName = PackageFileName.Substring(0, PackageFileName.IndexOf(".json") - 1) + "-" +
                DateTime.Now.ToString("yyyyMMddHHmmss-") + Guid.NewGuid() + ".json";
            var tempFilePath = System.Web.HttpContext.Current.Server.MapPath(CacheDir + tempFileName);

            //確認檔案是否存在
            if (File.Exists(PackagePath)) {
                File.Copy(PackagePath, tempFilePath);
                if (File.Exists(PackagePath)) return tempFilePath;
                throw new Exception("create language package error");
            }
            return string.Empty;
        }
        /// <summary>
        /// 取得 ViewModel object 中，有加入 [Display(Name="FieldName")] 註解的屬性，並用指定的Name取得對應多語言的欄位名稱
        /// </summary>
        /// <param name="properties">使用 Object.GetType().GetProperties() 取得所有 annotation 後傳入</param>
        /// <returns>第一個string 為指定的Name，第二個string 為對應的多語言名稱</returns>
        public Dictionary<string, string> GetObjectDisplayName(PropertyInfo[] properties)
        {
            Dictionary<string, string> FieldName = new Dictionary<string, string>();
            foreach (var property in properties)
            {
                var attribute = property.GetCustomAttributes(typeof(DisplayAttribute), false);
                if (attribute.Length > 0)
                {
                    DisplayAttribute DA = attribute[0] as DisplayAttribute;
                    //如果已經存在就不再新增
                    var tempValue = string.Empty;
                    if (!FieldName.TryGetValue(DA.Name, out tempValue))
                        FieldName.Add(DA.Name, getContent(DA.Name));
                }
            }
            return new Dictionary<string, string>(FieldName);
        }

        public string getContent(string field)
        {
            if (field.ToLower().IndexOf("resource.") != 0) return field; //只比對resource.開頭的
            var actualFieldName = field.Substring(9, field.Length-9);

            return getContentWithNoPrefix(actualFieldName);
        }

        public string getContentWithNoPrefix(string field)
        {
            JToken content;
            if (!LPFile.IsResourceReady) return field;

            if (LPFile.Resources.ContainsKey(CountryCode))
                LPFile.Resources[CountryCode].TryGetValue(field, out content);
            else
                LPFile.Resources[DefaultCountryCode].TryGetValue(field, out content);
            
            var fieldDesc = (content == null || string.IsNullOrEmpty(content.ToString()) ? field : content.ToString());
            
            //如果content中包含了\'的內容，要替換成'
            fieldDesc = fieldDesc.Replace("\\'", "'");

            return fieldDesc;
        }

        public bool UpdateLanguagePackageFile(string newContent, string modifier)
        {
            bool isSuccessed = true;
            JObject newContentObj = JObject.Parse(newContent);
            JToken tempToken;

            //先取目前檔案中的資料
            var originalResource = LoadLanguageResources();

            //Language1
            if (newContentObj.TryGetValue("Language1", out tempToken))
            {
                var languagePack = JObject.Parse(tempToken.ToString());
                if (!breakdownNewContent(languagePack,originalResource))
                    isSuccessed = false;
            }
            else
                isSuccessed = false;

            //Language2 //不一定會有
            if (newContentObj.TryGetValue("Language2", out tempToken))
            {
                var languagePack = JObject.Parse(tempToken.ToString());
                if (!breakdownNewContent(languagePack, originalResource))
                    isSuccessed = false;
            }

            //排序
            originalResource = new JObject(originalResource.Properties().OrderBy(p=>p.Name));

            //成功才更新
            if (isSuccessed)
            {
                //修改
                lock (fileLock)
                {
                    var newFileContent = new JObject();

                    //這次修改的內容
                    newFileContent.Add(new JProperty("Content", originalResource));

                    //加上修改者
                    newFileContent.Add(new JProperty("Modifier", modifier));

                    //先備份
                    var backupFileName = PackageFileName + DateTime.Now.ToString("yyyyMMdd-HHmmss.fff");
                    string packagePath = System.Web.Hosting.HostingEnvironment.MapPath(PackageDir + PackageFileName);
                    string backupPath = System.Web.Hosting.HostingEnvironment.MapPath(CacheDir + backupFileName);
                    File.Copy(packagePath, backupPath);

                    //覆寫
                    File.WriteAllText(packagePath, newFileContent.ToString(), Encoding.Unicode);

                    //更新
                    LoadLanguagePackage();
                }
            }

            return isSuccessed;
        }

        public bool UpdateLanguagePackageFile(List<string> countries, List<JObject> contents, string modifier, bool isSuperUser = false) {
            bool isSuccessed = true;

            //先取目前檔案中的資料
            var originalResource = LoadLanguageResources();

            for (var i = 0 ; i < countries.Count ; i++){
                //如果不是Super User，不允許修改英文資料
                if (countries.ElementAt(i).ToLower().Equals(DefaultCountryCode.ToLower()) && !isSuperUser)
                {
                    continue;
                }

                JObject newContent = new JObject(new JProperty("Language", countries.ElementAt(i)));

                //如果沒資料就不用送
                if (contents.ElementAt(i).Count > 0) {
                    newContent.Add(new JProperty("Content", contents.ElementAt(i)));
                    if (!breakdownNewContent(newContent, originalResource, isSuperUser))
                        isSuccessed = false;
                }
            }

            //更新資料
            if (isSuccessed)
                isSuccessed = UpdateFileAndBackup(originalResource, modifier);

            return isSuccessed;
        }

        public bool UpdateFileAndBackup(JObject newResources, string modifier) {
            bool isSuccessed = true;

            //排序
            newResources = new JObject(newResources.Properties().OrderBy(p => p.Name));

            //修改
            lock (fileLock)
            {
                var newFileContent = new JObject();

                //這次修改的內容
                newFileContent.Add(new JProperty("Content", newResources));

                //加上修改者
                newFileContent.Add(new JProperty("Modifier", modifier));

                //先備份
                var backupFileName = PackageFileName + DateTime.Now.ToString("yyyyMMdd-HHmmss.fff");
                string packagePath = System.Web.Hosting.HostingEnvironment.MapPath(PackageDir + PackageFileName);
                string backupPath = System.Web.Hosting.HostingEnvironment.MapPath(CacheDir + backupFileName);
                File.Copy(packagePath, backupPath);

                //覆寫
                File.WriteAllText(packagePath, newFileContent.ToString(), Encoding.Unicode);

                //更新
                LoadLanguagePackage();
            }

            return isSuccessed;
        }

        private bool breakdownNewContent(JObject languagePack, JObject oriResources, bool isSuperUser = false)
        {
            bool isSuccessed = true;

            JToken tempToken;
            string countryCode;
            JObject newContentObj = new JObject();

            //取語言別
            if (languagePack.TryGetValue("Language", out tempToken))
                countryCode = tempToken.ToString().ToLower();
            else
                return false;

            //取設定的內容
            if (languagePack.TryGetValue("Content", out tempToken))
                newContentObj = JObject.Parse(tempToken.ToString());
            else
                return false;

            //開始更新
            foreach (var item in newContentObj)
            {
                var id = item.Key.ToString();
                //var desc = System.Web.HttpUtility.HtmlEncode(item.Value.ToString());
                var desc = item.Value.ToString();

                //如果字串中出現'，要加上\來區分(json格式限制)
                desc = desc.Replace("&apos;", "\\&apos;").Replace("'","\\'");

                desc = desc.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&").Replace("&quot;", "\"").Replace("&apos;", "'");

                var resourceProperty = oriResources.Properties().Where(p => p.Name.Equals(id)).FirstOrDefault();
                if (resourceProperty != null && !string.IsNullOrEmpty(resourceProperty.Name))
                {
                    var set = JObject.Parse(resourceProperty.Value.ToString());

                    //修改 package 下 特定語言的內容
                    set.TryGetValue("Package", out tempToken);
                    var package = JObject.Parse(tempToken.ToString());
                    package.Remove(countryCode);
                    package.Add(new JProperty(countryCode, desc));

                    set.Remove("Package");
                    set.Add(new JProperty("Package",package));

                    oriResources.Remove(id);
                    oriResources.Add(id, set);
                }
                else {  
                    if (isSuperUser)
                    {
                        //SuperUser 才能定義新的欄位
                        var newSet = new JObject();
                        var newPackage = new JObject(new JProperty(countryCode, desc));
                        newSet.Add(new JProperty("Package", newPackage));
                        newSet.Add(new JProperty("Type", "Message"));   //預設 Message
                        oriResources.Add(id, newSet);
                    }
                    else {
                        return false;
                    }
                }
            }
            return isSuccessed;
        }

        /// <summary>
        /// 刪除特定語系的資料
        /// </summary>
        /// <param name="countryCode"></param>
        public void RemoveCountryCode(string countryCode, string modifier) {

            var isSuccess = true;
            var resources = LoadLanguageResources();
            var newResources = new JObject();

            try {
                foreach (var property in resources.Properties())
                {
                    var packageObj = JObject.Parse(property.Value.ToString()).Value<JObject>("Package");

                    //先刪除掉特定語言的資料
                    packageObj.Remove(countryCode.ToLower());

                    //重新組資料並加到 newResources
                    if (packageObj.Count > 0)
                    {
                        var newProperty = new JObject();
                        newProperty.Add(new JProperty("Type", JObject.Parse(property.Value.ToString()).Value<string>("Type")));
                        newProperty.Add(new JProperty("Package", packageObj));
                        newResources.Add(property.Name, newProperty);
                    }
                }
            }
            catch (Exception ex) {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                isSuccess = false;
            }
            if (isSuccess) UpdateFileAndBackup(newResources, modifier);
        }
    }
}