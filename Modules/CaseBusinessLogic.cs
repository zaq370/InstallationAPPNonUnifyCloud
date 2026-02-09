using MvcPaging;
using Newtonsoft.Json.Linq;
using InstallationAPPNonUnify.Models;
using InstallationAPPNonUnify.Modules;
using InstallationAPPNonUnify.Modules.Filters;
using InstallationAPPNonUnify.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI;
using System.Data.SqlClient;
using Microsoft.Xrm.Sdk;
using System.Text;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;

namespace InstallationAPPNonUnify.Modules
{
    public class CaseBusinessLogic
    {
        public void GetCustomerOrderInformation(CustomerOrderInformationViewModel cphvm, FormsAuthenticationTicketReader fatr)//Stanley
        {
            var detail = new List<ViewCustomerOrderInformationModel>();

            GetRepairRequests(fatr.CrmAccountId, false, cphvm.SalesOrderID).ForEach(item =>
            {
                detail.Add(new ViewCustomerOrderInformationModel(item));
            });

            if (!string.IsNullOrWhiteSpace(cphvm.OrderBy))
            {
                if (detail != null && detail.Count > 1)
                {
                    var unsort = new List<ViewCustomerOrderInformationModel>(detail);
                    if (cphvm.Asc)
                        detail = unsort.OrderBy(item => item.GetType().GetProperty(cphvm.OrderBy).GetValue(item)).ToList();
                    else
                        detail = unsort.OrderByDescending(item => item.GetType().GetProperty(cphvm.OrderBy).GetValue(item)).ToList();
                }
            }

            cphvm.Detail = detail;
        }

        /// <summary>
        /// 取得客戶所有 active 的 CustomerProduct 資料（提供 Product Service History 使用）
        /// </summary>
        /// <param name="pshvm">輸入條件 ViewModel</param>
        /// <param name="fatr">身份驗證資訊</param>
        public void GetCustomerProduct(OrderInformationViewModel pshvm, FormsAuthenticationTicketReader fatr)
        {
            // 判斷是否空值
            if (string.IsNullOrWhiteSpace(pshvm.SalesOrderNumber))
            {
                var result = new { success = false, message = "Please enter the sales order number." };
                var response = HttpContext.Current.Response;
                response.ContentType = "application/json; charset=utf-8";
                response.Write(JsonConvert.SerializeObject(result));
                response.Flush();
                response.End();   // 中止後續流程（很關鍵）
                return;
            }
            pshvm.ProductDetail = new List<CustomerOrderInformationModel>();

            // 組合查詢條件
            var conditions = new JObject();
            if (!string.IsNullOrWhiteSpace(pshvm.SalesOrderNumber))
                conditions["SalesOrderNumber"] = pshvm.SalesOrderNumber;

            //if (!string.IsNullOrWhiteSpace(pshvm.CustomerName))
            //    conditions["CustomerName"] = pshvm.CustomerName;

            // 執行查詢命令
            using (var sqlCommand = GetCustomerProductSqlCommand(fatr, conditions, pshvm.OrderBy, pshvm.Asc))
            using (var sdr = sqlCommand.ExecuteReader())
            {
                var customerProductList = new List<CustomerOrderInformationModel>();

                while (sdr.Read())
                {
                    customerProductList.Add(new CustomerOrderInformationModel
                    {
                        CustomerName = sdr["CustomerName"]?.ToString(),
                        SalesOrderNumber = sdr["new_erporderno"]?.ToString(),
                        SalesOrderId = sdr["SalesOrderId"]?.ToString()
                    });
                }

                pshvm.ProductDetail.AddRange(customerProductList);
            }
        }


        /// <summary>
        /// 取得客戶產品的 SQL 查詢命令
        /// </summary>
        /// <param name="fatr">身份驗證票證讀取器</param>
        /// <param name="condition">查詢條件</param>
        /// <param name="orderby">排序欄位</param>
        /// <param name="asc">是否升序</param>
        /// <returns>SqlCommand 查詢命令</returns>
        public SqlCommand GetCustomerProductSqlCommand(
            FormsAuthenticationTicketReader fatr,
            JObject condition,
            string orderby = "new_erporderno",
            bool asc = true)
        {
            var dbConnection = new DBConnetion().GetConnection();
            if (!dbConnection.Item1)
            {
                throw new Exception("DB Connection Failed!");
            }

            // 初始化 SQL 查詢字串
            StringBuilder queryString = new StringBuilder();

            queryString.AppendLine("WITH SalesOrderRanked AS (");
            queryString.AppendLine("    SELECT");
            queryString.AppendLine("        so.SalesOrderId,");
            queryString.AppendLine("        so.new_erporderno,");
            queryString.AppendLine("        so.customerid,");
            queryString.AppendLine("        acc.Name AS CustomerName,");
            queryString.AppendLine("        so.RequestDeliveryBy AS RequestDeliveryBy,"); // 主表預計交貨日期
            queryString.AppendLine("        sod.RequestDeliveryBy AS DetailRequestDeliveryBy,"); // 明細預計交貨日期
                                                                                                 // 使用 ROW_NUMBER() 對每個 SalesOrderId 進行分區，並按 SalesOrderDetailId 升序排序
                                                                                                 // 這確保每個 SalesOrderId 只會選擇一個 SalesOrderDetail 的行
            queryString.AppendLine("        ROW_NUMBER() OVER (PARTITION BY so.SalesOrderId ORDER BY sod.SalesOrderDetailId ASC) AS rn");
            queryString.AppendLine("    FROM SalesOrder AS so WITH (NOLOCK)"); // 假設 NOLOCK 是您希望保留的
            queryString.AppendLine("    JOIN Account AS acc WITH (NOLOCK) ON acc.AccountId = so.CustomerId"); // 假設 NOLOCK 是您希望保留的
            queryString.AppendLine("    JOIN SalesOrderDetail AS sod WITH (NOLOCK) ON so.SalesOrderId = sod.SalesOrderId"); // 假設 NOLOCK 是您希望保留的
            queryString.AppendLine("    WHERE sod.new_itemstatus <> 3 AND so.new_ShippingStatus <> 3");
            queryString.AppendLine("      AND so.new_OrderType IN (1,2,5,22,23)");
            queryString.AppendLine("      AND so.RequestDeliveryBy IS NOT NULL");
            queryString.AppendLine("      AND sod.RequestDeliveryBy IS NOT NULL");
            queryString.AppendLine(")");
            queryString.AppendLine("SELECT");
            queryString.AppendLine("    SalesOrderId,");
            queryString.AppendLine("    new_erporderno,");
            queryString.AppendLine("    customerid,");
            queryString.AppendLine("    CustomerName,");
            queryString.AppendLine("    RequestDeliveryBy,");
            queryString.AppendLine("    DetailRequestDeliveryBy");
            queryString.AppendLine("FROM SalesOrderRanked");
            queryString.AppendLine("WHERE rn = 1"); // 只選擇每個分區的第一行，從而實現 SalesOrderId 的唯一性
            var command = new SqlCommand { Connection = dbConnection.Item2 };

            // 條件參數處理
            if (condition != null)
            {
                foreach (var prop in condition)
                {
                    var key = prop.Key;
                    var value = prop.Value?.ToString()?.Trim();
                    if (string.IsNullOrEmpty(value)) continue;

                    switch (key)
                    {
                        case "SalesOrderNumber":
                            queryString.AppendLine("AND new_erporderno = @SalesOrderNumber");
                            command.Parameters.AddWithValue("@SalesOrderNumber", value);
                            break;
                        case "CustomerName":
                            queryString.AppendLine("AND CustomerName LIKE @CustomerName");
                            command.Parameters.AddWithValue("@CustomerName", "%" + value + "%");
                            break;
                    }
                }
            }

            // 排序欄位白名單，避免 SQL Injection
            var allowedOrderBy = new[] { "new_erporderno", "SalesOrderId", "CustomerName" };
            if (!allowedOrderBy.Contains(orderby))
            {
                orderby = "new_erporderno"; // fallback
            }

            queryString.AppendLine($"ORDER BY {orderby} {(asc ? "ASC" : "DESC")}");

            command.CommandText = queryString.ToString();
            return command;
        }


        public void GetOrderProductInformation(InstalledProductsViewModel pshvm, FormsAuthenticationTicketReader fatr)
        {
            pshvm.ProductDetail = new List<CustomerOrderInformationModel>();

            var conditions = new JObject();
            if (!string.IsNullOrWhiteSpace(pshvm.SalesOrderNumber))
                conditions["SalesOrderNumber"] = pshvm.SalesOrderNumber;

            if (!string.IsNullOrWhiteSpace(pshvm.CustomerName))
                conditions["CustomerName"] = pshvm.CustomerName;

            if (!string.IsNullOrWhiteSpace(pshvm.ProductNo))
                conditions["ProductNo"] = pshvm.ProductNo;

            using (var sqlCommand = GetOrderProductSqlCommand(fatr, conditions, pshvm.OrderBy, pshvm.Asc))
            {
                sqlCommand.Parameters.AddWithValue("@SalesOrderId", pshvm.SalesOrderID);

                var baseDate = DateTime.Parse(pshvm.RequestDate.ToString("yyyy-MM-dd")).Date;
                sqlCommand.Parameters.AddWithValue("@StartDate", baseDate.AddDays(-1));
                sqlCommand.Parameters.AddWithValue("@EndDate", baseDate.AddDays(1));

                using (var sdr = sqlCommand.ExecuteReader())
                {
                    var customerProductList = new List<CustomerOrderInformationModel>();

                    while (sdr.Read())
                    {
                        customerProductList.Add(new CustomerOrderInformationModel
                        {
                            ProductNo = sdr["ProductNo"]?.ToString(),
                            Product = sdr["ProductName"]?.ToString(),
                            Productguid = sdr["ProductID"]?.ToString(),
                            SerialNo = sdr["SerialNo"]?.ToString(),
                            InstalledQty = Math.Floor(Convert.ToDecimal(sdr["ExpectedQty"])).ToString()
                        });
                    }

                    pshvm.ProductDetail.AddRange(customerProductList);
                }
            }
        }

        public SqlCommand GetOrderProductSqlCommand(
            FormsAuthenticationTicketReader fatr,
            JObject condition,
            string orderby = "ProductNo",
            bool asc = true)
        {
            var dbConnection = new DBConnetion().GetConnection();
            if (!dbConnection.Item1)
                throw new Exception("DB Connection Failed!");

            // 安全排序欄位白名單
            var allowedOrderBy = new[] { "ProductNo", "ProductName", "ExpectedQty" };
            if (!allowedOrderBy.Contains(orderby))
                orderby = "ProductNo";

            var query = new StringBuilder();
            query.AppendLine("SELECT");
            query.AppendLine("  a.ProductId AS ProductID,");
            query.AppendLine("  b.productnumber AS ProductNo,");
            query.AppendLine("  b.name AS ProductName,");
            query.AppendLine("  a.quantity AS ExpectedQty,");
            query.AppendLine("  b.new_ModelNo AS SerialNo"); 
            query.AppendLine("FROM SalesOrderDetail a");
            query.AppendLine("JOIN Product b ON a.ProductId = b.ProductId");
            query.AppendLine("JOIN new_installation ni ON ni.new_Order = a.SalesOrderId");

            // 如果條件用到 OrderNumber 或 CustomerName，加入 JOIN
            bool needsSalesOrderJoin = condition?.ContainsKey("SalesOrderNumber") == true;
            bool needsAccountJoin = condition?.ContainsKey("CustomerName") == true;

            if (needsSalesOrderJoin)
                query.AppendLine("JOIN SalesOrder so ON so.SalesOrderId = a.SalesOrderId");

            if (needsAccountJoin)
                query.AppendLine("JOIN Account acc ON acc.AccountId = so.CustomerId");

            query.AppendLine("WHERE a.new_itemstatus <> '3'");
            query.AppendLine("  AND a.SalesOrderId = @SalesOrderId");
            query.AppendLine("  AND CONVERT(DATE, ni.new_RequestDeliveryBy) >= @StartDate");
            query.AppendLine("  AND CONVERT(DATE, ni.new_RequestDeliveryBy) < @EndDate");
            query.AppendLine("  AND NOT EXISTS (");
            query.AppendLine("    SELECT 1 FROM new_installationdetail nid");
            query.AppendLine("    WHERE nid.new_orderproductid = a.ProductId");
            query.AppendLine("      AND nid.new_installationid = ni.new_installationid");
            query.AppendLine(")");

            var command = new SqlCommand { Connection = dbConnection.Item2 };

            // 條件處理
            if (condition != null)
            {
                foreach (var prop in condition)
                {
                    string key = prop.Key;
                    string value = prop.Value?.ToString()?.Trim();
                    if (string.IsNullOrEmpty(value)) continue;

                    switch (key)
                    {
                        case "SalesOrderNumber":
                            query.AppendLine("AND so.new_erporderno = @SalesOrderNumber");
                            command.Parameters.AddWithValue("@SalesOrderNumber", value);
                            break;
                        case "CustomerName":
                            query.AppendLine("AND acc.Name LIKE @CustomerName");
                            command.Parameters.AddWithValue("@CustomerName", "%" + value + "%");
                            break;
                        case "ProductNo":
                            query.AppendLine("AND b.productnumber LIKE @ProductNo");
                            command.Parameters.AddWithValue("@ProductNo", "%" + value + "%");
                            break;
                    }
                }
            }

            // 排序
            query.AppendLine($"ORDER BY {orderby} {(asc ? "ASC" : "DESC")}");

            command.CommandText = query.ToString();
            return command;
        }


        /// <summary>
        /// 取得OrderProduct
        /// </summary>
        /// <param name="pshvm"></param>
        /// <param name="fatr"></param>
        public void GetOrderProductInformation2(InstalledProductsViewModel pshvm, FormsAuthenticationTicketReader fatr)
        {
            pshvm.ProductDetail = new List<CustomerOrderInformationModel>();

            // 使用靜態屬性 DefaultPageSize
            //var offset = (pshvm.Page - 1) * InstalledProductsViewModel.DefaultPageSizeIn;

            // 組條件
            var conditions = new JObject();
            if (!string.IsNullOrWhiteSpace(pshvm.SalesOrderNumber))
                conditions.Add(new JProperty("SalesOrderNumber", pshvm.SalesOrderNumber));

            if (!string.IsNullOrWhiteSpace(pshvm.CustomerName))
                conditions.Add(new JProperty("CustomerName", pshvm.CustomerName));

            if (!string.IsNullOrWhiteSpace(pshvm.ProductNo))
                conditions.Add(new JProperty("ProductNo", pshvm.ProductNo));

            // 取得總數
            //using (var totalCountCommand = GetTotalItemCountSqlCommand(fatr, conditions))
            //{
            //    totalCountCommand.Parameters.AddWithValue("@SalesOrderId", pshvm.SalesOrderID);
            //    totalCountCommand.Parameters.AddWithValue("@RequestDate", pshvm.RequestDate.ToString("yyyy-MM-dd"));

            //    int totalCount = (int)totalCountCommand.ExecuteScalar();
            //    pshvm.TotalItems = totalCount;  // 設置總項目數

            //    // 設置總頁數 (可選)
            //    pshvm.TotalPages = (int)Math.Ceiling((double)totalCount / InstalledProductsViewModel.DefaultPageSizeIn);
            //}

            // 取得 SQL 查詢指令
            using (var sqlCommand = GetOrderProductSqlCommand2(fatr, conditions, pshvm.OrderBy, pshvm.Asc))
            {
                // 新增偏移量與限制條件
                sqlCommand.Parameters.AddWithValue("@SalesOrderId", pshvm.SalesOrderID);
                sqlCommand.Parameters.AddWithValue("@RequestDate", pshvm.RequestDate.ToString("yyyy-MM-dd"));
                //sqlCommand.Parameters.AddWithValue("@Offset", offset);
                //sqlCommand.Parameters.AddWithValue("@Limit", InstalledProductsViewModel.DefaultPageSizeIn);

                using (var sdr = sqlCommand.ExecuteReader())
                {
                    var customerProductList = new List<CustomerOrderInformationModel>();

                    while (sdr.Read())
                    {
                        var cphm = new CustomerOrderInformationModel
                        {
                            ProductNo = sdr["ProductNo"].ToString(),
                            Product = sdr["ProductName"].ToString(),
                            Productguid = sdr["ProductID"].ToString(),
                            InstalledQty = Math.Floor(Convert.ToDecimal(sdr["ExpectedQty"])).ToString()
                        };

                        customerProductList.Add(cphm);
                    }

                    pshvm.ProductDetail.AddRange(customerProductList);
                }
            }
        }

        public SqlCommand GetOrderProductSqlCommand2(FormsAuthenticationTicketReader fatr, JObject condition, string orderby = "ProductNo", bool asc = true)
        {
            var dbConnection = new DBConnetion().GetConnection();
            if (!dbConnection.Item1)
            {
                throw new Exception("DB Connection Failed!");
            }

            // 初始化 SQL 查詢字串
            StringBuilder queryString = new StringBuilder();
            queryString.Append("SELECT a.ProductId AS ProductID ,b.productnumber AS ProductNo, ProductIdName AS ProductName, a.quantity AS ExpectedQty ");
            queryString.Append("FROM SalesOrderDetail a ");
            queryString.Append("JOIN Product b ON a.ProductId = b.ProductId ");
            queryString.Append("JOIN new_installation ni ON ni.new_Order = a.SalesOrderId ");
            queryString.Append("WHERE a.new_itemstatus <> '3' ");
            queryString.Append("AND a.SalesOrderId = @SalesOrderId ");
            //queryString.Append("AND ni.new_iscompleted = 1 ");
            queryString.Append("AND CONVERT(DATE, a.RequestDeliveryBy) = @RequestDate");

            SqlCommand command = new SqlCommand();
            command.Connection = dbConnection.Item2;

            // 條件拼接
            if (condition != null && condition.Count > 0)
            {
                foreach (var property in condition)
                {
                    var propertyValue = property.Value.ToString().Trim();
                    if (string.IsNullOrEmpty(propertyValue)) continue;

                    switch (property.Key.ToString())
                    {
                        case "SalesOrderNumber":
                            queryString.Append(" AND so.new_erporderno = @SalesOrderNumber ");
                            command.Parameters.AddWithValue("@SalesOrderNumber", propertyValue);
                            break;
                        case "CustomerName":
                            queryString.Append(" AND acc.Name LIKE @CustomerName ");
                            command.Parameters.AddWithValue("@CustomerName", "%" + propertyValue + "%");
                            break;
                        case "ProductNo":
                            queryString.Append(" AND b.productnumber LIKE @ProductNo ");
                            command.Parameters.AddWithValue("@ProductNo", "%" + propertyValue + "%");
                            break;
                    }
                }
            }

            // 排序設定
            if (string.IsNullOrWhiteSpace(orderby))
            {
                orderby = "ProductNo"; // 默認排序欄位
            }

            // 排序條件拼接 (升序或降序)
            queryString.Append(" ORDER BY " + orderby + (asc ? " ASC" : " DESC"));

            // 分頁：使用 OFFSET 和 LIMIT
            //queryString.Append(" OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY");

            command.CommandText = queryString.ToString();

            return command;
        }

        public SqlCommand GetTotalItemCountSqlCommand(FormsAuthenticationTicketReader fatr, JObject condition)
        {
            var dbConnection = new DBConnetion().GetConnection();
            if (!dbConnection.Item1)
            {
                throw new Exception("DB Connection Failed!");
            }

            // 計算總數的查詢
            StringBuilder countQueryString = new StringBuilder();
            countQueryString.Append("SELECT COUNT(*) ");
            countQueryString.Append("FROM SalesOrderDetail a ");
            countQueryString.Append("JOIN Product b ON a.ProductId = b.ProductId ");
            countQueryString.Append("WHERE a.new_itemstatus <> '3' ");
            countQueryString.Append("AND a.SalesOrderId = @SalesOrderId ");
            countQueryString.Append("AND CONVERT(DATE, a.RequestDeliveryBy) = @RequestDate");

            SqlCommand countCommand = new SqlCommand();
            countCommand.Connection = dbConnection.Item2;

            // 條件拼接
            if (condition != null && condition.Count > 0)
            {
                foreach (var property in condition)
                {
                    var propertyValue = property.Value.ToString().Trim();
                    if (string.IsNullOrEmpty(propertyValue)) continue;

                    switch (property.Key.ToString())
                    {
                        case "SalesOrderNumber":
                            countQueryString.Append(" AND so.new_erporderno = @SalesOrderNumber ");
                            break;
                        case "CustomerName":
                            countQueryString.Append(" AND acc.Name LIKE @CustomerName ");
                            break;
                        case "ProductNo":
                            countQueryString.Append(" AND b.productnumber LIKE @ProductNo ");
                            break;
                    }
                }
            }

            countCommand.CommandText = countQueryString.ToString();

            return countCommand;
        }

        public void GetRepairReqeustViewData(RepairRequestViewModel rrvm, FormsAuthenticationTicketReader fatr)//Stanley
        {
            var detail = new List<RepairRequestDetailModel>();

            GetRepairRequests(fatr.CrmAccountId, true, rrvm.SalesOrderId).ForEach(item =>
            {
                detail.Add(new RepairRequestDetailModel(item));
            });

            if (!string.IsNullOrWhiteSpace(rrvm.OrderBy))
            {
                var unsort = new List<RepairRequestDetailModel>(detail);
                if (rrvm.Asc)
                    detail = unsort.OrderBy(item => item.GetType().GetProperty(rrvm.OrderBy).GetValue(item)).ToList();
                else
                    detail = unsort.OrderByDescending(item => item.GetType().GetProperty(rrvm.OrderBy).GetValue(item)).ToList();
            }

            rrvm.Detail = detail;
        }

        /// <summary>
        /// 抓取Case Product History，如果statusFitler=false，只顯示Case Solved的資料，//Stanley
        /// 如果statusFitler=true，只要不是 Inactive,Case Solved,Cancelled,Completed 都要顯示。
        /// 以上條件會依國家別會有不同設定。
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="statusFilter">如果statusFitler=false，只顯示Case Solved的資料，如果statusFitler=true，只要不是 Inactive,Case Solved,Cancelled,Completed 都要顯示。以上條件會依國家別會有不同設定。</param>
        /// <param name="customerProductId"></param>
        /// <returns></returns>
        public List<CaseHistoryModel> GetRepairRequests(string accountId, bool statusFilter = true, string SalesOrderID = "")
        {
            var result = new List<CaseHistoryModel>();

            var dbConnection = new DBConnetion().GetConnection();
            if (!dbConnection.Item1)
            {
                throw new Exception("DB Connection Failed!");
            }

            BackendInfo bi = new BackendInfo();
            var org = bi.GetSetting("Organization");

            // 合併查詢語句
            string queryString = @"
            SELECT sod.RequestDeliveryBy, so.new_erporderno, sod.SalesOrderId 
            FROM SalesOrderDetail sod
            JOIN SalesOrder so ON sod.SalesOrderId = so.SalesOrderId
            WHERE sod.new_itemstatus <> 3 AND so.new_ShippingStatus <> 3";

            if (!string.IsNullOrWhiteSpace(SalesOrderID))
            {
                queryString += " AND sod.SalesOrderId = @SalesOrderId group by sod.RequestDeliveryBy, so.new_erporderno, sod.SalesOrderId";
            }

            // 使用 using 確保連線資源被正確釋放
            using (var command = new SqlCommand(queryString, dbConnection.Item2))
            {
                if (!string.IsNullOrWhiteSpace(SalesOrderID))
                {
                    command.Parameters.AddWithValue("@SalesOrderId", SalesOrderID);
                }

                using (SqlDataReader sdr = command.ExecuteReader())
                {
                    while (sdr.Read())
                    {
                        var chm = new CaseHistoryModel();

                        // 讀取 RequestDeliveryBy 並格式化日期
                        DateTime requestDeliveryBy = Convert.ToDateTime(sdr.GetValue(sdr.GetOrdinal("RequestDeliveryBy")));
                        chm.RequestDeliveryBy = requestDeliveryBy.ToString("yyyy-MM-dd");

                        // 讀取 OrderNumber
                        chm.SalesOrderNumber = sdr.GetValue(sdr.GetOrdinal("new_erporderno")).ToString();
                        chm.SalesOrderID = sdr.GetValue(sdr.GetOrdinal("SalesOrderId")).ToString();
                        result.Add(chm);
                    }
                }
            }

            dbConnection.Item2.Dispose(); // 確保連線資源釋放
            return result;
        }

        public Tuple<bool, string> ChangeUserPassword(ChangePasswordViewModel cpvm, FormsAuthenticationTicketReader fatr)
        {
            LanguagePackage lp = new LanguagePackage(cpvm.CountryCode);
            CRMManipulate cmm = new CRMManipulate();
            BackendInfo bi = new BackendInfo();
            var newportalpasswordencrypted = bi.IsEncryptPassword ? true : false;

            try
            {
                if (cmm.IsInService())
                {
                    //先取 account
                    Guid accountId;
                    Guid.TryParse(fatr.CrmAccountId, out accountId);
                    if (accountId != null)
                    {
                        var account = cmm.GetService().Retrieve("account", accountId, new ColumnSet("name", "new_portalpassword", "new_portalpasswordencrypted"));
                        var originalPassword = string.Empty;

                        if (account.Attributes.ContainsKey("new_portalpassword"))
                        {
                            originalPassword = account["new_portalpassword"].ToString();
                        }

                        //如果有加密，要先解密
                        if (account.Attributes.ContainsKey("new_portalpasswordencrypted"))
                        {
                            if (account["new_portalpasswordencrypted"].ToString().ToLower().Equals("true"))
                            {
                                originalPassword = PasswordEncryption.Decrypt(originalPassword);
                            }
                        }

                        //先檢查新舊密碼不能相同
                        if (!String.IsNullOrEmpty(originalPassword) && !String.IsNullOrWhiteSpace(originalPassword))
                        {
                            if (originalPassword.Equals(cpvm.NewPassword))
                            {
                                return new Tuple<bool, string>(false, lp.getContentWithNoPrefix("PasswordIsTheSame"));
                            }
                        }
                        account["new_portalpassword"] = newportalpasswordencrypted ? PasswordEncryption.Encrypt(cpvm.NewPassword) : cpvm.NewPassword;
                        account["new_portalpasswordencrypted"] = newportalpasswordencrypted;

                        cmm.GetService().Update(account);
                    }
                }
                else
                {
                    Elmah.ErrorSignal.FromCurrentContext().Raise(new Exception("CRM Connection Failed"));
                    return new Tuple<bool, string>(false, lp.getContentWithNoPrefix("PasswordUpdateFail"));
                }
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return new Tuple<bool, string>(false, lp.getContentWithNoPrefix("PasswordUpdateFail"));
            }

            return new Tuple<bool, string>(true, "");
        }

        public EmailUsViewModel CreateMailContent(FormsAuthenticationTicketReader fatr)
        {
            EmailUsViewModel euvm = new EmailUsViewModel();
            var dbConnection = new DBConnetion().GetConnection();
            if (!dbConnection.Item1)
            {
                return euvm;
            }

            string queryAccount =
                "select accountid, name, address1_line1, address1_name, telephone1, emailaddress1, address1_postalcode"
                + ",emailaddress2 "
                + " from account where accountid = @Accountid";
            SqlCommand command = new SqlCommand(queryAccount, dbConnection.Item2);
            command.Parameters.AddWithValue("@Accountid", fatr.CrmAccountId);

            using (SqlDataReader sdr = command.ExecuteReader())
            {
                if (sdr.HasRows)
                {
                    sdr.Read();
                    euvm.Company = sdr.GetValue(sdr.GetOrdinal("name")).ToString();
                    euvm.TelephoneNumber = sdr.GetValue(sdr.GetOrdinal("telephone1")).ToString();
                    euvm.EmailAddress = sdr.GetValue(sdr.GetOrdinal("emailaddress1")).ToString();
                    euvm.Address = sdr.GetValue(sdr.GetOrdinal("address1_line1")).ToString();
                    euvm.PostCode = sdr.GetValue(sdr.GetOrdinal("address1_postalcode")).ToString();

                    //荷蘭，以EmailAddress以emailaddress12為主，emailaddress1為輔
                    if (euvm.Organization.ToLower().Equals("jhtnl"))
                    {
                        var mainAddress = sdr.GetValue(sdr.GetOrdinal("emailaddress2")).ToString();
                        if (!string.IsNullOrEmpty(mainAddress) && !string.IsNullOrWhiteSpace(mainAddress))
                            euvm.EmailAddress = mainAddress;
                    }
                }
            }
            dbConnection.Item2.Dispose();
            return euvm;
        }

        public string GenerateInstallationNumber(string countryAbbreviation)
        {
            string prefix = String.Format("{0}INS{1:yyMM}", countryAbbreviation, DateTime.Now);
            string newInstallationNumber;

            // SQL 查詢最大流水號
            string query = @"
    SELECT MAX(CAST(SUBSTRING([new_name], 10, 4) AS INT)) AS MaxSequence
    FROM [new_installationBase]
    WHERE [new_name] LIKE @Prefix";

            int maxSequence = 0;

            try
            {
                using (var connection = GetDbConnection())
                {
                    //connection.Open(); // 確保連線開啟
                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Prefix", String.Format("{0}%", prefix));

                        var result = cmd.ExecuteScalar();
                        if (result != DBNull.Value && result != null)
                        {
                            maxSequence = Convert.ToInt32(result);
                        }
                    }
                }

                // 計算新的流水號
                int newSequence = maxSequence + 1;
                string sequenceString = newSequence.ToString("D4");

                // 組合完整的 Installation Number
                newInstallationNumber = String.Format("{0}{1}", prefix, sequenceString);
            }
            catch (Exception ex)
            {
                // 捕捉異常並記錄

                throw new Exception("Error generating installation number.", ex);
            }

            return newInstallationNumber;
        }
        /// <summary>
        /// 更新 Installation 資料，並附加簽名圖檔（完成安裝）
        /// </summary>
        /// <param name="fatr">身份驗證資訊</param>
        /// <param name="installationId">安裝單 ID</param>
        /// <param name="model">輸入資料</param>
        /// <returns>是否成功</returns>
        public bool UpdateInstallationComplete(FormsAuthenticationTicketReader fatr, string installationId, InstallationViewModel model)
        {
            if (string.IsNullOrEmpty(installationId) || !Guid.TryParse(installationId, out Guid installationGuid))
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(new Exception("installationId 無效"));
                return false;
            }

            try
            {
                var service = CRMService.Service as IOrganizationService;
                if (service == null)
                {
                    throw new Exception("CRMService.Service 未初始化，請先建立連線。");
                }

                // 讀取現有 Installation
                var existingInstallation = service.Retrieve("new_installation", installationGuid, new ColumnSet("new_installationteam", "new_transportteam"));
                if (existingInstallation == null)
                {
                    Elmah.ErrorSignal.FromCurrentContext().Raise(new Exception($"找不到 Installation 記錄，ID：{installationId}"));
                    return false;
                }

                // 更新欄位
                var updateInstallation = new Entity("new_installation")
                {
                    Id = installationGuid,
                    ["new_installer"] = model.Installer,
                    ["new_clubmanager"] = model.ClubManager,
                    ["new_dateofsignature"] = DateTime.TryParse(model.SignatureDate, out DateTime signDate) ? signDate.Date : DateTime.Now.Date,
                    ["modifiedon"] = DateTime.Now,
                    ["new_iscompleted"] = true,
                    ["modifiedby"] = new EntityReference("systemuser", Guid.Parse(fatr.CrmAccountId))
                };

                service.Update(updateInstallation);

                // 上傳簽名圖片
                bool installerSuccess = TryUploadSignature(service, installationGuid, model.InstallerSignatureimg, "InstallerSignatureImg");
                bool managerSuccess = TryUploadSignature(service, installationGuid, model.ClubManagerSignatureimg, "ClubManagerSignatureImg");

                return installerSuccess && managerSuccess;
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return false;
            }
        }

        private bool TryUploadSignature(IOrganizationService service, Guid installationGuid, string base64Img, string filePrefix)
        {
            if (string.IsNullOrWhiteSpace(base64Img)) return true;

            try
            {
                if (base64Img.StartsWith("data:image/png;base64,"))
                {
                    base64Img = base64Img.Replace("data:image/png;base64,", "");
                }

                var annotation = new Entity("annotation")
                {
                    ["documentbody"] = base64Img,
                    ["filename"] = $"{filePrefix}_{installationGuid}.png",
                    ["isdocument"] = true,
                    ["objectid"] = new EntityReference("new_installation", installationGuid),
                    ["subject"] = $"signature(installation_{filePrefix})"
                };

                var id = service.Create(annotation);
                return id != Guid.Empty;
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(new Exception($"上傳簽名圖片失敗 ({filePrefix})", ex));
                return false;
            }
        }



        /// <summary>
        /// 更新 Installation 的 team 與交期資訊
        /// </summary>
        /// <param name="fatr">登入者資訊</param>
        /// <param name="installationId">安裝單 ID</param>
        /// <param name="requestDeliveryBy">預計交貨日（字串格式）</param>
        /// <param name="installationteamname">安裝團隊（GUID 字串）</param>
        /// <param name="transportteamname">運送團隊（OptionSet 整數字串）</param>
        /// <returns>是否更新成功</returns>
        public bool UpdateInstallation(
            FormsAuthenticationTicketReader fatr,
            string installationId,
            string requestDeliveryBy,
            string installationteamname,
            string transportteamname)
        {
            // 驗證 installationId
            if (string.IsNullOrWhiteSpace(installationId) || !Guid.TryParse(installationId, out Guid installationGuid))
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(new Exception("installationId 無效"));
                return false;
            }

            // 驗證安裝團隊 GUID
            if (string.IsNullOrWhiteSpace(installationteamname) || !Guid.TryParse(installationteamname, out Guid installationTeamGuid))
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(new Exception("installationteamname 不是有效的 GUID"));
                return false;
            }
            var installationTeamRef = new EntityReference("equipment", installationTeamGuid);

            // 驗證運送團隊 OptionSet
            if (string.IsNullOrWhiteSpace(transportteamname) || !int.TryParse(transportteamname, out int transportTeamId))
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(new Exception("transportteamname 不是有效的 int"));
                return false;
            }
            var transportTeamOption = new OptionSetValue(transportTeamId);

            // 驗證交期
            if (!DateTime.TryParse(requestDeliveryBy, out DateTime deliveryDate))
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(new Exception("requestDeliveryBy 不是有效的日期"));
                return false;
            }

            try
            {
                var service = CRMService.Service as IOrganizationService;
                if (service == null)
                {
                    throw new Exception("CRMService.Service 未初始化，請先建立連線。");
                }

                // 查詢原始資料
                var existingInstallation = service.Retrieve("new_installation", installationGuid, new ColumnSet("new_installationteam", "new_transportteam"));
                if (existingInstallation == null)
                {
                    Elmah.ErrorSignal.FromCurrentContext().Raise(new Exception("找不到 Installation 記錄，ID：" + installationId));
                    return false;
                }

                // 建立更新資料
                var updateInstallation = new Entity("new_installation")
                {
                    Id = installationGuid,
                    ["new_installationteam"] = installationTeamRef,
                    ["new_transportteam"] = transportTeamOption,
                    ["new_requestdeliveryby"] = deliveryDate,
                    ["modifiedon"] = DateTime.Now,
                    ["modifiedby"] = new EntityReference("systemuser", Guid.Parse(fatr.CrmAccountId))
                };

                service.Update(updateInstallation);
                return true;
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return false;
            }
        }

        public Tuple<bool, Guid> InsertNewInstallation(InstallationViewModel nscvm, FormsAuthenticationTicketReader fatr, string salesOrderId, string RequestDeliveryBy)
        {
            bool isSuccessed = true;
            Guid newInstallationNumber = Guid.Empty;

            // 檢查必要資料是否缺失
            if (string.IsNullOrEmpty(salesOrderId))
            {
                isSuccessed = false;
                Elmah.ErrorSignal.FromCurrentContext().Raise(new Exception("Required data is missing: countryAbbreviation, salesOrderId, or equipment"));
                return new Tuple<bool, Guid>(false, Guid.Empty);
            }

            try
            {
                // 建立 Installation 實體
                var InstallationEntity = new Entity("new_installation");
                //建立CRM操作類別
                CRMManipulate CRMManiplute = new CRMManipulate();


                // 設定實體屬性
                InstallationEntity["new_name"] = GenerateInstallationNumber(nscvm.CountryCode2); // 安裝單號
                InstallationEntity["new_order"] = new EntityReference("salesorder", new Guid(salesOrderId)); // 銷售訂單參考
                InstallationEntity["new_installationteam"] = null;
                InstallationEntity["new_transportteam"] = null;
                //InstallationEntity["new_installer"] = "";  // 安裝人員
                InstallationEntity["createdon"] = DateTime.Now; // 建立日期
                InstallationEntity["createdby"] = new EntityReference("systemuser", Guid.Parse(fatr.CrmAccountId)); // 建立人員
                InstallationEntity["statecode"] = new OptionSetValue(0); // 狀態
                InstallationEntity["statuscode"] = new OptionSetValue(1); // 狀態碼
                InstallationEntity["new_iscompleted"] = false; // 將 iscompleted 設置為 No
                InstallationEntity["new_requestdeliveryby"] = Convert.ToDateTime(RequestDeliveryBy).Date;
                // 使用 SDK 創建記錄
                newInstallationNumber = CRMManiplute.Create(InstallationEntity);

                if (newInstallationNumber == null || newInstallationNumber == Guid.Empty)
                {
                    isSuccessed = false;
                    new Exception("Create  Failed");
                }
                //CustomerOrderInformationViewModel cusViewModel = new CustomerOrderInformationViewModel();
                //cusViewModel.NewInstallationId = newInstallationNumber.ToString();
                //newInstallationNumber = InstallationEntity.Id; // 取得創建成功的記錄 ID
            }
            catch (ArgumentException ex)
            {
                // 記錄具體錯誤並返回失敗
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);

                isSuccessed = false;
            }
            catch (Exception ex)
            {
                // 捕捉其他異常並返回失敗
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                isSuccessed = false;
            }

            // 返回操作是否成功及安裝 ID
            return new Tuple<bool, Guid>(isSuccessed, newInstallationNumber);
        }

        public Tuple<bool, Guid> InsertNewInstallationIssue(ReportIssueVieModel nscvm, FormsAuthenticationTicketReader fatr, string salesOrderId, string InstallationId)
        {
            bool isSuccessed = true;
            Guid newInstallationDetailNumber = Guid.Empty;

            // 檢查必要資料是否缺失
            if (string.IsNullOrEmpty(salesOrderId))
            {
                isSuccessed = false;
                Elmah.ErrorSignal.FromCurrentContext().Raise(new Exception("Required data is missing: countryAbbreviation, salesOrderId, or equipment"));
                return new Tuple<bool, Guid>(false, Guid.Empty);
            }

            try
            {
                // 建立 InstallationDetail 實體
                var InstallationDetailEntity = new Entity("new_installationdetail");
                //建立CRM操作類別
                CRMManipulate CRMManiplute = new CRMManipulate();


                // 設定實體屬性
                InstallationDetailEntity["new_name"] = nscvm.TitleName;
                InstallationDetailEntity["new_installationid"] = new EntityReference("new_installation", Guid.Parse(InstallationId));
                InstallationDetailEntity["new_quantityinstalled"] = int.Parse(nscvm.DeliveredAmount);
                InstallationDetailEntity["new_remark"] = nscvm.Notes;
                InstallationDetailEntity["new_reportissue"] = new OptionSetValue(int.Parse(nscvm.IssueType));
                InstallationDetailEntity["createdon"] = DateTime.Now; // 建立日期
                InstallationDetailEntity["createdby"] = new EntityReference("systemuser", Guid.Parse(fatr.CrmAccountId)); // 建立人員
                InstallationDetailEntity["statecode"] = new OptionSetValue(0); // 狀態
                InstallationDetailEntity["statuscode"] = new OptionSetValue(1); // 狀態碼
                InstallationDetailEntity["new_orderproductid"] = nscvm.ProductId;

                // 使用 SDK 創建記錄
                newInstallationDetailNumber = CRMManiplute.Create(InstallationDetailEntity);

                if (newInstallationDetailNumber == null || newInstallationDetailNumber == Guid.Empty)
                {
                    isSuccessed = false;
                    new Exception("Create  Failed");
                }

                //Attachments
                if (nscvm.ImageBase64 != null && nscvm.ImageBase64.Length > 0)
                {
                    string base64Data = nscvm.ImageBase64;
                    if (base64Data.StartsWith("data:image/png;base64,"))
                    {
                        base64Data = base64Data.Replace("data:image/png;base64,", "");
                    }

                    var Annotation = new Entity("annotation");

                    Annotation["documentbody"] = base64Data;
                    Annotation["filename"] = "InstallationDetailImg" + newInstallationDetailNumber + ".png";
                    Annotation["isdocument"] = true;
                    Annotation["objectid"] = new EntityReference("new_installationdetail", newInstallationDetailNumber);
                    Annotation["subject"] = "InstallationDetail(installation_InstallationDetailImg)";

                    var AnnotationId = CRMManiplute.Create(Annotation);
                    if (AnnotationId == null || AnnotationId == Guid.Empty)
                    {
                        isSuccessed = false;
                        new Exception("Create Annotation Failed");
                    }
                    //}
                }
                //CustomerOrderInformationViewModel cusViewModel = new CustomerOrderInformationViewModel();
                //cusViewModel.NewInstallationId = newInstallationNumber.ToString();
                //newInstallationNumber = InstallationEntity.Id; // 取得創建成功的記錄 ID
            }
            catch (ArgumentException ex)
            {
                // 記錄具體錯誤並返回失敗
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);

                isSuccessed = false;
            }
            catch (Exception ex)
            {
                // 捕捉其他異常並返回失敗
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                isSuccessed = false;
            }

            // 返回操作是否成功及安裝 ID
            return new Tuple<bool, Guid>(isSuccessed, newInstallationDetailNumber);
        }
        public Tuple<bool, List<Guid>> InsertAllInstallationDetailsFromOrder(string salesOrderId, string installationId, string requestDate, FormsAuthenticationTicketReader fatr)
        {
            bool isSuccessed = true;
            List<Guid> createdDetailIds = new List<Guid>();
            if (string.IsNullOrEmpty(salesOrderId) || string.IsNullOrEmpty(installationId) || fatr == null)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(new Exception("Required data is missing: salesOrderId, installationId, or fatr"));
                return new Tuple<bool, List<Guid>>(false, createdDetailIds);
            }

            try
            {
                // 準備 SQL 查詢字串
                StringBuilder queryString = new StringBuilder();
                queryString.Append("SELECT a.ProductId AS ProductID, b.productnumber AS ProductNo, ProductIdName AS ProductName, a.quantity AS ExpectedQty ");
                queryString.Append("FROM SalesOrderDetail a ");
                queryString.Append("JOIN Product b ON a.ProductId = b.ProductId ");
                queryString.Append("JOIN new_installation ni ON ni.new_Order = a.SalesOrderId ");
                queryString.Append("WHERE a.new_itemstatus <> '3' ");
                queryString.Append("AND a.SalesOrderId = @SalesOrderId ");
                queryString.Append("AND CONVERT(DATE, a.RequestDeliveryBy) = @RequestDate ");
                queryString.Append("AND NOT EXISTS ( ");
                queryString.Append("    SELECT 1 ");
                queryString.Append("    FROM new_installationdetail nid ");
                queryString.Append("    WHERE nid.new_orderproductid LIKE '%' + CAST(a.ProductId AS NVARCHAR(50)) + '%' ");
                queryString.Append("    AND nid.new_installationid = ni.new_installationid ");
                queryString.Append(")");

                CaseBusinessLogic cbl = new CaseBusinessLogic();
                // 連接資料庫
                using (var connection = cbl.GetDbConnection())
                {
                    SqlCommand cmd = new SqlCommand(queryString.ToString(), connection);
                    cmd.Parameters.AddWithValue("@SalesOrderId", salesOrderId);
                    cmd.Parameters.AddWithValue("@RequestDate", requestDate);
                    //conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        CRMManipulate CRMManiplute = new CRMManipulate();

                        while (reader.Read())
                        {
                            var InstallationDetailEntity = new Entity("new_installationdetail");
                            InstallationDetailEntity["new_name"] = reader["ProductName"].ToString();
                            InstallationDetailEntity["new_installationid"] = new EntityReference("new_installation", Guid.Parse(installationId));
                            InstallationDetailEntity["new_quantityinstalled"] = Convert.ToInt32(reader["ExpectedQty"]);
                            InstallationDetailEntity["new_remark"] = "No Issue"; // 如有備註可補上
                            InstallationDetailEntity["new_reportissue"] = new OptionSetValue(1); // 預設為問題類型1
                            InstallationDetailEntity["createdon"] = DateTime.Now;
                            InstallationDetailEntity["createdby"] = new EntityReference("systemuser", Guid.Parse(fatr.CrmAccountId));
                            InstallationDetailEntity["statecode"] = new OptionSetValue(0);
                            InstallationDetailEntity["statuscode"] = new OptionSetValue(1);
                            InstallationDetailEntity["new_orderproductid"] = reader["ProductID"].ToString();

                            Guid resultId = CRMManiplute.Create(InstallationDetailEntity);
                            if (resultId == Guid.Empty)
                            {
                                isSuccessed = false;
                                Elmah.ErrorSignal.FromCurrentContext().Raise(new Exception("Create Failed for ProductID: " + reader["ProductID"].ToString()));
                            }
                            else
                            {
                                createdDetailIds.Add(resultId);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                isSuccessed = false;
            }

            return new Tuple<bool, List<Guid>>(isSuccessed, createdDetailIds);
        }
        public Tuple<bool, List<Guid>> InsertInstallationDetailsNoIssueFromOrder(string salesOrderId, string installationId, string requestDate, string productNo, FormsAuthenticationTicketReader fatr)
        {
            bool isSuccessed = true;
            List<Guid> createdDetailIds = new List<Guid>();
            if (string.IsNullOrEmpty(salesOrderId) || string.IsNullOrEmpty(installationId) || string.IsNullOrEmpty(productNo) || fatr == null)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(new Exception("Required data is missing: salesOrderId, productNo, or fatr"));
                return new Tuple<bool, List<Guid>>(false, createdDetailIds);
            }

            try
            {
                // 準備 SQL 查詢字串
                StringBuilder queryString = new StringBuilder();
                queryString.Append("SELECT a.ProductId AS ProductID, b.productnumber AS ProductNo, ProductIdName AS ProductName, a.quantity AS ExpectedQty ");
                queryString.Append("FROM SalesOrderDetail a ");
                queryString.Append("JOIN Product b ON a.ProductId = b.ProductId ");
                queryString.Append("JOIN new_installation ni ON ni.new_Order = a.SalesOrderId ");
                queryString.Append("WHERE a.new_itemstatus <> '3' ");
                queryString.Append("AND a.SalesOrderId = @SalesOrderId ");
                queryString.Append("AND CONVERT(DATE, a.RequestDeliveryBy) = @RequestDate ");
                queryString.Append("AND b.productnumber = @ProductNo ");
                queryString.Append("AND NOT EXISTS ( ");
                queryString.Append("    SELECT 1 ");
                queryString.Append("    FROM new_installationdetail nid ");
                queryString.Append("    WHERE nid.new_orderproductid LIKE '%' + CAST(a.ProductId AS NVARCHAR(50)) + '%' ");
                queryString.Append("    AND nid.new_installationid = ni.new_installationid ");
                queryString.Append(")");

                CaseBusinessLogic cbl = new CaseBusinessLogic();
                // 連接資料庫
                using (var connection = cbl.GetDbConnection())
                {
                    SqlCommand cmd = new SqlCommand(queryString.ToString(), connection);
                    cmd.Parameters.AddWithValue("@SalesOrderId", salesOrderId);
                    cmd.Parameters.AddWithValue("@RequestDate", requestDate);
                    cmd.Parameters.AddWithValue("@ProductNo", productNo);
                    //conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        CRMManipulate CRMManiplute = new CRMManipulate();

                        while (reader.Read())
                        {
                            var InstallationDetailEntity = new Entity("new_installationdetail");
                            InstallationDetailEntity["new_name"] = reader["ProductName"].ToString();
                            InstallationDetailEntity["new_installationid"] = new EntityReference("new_installation", Guid.Parse(installationId));
                            InstallationDetailEntity["new_quantityinstalled"] = Convert.ToInt32(reader["ExpectedQty"]);
                            InstallationDetailEntity["new_remark"] = "No Issue"; // 如有備註可補上
                            InstallationDetailEntity["new_reportissue"] = new OptionSetValue(1); // 預設為問題類型1
                            InstallationDetailEntity["createdon"] = DateTime.Now;
                            InstallationDetailEntity["createdby"] = new EntityReference("systemuser", Guid.Parse(fatr.CrmAccountId));
                            InstallationDetailEntity["statecode"] = new OptionSetValue(0);
                            InstallationDetailEntity["statuscode"] = new OptionSetValue(1);
                            InstallationDetailEntity["new_orderproductid"] = reader["ProductID"].ToString();

                            Guid resultId = CRMManiplute.Create(InstallationDetailEntity);
                            if (resultId == Guid.Empty)
                            {
                                isSuccessed = false;
                                Elmah.ErrorSignal.FromCurrentContext().Raise(new Exception("Create Failed for ProductID: " + reader["ProductID"].ToString()));
                            }
                            else
                            {
                                createdDetailIds.Add(resultId);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                isSuccessed = false;
            }

            return new Tuple<bool, List<Guid>>(isSuccessed, createdDetailIds);
        }
        public Tuple<bool> UpdateInstallationIssue(ReportIssueVieModel nscvm, FormsAuthenticationTicketReader fatr, string InstallationdetailId)
        {
            bool isSuccessed = true;

            // 確保 installationId 有效
            Guid InstallationdetailGuid;
            if (string.IsNullOrEmpty(InstallationdetailId) || !Guid.TryParse(InstallationdetailId, out InstallationdetailGuid))
            {
                isSuccessed = false;
                Elmah.ErrorSignal.FromCurrentContext().Raise(new Exception("InstallationId 無效"));
                return new Tuple<bool>(false);
            }

            try
            {
                // 取得 CRM 連線
                if (CRMService.Service == null)
                {
                    throw new Exception("CRMService.Service 未初始化，請先建立連線。");
                }

                var service = (IOrganizationService)CRMService.Service;

                // 先檢索 Installation 記錄
                ColumnSet columns = new ColumnSet(new string[] { "new_name", "new_quantityinstalled", "new_remark", "new_reportissue" });
                Entity existingInstallation = service.Retrieve("new_installationdetail", InstallationdetailGuid, columns);

                if (existingInstallation == null)
                {
                    isSuccessed = false;
                    Elmah.ErrorSignal.FromCurrentContext().Raise(new Exception("找不到 Installationdetail 記錄，ID：" + InstallationdetailId));
                    return new Tuple<bool>(false);
                }

                // 建立要更新的 Entity
                Entity updateInstallation = new Entity("new_installationdetail");
                updateInstallation.Id = InstallationdetailGuid;
                updateInstallation["new_name"] = nscvm.TitleName;
                updateInstallation["new_quantityinstalled"] = int.Parse(nscvm.DeliveredAmount);
                updateInstallation["new_remark"] = nscvm.Notes;
                updateInstallation["new_reportissue"] = new OptionSetValue(int.Parse(nscvm.IssueType));
                updateInstallation["modifiedon"] = DateTime.Now; // 更新日期
                updateInstallation["modifiedby"] = new EntityReference("systemuser", Guid.Parse(fatr.CrmAccountId)); // 更新人員

                // 更新記錄
                service.Update(updateInstallation);

                //Attachments
                if (nscvm.ImageBase64 != null && nscvm.ImageBase64.Length > 0)
                {
                    string base64Data = nscvm.ImageBase64;
                    if (base64Data.StartsWith("data:image/png;base64,"))
                    {
                        base64Data = base64Data.Replace("data:image/png;base64,", "");

                    }

                    var Annotation = new Entity("annotation");

                    Annotation["documentbody"] = base64Data;
                    Annotation["filename"] = "InstallationDetailImg" + InstallationdetailGuid + ".png";
                    Annotation["isdocument"] = true;
                    Annotation["objectid"] = new EntityReference("new_installationdetail", InstallationdetailGuid);
                    Annotation["subject"] = "InstallationDetail(installation_InstallationDetailImg))";

                    var AnnotationId = service.Create(Annotation);
                    if (AnnotationId == null || AnnotationId == Guid.Empty)
                    {
                        isSuccessed = false;
                        new Exception("Create Annotation Failed");
                    }
                }
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                isSuccessed = false;
            }

            // 返回操作是否成功
            return new Tuple<bool>(isSuccessed);

        }

        public Tuple<bool, Guid> InsertAllNoMarkIssue(string countryAbbreviation, string salesOrderId, FormsAuthenticationTicketReader fatr, string newInstallationId)
        {
            bool isSuccessed = true;
            Guid newInstallationNumber = Guid.Empty;

            // 檢查必要資料是否缺失
            if (string.IsNullOrEmpty(countryAbbreviation) || string.IsNullOrEmpty(salesOrderId))
            {
                isSuccessed = false;
                Elmah.ErrorSignal.FromCurrentContext().Raise(new Exception("Required data is missing: countryAbbreviation, salesOrderId"));
                return new Tuple<bool, Guid>(false, Guid.Empty);
            }

            try
            {
                // 建立 InstallationDetail 實體
                var InstallationDetailEntity = new Entity("new_installationdetail");
                //建立CRM操作類別
                CRMManipulate CRMManiplute = new CRMManipulate();

                if (!String.IsNullOrEmpty(newInstallationId) && !String.IsNullOrWhiteSpace(newInstallationId)) Guid.TryParse(newInstallationId, out newInstallationNumber);

                // 設定實體屬性
                InstallationDetailEntity["new_name"] = GenerateInstallationNumber(countryAbbreviation);
                InstallationDetailEntity["new_installationid"] = newInstallationNumber;
                InstallationDetailEntity["new_order"] = new EntityReference("salesorder", new Guid(salesOrderId));
                InstallationDetailEntity["new_installationteam"] = "";
                InstallationDetailEntity["new_transportteam"] = "";
                InstallationDetailEntity["createdon"] = DateTime.Now;
                InstallationDetailEntity["createdby"] = new EntityReference("systemuser", Guid.Parse(fatr.CrmAccountId));
                InstallationDetailEntity["modifiedon"] = DateTime.Now;
                InstallationDetailEntity["modifiedby"] = new EntityReference("systemuser", Guid.Parse(fatr.CrmAccountId));
                InstallationDetailEntity["statecode"] = new OptionSetValue(0);
                InstallationDetailEntity["statuscode"] = new OptionSetValue(1);
                InstallationDetailEntity["new_quantityinstalled"] = 0;
                InstallationDetailEntity["new_orderproductid"] = "";


                // 使用 SDK 創建記錄
                newInstallationNumber = CRMManiplute.Create(InstallationDetailEntity);

                if (newInstallationNumber == null || newInstallationNumber == Guid.Empty)
                {
                    isSuccessed = false;
                    new Exception("Create  Failed");
                }
                newInstallationNumber = InstallationDetailEntity.Id; // 取得創建成功的記錄 ID
            }
            catch (ArgumentException ex)
            {
                // 記錄具體錯誤並返回失敗
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                JObject messageContent = new JObject
        {
            { "Function", "InsertNewInstallation" },
            { "CountryAbbreviation", countryAbbreviation },
            { "SalesOrderId", salesOrderId }
            //{ "Equipment", JObject.FromObject(equipment) }
        };
                var newEx = new Exception(messageContent.ToString());
                Elmah.ErrorSignal.FromCurrentContext().Raise(newEx);

                isSuccessed = false;
            }
            catch (Exception ex)
            {
                // 捕捉其他異常並返回失敗
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                isSuccessed = false;
            }

            // 返回操作是否成功及安裝 ID
            return new Tuple<bool, Guid>(isSuccessed, newInstallationNumber);
        }

        public EquipmentViewModel GetEquipmentDetails(Guid equipmentId)
        {
            string query = @"
                SELECT [EquipmentId],
                       [SiteId],
                       [ModifiedBy],
                       [CreatedBy],
                       [ModifiedOn],
                       [BusinessUnitId],
                       [Skills],
                       [VersionNumber],
                       [CreatedOn],
                       [TimeZoneCode],
                       [DisplayInServiceViews],
                       [IsDisabled],
                       [Name],
                       [CalendarId],
                       [Description],
                       [EMailAddress],
                       [OrganizationId],
                       [ImportSequenceNumber],
                       [OverriddenCreatedOn],
                       [TimeZoneRuleVersionNumber],
                       [UTCConversionTimeZoneCode],
                       [ExchangeRate],
                       [ModifiedOnBehalfBy],
                       [CreatedOnBehalfBy],
                       [TransactionCurrencyId]
                FROM [JHTNLQAS_MSCRM].[dbo].[EquipmentBase]
                WHERE [EquipmentId] = @EquipmentId";

            using (var connection = GetDbConnection())
            {
                connection.Open();

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@EquipmentId", equipmentId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new EquipmentViewModel
                            {
                                EquipmentId = reader.GetGuid(reader.GetOrdinal("EquipmentId")),
                                SiteId = reader["SiteId"] as Guid?,
                                ModifiedBy = reader["ModifiedBy"] as Guid?,
                                CreatedBy = reader["CreatedBy"] as Guid?,
                                ModifiedOn = reader["ModifiedOn"] as DateTime?,
                                BusinessUnitId = reader["BusinessUnitId"] as Guid?,
                                Skills = reader["Skills"] as string,
                                VersionNumber = reader["VersionNumber"] as byte[],
                                CreatedOn = reader["CreatedOn"] as DateTime?,
                                TimeZoneCode = reader["TimeZoneCode"] as int?,
                                DisplayInServiceViews = reader["DisplayInServiceViews"] as bool? ?? false,
                                IsDisabled = reader["IsDisabled"] as bool? ?? false,
                                Name = reader["Name"] as string,
                                CalendarId = reader["CalendarId"] as Guid?,
                                Description = reader["Description"] as string,
                                EMailAddress = reader["EMailAddress"] as string,
                                OrganizationId = reader.GetGuid(reader.GetOrdinal("OrganizationId")),
                                ImportSequenceNumber = reader["ImportSequenceNumber"] as int?,
                                OverriddenCreatedOn = reader["OverriddenCreatedOn"] as DateTime?,
                                TimeZoneRuleVersionNumber = reader["TimeZoneRuleVersionNumber"] as int?,
                                UTCConversionTimeZoneCode = reader["UTCConversionTimeZoneCode"] as int?,
                                ExchangeRate = reader["ExchangeRate"] as decimal?,
                                ModifiedOnBehalfBy = reader["ModifiedOnBehalfBy"] as Guid?,
                                CreatedOnBehalfBy = reader["CreatedOnBehalfBy"] as Guid?,
                                TransactionCurrencyId = reader["TransactionCurrencyId"] as Guid?
                            };
                        }
                    }
                }
            }

            // 如果未找到設備，返回 null 或者拋出異常
            return null;
        }

        /// <summary>
        /// 取得已安裝產品清單
        /// </summary>
        /// <param name="pshvm">輸入 ViewModel</param>
        /// <param name="fatr">身份驗證資訊</param>
        public void GetAlreadyInstalledProducts(InstalledProductsViewModel pshvm, FormsAuthenticationTicketReader fatr)
        {
            pshvm.ProductDetail3 = new List<CustomerOrderInformationModel>();
            // 若完全不再用，可把 ProductDetailNonSN 相關程式移除
            //pshvm.ProductDetailNonSN = new List<CustomerOrderInformationModel>();

            using (var sqlCommand = GetAlreadyInstalledProductsSqlCommand(fatr, pshvm.OrderBy, pshvm.Asc))
            {
                sqlCommand.Parameters.AddWithValue("@SalesOrderId", pshvm.SalesOrderID);

                using (var sdr = sqlCommand.ExecuteReader())
                {
                    while (sdr.Read())
                    {
                        var model = new CustomerOrderInformationModel
                        {
                            ProductNo = sdr["ProductNo"]?.ToString(),
                            Product = sdr["ProductName"]?.ToString(),
                            Productguid = sdr["ProductID"]?.ToString(),
                            ProductSN = sdr["ProductSN"]?.ToString(),      // 可能為空，View 會顯示「No Serial Number」
                            SerialNo = sdr["SerialNo"]?.ToString(),

                            ExpectedQty = sdr["ExpectedQty"] != DBNull.Value
                                ? Math.Floor(Convert.ToDecimal(sdr["ExpectedQty"])).ToString()
                                : null,
                            InstalledQty = sdr["InstalledQty"] != DBNull.Value
                                ? Math.Floor(Convert.ToDecimal(sdr["InstalledQty"])).ToString()
                                : null,

                            InstallMainIssue = sdr["InstallMainIssue"]?.ToString(),
                            InstallMainIssueNotes = sdr["InstallMainIssueNotes"]?.ToString(),

                            // TODO：如果有實際欄位，就在 SQL + 這裡一起補上
                            // InstallMainIssueUpload = sdr["InstallMainIssueUpload"]?.ToString()
                        };

                        pshvm.ProductDetail3.Add(model);
                    }
                }
            }
        }


        public SqlCommand GetAlreadyInstalledProductsSqlCommand(
    FormsAuthenticationTicketReader fatr,
    string orderby = "ProductNo",
    bool asc = true)
        {
            var dbConnection = new DBConnetion().GetConnection();
            if (!dbConnection.Item1)
            {
                throw new Exception("DB Connection Failed!");
            }

            var allowedOrderByFields = new[]
            {
        "ProductNo", "ProductName", "ExpectedQty", "InstalledQty",
        "ProductSN", "InstallMainIssue", "InstallMainIssueNotes"
        // 注意：這裡先不要放 InstallMainIssueUpload，除非 SQL 有選出對應欄位
    };

            if (!allowedOrderByFields.Contains(orderby))
            {
                orderby = "ProductNo";
            }

            var queryString = new StringBuilder();
            queryString.AppendLine("SELECT");
            queryString.AppendLine("  a.ProductId AS ProductID,");
            queryString.AppendLine("  nid.new_QuantityInstalled AS InstalledQty,");
            queryString.AppendLine("  CASE");
            queryString.AppendLine("    WHEN nid.new_reportissue = 1 THEN 'Product Unavailable'");
            queryString.AppendLine("    WHEN nid.new_reportissue = 2 THEN 'Product Picking Error'");
            queryString.AppendLine("    WHEN nid.new_reportissue = 3 THEN 'Johnson OOB Issue'");
            queryString.AppendLine("    WHEN nid.new_reportissue = 4 THEN 'Johnson Pre-assembly Error'");
            queryString.AppendLine("    WHEN nid.new_reportissue = 5 THEN 'Damaged Product'");
            queryString.AppendLine("    WHEN nid.new_reportissue = 6 THEN 'Installed Different Product from Product Checklist'");
            queryString.AppendLine("    WHEN nid.new_reportissue = 7 THEN 'Customer Site Not Ready'");
            queryString.AppendLine("    WHEN nid.new_reportissue = 8 THEN 'Others '");
            queryString.AppendLine("    ELSE 'Unknown Issue'");
            queryString.AppendLine("  END AS InstallMainIssue,");
            queryString.AppendLine("  nid.new_remark AS InstallMainIssueNotes,");

            // TODO：如果有 Upload 欄位，在這裡選出來
            // queryString.AppendLine("  nid.new_uploadfield AS InstallMainIssueUpload,");

            queryString.AppendLine("  b.productnumber AS ProductNo,");
            queryString.AppendLine("  b.new_ModelNo AS SerialNo,");
            queryString.AppendLine("  b.name AS ProductName,");
            queryString.AppendLine("  b.new_ModelNo AS ProductSN,");
            queryString.AppendLine("  a.quantity AS ExpectedQty");

            queryString.AppendLine("FROM new_installationdetail AS nid");
            queryString.AppendLine("JOIN new_installation ni ON ni.new_installationId = nid.new_InstallationId");
            queryString.AppendLine("JOIN Product b ON nid.new_orderproductid = b.ProductId");
            queryString.AppendLine("JOIN SalesOrderDetail a ON a.ProductId = nid.new_orderproductid");

            queryString.AppendLine("WHERE a.new_itemstatus <> '3'");
            queryString.AppendLine("  AND a.SalesOrderId = @SalesOrderId");
            // 關鍵：不再用 new_ModelNo 判斷有 / 無 SN，兩種都抓
            // 不要再加 (b.new_ModelNo IS NULL/NOT NULL) 的條件

            queryString.AppendLine($"ORDER BY {orderby} {(asc ? "ASC" : "DESC")}");

            var command = new SqlCommand
            {
                Connection = dbConnection.Item2,
                CommandText = queryString.ToString()
            };

            return command;
        }


        //      public SqlCommand GetAlreadyInstalledProductsNonSNSqlCommand(
        //FormsAuthenticationTicketReader fatr,
        //string orderby = "ProductNo",
        //bool asc = true)
        //      {
        //          var dbConnection = new DBConnetion().GetConnection();
        //          if (!dbConnection.Item1)
        //          {
        //              throw new Exception("DB Connection Failed!");
        //          }

        //          var allowedOrderByFields = new[] { "ProductNo", "ProductName", "ExpectedQty", "InstalledQty", "ProductSN", "InstallMainIssue", "InstallMainIssueNotes" };
        //          if (!allowedOrderByFields.Contains(orderby))
        //          {
        //              orderby = "ProductNo";
        //          }

        //          var queryString = new StringBuilder();
        //          queryString.AppendLine("SELECT");

        //          queryString.AppendLine("  a.ProductId AS ProductID,");
        //          queryString.AppendLine("  nid.new_QuantityInstalled AS InstalledQty,");
        //          queryString.AppendLine("  CASE");
        //          queryString.AppendLine("    WHEN nid.new_reportissue = 1 THEN 'Product Unavailable'");
        //          queryString.AppendLine("    WHEN nid.new_reportissue = 2 THEN 'Product Picking Error'");
        //          queryString.AppendLine("    WHEN nid.new_reportissue = 3 THEN 'Johnson OOB Issue'");
        //          queryString.AppendLine("    WHEN nid.new_reportissue = 4 THEN 'Johnson Pre-assembly Error'");
        //          queryString.AppendLine("    WHEN nid.new_reportissue = 5 THEN 'Damaged Product'");
        //          queryString.AppendLine("    WHEN nid.new_reportissue = 6 THEN 'Installed Different Product from Product Checklist'");
        //          queryString.AppendLine("    WHEN nid.new_reportissue = 7 THEN 'Customer Site Not Ready'");
        //          queryString.AppendLine("    WHEN nid.new_reportissue = 8 THEN 'Others '");
        //          queryString.AppendLine("    ELSE 'Unknown Issue'");
        //          queryString.AppendLine("  END AS InstallMainIssue,");
        //          queryString.AppendLine("  nid.new_remark AS InstallMainIssueNotes,");
        //          queryString.AppendLine("  b.productnumber AS ProductNo,");
        //          queryString.AppendLine("  b.name AS ProductName,");
        //          queryString.AppendLine("  b.new_ModelNo AS ProductSN,");
        //          queryString.AppendLine("  a.quantity AS ExpectedQty");

        //          queryString.AppendLine("FROM new_installationdetail AS nid");
        //          queryString.AppendLine("JOIN new_installation ni ON ni.new_installationId = nid.new_InstallationId");
        //          queryString.AppendLine("JOIN Product b ON nid.new_orderproductid = b.ProductId");
        //          queryString.AppendLine("JOIN SalesOrderDetail a ON a.ProductId = nid.new_orderproductid");

        //          queryString.AppendLine("WHERE a.new_itemstatus <> '3'");
        //          queryString.AppendLine("  AND a.SalesOrderId = @SalesOrderId");
        //          queryString.AppendLine("  AND (b.new_ModelNo IS NULL OR LTRIM(RTRIM(b.new_ModelNo)) = '')");
        //          // queryString.AppendLine("  AND ni.new_iscompleted = 1");
        //          // queryString.AppendLine("  AND CONVERT(DATE, a.RequestDeliveryBy) = @RequestDate");

        //          queryString.AppendLine($"ORDER BY {orderby} {(asc ? "ASC" : "DESC")}");

        //          var command = new SqlCommand
        //          {
        //              Connection = dbConnection.Item2,
        //              CommandText = queryString.ToString()
        //          };

        //          return command;
        //      }

        // 建立 DB 連線的共用方法
        public SqlConnection GetDbConnection()
        {
            var dbConnection = new DBConnetion().GetConnection();
            if (!dbConnection.Item1)
            {
                throw new Exception("DB Connection Failed!");
            }
            return dbConnection.Item2;
        }
    }
}