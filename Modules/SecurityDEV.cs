using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web;

/// <summary>
/// Summary description for Security
/// </summary>
public class Security
{
    public Security()
    {
        //
        // TODO: Add constructor logic here
        //
    }

    public static string aesDecryptBase64(string SourceStr)
    {
        string decrypt = "";
        try
        {
            // AES Key 和 IV
            byte[] key = {
                157, 111, 146, 12, 86, 58, 48, 80,
                236, 171, 116, 94, 136, 98, 12, 10,
                102, 222, 199, 253, 66, 150, 157, 32,
                201, 2, 63, 166, 230, 204, 91, 26
            };

            byte[] iv = {
                101, 174, 47, 104, 61, 155, 253, 144,
                124, 169, 114, 110, 120, 41, 64, 77
            };

            byte[] dataByteArray = Convert.FromBase64String(SourceStr);

            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (MemoryStream ms = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(dataByteArray, 0, dataByteArray.Length);
                    cs.FlushFinalBlock();
                    decrypt = Encoding.UTF8.GetString(ms.ToArray());
                }
            }
        }
        catch (CryptographicException ex)
        {
            Console.WriteLine("解密失敗，可能是填補或金鑰/IV 錯誤：");
            Console.WriteLine(ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("其他錯誤：" + ex.Message);
        }

        return decrypt;
    }

    //public static string aesDecryptBase64(string SourceStr)
    //{
    //    string decrypt = "";
    //    try
    //    {
    //        AesCryptoServiceProvider aes = new AesCryptoServiceProvider();
    //        MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
    //        SHA256CryptoServiceProvider sha256 = new SHA256CryptoServiceProvider();
    //        byte[] key = { 157, 111, 146, 12, 86, 58, 48, 80, 236, 171, 116, 94, 136, 98, 12, 10, 102, 222, 199, 253, 66, 150, 157, 32, 201, 2, 63, 166, 230, 204, 91, 26 };
    //        byte[] iv = { 101, 174, 47, 104, 61, 155, 253, 144, 124, 169, 114, 110, 120, 41, 64, 77 };
    //        //byte[] key = sha256.ComputeHash(Encoding.UTF8.GetBytes(CryptoKey));
    //        //byte[] iv = md5.ComputeHash(Encoding.UTF8.GetBytes(CryptoKey));

    //        aes.Key = key;
    //        aes.IV = iv;

    //        byte[] dataByteArray = Convert.FromBase64String(SourceStr);
    //        using (MemoryStream ms = new MemoryStream())
    //        {
    //            using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
    //            {
    //                cs.Write(dataByteArray, 0, dataByteArray.Length);
    //                cs.FlushFinalBlock();
    //                decrypt = Encoding.UTF8.GetString(ms.ToArray());
    //            }
    //        }
    //    }
    //    catch (Exception ex)
    //    {

    //    }
    //    return decrypt;
    //}
}

public class SystemUserInfo
{
    public string DomainName { get; set; }
    public string UserName { get; set; }
    public string UserPwd { get; set; }
}