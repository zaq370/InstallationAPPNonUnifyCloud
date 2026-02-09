using Elmah;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace InstallationAPPNonUnify.Modules
{
    public static class PasswordEncryption
    {

        public static string DesKey = "matrixxx";
        public static string DesIv = "fitnesss";

        /// <summary>
        /// 密碼加密
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string Encrypt(string input) {
            return DesEncrypt(input);
        }

        /// <summary>
        /// 密碼解密
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string Decrypt(string input) {
            return DesDecrypt(input);
        }

        private static string DesDecrypt(string input) {
            string decrypt = input;

            try
            {
                var des = new DESCryptoServiceProvider();
                des.Key = Encoding.ASCII.GetBytes(DesKey);
                des.IV = Encoding.ASCII.GetBytes(DesIv);
                byte[] dataByteArray = Convert.FromBase64String(decrypt);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(dataByteArray, 0, dataByteArray.Length);
                        cs.FlushFinalBlock();
                        decrypt = Encoding.UTF8.GetString(ms.ToArray());
                    }
                }
            }
            catch (Exception ex) {
                Elmah.ErrorLog.GetDefault(null).Log(new Error(ex));
            }
            return decrypt;
        }

        private static string DesEncrypt(string input) {
            string encrypt = input;

            var des = new DESCryptoServiceProvider();
            des.Key = Encoding.ASCII.GetBytes(DesKey);
            des.IV = Encoding.ASCII.GetBytes(DesIv);
            byte[] dataByteArray = Encoding.UTF8.GetBytes(input);

            using (MemoryStream ms = new MemoryStream()) {
                try {
                    using (CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(dataByteArray, 0, dataByteArray.Length);
                        cs.FlushFinalBlock();
                        encrypt = Convert.ToBase64String(ms.ToArray());
                    }
                } catch (Exception ex) {
                    Elmah.ErrorLog.GetDefault(null).Log(new Error(ex));
                }
            }
            return encrypt;
        }

        //private static string KeyContainerName = "ServiceDirect";
        //private static int DwKeySize = 256;

        ///// <summary> 
        ///// RSA加密資料，如果系統沒有設定需要加密，則直接回傳同樣資料
        ///// </summary> 
        ///// <param name="input">要加密資料</param> 
        ///// <returns></returns> 
        //public static string RSAEncryption(string input)
        //{
        //    BackendInfo bi = new BackendInfo();
        //    if (!bi.IsEncryptPassword) return input;

        //    return RSAEncryptionImp(input);
        //}

        ///// <summary> 
        ///// RSA加密資料，回加密資料
        ///// </summary> 
        ///// <param name="input">要加密資料</param> 
        ///// <returns></returns> 
        //public static string RSAEncryptionImp(string input) {
        //    CspParameters RSAParams = new CspParameters();
        //    //RSAParams.Flags = CspProviderFlags.UseMachineKeyStore;
        //    RSAParams.Flags = CspProviderFlags.NoFlags;
        //    RSAParams.KeyContainerName = KeyContainerName;
        //    using (System.Security.Cryptography.RSACryptoServiceProvider provider = new RSACryptoServiceProvider(DwKeySize, RSAParams))
        //    {
        //        try {
        //            byte[] plaindata = System.Text.Encoding.Default.GetBytes(input);//將要加密的字串轉換為位元組陣列
        //            byte[] encryptdata = provider.Encrypt(plaindata, false);//將加密後的位元組資料轉換為新的加密位元組陣列
        //            return Convert.ToBase64String(encryptdata);//將加密後的位元組陣列轉換為字串
        //        }
        //        catch (Exception ex)
        //        {
        //            Elmah.ErrorLog.GetDefault(null).Log(new Error(ex));
        //            return input;
        //        }
        //    }
        //}

        ///// <summary> 
        ///// RSA解密資料，如果系統沒有設定需要加密，則直接回傳同樣資料
        ///// </summary> 
        ///// <param name="input">要解密資料</param> 
        ///// <returns></returns> 
        //public static string RSADecrypt(string input)
        //{
        //    BackendInfo bi = new BackendInfo();
        //    if (!bi.IsEncryptPassword) return input;

        //    return RSADecryptImp(input);
            
        //}

        ///// <summary> 
        ///// RSA解密資料，回傳解密
        ///// </summary> 
        ///// <param name="input">要解密資料</param> 
        ///// <returns></returns> 
        //public static string RSADecryptImp(string input)
        //{
        //    CspParameters RSAParams = new CspParameters();
        //    //RSAParams.Flags = CspProviderFlags.UseMachineKeyStore;
        //    RSAParams.Flags = CspProviderFlags.NoFlags;
        //    RSAParams.KeyContainerName = KeyContainerName;
        //    using (System.Security.Cryptography.RSACryptoServiceProvider provider = new RSACryptoServiceProvider(DwKeySize, RSAParams))
        //    {
        //        try
        //        {
        //            byte[] encryptdata = Convert.FromBase64String(input);
        //            byte[] decryptdata = provider.Decrypt(encryptdata, false);
        //            return System.Text.Encoding.Default.GetString(decryptdata);
        //        }
        //        catch (Exception ex)
        //        {
        //            Elmah.ErrorLog.GetDefault(null).Log(new Error(ex));
        //            return input;
        //        }
        //    }
        //}
    }
}