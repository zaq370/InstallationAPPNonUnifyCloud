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
using System.IO;
using System.Data;


namespace InstallationAPPNonUnify.Controllers
{
    [OutOfServiceAttribute(AllowRole = new string[] { "administrator", "superuser" })]
    [Authorize]
    [FormsAuthenticationAttribute]
    public class OrderController : BaseController
    {
        protected string Layout = "_Layout_Case.cshtml";
        protected string Title = "Johnson Health Tech - Matrix Fitness Installation Confirmation";
        protected string Title2 = "Johnson Health Tech";
        private CaseBusinessLogic cbl = new CaseBusinessLogic();

        public ActionResult InstallationCompletion(string salesOrderId, string requestDeliveryBy)
        {
            var fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName]);
            CaseBusinessLogic cbl = new CaseBusinessLogic();
            // 取得 Installation 記錄
            Installation installation = GetInstallationByOrderId(salesOrderId, requestDeliveryBy);
            if (installation == null)
            {
                return Json(new { success = false, message = "Installation not found" }, JsonRequestBehavior.AllowGet);
            }

            // 呼叫合併方法
            var insertResult = cbl.InsertAllInstallationDetailsFromOrder(salesOrderId, installation.NewInstallationId, requestDeliveryBy, fatr);
            if (!insertResult.Item1)
            {
                return Json(new { success = false, message = "Failed to insert new installation issue" }, JsonRequestBehavior.AllowGet);
            }

            GetViewBag("InstallationCompletion");
            return View();
        }
        public ActionResult InstallationNoIssue(string salesOrderId, string requestDeliveryBy, string productNo)
        {
            var fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName]);
            CaseBusinessLogic cbl = new CaseBusinessLogic();
            // 取得 Installation 記錄
            Installation installation = GetInstallationByOrderId(salesOrderId, requestDeliveryBy);
            if (installation == null)
            {
                return Json(new { success = false, message = "Installation not found" }, JsonRequestBehavior.AllowGet);
            }

            // 呼叫合併方法
            var insertResult = cbl.InsertInstallationDetailsNoIssueFromOrder(salesOrderId, installation.NewInstallationId, requestDeliveryBy, productNo, fatr);
            if (!insertResult.Item1)
            {
                return Json(new { success = false, message = "Failed to insert new installation issue" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }
        // POST: Installation/ConfirmInstallation
        [HttpPost]
        public ActionResult ConfirmInstallation(string installerName, string clubManagerName, DateTime signatureDate)
        {
            // Handle the confirmation logic here
            // e.g., save the installation data to the database

            // Redirect to a success page or back to the CustomerSigning view
            return RedirectToAction("Success");
        }

        // GET: Installation/Success
        public ActionResult Success()
        {
            return View();
        }

        public ActionResult SearchOrderProductInformation(InstalledProductsViewModel pshvm, string OriginalOrderBy = "", bool OriginalAsc = true, bool KeepSameSort = false)
        {
            GetViewBag("InstallMain");

            var fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName]);

            if (pshvm.Search)
            {
                // 簡化排序邏輯
                if (KeepSameSort)
                {
                    pshvm.OrderBy = OriginalOrderBy;
                    pshvm.Asc = OriginalAsc;
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(OriginalOrderBy) && OriginalOrderBy.Equals(pshvm.OrderBy))
                    {
                        pshvm.Asc = !OriginalAsc;
                    }
                    else
                    {
                        pshvm.Asc = true;
                    }
                }

                ModelState.Clear();

                var cbl = new CaseBusinessLogic();
                cbl.GetOrderProductInformation(pshvm, fatr);

                // 分頁處理
                pshvm.PagedProductDetail = pshvm.ProductDetail.ToPagedList(pshvm.Page > 0 ? pshvm.Page - 1 : 0, DefaultPageSize);

                // 將資料存入 TempData
                TempData["ProductDetail"] = pshvm.ProductDetail;

                // 清空 ProductDetail
                pshvm.ProductDetail.Clear();

                return RedirectToAction("InstallMain");
            }

            return View(pshvm);
        }
        public ActionResult InstallMain(
            string SalesOrderID = "",
            string RequestDeliveryBy = "",
            int Page = 1,  // 預設頁碼為 1
            string OriginalOrderBy = "",
            bool OriginalAsc = true,
            bool KeepSameSort = false)
        {
            GetViewBag("InstallMain");

            // 設置頁碼與其他參數
            var pshvm = new InstalledProductsViewModel(CountryCode)
            {
                SalesOrderID = SalesOrderID,
                Page = Page, // 直接將當前頁碼傳遞給模型
                OrderBy = OriginalOrderBy,
                Asc = OriginalAsc
            };

            // 處理日期轉換
            if (!string.IsNullOrEmpty(RequestDeliveryBy))
            {
                DateTime requestDate;
                if (DateTime.TryParse(RequestDeliveryBy, out requestDate))
                {
                    pshvm.RequestDate = requestDate;
                }
            }

            // 呼叫商業邏輯處理資料
            var cbl = new CaseBusinessLogic();
            cbl.GetOrderProductInformation(pshvm, new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName]));
            cbl.GetAlreadyInstalledProducts(pshvm, new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName]));
            // 分頁邏輯：確保使用正確的 Page 參數來加載對應的頁面資料
            pshvm.PagedProductDetail = pshvm.ProductDetail.ToPagedList(pshvm.Page > 0 ? pshvm.Page - 1 : 0, DefaultPageSize);
            pshvm.PagedProductDetail2 = pshvm.ProductDetail3.ToPagedList(pshvm.Page > 0 ? pshvm.Page - 1 : 0, DefaultPageSize);
            //pshvm.ProductDetail = pshvm.ProductDetail.ToList();
            //pshvm.ProductDetail3 = pshvm.ProductDetail3.ToList();
            return View(pshvm);
        }



        //        private List<InstalledProductsViewModel.ProductDetail2> LoadInstalledProducts(string salesOrderID, DateTime requestDate)
        //        {
        //            var installedProducts = new List<InstalledProductsViewModel.ProductDetail2>();
        //            var rawData = new List<dynamic>();

        //            using (var connection = cbl.GetDbConnection())
        //            {
        //                var query = @"
        //            SELECT b.new_ModelNo AS ProductNo, 
        //                   ProductIdName AS ProductName, 
        //                   a.quantity AS ExpectedQty               
        //            FROM SalesOrderDetail a 
        //            JOIN Product b ON a.ProductId = b.ProductId
        //            WHERE a.new_itemstatus <> '3'
        //            AND a.SalesOrderId = @SalesOrderId
        //            AND CONVERT(DATE, a.RequestDeliveryBy) = @RequestDate";

        //                using (var command = new SqlCommand(query, connection))
        //                {
        //                    command.Parameters.AddWithValue("@SalesOrderId", salesOrderID);
        //                    command.Parameters.AddWithValue("@RequestDate", requestDate.Date);

        //                    using (var reader = command.ExecuteReader())
        //                    {
        //                        while (reader.Read())
        //                        {
        //                            rawData.Add(new
        //                            {
        //                                ProductName = reader["ProductName"] as string,
        //                                ExpectedQty = reader["ExpectedQty"] != DBNull.Value ? Convert.ToInt32(reader["ExpectedQty"]) : 0
        //                            });
        //                        }
        //                    }
        //                }
        //            }

        //            // 使用 LINQ 將資料映射到 ViewModel
        //            installedProducts = rawData.Select(data => new InstalledProductsViewModel.ProductDetail2
        //            {
        //                Name = data.ProductName ?? "Unknown Product",
        //                Quantity = data.ExpectedQty
        //            }).ToList();

        //            return installedProducts;
        //        }

        //        private List<InstalledProductsViewModel.ProductDetail> LoadInstalledProducts2(string salesOrderID)
        //        {
        //            var installedProducts2 = new List<InstalledProductsViewModel.ProductDetail>();
        //            var rawData = new List<dynamic>();

        //            using (var connection = cbl.GetDbConnection())
        //            {
        //                var queryString = @"
        //            SELECT b.new_orderproductid AS ProductNo, 
        //                   b.new_name AS ProductName, 
        //                   b.new_QuantityInstalled AS InstalledQty               
        //            FROM new_installation a 
        //            JOIN new_installationdetail b ON a.new_installationId = b.new_InstallationId
        //            WHERE a.new_Order = @SalesOrderId";

        //                using (var command = new SqlCommand(queryString, connection))
        //                {
        //                    command.Parameters.AddWithValue("@SalesOrderId", salesOrderID);

        //                    using (var reader = command.ExecuteReader())
        //                    {
        //                        while (reader.Read())
        //                        {
        //                            rawData.Add(new
        //                            {
        //                                ProductName = reader["ProductName"] as string,
        //                                ProductNo = reader["ProductNo"] as string,
        //                                InstalledQty = reader["InstalledQty"] != DBNull.Value ? Convert.ToInt32(reader["InstalledQty"]) : 0
        //                            });
        //                        }
        //                    }
        //                }
        //            }

        //            // 使用 LINQ 處理並映射到 ProductDetail
        //            installedProducts2 = rawData.Select(data => new InstalledProductsViewModel.ProductDetail
        //            {
        //                ProductName = data.ProductName ?? "Unknown Product",
        //                ProductNo = data.ProductNo ?? "Unknown Product",
        //                InstalledQty = data.InstalledQty
        //            }).ToList();

        //            return installedProducts2;
        //        }

        public ActionResult AlreadyInstalled(InstalledProductsViewModel pshvm, string OriginalOrderBy = "", bool OriginalAsc = true, bool KeepSameSort = false)
        {
            //if (string.IsNullOrWhiteSpace(salesOrderID))
            //    return null;

            GetViewBag("AlreadyInstalled");

            // 取得權限資料，並考慮將常用權限資料緩存起來
            var fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName]);

            //if (pshvm.Search)
            //{
            // 簡化排序邏輯
            if (KeepSameSort)
            {
                pshvm.OrderBy = OriginalOrderBy;
                pshvm.Asc = OriginalAsc;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(OriginalOrderBy) && OriginalOrderBy.Equals(pshvm.OrderBy))
                {
                    pshvm.Asc = !OriginalAsc;
                }
                else
                {
                    pshvm.Asc = true;
                }
            }

            // 清除 ModelState 如有必要
            ModelState.Clear();

            // 優化資料取得的商業邏輯，考慮延遲加載
            var cbl = new CaseBusinessLogic();
            cbl.GetAlreadyInstalledProducts(pshvm, fatr);

            // 分頁處理
            //pshvm.PagedProductDetail = pshvm.ProductDetail3.ToPagedList(pshvm.Page > 0 ? pshvm.Page - 1 : 0, DefaultPageSize);

            // 確保分頁處理後清空不必要的資料
            //pshvm.ProductDetail3.Clear();
            //}

            return View("AlreadyInstalled", pshvm);
        }

        // 根據 salesOrderID 從資料庫中取得已安裝的產品資訊


        public ActionResult EmailUs()
        {
            //取相關訊息
            GetViewBag("EmailUs");

            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName]);

            //取得畫面上的資料
            CaseBusinessLogic cm = new CaseBusinessLogic();
            var evm = new EmailUsViewModel(cm.CreateMailContent(fatr), CountryCode);

            //有送來訊息
            if (TempData.ContainsKey("alertMessage")) evm.AlertMessage = TempData["alertMessage"].ToString();

            return View("EmailUs", evm);
        }

        [HttpPost]
        public ActionResult EmailUs(EmailUsViewModel euvm)
        {
            Email mail = new Email(CountryCode);
            mail.SendRequest(euvm);

            if (mail.IsSuccessed)
            {
                LoginViewModel lvm = new LoginViewModel(CountryCode);
                return RedirectToAction("EmailCompleted");
            }
            else
            {
                GetViewBag("EmailUs");  //取相關訊息
                EmailUsViewModel evm = new EmailUsViewModel(CountryCode);
                evm.AlertMessage = new LanguagePackage(CountryCode).getContentWithNoPrefix("FeedbackFailed");
                return View(evm);
            }
        }

        [FormsAuthenticationAttribute(AllowTempUser = true)]
        public ActionResult EmailCompleted()
        {
            GetViewBag("EmailCompleted");  //取相關訊息
            return View("Messages");
        }

        [FormsAuthenticationAttribute(AllowTempUser = true)]
        public ActionResult ChangePassword()
        {
            GetViewBag("ChangePassword");  //取相關訊息

            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName]);

            //更新畫面上的資料
            ChangePasswordViewModel cpvm = new ChangePasswordViewModel(CountryCode);
            cpvm.UserId = fatr.CRMUserId;

            //有送來訊息
            if (TempData.ContainsKey("alertMessage")) cpvm.AlertMessage = TempData["alertMessage"].ToString();

            return View(cpvm);
        }

        [HttpPost]
        [FormsAuthenticationAttribute(AllowTempUser = true)]
        public ActionResult ChangePassword(ChangePasswordViewModel cpvm)
        {
            GetViewBag("ChangePassword");    //取相關訊息

            LanguagePackage lp = new LanguagePackage(CountryCode);
            //如果資料不一致
            if (!cpvm.NewPassword.Equals(cpvm.NewPasswordConfirm))
            {
                ModelState.AddModelError("NewPassword", lp.getContentWithNoPrefix("PasswordNotEqual"));
                return View(cpvm);
            }

            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName]);

            //更新資料
            CaseBusinessLogic cm = new CaseBusinessLogic();
            var result = cm.ChangeUserPassword(cpvm, fatr);

            if (!result.Item1)
            {
                cpvm.AlertMessage = result.Item2;
                if (string.IsNullOrEmpty(cpvm.AlertMessage) || string.IsNullOrWhiteSpace(cpvm.AlertMessage))
                {
                    cpvm.AlertMessage = lp.getContentWithNoPrefix("PasswordUpdateFail");
                }
                return View(cpvm);
            }
            else
            {
                TempData["alertMessage"] = lp.getContentWithNoPrefix("PasswordUpdateSuccessed");

                //如果是 TempRegularUser，轉到出口
                if (fatr.Role.ToLower().Equals("tempregularuser"))
                    return RedirectToAction("ChangePasswordDone", "Main");
            }
            return RedirectToAction("ChangePassword");
        }

        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
        public ActionResult OrderMain(string accountProductId = "", bool duplicateAlert = true, string SalesOrderID = "", string RequestDeliveryBy = "")
        {
            GetViewBag("OrderMain");

            var nscvm = new InstallationConfirmationViewModel(CountryCode);

            if (!string.IsNullOrWhiteSpace(SalesOrderID))
            {
                nscvm.SalesOrderID = SalesOrderID;
            }

            Installation installation = GetInstallationByOrderId(SalesOrderID, RequestDeliveryBy);

            if (installation != null && !string.IsNullOrWhiteSpace(installation.NewInstallationId))
            {
                nscvm.InstallationID = installation.NewInstallationId;
            }

            // 模擬從資料庫或服務中取得資料
            var salesOrder = GetSalesOrderById(SalesOrderID);
            var account = GetAccountByCustomerId(salesOrder.CustomerId);
            var installation2 = GetInstallationByInstallationID(nscvm.InstallationID);

            // 準備 ViewModel
            var model = new InstallationConfirmationViewModel
            {
                InstallationDate = Request.QueryString["RequestDeliveryBy"]
            };

            // 如果 salesOrder 不為 null 且 CustomerIdName 有值，才設定 CustomerName
            if (salesOrder != null && !string.IsNullOrEmpty(salesOrder.CustomerIdName))
            {
                model.CustomerName = salesOrder.CustomerIdName;
                model.OrderNumber = salesOrder.OrderNumber;
            }

            // 如果 account 不為 null 且 EmailAddress1 有值，才設定 CustomerEmail
            if (account != null && !string.IsNullOrEmpty(account.EmailAddress1))
            {
                model.CustomerEmail = account.EmailAddress1;
            }

            // 假設使用了某個 DbContext 來操作資料庫
            var installationTeams = new List<SelectListItem>();

            string installationQuery = @"
                    SELECT EquipmentId, Name 
                    FROM Equipment";

            using (var connection = cbl.GetDbConnection())
            {
                // 使用 SQL 查詢取得 InstallationTeam 資料
                using (var command = new SqlCommand(installationQuery, connection))
                {
                    //connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            installationTeams.Add(new SelectListItem
                            {
                                Value = reader["EquipmentId"].ToString(), // 確保 GUID 轉為字串
                                Text = reader["Name"].ToString()
                            });
                        }
                    }
                }
            }

            // 將 InstallationTeam 資料存入 ViewBag
            ViewBag.InstallationTeams = installationTeams;

            // 假設選項集的資料已經由某個 SDK 提供
            var transportTeams = new List<SelectListItem>
                {
                    new SelectListItem { Value = "100000000", Text = "JHTNL Transport Team" },
                };

            ViewBag.TransportTeams = transportTeams;

            // 設定 InstallationTeamName 與 TransportTeamName 為下拉選單的 Text 值
            if (installation2 != null)
            {
                if (!string.IsNullOrEmpty(installation2.NewInstallationTeam))
                {
                    model.InstallationTeamName = installation2.NewInstallationTeam; // 設為選項的 Value
                }

                if (!string.IsNullOrEmpty(installation2.NewTransportTeam))
                {
                    model.TransportTeamName = installation2.NewTransportTeam; // 設為選項的 Value
                }
            }


            return View(model);
        }

        // 查詢 SalesOrder
        private SalesOrder GetSalesOrderById(string salesOrderID)
        {
            if (string.IsNullOrWhiteSpace(salesOrderID))
                return null;

            var queryString = @"
        SELECT SalesOrderId, CustomerIdName, CustomerId, new_ERPOrderNo 
        FROM SalesOrder 
        WHERE SalesOrderId = @SalesOrderId";

            using (var connection = cbl.GetDbConnection())
            using (var command = new SqlCommand(queryString, connection))
            {
                command.Parameters.AddWithValue("@SalesOrderId", salesOrderID);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new SalesOrder
                        {
                            SalesOrderID = reader["SalesOrderId"].ToString(),
                            CustomerIdName = reader["CustomerIdName"].ToString(),
                            CustomerId = reader["CustomerId"].ToString(),
                            OrderNumber = reader["new_ERPOrderNo"].ToString(),
                        };
                    }
                }
            }

            return null;
        }

        // 查詢 Installation
        private Installation GetInstallationByOrderId(string salesOrderID, string requestDeliveryBy)
        {
            if (string.IsNullOrWhiteSpace(salesOrderID))
                return null;

            if (string.IsNullOrWhiteSpace(requestDeliveryBy))
                return null;

            var queryString = @"
                           SELECT new_installationId, CreatedOn, new_name, new_ClubManager
    FROM new_installation 
    WHERE new_Order = @SalesOrderId 
      AND new_requestdeliveryby >= @StartDate 
      AND new_requestdeliveryby < @EndDate 
      AND new_requestdeliveryby IS NOT NULL";

            using (var connection = cbl.GetDbConnection())
            using (var command = new SqlCommand(queryString, connection))
            {
                var baseDate = DateTime.Parse(requestDeliveryBy.Trim()).Date;
                var beforeDate = baseDate.AddDays(-1);
                var nextDate = baseDate.AddDays(1);

                command.Parameters.AddWithValue("@SalesOrderId", salesOrderID.Trim());
                command.Parameters.AddWithValue("@StartDate", beforeDate);
                command.Parameters.AddWithValue("@EndDate", nextDate);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Installation
                        {
                            NewInstallationId = reader["new_installationId"].ToString(),
                            CreatedOn = DateTime.Parse(reader["CreatedOn"].ToString()),
                            NewName = reader["new_name"].ToString(),
                            ClubManagerName = reader["new_ClubManager"].ToString()
                        };
                    }
                }
            }

            return null;
        }

        // 查詢 InstallationDetail
        private InstallationDetail GetInstallationDetailByInstallationId(string InstallationId, string ProductId)
        {
            if (string.IsNullOrWhiteSpace(InstallationId))
                return null;

            var queryString = @"
                SELECT new_installationdetailId
                FROM new_installationdetail 
                WHERE new_InstallationId = @InstallationId and new_orderproductid like @ProductId";

            using (var connection = cbl.GetDbConnection())
            using (var command = new SqlCommand(queryString, connection))
            {
                command.Parameters.AddWithValue("@InstallationId", InstallationId);
                command.Parameters.AddWithValue("@ProductId", "%" + ProductId + "%");

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new InstallationDetail
                        {
                            InstallationDetailId = reader["new_installationdetailId"].ToString()

                        };
                    }
                }
            }

            return null;
        }
        // 查詢 Account
        private Account GetAccountByCustomerId(string customerId)
        {
            if (string.IsNullOrWhiteSpace(customerId))
                return null;

            var queryString = @"
        SELECT EMailAddress1 
        FROM Account 
        WHERE AccountId = @CustomerId";

            using (var connection = cbl.GetDbConnection())
            using (var command = new SqlCommand(queryString, connection))
            {
                command.Parameters.AddWithValue("@CustomerId", customerId);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Account
                        {
                            EmailAddress1 = reader["EMailAddress1"].ToString(),
                        };
                    }
                }
            }

            return null;
        }

        // 查詢 Installation
        private Installation GetInstallationByInstallationID(string installationID)
        {
            if (string.IsNullOrWhiteSpace(installationID))
                return null;

            var queryString = @"
        SELECT new_installer, new_TransportTeam,new_InstallationTeam
        FROM new_installation 
        WHERE new_installationId = @InstallationID";

            using (var connection = cbl.GetDbConnection())
            using (var command = new SqlCommand(queryString, connection))
            {
                command.Parameters.AddWithValue("@InstallationID", installationID);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        //var transportTeam = reader["new_TransportTeam"].ToString() == "100000000" ? "JHTNL Transport Team" : string.Empty;

                        return new Installation
                        {
                            NewInstallationTeam = reader["new_InstallationTeam"] == DBNull.Value ? string.Empty : reader["new_InstallationTeam"].ToString(),
                            NewTransportTeam = reader["new_TransportTeam"] == DBNull.Value ? string.Empty : reader["new_TransportTeam"].ToString()
                        };
                    }
                }
            }

            return null;
        }


        public ActionResult RequestConfirmed()
        {
            GetViewBag("RequestConfirmed");  //取相關訊息
            return View("Messages");
        }
        public ActionResult OrderInformation()
        {
            GetViewBag("OrderInformation");    //取相關訊息
            OrderInformationViewModel pshvm = new OrderInformationViewModel(CountryCode);
            return View(pshvm);
        }

        [HttpPost]
        public ActionResult SearchOrderInformation(OrderInformationViewModel pshvm, string OriginalOrderBy = "", bool OriginalAsc = true, bool KeepSameSort = false)
        {
            GetViewBag("OrderInformation");  // 取相關訊息

            // 取得權限資料，並考慮將常用權限資料緩存起來
            var fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName]);

            if (pshvm.Search)
            {
                // 簡化排序邏輯
                if (KeepSameSort)
                {
                    pshvm.OrderBy = OriginalOrderBy;
                    pshvm.Asc = OriginalAsc;
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(OriginalOrderBy) && OriginalOrderBy.Equals(pshvm.OrderBy))
                    {
                        pshvm.Asc = !OriginalAsc;
                    }
                    else
                    {
                        pshvm.Asc = true;
                    }
                }

                // 清除 ModelState 如有必要
                ModelState.Clear();

                // 優化資料取得的商業邏輯，考慮延遲加載
                var cbl = new CaseBusinessLogic();
                cbl.GetCustomerProduct(pshvm, fatr);

                // 分頁處理
                pshvm.PagedProductDetail = pshvm.ProductDetail.ToPagedList(pshvm.Page > 0 ? pshvm.Page - 1 : 0, DefaultPageSize);

                // 確保分頁處理後清空不必要的資料
                pshvm.ProductDetail.Clear();
            }

            return PartialView("_OrderInformation", pshvm);
        }


        public ActionResult CustomerOrderInformation(
     string SalesOrderID = "",
     string OriginalOrderBy = "",
     bool OriginalAsc = true,
     bool KeepSameSort = false,
     int Page = 1)
        {
            GetViewBag("CustomerOrderInformation");    //取相關訊息

            if (string.IsNullOrWhiteSpace(SalesOrderID))
                throw new Exception("Can't Open This Page Without SalesOrderID");

            //取得權限資料
            FormsAuthenticationTicketReader fatr =
                new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName]);

            var cphvm = new CustomerOrderInformationViewModel(CountryCode);
            cphvm.SalesOrderID = SalesOrderID;

            var cbl = new CaseBusinessLogic();

            if (KeepSameSort)
            {
                cphvm.OrderBy = OriginalOrderBy;
                cphvm.Asc = OriginalAsc;
            }

            cbl.GetCustomerOrderInformation(cphvm, fatr);

            // 防止 Detail 為 null
            if (cphvm.Detail == null)
                cphvm.Detail = new List<ViewCustomerOrderInformationModel>();

            cphvm.PagedDetail = cphvm.Detail.ToPagedList(Page > 0 ? Page - 1 : 0, DefaultPageSize);

            // 🔹 AJAX 呼叫：只回傳 partial，不帶 Layout，不會有 footer 重複
            if (Request.IsAjaxRequest())
            {
                return PartialView("_CustomerOrderInformation", cphvm);
            }

            // 🔹 直接網址開啟：回原本整頁 View
            return View(cphvm);
        }


        [HttpPost]
        [AllowAnonymous]
        [FormsAuthenticationAttribute(Exception = true)]
        public ActionResult CustomerOrderInformation(string CustomerProductId = "", string OrderBy = "", string OriginalOrderBy = "", bool OriginalAsc = true, bool KeepSameSort = false, int Page = 1)
        {
            bool isSuccessed = true;

            GetViewBag("CustomerOrderInformation");    //取相關訊息

            //取得權限資料
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName]);
            if (!fatr.isValidTicket) if (!fatr.isValidTicket) return Json(new { error = "InvalidUser" }, JsonRequestBehavior.AllowGet);

            //只允許ajax call
            if (!Request.IsAjaxRequest())
            {
                if (!fatr.isValidTicket) return Json(new { error = "InvalidRequest" }, JsonRequestBehavior.AllowGet);
            }

            CustomerOrderInformationViewModel cphvm = new CustomerOrderInformationViewModel(CountryCode);
            cphvm.CustomerProductId = CustomerProductId;
            cphvm.OrderBy = OrderBy;

            //Ajax function 必須要包 try catch
            try
            {
                CaseBusinessLogic cbl = new CaseBusinessLogic();

                if (KeepSameSort)
                {
                    cphvm.OrderBy = OriginalOrderBy;
                    cphvm.Asc = OriginalAsc;
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(OriginalOrderBy))
                    {
                        if (OriginalOrderBy.Equals(cphvm.OrderBy))
                        {
                            cphvm.Asc = !OriginalAsc;
                        }
                        else
                        {
                            cphvm.Asc = true;
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(cphvm.OrderBy))
                    {
                        cphvm.Asc = true;
                    }
                }

                cbl.GetCustomerOrderInformation(cphvm, fatr);
                cphvm.PagedDetail = cphvm.Detail.ToPagedList(Page > 0 ? Page - 1 : 0, DefaultPageSize);
                cphvm.Detail = new List<ViewCustomerOrderInformationModel>();
            }
            catch (Exception ex)
            {
                isSuccessed = false;
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
            }

            if (isSuccessed)
            {
                return PartialView("_CustomerOrderInformation", cphvm);
            }
            else
            {
                return PartialView("_Error");
            }
        }

        private void GetViewBag(string viewName)
        {
            LanguagePackage lp = new LanguagePackage(CountryCode);
            GetActionTitles();  //取得共用目錄中按鈕的名稱

            ViewBag.Title = Title;
            ViewBag.Layout = Layout;
            ViewBag.ErrorMessage = lp.getContentWithNoPrefix("ErrorMessage");
            ViewBag.ErrorPageTitle = lp.getContentWithNoPrefix("ErrorPageTitle");

            switch (viewName)
            {
                case "EmailUs":
                    ViewBag.Title = Title2;

                    //取訊息
                    ViewBag.FooterAnnounce = lp.getContentWithNoPrefix("FooterAnnounce");
                    ViewBag.PageTitle = lp.getContentWithNoPrefix("ContactUs");
                    ViewBag.YourDetails = lp.getContentWithNoPrefix("YourDetails");
                    ViewBag.MachineDetails = lp.getContentWithNoPrefix("MachineDetails");
                    ViewBag.SendQuery = lp.getContentWithNoPrefix("SendQuery");
                    ViewBag.Statement = lp.getContentWithNoPrefix("Statement");
                    break;
                case "CustomerDetail":
                    ViewBag.PageTitle = lp.getContentWithNoPrefix("CustomerDetailPageTitle");
                    ViewBag.Surname = lp.getContentWithNoPrefix("Surname");
                    ViewBag.Forename = lp.getContentWithNoPrefix("Forename");
                    break;
                case "EmailCompleted":
                    ViewBag.Title = Title2;

                    ViewBag.CurrentPage = "EmailCompleted";
                    ViewBag.BackgroundImg = "~/Content/custom/images/CaseHeader/email-us.jpg";
                    ViewBag.ActionSelection = "menubarEmailM";
                    ViewBag.Content = lp.getContentWithNoPrefix("FeedbackSuccessed");
                    break;

                case "ChangePassword":
                    ViewBag.Title = Title2;

                    ViewBag.PageTitle = lp.getContentWithNoPrefix("ChangePasswordPageTitle");
                    ViewBag.Submit = lp.getContentWithNoPrefix("Submit");
                    break;
                case "OrderMain":
                    ViewBag.Title = Title;
                    ViewBag.SalesOrderNumber = lp.getContentWithNoPrefix("SalesOrderNumber");
                    ViewBag.CustomerName = lp.getContentWithNoPrefix("CustomerName");
                    ViewBag.Submit = lp.getContentWithNoPrefix("SubmitNewServiceCase");
                    ViewBag.DateOfInstallation = lp.getContentWithNoPrefix("DateOfInstallation");
                    ViewBag.CustomerEmail = lp.getContentWithNoPrefix("CustomerEmail");
                    ViewBag.OrderMainTitle = lp.getContentWithNoPrefix("OrderMainTitle");
                    ViewBag.TransportTeamName = lp.getContentWithNoPrefix("TransportTeamName");
                    ViewBag.InstallationTeamName = lp.getContentWithNoPrefix("InstallationTeamName");
                    ViewBag.AlreadyInstalled = lp.getContentWithNoPrefix("AlreadyInstalled");
                    ViewBag.Return = lp.getContentWithNoPrefix("Return");
                    ViewBag.InstallationPage = lp.getContentWithNoPrefix("InstallationPage");
                    break;
                case "AlreadyInstalled":
                    ViewBag.Title = Title;
                    ViewBag.Return = lp.getContentWithNoPrefix("Return");
                    ViewBag.CustomerProductNo = lp.getContentWithNoPrefix("CustomerProductNo");
                    ViewBag.ProductName = lp.getContentWithNoPrefix("ProductName");
                    ViewBag.ExpectedQty = lp.getContentWithNoPrefix("ExpectedQty");
                    ViewBag.InstalledQty = lp.getContentWithNoPrefix("InstalledQty");
                    ViewBag.ProductSN = lp.getContentWithNoPrefix("ProductSN");
                    ViewBag.InstallMainIssue = lp.getContentWithNoPrefix("InstallMainIssue");
                    ViewBag.InstallMainIssueUpload = lp.getContentWithNoPrefix("InstallMainIssueUpload");
                    ViewBag.InstallMainIssueNotes = lp.getContentWithNoPrefix("InstallMainIssueNotes");
                    ViewBag.NoData = lp.getContentWithNoPrefix("NoData");
                    break;
                case "InstallMain":
                    ViewBag.Title = Title;
                    ViewBag.SearchMachine = lp.getContentWithNoPrefix("SearchMachine");
                    ViewBag.RequestDate = lp.getContentWithNoPrefix("RequestDate");
                    ViewBag.ProductList = lp.getContentWithNoPrefix("ProductList");
                    ViewBag.InstallMainAction = lp.getContentWithNoPrefix("InstallMainAction");
                    ViewBag.ProductName = lp.getContentWithNoPrefix("ProductName");
                    ViewBag.ProductSN = lp.getContentWithNoPrefix("ProductSN");
                    ViewBag.InstallMainQuantity = lp.getContentWithNoPrefix("InstallMainQuantity");
                    ViewBag.InstallMainIssue = lp.getContentWithNoPrefix("InstallMainIssue");
                    ViewBag.InstallMainIssueUpload = lp.getContentWithNoPrefix("InstallMainIssueUpload");
                    ViewBag.InstallMainIssueNotes = lp.getContentWithNoPrefix("InstallMainIssueNotes");
                    ViewBag.OrderMainQuantity = lp.getContentWithNoPrefix("OrderMainQuantity");
                    ViewBag.ProductNo = lp.getContentWithNoPrefix("ProductNo");
                    ViewBag.ConfirmInstallation = lp.getContentWithNoPrefix("ConfirmInstallation");
                    ViewBag.ReportIssue = lp.getContentWithNoPrefix("ReportIssue");
                    ViewBag.ReportNoIssue = lp.getContentWithNoPrefix("ReportNoIssue");
                    ViewBag.SearchData = lp.getContentWithNoPrefix("SearchData");
                    ViewBag.ToBeInstalled = lp.getContentWithNoPrefix("ToBeInstalled");
                    ViewBag.InstalledThisTime = lp.getContentWithNoPrefix("InstalledThisTime");
                    ViewBag.AllproductsnoissueMess = lp.getContentWithNoPrefix("AllproductsnoissueMess");
                    ViewBag.ExceptedAmount = lp.getContentWithNoPrefix("ExceptedAmount");
                    ViewBag.DeliveredAmount = lp.getContentWithNoPrefix("DeliveredAmount");
                    ViewBag.IssueTitle = lp.getContentWithNoPrefix("IssueTitle");
                    ViewBag.IssueNotes = lp.getContentWithNoPrefix("IssueNotes");
                    ViewBag.AttachImage = lp.getContentWithNoPrefix("AttachImage");
                    ViewBag.IssueSave = lp.getContentWithNoPrefix("IssueSave");
                    ViewBag.IssueSaveSuccess = lp.getContentWithNoPrefix("IssueSaveSuccess");
                    ViewBag.NoData = lp.getContentWithNoPrefix("NoData");
                    break;
                case "InstallationCompletion":
                    ViewBag.Title = Title;

                    ViewBag.InstallerSigning = lp.getContentWithNoPrefix("InstallerSigning");
                    ViewBag.ClubManagerSigning = lp.getContentWithNoPrefix("ClubManagerSigning");
                    ViewBag.Return = lp.getContentWithNoPrefix("Return");
                    ViewBag.Next = lp.getContentWithNoPrefix("Next");
                    ViewBag.CompleteInstallation = lp.getContentWithNoPrefix("CompleteInstallation");
                    ViewBag.DateofSignature = lp.getContentWithNoPrefix("DateofSignature");
                    ViewBag.ClubManager = lp.getContentWithNoPrefix("ClubManager");
                    ViewBag.Installer = lp.getContentWithNoPrefix("Installer");
                    ViewBag.ClearSignature = lp.getContentWithNoPrefix("ClearSignature");
                    ViewBag.LockSignature = lp.getContentWithNoPrefix("LockSignature");
                    ViewBag.InstallationCompletedMess = lp.getContentWithNoPrefix("InstallationCompletedMess");
                    ViewBag.InstallerName = lp.getContentWithNoPrefix("InstallerName");
                    ViewBag.ClubManagerName = lp.getContentWithNoPrefix("ClubManagerName");
                    ViewBag.ClubManagerMail = lp.getContentWithNoPrefix("ClubManagerMail");
                    ViewBag.ClubManagerEnterEmail = lp.getContentWithNoPrefix("ClubManagerEnterEmail");
                    break;

                case "OrderInformation":
                    ViewBag.Title = Title;
                    ViewBag.PageTitle = lp.getContentWithNoPrefix("OrderInformation");
                    ViewBag.NoData = lp.getContentWithNoPrefix("NoData");
                    ViewBag.SearchData = lp.getContentWithNoPrefix("SearchData");
                    ViewBag.SalesOrderNumber = lp.getContentWithNoPrefix("SalesOrderNumber");
                    ViewBag.CustomerName = lp.getContentWithNoPrefix("CustomerName");
                    ViewBag.Detail = lp.getContentWithNoPrefix("Detail");
                    break;
                case "CustomerOrderInformation":
                    ViewBag.Title = Title;
                    ViewBag.Detail = lp.getContentWithNoPrefix("Detail");
                    ViewBag.PageTitle = lp.getContentWithNoPrefix("CustomerOrderInformation");
                    ViewBag.NoData = lp.getContentWithNoPrefix("NoData");
                    ViewBag.RequestDeliveryBy = lp.getContentWithNoPrefix("RequestDeliveryBy");
                    ViewBag.SalesOrderNumber = lp.getContentWithNoPrefix("SalesOrderNumber");

                    break;
            }
        }

        //取得layout中按鈕的說明
        private void GetActionTitles()
        {
            LanguagePackage lp = new LanguagePackage(CountryCode);
            ViewBag.ActionOrderInformation = lp.getContentWithNoPrefix("ActionOrderInformation");
            ViewBag.ActionEmailUs = lp.getContentWithNoPrefix("ActionEmailUs");
            ViewBag.ActionChangePassword = lp.getContentWithNoPrefix("ActionChangePassword");
            ViewBag.ActionLogOut = lp.getContentWithNoPrefix("ActionLogOut");
        }

        [HttpPost]
        public JsonResult SearchProductData(string productNo, string salesOrderID, string requestDeliveryBy)
        {
            GetViewBag("InstallMain");

            try
            {
                var fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName]);
                var pshvm = new InstalledProductsViewModel
                {
                    ProductNo = productNo,
                    Search = true,
                    SalesOrderID = salesOrderID, // 記得補這個
                    RequestDate = DateTime.Parse(requestDeliveryBy), // <- 請確保格式正確
                    Page = 1
                };

                var cbl = new CaseBusinessLogic();
                cbl.GetOrderProductInformation(pshvm, fatr);

                if (pshvm.ProductDetail.Count > 0)
                {
                    // 重點：把畫面內容渲染成 HTML
                    string html = RenderRazorViewToString(this.ControllerContext, "_InstallMain", pshvm);

                    return Json(new { success = true, html = html });
                }
                else
                {
                    return Json(new { success = false, message = "查無資料。" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "發生錯誤：" + ex.Message });
            }

        }

        [HttpPost]
        public JsonResult SearchProductData2(string productNo, string salesOrderID, string requestDeliveryBy)
        {
            GetViewBag("InstallMain");

            try
            {
                var fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName]);
                var pshvm = new InstalledProductsViewModel
                {
                    ProductNo = productNo,
                    Search = true,
                    SalesOrderID = salesOrderID, // 記得補這個
                    RequestDate = DateTime.Parse(requestDeliveryBy), // <- 請確保格式正確
                    Page = 1
                };

                var cbl = new CaseBusinessLogic();
                cbl.GetOrderProductInformation2(pshvm, fatr);

                if (pshvm.ProductDetail.Count > 0)
                {
                    // 重點：把畫面內容渲染成 HTML
                    string html = RenderRazorViewToString(this.ControllerContext, "_InstallMain", pshvm);

                    return Json(new { success = true, html = html });
                }
                else
                {
                    return Json(new { success = false, message = "查無資料。" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "發生錯誤：" + ex.Message });
            }

        }

        protected string RenderRazorViewToString(ControllerContext context, string viewName, object model)
        {
            context.Controller.ViewData.Model = model;
            using (var sw = new StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(context, viewName);
                var viewContext = new ViewContext(context, viewResult.View, context.Controller.ViewData, context.Controller.TempData, sw);
                viewResult.View.Render(viewContext, sw);
                viewResult.ViewEngine.ReleaseView(context, viewResult.View);
                return sw.GetStringBuilder().ToString();
            }
        }

        [HttpPost]
        public ActionResult OrderSaveData(string salesOrderId, string requestDeliveryBy)
        {
            if (string.IsNullOrWhiteSpace(salesOrderId))
            {
                return new HttpStatusCodeResult(400, "Invalid SalesOrderId");
            }

            try
            {
                FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName]);
                InstallationViewModel nscvm = new InstallationViewModel { CountryCode2 = "NL" };
                CaseBusinessLogic cbl = new CaseBusinessLogic();

                // 取得安裝資訊
                Installation installation = GetInstallationByOrderId(salesOrderId, requestDeliveryBy);

                // 只有當 installation 為 null 或沒有 NewInstallationId 時才執行插入
                if (installation == null)
                {
                    Tuple<bool, Guid> insertResult = cbl.InsertNewInstallation(nscvm, fatr, salesOrderId, requestDeliveryBy);
                    bool isSuccess = insertResult.Item1;
                    Guid newInstallationId = insertResult.Item2;

                    // 檢查插入是否成功
                    if (!isSuccess)
                    {
                        return new HttpStatusCodeResult(400, "Failed to insert new installation");
                    }
                }

                return new HttpStatusCodeResult(200, "Insert successful");
            }
            catch (Exception ex)
            {
                // 記錄完整錯誤資訊
                System.Diagnostics.Debug.WriteLine("Error inserting installation: " + ex.ToString());
                return new HttpStatusCodeResult(500, "Internal server error");
            }
        }

        [HttpPost]
        public ActionResult OrderUpdateData(string salesOrderId, string requestDeliveryBy, string installationTeamName, string transportTeamName)
        {
            if (string.IsNullOrWhiteSpace(salesOrderId))
            {
                return new HttpStatusCodeResult(400, "Invalid SalesOrderId");
            }

            try
            {
                FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName]);
                InstallationViewModel nscvm = new InstallationViewModel { CountryCode2 = "NL" };
                CaseBusinessLogic cbl = new CaseBusinessLogic();

                // 取得安裝資訊
                Installation installation = GetInstallationByOrderId(salesOrderId, requestDeliveryBy);

                // 只有當 installation 為 null 或沒有 NewInstallationId 時才執行插入
                if (installation != null)
                {
                    bool isSuccess = cbl.UpdateInstallation(fatr, installation.NewInstallationId, requestDeliveryBy, installationTeamName, transportTeamName);

                    // 檢查更新是否成功
                    if (!isSuccess)
                    {
                        return new HttpStatusCodeResult(400, "Failed to update installation");
                    }
                    return new HttpStatusCodeResult(200, "Update successful");
                }
                else
                {
                    return new HttpStatusCodeResult(404, "Installation not found. ");
                }
            }
            catch (Exception ex)
            {
                // 記錄完整錯誤資訊
                System.Diagnostics.Debug.WriteLine("Error updating installation: " + ex.ToString());
                return new HttpStatusCodeResult(500, "Internal server error");
            }
        }

        [HttpPost]
        public ActionResult SaveIssueData(ReportIssueVieModel nscvm)
        {
            if (string.IsNullOrWhiteSpace(nscvm.SalesOrderId))
            {
                return Json(new { success = false, message = "Invalid SalesOrderId" });
            }

            try
            {
                FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName]);

                nscvm.CountryCode2 = "NL";
                CaseBusinessLogic cbl = new CaseBusinessLogic();

                // 嘗試取得 Installation 資料
                Installation installation = GetInstallationByOrderId(nscvm.SalesOrderId, nscvm.RequestDeliveryBy);
                if (installation == null)
                {
                    return Json(new { error = false, message = "No installation found for this SalesOrderId" });
                }

                InstallationDetail installationDetail = GetInstallationDetailByInstallationId(installation.NewInstallationId, nscvm.ProductId);

                // 若 InstallationDetail 不存在，插入新的 issue
                if (installationDetail == null)
                {
                    var insertResult = cbl.InsertNewInstallationIssue(nscvm, fatr, nscvm.SalesOrderId, installation.NewInstallationId);
                    if (!insertResult.Item1)
                    {
                        return Json(new { error = false, message = "Failed to insert new installation issue" });
                    }

                    return Json(new { success = true, message = "Issue saved successfully" });
                }
                else
                {
                    var UpdateResult = cbl.UpdateInstallationIssue(nscvm, fatr, installationDetail.InstallationDetailId);
                    if (!UpdateResult.Item1)
                    {
                        return Json(new { error = false, message = "Failed to insert new installation issue" });
                    }
                    return Json(new { success = true, message = "Issue updated successfully" });
                }

                // 已存在相同 InstallationDetail
                //return Json(new { error = false, message = "Issue data already exists" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error saving issue data: " + ex.ToString());
                return Json(new { error = false, message = "Internal server error" });
            }
        }

        [HttpPost]
        public ActionResult SaveInstallationData(string salesOrderId, string requestDeliveryBy, InstallationViewModel model)
        {
            if (string.IsNullOrWhiteSpace(salesOrderId))
                return Json(new { success = false, message = "Invalid SalesOrderId" });

            try
            {
                var fatr = new FormsAuthenticationTicketReader(Request.Cookies[FormsAuthentication.FormsCookieName]);
                var cbl = new CaseBusinessLogic();

                // 取得 Installation 記錄
                var installation = GetInstallationByOrderId(salesOrderId, requestDeliveryBy);
                if (installation == null)
                    return Json(new { success = false, message = "Installation not found" });

                // 執行更新
                var updateResult = cbl.UpdateInstallationComplete(fatr, installation.NewInstallationId, model);
                if (!updateResult)
                    return Json(new { success = false, message = "Failed to update installation" });

                // 二次取得更新後安裝資料（若有必要）
                var refreshedInstallation = GetInstallationByOrderId(salesOrderId, requestDeliveryBy);
                if (refreshedInstallation == null)
                    return Json(new { success = false, message = "Installation not found after update" });

                var salesOrder = GetSalesOrderById(salesOrderId);
                var account = GetAccountByCustomerId(salesOrder?.CustomerId);

                // 下載 PDF 並寄送郵件
                byte[] pdfBytes = Modules.ReportService.DownLoadPDF(salesOrderId, refreshedInstallation.NewInstallationId, requestDeliveryBy);
                var pdfList = new List<byte[]> { pdfBytes };

                //  若為測試用途可替代 Sleep
                // System.Threading.Thread.Sleep(3000);

                bool emailSent = Modules.ReportService.SendEmailbyCase(
                    pdfList,
                    refreshedInstallation.NewName,
                    refreshedInstallation.ClubManagerName,
                    salesOrderId,
                    refreshedInstallation.NewInstallationId,
                    refreshedInstallation.CreatedOn.ToString("yyyy-MM-dd"),
                    account?.EmailAddress1 ?? string.Empty,
                    model?.ClubManagerEmail ?? string.Empty
                );

                return Json(new { success = true, message = "Update successful", emailSent });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error updating installation: " + ex);
                return Json(new { success = false, message = "Internal server error" });
            }
        }

    }
}
