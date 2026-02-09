using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using InstallationAPPNonUnify.Areas.CMS.ViewModels;
using InstallationAPPNonUnify.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Linq;

namespace InstallationAPPNonUnify.Areas.CMS.Models
{
    public class ImportExportLanguagePack
    {
        public Tuple<bool,string> import(HttpPostedFileBase file, string modifier, bool isSuperUser = false) {

            var returnValue = new Tuple<bool, string>(true,""); //defualt

            //讀檔
            var doc = XDocument.Load(file.InputStream);
            XNamespace ss = "urn:schemas-microsoft-com:office:spreadsheet";

            //檢查sheet name 要是 LanguagePackage
            XElement sheet = doc.Root.Element(ss + "Worksheet");
            if (!sheet.Attribute(ss + "Name").Value.Equals("LanguagePackage"))
                return new Tuple<bool, string>(false, "sheet:LanguagePackage not found in file");

            List<string> contries = new List<string>();
            List<JObject> contents = new List<JObject>();

            BackendInfo bf = new BackendInfo();
            var countries = bf.ReadCountries();
            var validCountries = new List<CountryModel>();

            //LanguagePackage 與 RedirectLanguagePackage不一致的代表與其它語言共用，所以要移掉
            //例如 brazil 與 Spain 都使用 es-ES 
            foreach (var country in countries)
            {
                if (country.LanguagePackage.Equals(country.RedirectLanguagePackage))
                    validCountries.Add(country);
            }

            //foreach rows
            XElement table = sheet.Element(ss + "Table");
            var rows = table.Elements(ss + "Row");
            if (rows.Count() == 0) return new Tuple<bool, string>(false, "data not found");   //找不到資料

            for (var i = 0; i < rows.Count(); i++ )
            {
                var cells = rows.ElementAt(i).Elements(ss + "Cell");

                //header
                if (i == 0)
                {
                    var countriesIndex = 0;
                    foreach (var cell in cells) {
                        countriesIndex++;
                        var index = cell.Attribute(ss+"Index") != null ? cell.Attribute(ss+"Index").Value : string.Empty;
                        if (!String.IsNullOrEmpty(index)) {
                            //第一筆index應該要是2
                            if (countriesIndex == 1 && index != "2")
                                return new Tuple<bool, string>(false, "country code may misplaced");

                            //除了第一筆之外，不該有
                            if (countriesIndex > 1)
                                return new Tuple<bool, string>(false, "country code syntax error");
                        }
                        else
                        {
                            //第一筆沒有index的會跳過，左上角的格子要忽略
                            if (countriesIndex == 1) continue;
                        }

                        //check country code valid or not
                        var countryCode = cell.Element(ss + "Data").Value;
                        bool valid = false;
                        validCountries.ForEach(item =>
                        {
                            if (item.LanguagePackage.ToLower().Equals(countryCode.ToLower()))
                                valid = true;
                        });
                        if (valid)
                        {
                            contries.Add(countryCode);
                            contents.Add(new JObject());
                            countriesIndex++;
                        }
                        else
                        {
                            return new Tuple<bool, string>(false, countryCode + " is not a valid country code");
                        }
                    }
                }
                else 
                { 
                    //body
                    int actualCellIndex = 0;
                    var fieldId = string.Empty;
                    for (int cellIndex = 0 ; cellIndex < cells.Count(); cellIndex++)
                    {
                        var cell = cells.ElementAt(cellIndex);

                        var index = cell.Attribute(ss+"Index") != null ? cell.Attribute(ss+"Index").Value : string.Empty;
                        if (!String.IsNullOrEmpty(index))
                            actualCellIndex = int.Parse(index) - 1;

                        //第一個cell應該要是欄位id，如果有發生actualCellIndex>0，代表資料不正確
                        if (cellIndex == 0  & actualCellIndex > 0)
                            return new Tuple<bool, string>(false, "row :" + i + " contains invalid data");

                        var data = cell.Element(ss+"Data");
                        if (data != null) { 
                            //欄位ID
                            if (actualCellIndex == 0)
                                fieldId = data.Value;
                            else { 
                                //依照欄位的位置，決定放在哪個語言底下
                                if (actualCellIndex > contents.Count) {
                                    return new Tuple<bool, string>(false, "row :" + i + " contains invalid data");
                                }
                                var desc = Regex.Replace(data.Value, "[`~#$^&=|{};\\[\\]~#￥……&（）——|{}【】‘；”“、？]", string.Empty);
                                contents.ElementAt(actualCellIndex - 1).Add(new JProperty(fieldId, desc));
                            }
                        }
                        actualCellIndex++;
                    }
                }
            }

            //把資料更新回檔案
            LanguagePackage lp = new LanguagePackage();
            if (!lp.UpdateLanguagePackageFile(contries, contents, modifier, isSuperUser))
                return new Tuple<bool, string>(false, " update language package error");

            return returnValue;
        }

        public Tuple<bool, string> Export()
        {
            //從Saple複製一份出來
            var OriginalFile = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/CMSLanguagePack.xml");
            var NewFile = System.Web.Hosting.HostingEnvironment
                .MapPath("~/App_Data/cache/CMSLanguagePack." + DateTime.Now.ToString("yyyyMMdd-HHmmss.fff") + ".xml");
            File.Copy(OriginalFile, NewFile);

            //讀檔
            var doc = XDocument.Load(NewFile);
            XNamespace ss = "urn:schemas-microsoft-com:office:spreadsheet";

            //檢查sheet name 要是 LanguagePackage
            XElement sheet = doc.Root.Element(ss + "Worksheet");
            if (!sheet.Attribute(ss + "Name").Value.Equals("LanguagePackage"))
                return new Tuple<bool, string>(false, "sheet:LanguagePackage not found in file");

            //語系資訊
            BackendInfo bf = new BackendInfo();
            var countries = bf.ReadCountries();
            var validCountries = new List<CountryModel>();

            //LanguagePackage 與 RedirectLanguagePackage不一致的代表與其它語言共用，所以要移掉
            //例如 brazil 與 Spain 都使用 es-ES 
            foreach (var country in countries) {
                if (country.LanguagePackage.Equals(country.RedirectLanguagePackage))
                    validCountries.Add(country);
            }

            LanguagePackage lp = new LanguagePackage();
            var resources = lp.LoadLanguageResources();

            //
            List<MultiLanguageModel> multiLanguageContent = new List<MultiLanguageModel>();

            //table
            XElement table = sheet.Element(ss + "Table");
            var headerRow = table.Element(ss + "Row");
            var backupRow = new XElement(headerRow);

            var headerCell = headerRow.Element(ss + "Cell");
            var headerData = headerCell.Element(ss + "Data");
            headerData.Value = string.Empty;
            for (var i = 0; i < validCountries.Count; i++ )
            {
                var newCell = new XElement(headerCell);
                var newData = newCell.Element(ss + "Data");
                newData.Value = validCountries.ElementAt(i).LanguagePackage;
                headerRow.Add(newCell);
                multiLanguageContent.Add(new MultiLanguageModel(
                    resources, validCountries.ElementAt(i).LanguagePackage, validCountries.ElementAt(i).CountryName));
            }
            table.SetAttributeValue(ss + "ExpandedColumnCount", validCountries.Count+1);

            //把所有欄位id找出來
            var ids = new List<string>();
            foreach (var item in multiLanguageContent) {
                item.Content.ForEach(p => ids.Add(p.FieldId));
            }
            ids = ids.Distinct().ToList<string>();
            table.SetAttributeValue(ss + "ExpandedRowCount", ids.Count+1);

            //依欄位id產生Row，並把多語言資料產生cell/data
            for (var i = 0; i < ids.Count; i++) {
                var newRow = new XElement(backupRow);
                var firstCell = newRow.Element(ss + "Cell");
                var firstData = firstCell.Element(ss + "Data");
                firstData.Value = ids.ElementAt(i);

                for (var j = 0; j < validCountries.Count; j++)
                {
                    var newCell = new XElement(firstCell);
                    var newData = newCell.Element(ss+"Data");
                    var filedDesc = multiLanguageContent.FirstOrDefault(item => item.CountryCode.Equals(validCountries.ElementAt(j).LanguagePackage)).Content.FirstOrDefault(item => item.FieldId.Equals(ids.ElementAt(i))).FieldDesc;
                    newData.Value = filedDesc.Replace("\\'", "'");
                    newRow.Add(newCell);
                }
                table.Add(newRow);
            }

            doc.Save(NewFile);
            return new Tuple<bool, string>(true, NewFile);
        }

        public Tuple<bool,string> ImportAndApplyLanguageFile(HttpFileCollectionBase files, string modifier, string countryCode, bool onlyNew) {

            Stream req = files[0].InputStream;
            var content = new StreamReader(req).ReadToEnd();

            //確認匯入的資料正常
            LanguagePackage lp = new LanguagePackage(countryCode);
            JObject contentObj = new JObject();
            try {
                contentObj = JObject.Parse(content);
            } catch (Exception ex)
            {
                return new Tuple<bool, string>(false, lp.getContentWithNoPrefix("InvalidContent"));
            }

            //取content
            JToken tempToken;
            contentObj.TryGetValue("Content", out tempToken);
            if (tempToken == null || string.IsNullOrEmpty(tempToken.ToString()))
                return new Tuple<bool, string>(false, lp.getContentWithNoPrefix("InvalidContent"));
            JObject newContent = JObject.Parse(tempToken.ToString());

            //如果只匯入有差異的資料
            //以原本的json為主，把新匯入檔案中，原本json沒有的加入
            if (onlyNew) {
                newContent = new LanguagePackage().LoadLanguageResources();
                var updateContent = JObject.Parse(tempToken.ToString());

                //目前設定中有使用到的country code
                List<string> preserveCountryCode = new List<string>();
                foreach (var country in new BackendInfo().ReadCountries())
                {
                    if (country.LanguagePackage.Equals(country.RedirectLanguagePackage))
                    {
                        preserveCountryCode.Add(country.LanguagePackage);
                    }
                    else {
                        if (!preserveCountryCode.Contains(country.RedirectLanguagePackage)) {
                            preserveCountryCode.Add(country.LanguagePackage);
                        }
                    }
                }

                //把新匯入的內容一筆一筆找出來
                foreach (var property in updateContent.Properties())
                {
                    //新匯入的資料，取 Package 這個node下的值
                    var package = JObject.Parse(property.Value.ToString()).Value<JObject>("Package");

                    if (!newContent.ContainsKey(property.Name))
                    {
                        //如果newContent(原本Language Package)沒有的，要加入一個空白的Object
                        var newValue = JObject.Parse(property.Value.ToString());
                        newValue.Property("Package").Remove();

                        //加入時，確認只保留目前設定有使用的語言
                        //這段先加入語言別完全相同的資料
                        JObject newPackage = new JObject();
                        foreach (var subProperty in package.Properties())
                        {
                            var country = preserveCountryCode.FirstOrDefault(i => i.ToLower().Equals(subProperty.Name.ToLower()));
                            if (!string.IsNullOrEmpty(country) && !string.IsNullOrWhiteSpace(country))
                            {
                                newPackage.Add(new JProperty(country.ToLower(), subProperty.Value.ToString()));
                            }
                        }

                        //這段先加入語言別前N碼相同的資料，例如目前設定中有fr-ch，而匯入的資料是fr-fr，因為同樣是法文，所以允許匯入
                        //但如果fr-ch已經有資料，就不匯入
                        foreach (var subProperty in package.Properties())
                        {
                            var shortName = subProperty.Name.Substring(0, subProperty.Name.IndexOf('-'));
                            var countries = preserveCountryCode.FindAll(i => i.Substring(0, shortName.Length).Equals(shortName));
                            foreach (var matchCountry in countries)
                            {
                                if (!newPackage.ContainsKey(matchCountry.ToLower()))
                                    newPackage.Add(new JProperty(matchCountry.ToLower(), subProperty.Value.ToString()));
                            }
                        }
                        if (newPackage != null && newPackage.HasValues)
                        {
                            newValue.Add(new JProperty("Package", newPackage));
                            newContent.Add(new JProperty(property.Name, newValue));
                        }
                    }
                    else { 
                        //雖然同樣名字的資料存在，但下面可能有新加的語言別資料，例如這次匯入了 fr-ch 的語言別資料
                        bool isUpdated = false;
                        var originalObj = newContent.Value<JObject>(property.Name); //先取出原本的資料(全部)
                        var originalPackage = originalObj.Value<JObject>("Package");    //只取出Package

                        //用新匯入的資料找語言別完全相同的資料
                        foreach (var subProperty in package.Properties())
                        {
                            var country = preserveCountryCode.FirstOrDefault(i => i.ToLower().Equals(subProperty.Name.ToLower()));
                            if (!string.IsNullOrEmpty(country) && !string.IsNullOrWhiteSpace(country))
                            {
                                //原本這個語言別有沒有資料
                                if (!originalPackage.ContainsKey(country.ToLower())) {
                                    originalPackage.Add(new JProperty(country.ToLower(), subProperty.Value.ToString()));
                                    isUpdated = true;
                                }
                            }
                        }

                        //用新匯入的資料找語言別前N碼相同的資料，例如目前設定中有fr-ch，而匯入的資料是fr-fr，因為同樣是法文，所以允許匯入
                        //但如果fr-ch已經有資料，就不匯入
                        foreach (var subProperty in package.Properties())
                        {
                            var shortName = subProperty.Name.Substring(0, subProperty.Name.IndexOf('-'));
                            var countries = preserveCountryCode.FindAll(i => i.Substring(0, shortName.Length).Equals(shortName));
                            foreach (var matchCountry in countries)
                            {
                                if (!originalPackage.ContainsKey(matchCountry.ToLower())) {
                                    originalPackage.Add(new JProperty(matchCountry.ToLower(), subProperty.Value.ToString()));
                                    isUpdated = true;
                                }
                            }
                        }

                        //如果有異動
                        if (isUpdated) {
                            newContent.Remove(property.Name);   //將這個Property 從 newContent 移掉
                            //加入新組成的
                            originalObj.Remove("Package");
                            originalObj.Add(new JProperty("Package", originalPackage));
                            newContent.Add(new JProperty(property.Name, originalObj));
                        }
                    }
                }
            } 

            //更新至檔案
            if (lp.UpdateFileAndBackup(newContent, modifier))
                return new Tuple<bool, string>(true, lp.getContentWithNoPrefix("ExecuteSuccessfully"));
            else
                return new Tuple<bool, string>(true, lp.getContentWithNoPrefix("FailToExecute"));
        }
    }
}