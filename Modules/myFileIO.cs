using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Ionic.Zip;
using System.Text;
using System.IO;
using System.Data;
using System.Data.OleDb;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace JHT
{
    /// <summary>
    /// myFileIO 的摘要描述
    /// 檔案處理相關Function 、新增、刪除、修改、壓縮等
    /// 鄭博仁
    /// </summary>
    public class myFileIO
    {
        //public string RootPath = System.Configuration.ConfigurationSettings.AppSettings["RootPath"].ToString().ToUpper();

        public string RootPath = "d";

        /// <summary>
        /// 刪除某一資料內所有檔案(保留資料夾)
        /// </summary>
        /// <returns></returns>
        public int DeleteAllFileInFolder(string FolderPath, string FileType)
        {
            FolderPath = GetVirtualPath(FolderPath);
            if (CheckHaveFolder(FolderPath))   //確認資料夾存在
            {

                //DirectoryInfo dir = new DirectoryInfo(HttpContext .Current .Server .MapPath (FolderPath ));
                //FileInfo[] Files = dir.GetFiles();            
                //for (int i = 0; i < Files.Length; i++)
                //{                
                //    DeleteOneFile(Files[i].FullName);                
                //}

                string[] Files = Directory.GetFiles(HttpContext.Current.Server.MapPath(FolderPath), FileType);
                for (int i = 0; i < Files.Length; i++)
                {
                    DeleteOneFile(Files[i]);
                }

                return 1;
            }
            else
            {
                return 10;
            }

        }

        /// <summary>
        /// 刪除一個檔案
        /// </summary>
        /// <param name="FilePath"></param>
        /// <returns></returns>
        public int DeleteOneFile(string FilePath)
        {
            FilePath = GetVirtualPath(FilePath); //轉成虛疑路徑
            // RecruitFN.AlertMsg(FilePath);

            if (CheckHaveFile(FilePath))  //確認檔案是否存在 
            {
                System.IO.File.Delete(HttpContext.Current.Server.MapPath(FilePath));
                return 1;
            }
            else
            {
                return 0;
            }
        }


        /// <summary>
        /// 刪除多個檔案
        /// </summary>
        /// <param name="FilePath"></param>
        /// <returns></returns>
        public int DeleteSomeFile(string[] FilePath)
        {
            foreach (string F in FilePath)
            {
                DeleteOneFile(F);
            }
            return 1;
        }


        /// <summary>
        /// 取得虛擬路徑
        /// </summary>
        /// <param name="FilePath">實體路徑或虛擬路徑</param>
        /// <returns></returns>
        public string GetVirtualPath(string FilePath)
        {
            string NewPath = "";
            if (FilePath.IndexOf(@"\") > 0)  //實體路徑 才需轉換
            {
                NewPath = FilePath.ToUpper().Replace(RootPath, "");                
                NewPath = "~/" + NewPath.Replace("\\", "/");              
                return NewPath;
            }
            else
            {
                return FilePath;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="FilePath"></param>
        /// <returns></returns>
        public string GetRealPath(string FilePath)
        {
            string NewPath = FilePath;
            //if (FilePath.IndexOf(@"/") > 0)  //實體路徑 才需轉換
            //{
                NewPath = HttpContext.Current.Server.MapPath(FilePath);                
            //}
            return NewPath;
            
        }

        /// <summary>
        /// 檢查檔案是否存在   true存在  false不存在
        /// </summary>
        /// <param name="FilePath">完整的真實儲存路徑</param>
        /// <returns></returns>
        public Boolean CheckHaveFile(string FilePath)
        {
            //FilePath = GetVirtualPath(FilePath);
            //System.IO.FileInfo file = new System.IO.FileInfo(HttpContext.Current.Server.MapPath(FilePath));
            System.IO.FileInfo file = new System.IO.FileInfo(FilePath);
            if (file.Exists)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 檢查資料夾是否存在
        /// </summary>
        /// <param name="FolderPath">完整的真實儲存路徑</param>
        /// <returns></returns>
        public Boolean CheckHaveFolder(string FolderPath)
        {
            FolderPath = GetVirtualPath(FolderPath);
            DirectoryInfo Dir = new DirectoryInfo(HttpContext.Current.Server.MapPath(FolderPath));
            ///RecruitFN.AlertMsg(FolderPath);
            if (Dir.Exists)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 檔案壓縮
        /// </summary>
        /// <param name="ZipFilePath"></param>
        /// <param name="SaveFileName"></param>
        /// <returns></returns>
        public Boolean ZipFile(string ZipFilePath, string SaveFileName)
        {
            ZipFilePath = GetVirtualPath(ZipFilePath);
            SaveFileName = GetVirtualPath(SaveFileName);
            using (ZipFile zip = new ZipFile(HttpContext.Current.Server.MapPath(SaveFileName)))
            {
                zip.AddFile(ZipFilePath);
                zip.Save();
            }
            return true;
        }

        /// <summary>
        /// 壓縮整個資料夾
        /// </summary>
        /// <param name="ZipFilePath"></param>
        /// <param name="SaveFileName"></param>
        /// <returns></returns>
        public Boolean ZipDir(string ZipFilePath, string SaveFileName)
        {
            ZipFilePath = GetVirtualPath(ZipFilePath);
            SaveFileName = GetVirtualPath(SaveFileName);
            using (ZipFile zip = new ZipFile(HttpContext.Current.Server.MapPath(SaveFileName)))
            {

                zip.AddDirectory(HttpContext.Current.Server.MapPath(ZipFilePath));
                zip.Save();
            }
            return true;
        }

        /// <summary>
        /// Merges pdf files from a byte list
        /// iTextSharp component is necessary
        /// </summary>
        /// <param name="files">list of files to merge</param>
        /// <returns>memory stream containing combined pdf</returns>
        public byte[] MergePdfForms(List<byte[]> files)
        {
            if (files.Count > 1)
            {
                string[] names;
                PdfStamper stamper;
                MemoryStream msTemp = null;
                PdfReader pdfTemplate = null;
                PdfReader pdfFile;
                Document doc;
                PdfWriter pCopy;
                MemoryStream msOutput = new MemoryStream();

                pdfFile = new PdfReader(files[0]);

                doc = new Document();
                pCopy = new PdfSmartCopy(doc, msOutput);
                pCopy.PdfVersion = PdfWriter.VERSION_1_7;

                doc.Open();

                for (int k = 0; k < files.Count; k++)
                {
                    for (int i = 1; i < pdfFile.NumberOfPages + 1; i++)
                    {
                        msTemp = new MemoryStream();
                        pdfTemplate = new PdfReader(files[k]);

                        stamper = new PdfStamper(pdfTemplate, msTemp);

                        names = new string[stamper.AcroFields.Fields.Keys.Count];
                        stamper.AcroFields.Fields.Keys.CopyTo(names, 0);
                        foreach (string name in names)
                        {
                            stamper.AcroFields.RenameField(name, name + "_file" + k.ToString());
                        }

                        stamper.Close();
                        pdfFile = new PdfReader(msTemp.ToArray());
                        ((PdfSmartCopy)pCopy).AddPage(pCopy.GetImportedPage(pdfFile, i));
                        pCopy.FreeReader(pdfFile);
                    }
                }

                pdfFile.Close();
                pCopy.Close();
                doc.Close();

                return msOutput.ToArray();
            }
            else if (files.Count == 1)
            {
                return new MemoryStream(files[0]).ToArray();
            }

            return null;
        }

        /// <summary>
        /// export byte[] content to file
        /// </summary>
        /// <param name="FileContent"></param>
        /// <param name="ContentType">excel or pdf</param>
        /// <param name="FileName">no extended file name</param>
        public void ExportToFile(byte[] FileContent, string ContentType, string FileName)
        {
            System.IO.MemoryStream MS = new System.IO.MemoryStream(FileContent);
            ExportToFile(MS, ContentType, FileName);
        }

        /// <summary>
        /// export several files to one file (PDF only)
        /// </summary>
        /// <param name="FileContent">PDF only</param>
        /// <param name="FileName"></param>
        //public void ExportToFile(List<byte[]> FileContent, string FileName)
        //{
        //    System.IO.MemoryStream MS = MergePdfForms(FileContent);
        //    ExportToFile(MS, "pdf", FileName);
        //}

        public void ExportToFile(MemoryStream MS, string ContentType, string FileName)
        {
            if (ContentType == "excel")
                FileName += ".xls";
            if (ContentType == "pdf")
                FileName += ".pdf";

            HttpContext.Current.Response.Clear();
            HttpContext.Current.Response.ContentType = "application/" + ContentType;
            HttpContext.Current.Response.AddHeader("Content-Disposition", "attachment;filename=\"" + FileName + "\"");
            HttpContext.Current.Response.BinaryWrite(MS.ToArray());
            HttpContext.Current.Response.Flush();
            HttpContext.Current.Response.End();
        }


        /// <summary>
        /// 上傳檔案 成功傳回"" 路徑存為Session["UploadedFile"], 若失敗則傳回錯誤路徑
        /// </summary>
        /// <param name="FU">上傳控制項</param>
        /// <param name="AllowFile">允許的副檔名，不需分大小寫</param>
        /// <param name="FilePath">儲存的路徑 空預設為C:\\Inetpub\\wwwroot\\HRDNTB2E04\\Attach\\ </param>
        /// <param name="SaveMode">儲存方式 1以時間為檔名  2以原檔名  3以AllowFile[]的第0個值為檔名(直接覆蓋) 4以原檔名+日期時間</param>
        /// <returns></returns>
        /*
        public string UploadFileFN(FileUpload FU, string[] AllowFile, string FilePath, int SaveMode)
        {
            string RetrunString = "";
            //上傳檔案
            if (FU.HasFile && CheckHaveFolder (FilePath) )
            {
                Boolean FileOK = false;
                //   "C:\\Inetpub\\wwwroot\\HRDNTB2E04\\Attach\\"
                string Path = HttpContext.Current.Server.MapPath(System.Web.Configuration.WebConfigurationManager.AppSettings["FilePath"].ToString() + FilePath);

                //檢查附檔名
                string FileExtension = System.IO.Path.GetExtension(FU.FileName).ToUpper();
                //string[] AllowFile = { ".XLS" };
                for (int i = 0; i < AllowFile.Length; i++)
                {
                    if (AllowFile[i].ToUpper() == FileExtension)
                    {
                        FileOK = true;
                    }
                }

                if (FileOK == true)
                {
                    string FileName = "";
                    switch (SaveMode)
                    {
                        case 1://時間檔名
                            FileName = Path + DateTime.Now.ToString("yyyyMMddmmss") + FileExtension;
                            break;

                        case 2://原檔名
                            FileName = Path + FU.FileName;
                            //以原檔名儲存需判斷重覆的問題
                            if (CheckHaveFile(FileName))
                            {
                                return "ERROR：檔案名稱重覆，請先刪除原本的檔案後再上傳!\n" + FU.FileName;
                            }
                            break;
                        case 3:
                            FileName = Path + AllowFile[0];
                            break;
                        case 4:
                            FileName = Path + FU.FileName + DateTime.Now.ToString("yyyyMMddmmss");
                            break;    
                    }     

                    FU.PostedFile.SaveAs(FileName);
                    HttpContext.Current.Session["UploadedFile"] = FileName;
                    //RetrunString = FileName;
                }
                else
                {
                    RetrunString = "ERROR：" + Resources.FileUpload.FileFormatError;
                }

            }
            else
            {
                RetrunString = "ERROR：" + Resources .FileUpload.NoFile ;
            }
            return RetrunString;
        }
        */

        /// <summary>
        /// 抓取指定的Excel檔案 列出所有的SheetName，以StringDictionary傳回
        /// </summary>
        /// <param name="FilePath">檔案位置+檔名</param>
        /// <returns></returns>
        public System.Collections.Specialized.StringDictionary ExcelSheetList(string FilePath)
        {
            string connectstring = "";
            connectstring = "Provider=Microsoft.Ace.OleDb.12.0;" +
                            "Data Source=" + FilePath + ";" +
                            "Extended Properties='Excel 12.0;HDR=YES'";

            OleDbConnection ExcelCN = new OleDbConnection(connectstring);
            OleDbCommand ExcelCMD = new OleDbCommand();           
            
            //取得Excel裡的Sheet名稱
            ExcelCN.Open();
            DataTable SheetName = ExcelCN.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
            ExcelCN.Close();

            System.Collections.Specialized.StringDictionary SD = new System.Collections.Specialized.StringDictionary();
            
            for (int i = 0; i < SheetName.Rows.Count; i++)
            {
               // SD.Add(SheetName.Rows[i]["Table_Name"].ToString(), FilePath);
               // dorpdownlist 中 key不能重覆，所以改用數字當key
                SD.Add(i.ToString(),SheetName.Rows[i]["Table_Name"].ToString());
            }
            return SD;
        }


        /// <summary>
        /// 將Server上的Excel檔的第一個Sheet 轉成DataTable
        /// </summary>
        /// <param name="FilePath">檔案路徑</param>
        /// <returns></returns>
        public DataTable ExcelToDataTable(string FilePath)
        {
            return ExcelToDataTable(FilePath, 0);
        }

        /// <summary>
        /// 將Server上的Excel檔指定的Sheet 轉成DataTable
        /// </summary>
        /// <param name="FilePath">檔案路徑</param>
        /// <param name="StrSheetName">Sheet key</param>
        /// <returns></returns>
        public DataTable ExcelToDataTable(string FilePath, int SheetKey)
        {           
            //開始檢查        
            string ExcelFilePath = FilePath;
            string connectstring = "";         
            connectstring = "Provider=Microsoft.Ace.OleDb.12.0;" +
                            "Data Source=" + ExcelFilePath + ";" +
                            "Extended Properties='Excel 12.0;HDR=YES'";           

            OleDbConnection ExcelCN = new OleDbConnection(connectstring);
            OleDbCommand ExcelCMD = new OleDbCommand();
            ExcelCN.Open();

            //取得Excel裡的Sheet名稱
            DataTable SheetName = ExcelCN.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);

            string StrSheetName = SheetName.Rows[SheetKey]["TABLE_NAME"].ToString();     

            //string SheetNameString = "DataExample";
            ExcelCMD.CommandText = @"SELECT * FROM [" + StrSheetName + "]";
            ExcelCMD.CommandType = CommandType.Text;
            ExcelCMD.Connection = ExcelCN;

            DataTable DT = new DataTable();
            DT.Load(ExcelCMD.ExecuteReader());

            ExcelCMD.Dispose();
            ExcelCN.Close();
            ExcelCN.Dispose();

            return DT;
        }


        

    }

}