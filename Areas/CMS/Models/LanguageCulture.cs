using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace InstallationAPPNonUnify.Areas.CMS.Models
{
    public class LanguageCulture
    {
        public bool IsReadSuccessed { get; set; }
        public JArray Content { get; set; }

        private static string infoDir = "~/App_Data/";
        private static string packageFileName = "LanguageCulture.json";

        public LanguageCulture() {
            ReadFileContent();
        }
        /// <summary>
        /// 從LanguageCulture.json檔中取出資料
        /// </summary>
        public void ReadFileContent()
        {
            string packagePath = System.Web.Hosting.HostingEnvironment.MapPath(infoDir + packageFileName);

            using (var fs = new FileStream(packagePath, FileMode.OpenOrCreate))
            {
                using (var sr = new StreamReader(fs))
                {
                    var fileContent = JArray.Parse(sr.ReadToEnd().ToString());
                    if (fileContent != null && fileContent.Count > 0)
                    {
                        Content = fileContent;
                        IsReadSuccessed = true;
                    }
                }
            }
        }
    }
}