using Newtonsoft.Json.Linq;
using System;
using System.Reflection;

namespace InstallationAPP2024.Models
{
    public class CustomerProductDetailModel
    {
        public string ProductName { get; set; }
        public string ProductSerialNo { get; set; }
        public string ConsoleSerialNo { get; set; }
        public string InternalSN { get; set; }
        public DateTime? InstallDate { get; set; }
        public DateTime? PartsExpiryDate { get; set; }
        public string AccountProductId { get; set; }
        public string ProductId { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public DateTime? LabourExpiryDate { get; set; }
        public bool CaseExist { get; set; }
        public string ModelNo { get; set; }
        public string AccountName { get; set; }
        public string ProductNumber { get; set; }
        public string DrawingNo { get; set; }
        public string CustomerName { get; set; }
        public string SalesOrderNumber { get; set; }

        public static void TryParse(string content,out CustomerProductDetailModel cpdm){

            cpdm = new CustomerProductDetailModel();
            JObject objContent = new JObject();
            try
            {
                objContent = JObject.Parse(content);
            } catch (Exception e){
                cpdm = new CustomerProductDetailModel();
            }

            if (objContent.Count > 0 )
            {
                JToken tempToken;
                foreach (var property in cpdm.GetType().GetProperties()) {
                    var propertyName = property.Name;
                    
                    
                    objContent.TryGetValue(propertyName, out tempToken);
                    if (tempToken != null)
                    {
                        object[] metadata = new Object[1];
                        bool IsIdentify = false;

                        if (property.PropertyType == typeof(string))
                        {
                            metadata[0] = tempToken.ToString();
                            IsIdentify = true;
                        }
                        if (property.PropertyType == typeof(bool)) {
                            metadata[0] = bool.Parse(tempToken.ToString());
                            IsIdentify = true;
                        }
                        if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?))
                        {
                            metadata[0] = DateTime.Parse(tempToken.ToString());
                            IsIdentify = true;
                        }

                        if (IsIdentify) {
                            cpdm.GetType().InvokeMember(propertyName,
                                BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty,
                                Type.DefaultBinder, cpdm, metadata);
                        }
                    }
                }
            }
        }
    }
}