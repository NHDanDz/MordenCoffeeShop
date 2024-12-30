using Microsoft.AspNetCore.Mvc;
using DemoApp_Test.Areas.Admin;
using DemoApp_Test.Models;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Rendering;
using DemoApp_Test.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using DemoApp_Test.Repository;
using DemoApp_Test.Enums;
using Microsoft.EntityFrameworkCore.Storage;
using System.Text.Json;
using DemoApp_Test.Authorization;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Text;
using System.ComponentModel.DataAnnotations;
using DemoApp_Test.Services;


namespace DemoApp_Test.Area.Admin.Controllers
{

    [Area("Admin")]
     public class AdminController : Controller
    {
        private readonly ILogger<AdminController> _logger;
        private readonly DataContext _db;
        private readonly IWebHostEnvironment _webHostEnvironment;


        public AdminController(DataContext context, ILogger<AdminController> logger, IWebHostEnvironment webHostEnvironment)
        {
            _db = context;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }

        private async Task LogAdminActivity(string actionType, string actionDetails)
        {
            try
            {
                var username = HttpContext.Session.GetString("username");
                if (string.IsNullOrEmpty(username))
                {
                    _logger.LogWarning("No username found in session while trying to log admin activity");
                    return;
                }

                var activity = new AdminActivity
                {
                    Username = username,
                    ActionType = actionType,
                    ActionDetails = actionDetails,
                    ActionTime = DateTime.Now
                };

                await _db.AdminActivity.AddAsync(activity);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging admin activity");
            }
        }
        [AuthorizeRoles(UserRole.Admin, UserRole.Staff)]
        public async Task<IActionResult> Index()
        {
            try
            {
                var (totalVisitors, onlineUsers) = VisitorCounter.GetVisitorStats(HttpContext);
                ViewBag.TotalVisitors = totalVisitors;
                ViewBag.OnlineUsers = onlineUsers;
                var endDate = DateTime.Now;
                var startDate = endDate.AddMonths(-5); // 6 months including current
                var previousStartDate = startDate.AddMonths(-6); // previous 6 months period

                // Basic metrics for bills
                var billMetrics = await (from b in _db.Bill
                                         group b by 1 into g
                                         select new
                                         {
                                             TotalRevenue = g.Where(b => b.PaymentStatus).Sum(b => b.Total),
                                             MonthlyRevenue = g.Where(b => b.PaymentStatus && b.Date.Month == DateTime.Now.Month).Sum(b => b.Total),
                                             TotalOrders = g.Count(),
                                             PendingOrders = g.Count(b => !b.Status),
                                             PaidOrders = g.Count(b => b.PaymentStatus),
                                             AverageOrderValue = g.Where(b => b.PaymentStatus).Average(b => b.Total),
                                             HighestOrderValue = g.Max(b => b.Total)
                                         }).FirstOrDefaultAsync();

                // Product metrics
                var productMetrics = await (from p in _db.Product
                                            group p by 1 into g
                                            select new
                                            {
                                                ActiveProducts = g.Count(p => p.Status),
                                                TotalProducts = g.Count(),
                                                ArchivedProducts = g.Count(p => !p.Status),
                                                AverageRating = g.Average(p => p.Rating ?? 0)
                                            }).FirstOrDefaultAsync();

                // Assign basic metrics
                ViewBag.TotalRevenue = billMetrics.TotalRevenue;
                ViewBag.MonthlyRevenue = billMetrics.MonthlyRevenue;
                ViewBag.TotalOrders = billMetrics.TotalOrders;
                ViewBag.PendingOrders = billMetrics.PendingOrders;
                ViewBag.ActiveProducts = productMetrics.ActiveProducts;
                ViewBag.TotalProducts = productMetrics.TotalProducts;
                ViewBag.AverageOrderValue = billMetrics.AverageOrderValue;
                ViewBag.HighestOrderValue = billMetrics.HighestOrderValue;

                // Revenue trend data
                var bills = await _db.Bill
                    .Where(b => b.Date >= startDate && b.PaymentStatus)
                    .Select(b => new { b.Date, b.Total })
                    .ToListAsync();

                var revenueByMonth = bills
                    .GroupBy(b => new { b.Date.Year, b.Date.Month })
                    .Select(g => new
                    {
                        Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                        Revenue = g.Sum(b => b.Total),
                        OrderCount = g.Count()
                    })
                    .OrderBy(x => x.Date)
                    .ToList();

                ViewBag.RevenueDates = revenueByMonth.Select(x => x.Date.ToString("MMM yyyy")).ToList();
                ViewBag.RevenueData = revenueByMonth.Select(x => x.Revenue).ToList();
                ViewBag.OrdersData = revenueByMonth.Select(x => x.OrderCount).ToList();

                // Calculate revenue growth
                var previousRevenue = await _db.Bill
                    .Where(b => b.Date >= previousStartDate && b.Date < startDate && b.PaymentStatus)
                    .SumAsync(b => b.Total);

                var currentRevenue = bills.Sum(b => b.Total);
                var revenueGrowth = previousRevenue > 0
                    ? ((currentRevenue - previousRevenue) / previousRevenue) * 100
                    : 100;
                ViewBag.RevenueGrowth = Math.Round(revenueGrowth, 1);

                // Payment statistics
                ViewBag.PaidOrdersCount = billMetrics.PaidOrders;
                ViewBag.UnpaidOrdersCount = billMetrics.TotalOrders - billMetrics.PaidOrders;
                ViewBag.PaidOrdersPercentage = billMetrics.TotalOrders > 0
                    ? ((double)billMetrics.PaidOrders / billMetrics.TotalOrders * 100).ToString("N1")
                    : "0.0";
                ViewBag.UnpaidOrdersPercentage = billMetrics.TotalOrders > 0
                    ? (100 - ((double)billMetrics.PaidOrders / billMetrics.TotalOrders * 100)).ToString("N1")
                    : "0.0";
                // Payment methods distribution
                var paymentMethods = await _db.Bill
                    .Where(b => !string.IsNullOrEmpty(b.PaymentMethod))
                    .GroupBy(b => b.PaymentMethod)
                    .Select(g => new { Method = g.Key, Count = g.Count() })
                    .ToListAsync();

                // Translate payment methods to Vietnamese
                var translatedPaymentMethods = paymentMethods.Select(p =>
                {
                    switch (p.Method)
                    {
                        case "Cash":
                            return "Tiền mặt";
                        case "Credit Card":
                            return "Thẻ tín dụng";
                        case "Bank Transfer":
                            return "Chuyển khoản ngân hàng";
                        default:
                            return p.Method; // Retain the original method if no translation is defined
                    }
                }).ToList();

                // Assign translated payment methods and their counts to ViewBag
                ViewBag.PaymentMethods = translatedPaymentMethods;
                ViewBag.PaymentMethodCounts = paymentMethods.Select(p => p.Count).ToList();


                // Recent orders
                ViewBag.RecentOrders = await _db.Bill
                    .Include(b => b.Client)
                    .OrderByDescending(b => b.Date)
                    .Take(10)
                    .ToListAsync();

                // Product distribution by brand
                var brandDistribution = await _db.Product
                    .GroupBy(p => p.Brand.BrandName)
                    .Select(g => new { Brand = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync();

                ViewBag.BrandLabels = brandDistribution.Select(x => x.Brand).ToList();
                ViewBag.BrandData = brandDistribution.Select(x => x.Count).ToList();

                // Product distribution by category
                var categoryDistribution = await _db.Product
                    .GroupBy(p => p.TypeCoffee.TypeName)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync();

                ViewBag.CategoryLabels = categoryDistribution.Select(x => x.Type).ToList();
                ViewBag.CategoryData = categoryDistribution.Select(x => x.Count).ToList();

                // Processing time statistics
                var completedOrders = await _db.Bill
                    .Where(b => b.Status && b.ProcessDate.HasValue)
                    .Select(b => EF.Functions.DateDiffHour(b.Date, b.ProcessDate.Value))
                    .ToListAsync();

                ViewBag.AverageProcessingHours = completedOrders.Any()
                    ? Math.Round(completedOrders.Average(), 1)
                    : 0;
                ViewBag.FastestProcessingHours = completedOrders.Any()
                    ? completedOrders.Min()
                    : 0;

                // Additional counts
                ViewBag.ArchivedProducts = productMetrics.ArchivedProducts;
                ViewBag.AverageRating = Math.Round(productMetrics.AverageRating, 1);
                ViewBag.TotalBrands = await _db.Brand.CountAsync();
                ViewBag.TotalTypes = await _db.TypeCoffee.CountAsync();

                await LogAdminActivity("VIEW_DASHBOARD", "Accessed dashboard overview");

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard data");
                return View();
            }
        }
        [AuthorizeRoles(UserRole.Admin)]
        public IActionResult AccountView(string query, string role)
        {
            try
            {
                var accountsQuery = _db.Account
                    .Include(a => a.Client)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(query))
                {
                    query = query.Trim().ToLower();
                    accountsQuery = accountsQuery.Where(a =>
                        a.Username.ToLower().Contains(query) ||
                        a.Client.Gmail.ToLower().Contains(query) ||
                        a.Client.Name.ToLower().Contains(query));
                }

                if (!string.IsNullOrEmpty(role) && Enum.TryParse<UserRole>(role, out UserRole userRole))
                {
                    accountsQuery = accountsQuery.Where(a => a.Role == userRole);
                }

                var model = new AccountSearchViewModel
                {
                    Query = query,
                    Role = role,
                    Accounts = accountsQuery.ToList()
                };
                var accounts = model.Accounts;
                ViewBag.TotalAccounts = accounts.Count();
                ViewBag.AdminCount = accounts.Count(a => a.Role == UserRole.Admin);
                ViewBag.StaffCount = accounts.Count(a => a.Role == UserRole.Staff);
                ViewBag.CustomerCount = accounts.Count(a => a.Role == UserRole.Customer);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AccountView");
                return View(new AccountSearchViewModel());
            }
        }

        [HttpGet]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<IActionResult> GetAccount(string username)
        {
            try
            {
                var account = await _db.Account
                    .Include(a => a.Client)
                    .FirstOrDefaultAsync(a => a.Username == username);

                if (account == null)
                {
                    return Json(new { success = false, message = "Account not found" });
                }

                return Json(new { success = true, account = account });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching account details");
                return Json(new { success = false, message = "Error fetching account details" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<IActionResult> CreateAccount([FromBody] AccountCreateModel model)
        {
            try
            {
 
                // Validate required fields 

                // Check if username already exists
                if (await _db.Account.AnyAsync(a => a.Username == model.Username))
                {
                    return Json(new { success = false, message = "Username already exists" });
                }

                // Check if email already exists
                if (await _db.Client.AnyAsync(c => c.Gmail == model.Email))
                {
                    return Json(new { success = false, message = "Email already exists" });
                }
                if (!Enum.TryParse<UserRole>(model.Role, out UserRole userRole))
                {
                    return Json(new { success = false, message = "Invalid role specified" });
                }
                try
                {
                    string clientId = await GenerateNewClientId();
                    clientId = clientId.PadRight(10);  // Đảm bảo clientId đủ 10 ký tự

                    // Tạo client trước
                    var client = new Client
                    {
                        Client_id = clientId,
                        Name = model.Name,
                        Gmail = model.Email,
                        Contact = model.Contact,  // Fix lỗi truncate bằng cách pad đủ 10 ký tự
                        Location = model.Location
                    };

                    // Thêm và lưu client trước
                    _db.Client.Add(client);
                    await _db.SaveChangesAsync();

                    // Sau khi client đã được tạo, tạo account
                    var account = new Account
                    {
                        Username = model.Username,
                        Password = model.Password,
                        RegistrationDate = DateTime.Now,
                        Role = userRole,
                        Client_id = clientId  // Sử dụng lại clientId đã pad
                    };

                    _db.Account.Add(account);
                    await _db.SaveChangesAsync();
                    _logger.LogInformation($"Successfully created account for {model.Username}");
                    await LogAdminActivity("CREATE_ACCOUNT", $"Created account: {model.Username}");

                    return Json(new { success = true, message = "Account created successfully" });
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError($"Database update error: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                    }
                    return Json(new { success = false, message = "Database error occurred" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in CreateAccount");
                return Json(new { success = false, message = "An unexpected error occurred" });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<IActionResult> EditAccount([FromBody] AccountEditModel model)
        {
            try
            {
                _logger.LogInformation($"Received edit request for username: {model.Username}");

                // Validate the model
                if (string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Name) ||
                    string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Role))
                {
                    return Json(new { success = false, message = "Required fields are missing" });
                }

                // Find the account
                Account account = await _db.Account
                    .Include(a => a.Client)
                    .FirstOrDefaultAsync(a => a.Username == model.Username);

                if (account == null)
                {
                    _logger.LogError($"Account not found: {model.Username}");
                    return Json(new { success = false, message = "Account not found" });
                }

                // Parse role
                if (!Enum.TryParse<UserRole>(model.Role, out UserRole userRole))
                {
                    _logger.LogError($"Invalid role value: {model.Role}");
                    return Json(new { success = false, message = "Invalid role specified" });
                }

                try
                {
                    // Update or create client information
                    if (account.Client == null)
                    {
                        // Create new client with a new client ID
                        string clientId = await GenerateNewClientId();
                        account.Client = new Client
                        {
                            Client_id = clientId,
                            Name = model.Name,
                            Gmail = model.Email,
                            Contact = model.Contact,
                            Location = model.Location
                        };
                        account.Client_id = clientId;  // Update the foreign key
                        _db.Client.Add(account.Client);
                        _logger.LogInformation($"Created new client with ID: {clientId}");
                    }
                    else
                    {
                        // Update existing client
                        account.Client.Name = model.Name;
                        account.Client.Gmail = model.Email;
                        account.Client.Contact = model.Contact;
                        account.Client.Location = model.Location;
                        _logger.LogInformation($"Updated existing client with ID: {account.Client_id}");
                    }
                    if(model.Password != "" && model.Password != null)
                          account.Password = model.Password;
                    

                    // Update role
                    account.Role = userRole;

                    // Save changes
                    await _db.SaveChangesAsync();

                    _logger.LogInformation($"Successfully updated account for {model.Username}");
                    await LogAdminActivity("EDIT_ACCOUNT", $"Updated account: {model.Username}");

                    return Json(new { success = true, message = "Account updated successfully" });
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError($"Database update error: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                    }
                    return Json(new { success = false, message = "Database update error" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in EditAccount");
                return Json(new { success = false, message = "An unexpected error occurred" });
            }
        }
        private async Task<string> GenerateNewClientId()
        {
            var lastId = await _db.Client
                .Where(c => c.Client_id.StartsWith("KH"))
                .Select(c => c.Client_id)
                .OrderByDescending(id => id)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(lastId))
            {
                return "KH001";
            }

            if (int.TryParse(lastId.Substring(2), out int lastNumber))
            {
                return $"KH{(lastNumber + 1):D3}";
            }

            throw new Exception("Invalid client ID format in database");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<IActionResult> DeleteAccount(string id)  // Thay đổi tham số để nhận username từ route
        {
            try
            {
                // Log thông tin xóa
                _logger.LogInformation($"Attempting to delete account with username: {id}");

                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("Delete attempt with null or empty username");
                    return Json(new { success = false, message = "Username is required" });
                }

                // Tìm account kèm theo thông tin client
                var account = await _db.Account
                    .Include(a => a.Client)
                    .FirstOrDefaultAsync(a => a.Username == id);

                if (account == null)
                {
                    _logger.LogWarning($"Account not found for deletion: {id}");
                    return Json(new { success = false, message = "Account not found" });
                }

                // Prevent deleting your own account
                if (User.Identity.Name == id)
                {
                    _logger.LogWarning($"Attempt to delete own account: {id}");
                    return Json(new { success = false, message = "Cannot delete your own account" });
                }

                // Xóa client trước nếu có
                if (account.Client != null)
                {
                    _db.Client.Remove(account.Client);
                }

                // Xóa account
                _db.Account.Remove(account);
                await LogAdminActivity("DELETE_ACCOUNT", $"Deleted account: {id}");

                await _db.SaveChangesAsync();

                _logger.LogInformation($"Successfully deleted account: {id}");
                return Json(new { success = true, message = $"Account {id} has been deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting account: {id}");
                return Json(new { success = false, message = "Error deleting account. Please try again." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<IActionResult> BulkDeleteAccounts([FromBody] List<string> usernames)
        {
            try
            {
                if (usernames == null || !usernames.Any())
                {
                    return Json(new { success = false, message = "No accounts selected" });
                }

                // Remove current user's username from the list
                usernames = usernames.Where(u => u != User.Identity.Name).ToList();

                var accounts = await _db.Account
                    .Where(a => usernames.Contains(a.Username))
                    .ToListAsync();
                var accountDetails = string.Join(", ", accounts.Select(a =>
    $"{a.Username} (Role: {a.Role}, Client: {a.Client?.Name ?? "N/A"})"
));

                _db.Account.RemoveRange(accounts);
                await _db.SaveChangesAsync();
                await LogAdminActivity("BULK_DELETE_ACCOUNTS",
    $"Bulk deleted {accounts.Count} accounts: {accountDetails}");

                return Json(new
                {
                    success = true,
                    message = $"Successfully deleted {accounts.Count} accounts"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk delete");
                return Json(new { success = false, message = "Error deleting accounts" });
            }
        }




        [AuthorizeRoles(UserRole.Admin, UserRole.Staff)]
        public IActionResult ShowProduct(string query, float? minPrice, float? maxPrice, string brandId, float? minRate, float? maxRate, int? minDiscount, int? maxDiscount, string TypeId)
        {
            var userRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            _logger.LogInformation($"Current user role: {userRole}");
            query = query?.Trim();

            var model = new ProductSearchViewModel
            {
                Query = query,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                BrandId = brandId,
                MinRate = minRate,
                MaxRate = maxRate,
                MinDiscount = minDiscount,
                MaxDiscount = maxDiscount,
                TypeId = TypeId,

                Product = _db.Product
                    .Include(p => p.Brand)
                    .Include(p => p.TypeCoffee)
                    .Where(p => p.Status == true) // Only show active products
                    .Where(p => (string.IsNullOrEmpty(query) || p.ProductName.Contains(query) || p.Brand.BrandName.Contains(query))
                        && (!minPrice.HasValue || p.Price >= minPrice)
                        && (!maxPrice.HasValue || p.Price <= maxPrice)
                        && (!minRate.HasValue || p.Rating >= minRate)
                        && (!maxRate.HasValue || p.Rating <= maxRate)
                        && (!minDiscount.HasValue || p.Discount >= minDiscount)
                        && (!maxDiscount.HasValue || p.Discount <= maxDiscount)
                        && (string.IsNullOrEmpty(brandId) || p.Brand_id == brandId)
                        && (string.IsNullOrEmpty(TypeId) || p.Type_id == TypeId))
                    .ToList()
            };

            ViewBag.Brands = new SelectList(_db.Brand, "Brand_id", "BrandName");
            ViewBag.TypeCoffees = new SelectList(_db.TypeCoffee, "Type_id", "TypeName");
            // Tính toán các thống kê từ danh sách sản phẩm đã lọc
            var products = model.Product; // Lấy danh sách sản phẩm đã filter
            ViewBag.FilteredTotal = products.Count();
            ViewBag.FilteredAvgPrice = products.Any() ? products.Average(p => p.Price) : 0;
            ViewBag.FilteredAvgRating = products.Any() ? products.Average(p => p.Rating ?? 0) : 0;
            ViewBag.FilteredAvgDiscount = products.Any() ? products.Average(p => p.Discount) : 0;
            return View(model);
        }


        [AuthorizeRoles(UserRole.Admin, UserRole.Staff)]
        public IActionResult ProductStorageView(string query, float? minPrice, float? maxPrice, string brandId, float? minRate, float? maxRate, int? minDiscount, int? maxDiscount, string TypeId)
        {
            var userRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            _logger.LogInformation($"Current user role: {userRole}");
            query = query?.Trim();

            var model = new ProductSearchViewModel
            {
                Query = query,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                BrandId = brandId,
                MinRate = minRate,
                MaxRate = maxRate,
                MinDiscount = minDiscount,
                MaxDiscount = maxDiscount,
                TypeId = TypeId,

                Product = _db.Product
                    .Include(p => p.Brand)
                    .Include(p => p.TypeCoffee)
                    .Where(p => p.Status == false) // Only show active products
                    .Where(p => (string.IsNullOrEmpty(query) || p.ProductName.Contains(query) || p.Brand.BrandName.Contains(query))
                        && (!minPrice.HasValue || p.Price >= minPrice)
                        && (!maxPrice.HasValue || p.Price <= maxPrice)
                        && (!minRate.HasValue || p.Rating >= minRate)
                        && (!maxRate.HasValue || p.Rating <= maxRate)
                        && (!minDiscount.HasValue || p.Discount >= minDiscount)
                        && (!maxDiscount.HasValue || p.Discount <= maxDiscount)
                        && (string.IsNullOrEmpty(brandId) || p.Brand_id == brandId)
                        && (string.IsNullOrEmpty(TypeId) || p.Type_id == TypeId))
                    .ToList()
            };

            ViewBag.Brands = new SelectList(_db.Brand, "Brand_id", "BrandName");
            ViewBag.TypeCoffees = new SelectList(_db.TypeCoffee, "Type_id", "TypeName");

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserRole.Admin, UserRole.Staff)]
        public async Task<IActionResult> RestoreProduct(string id)
        {
            try
            {
                var product = await _db.Product.FindAsync(id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                product.Status = true; // Restore the product
                await _db.SaveChangesAsync();
                await LogAdminActivity("RESTORE_PRODUCT", $"Restored product: {product.Product_id} - {product.ProductName}");

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring product");
                return Json(new { success = false, message = "Error restoring product" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserRole.Admin, UserRole.Staff)]
        public async Task<IActionResult> DeleteProductPermanently(string id)
        {
            try
            {
                var product = await _db.Product.FindAsync(id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                // Delete product image if it exists
                if (!string.IsNullOrEmpty(product.Image))
                {
                    string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "img",
                        product.Brand?.BrandName ?? "default",
                        product.TypeCoffee?.TypeName ?? "default",
                        product.Image);

                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _db.Product.Remove(product);
                await _db.SaveChangesAsync();
                await LogAdminActivity("DELETE_PRODUCT_PERMANENT", $"Permanently deleted product: {product.Product_id} - {product.ProductName}");

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error permanently deleting product");
                return Json(new { success = false, message = "Error deleting product" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserRole.Admin, UserRole.Staff)]
        public async Task<IActionResult> BulkRestoreProducts([FromBody] List<string> productIds)
        {
            if (productIds == null || !productIds.Any())
            {
                return Json(new { success = false, message = "No products selected" });
            }

            try
            {
                var products = await _db.Product.Where(p => productIds.Contains(p.Product_id)).ToListAsync();
                var productsInfo = string.Join(", ", products.Select(p => $"{p.Product_id} - {p.ProductName}"));

                foreach (var product in products)
                {
                    product.Status = true; // Restore the product
                }

                await _db.SaveChangesAsync();
                await LogAdminActivity("BULK_RESTORE_PRODUCTS", $"Bulk restored products: {productsInfo}");

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring products");
                return Json(new { success = false, message = "Error restoring products" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserRole.Admin, UserRole.Staff)]
        public async Task<IActionResult> ArchiveProduct(string id)
        {
            try
            {
                var product = await _db.Product.FindAsync(id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                product.Status = false; // Set to archived/inactive
                await _db.SaveChangesAsync();
                await LogAdminActivity("ARCHIVE_PRODUCT", $"Archived product: {product.Product_id} - {product.ProductName}");

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving product");
                return Json(new { success = false, message = "Error archiving product" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserRole.Admin, UserRole.Staff)]
        public async Task<IActionResult> BulkArchiveProducts([FromBody] List<string> productIds)
        {
            if (productIds == null || !productIds.Any())
            {
                return Json(new { success = false, message = "No products selected" });
            }

            try
            {
                var products = await _db.Product.Where(p => productIds.Contains(p.Product_id)).ToListAsync();
                var productsInfo = string.Join(", ", products.Select(p => $"{p.Product_id} - {p.ProductName}"));

                foreach (var product in products)
                {
                    product.Status = false; // Set to archived/inactive
                }

                await _db.SaveChangesAsync();
                await LogAdminActivity("BULK_ARCHIVE_PRODUCTS", $"Bulk archived products: {productsInfo}");

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving products");
                return Json(new { success = false, message = "Error archiving products" });
            }
        }


        [AuthorizeRoles(UserRole.Admin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] ProductCreateModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Validation failed" });
                }

                // Check if product with same name exists in the same brand
                var existingProduct = await _db.Product
                    .FirstOrDefaultAsync(p => p.ProductName.ToLower() == model.ProductName.ToLower()
                                            && p.Brand_id == model.BrandId);

                if (existingProduct != null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Sản phẩm đã tồn tại với nhãn hàng được chọn"
                    });
                }

                // Handle image file (Base64 decoding)
                string fileName = null;
                if (!string.IsNullOrEmpty(model.ImageBase64))
                {
                    var brand = await _db.Brand.FindAsync(model.BrandId);
                    var type = await _db.TypeCoffee.FindAsync(model.TypeId);
                    string targetDirectory = Path.Combine(
                        _webHostEnvironment.WebRootPath,
                        "img",
                        brand.BrandName,
                        type.TypeName
                    );
                    Directory.CreateDirectory(targetDirectory);
                    fileName = $"{model.ProductId}.jpg";
                    string filePath = Path.Combine(targetDirectory, fileName);
                    // Convert Base64 string to byte array
                    var imageBytes = Convert.FromBase64String(model.ImageBase64.Split(',')[1]); // Skip the "data:image/jpeg;base64," part
                                                                                                // Write the byte array to a file
                    await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);
                }

                // Create product object and save to database
                var product = new Product
                {
                    Product_id = model.ProductId,
                    ProductName = model.ProductName,
                    Price = model.Price,
                    Rating = model.Rating,
                    Discount = model.Discount,
                    Brand_id = model.BrandId,
                    Type_id = model.TypeId,
                    Status = true,
                    ReviewCount = model.ReviewCount,
                    Image = fileName,
                    Date = DateTime.Now
                };

                _db.Product.Add(product);
                await _db.SaveChangesAsync();
                await LogAdminActivity("CREATE_PRODUCT", $"Created product: {model.ProductId} - {model.ProductName}");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<IActionResult> Delete(string id)
        {
            // Tìm sản phẩm theo ID (đồng bộ)
            var product = _db.Product.Find(id);
            Console.WriteLine(product);
            var productInfo = $"{product.Product_id} - {product.ProductName}";

            if (product != null)
            {
                // Kiểm tra xem sản phẩm có hình ảnh không và nếu có thì xóa hình ảnh
                if (!string.IsNullOrEmpty(product.Image))
                {
                    string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "productImages", product.Image);
                    Console.WriteLine(imagePath);
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath); // Xóa file hình ảnh
                    }
                }

                _db.Product.Remove(product); // Xóa sản phẩm
                _db.SaveChanges(); // Lưu thay đổi vào cơ sở dữ liệu
            }
            await LogAdminActivity("DELETE_PRODUCT",
    $"Deleted product: {productInfo}");
            return RedirectToAction("ShowProduct"); // Chuyển hướng đến action ShowProduct
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<IActionResult> BulkDelete([FromBody] List<string> productIds)
        {
            if (productIds == null || !productIds.Any())
            {
                return Json(new { success = false, message = "No products selected" });
            }

            try
            {
                var products = _db.Product.Where(p => productIds.Contains(p.Product_id)).ToList();
                var productsInfo = string.Join(", ", products.Select(p => $"{p.Product_id} - {p.ProductName}"));

                foreach (var product in products)
                {
                    // Delete associated image file if it exists
                    if (!string.IsNullOrEmpty(product.Image))
                    {
                        string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "productImages", product.Image);
                        if (System.IO.File.Exists(imagePath))
                        {
                            System.IO.File.Delete(imagePath);
                        }
                    }

                    _db.Product.Remove(product);
                }

                _db.SaveChanges();
                await LogAdminActivity("BULK_DELETE_PRODUCTS", $"Bulk deleted products: {productsInfo}");
                return Json(new { success = true, message = $"Successfully deleted {products.Count} products" });
            }
            catch (Exception ex)
            {
                await LogAdminActivity("BULK_DELETE_PRODUCTS_ERROR",
      $"Failed to bulk delete products: {string.Join(", ", productIds)} - {ex.Message}");
                _logger.LogError(ex, "Error during bulk delete");
                return Json(new { success = false, message = "Error deleting products" });
            }
        }

        [HttpGet]
        [AuthorizeRoles(UserRole.Admin, UserRole.Staff)]
        public async Task<IActionResult> GetNewProductId()
        {

            try
            {
                // Lấy mã SP lớn nhất từ database
                string maxId = _db.Product
                    .Where(p => p.Product_id.StartsWith("SP"))
                    .Select(p => p.Product_id)
                    .OrderByDescending(id => id)
                    .FirstOrDefault();

                if (string.IsNullOrEmpty(maxId))
                {
                    // Nếu chưa có sản phẩm nào, bắt đầu từ SP001
                    return Json(new { success = true, productId = "SP001" });
                }
                if (int.TryParse(maxId.Substring(2), out int currentNumber))
                {
                    string newId = $"SP{(currentNumber + 1).ToString("D3")}";
                    return Json(new { success = true, productId = newId });
                }



 



                // Nếu có lỗi khi parse số, trả về thông báo lỗi
                return Json(new { success = false, message = "Invalid product ID format in database" });
            }
            catch (Exception ex)
            {
                await LogAdminActivity("GENERATE_PRODUCT_ID_ERROR",
    $"Failed to generate new product ID - {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        } 
        [AuthorizeRoles(UserRole.Admin, UserRole.Staff)]

        public class ProductCreateModel
        {
            public string ProductId { get; set; }
            public string ProductName { get; set; }
            public double Price { get; set; }
            public int Discount { get; set; }
            public string BrandId { get; set; }
            public string TypeId { get; set; }
            public int Rating { get; set; }
            public int ReviewCount { get; set; }
            public string ImageBase64 { get; set; } 

        }
        [AuthorizeRoles(UserRole.Admin, UserRole.Staff)]

        public class ProductEditModel
        {
            public string ProductId { get; set; }
            public string ProductName { get; set; }
            public double Price { get; set; }
            public int Discount { get; set; }
            public string BrandId { get; set; }
            public string TypeId { get; set; }
            public int Rating { get; set; }
            public int ReviewCount { get; set; }
            public string ImageBase64 { get; set; }
            public string Description { get; set; }

        } 

        [HttpGet]
        [AuthorizeRoles(UserRole.Admin, UserRole.Staff)]
        public async Task<IActionResult> GetProduct(string id)
        {
            try
            {
                var product = await _db.Product
                    .Include(p => p.Brand)
                    .Include(p => p.TypeCoffee)
                    .FirstOrDefaultAsync(p => p.Product_id == id);

                if (product == null)
                    return NotFound(new { success = false, message = "Product not found" });

                var response = new
                {
                    success = true,
                    product_id = product.Product_id,
                    productName = product.ProductName,
                    price = product.Price,
                    rating = product.Rating,
                    discount = product.Discount,
                    reviewCount = product.ReviewCount,
                    brand_id = product.Brand_id,
                    type_id = product.Type_id,
                    description = product.Description,
                    brand = new { brandName = product.Brand?.BrandName },
                    typeCoffee = new { typeName = product.TypeCoffee?.TypeName },
                    image = product.Image
                };

                return Json(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching product: {id}");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
        [AuthorizeRoles(UserRole.Admin, UserRole.Staff)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromBody] ProductEditModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Validation failed" });
                }

                var product = await _db.Product.FindAsync(model.ProductId);

                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                // Check if brand and type exist
                var brand = await _db.Brand.FindAsync(model.BrandId);
                var type = await _db.TypeCoffee.FindAsync(model.TypeId);

                if (brand == null || type == null)
                {
                    return Json(new { success = false, message = "Invalid brand or type" });
                }

                // Check if another product with the same name exists in the same brand
                var existingProduct = await _db.Product
                    .FirstOrDefaultAsync(p => p.ProductName.ToLower() == model.ProductName.ToLower()
                                            && p.Brand_id == model.BrandId
                                            && p.Product_id != model.ProductId); // Exclude current product

                if (existingProduct != null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Sản phẩm đã tồn tại tại với Nhãn hàng bạn chọn"
                    });
                }

                // Handle image update if new image is provided
                if (!string.IsNullOrEmpty(model.ImageBase64))
                {
                    try
                    {
                        // Delete old image if it exists
                        if (!string.IsNullOrEmpty(product.Image))
                        {
                            string oldImagePath = Path.Combine(
                                _webHostEnvironment.WebRootPath,
                                "img",
                                brand.BrandName,
                                type.TypeName,
                                product.Image
                            );

                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        // Save new image
                        string targetDirectory = Path.Combine(
                            _webHostEnvironment.WebRootPath,
                            "img",
                            brand.BrandName,
                            type.TypeName
                        );

                        Directory.CreateDirectory(targetDirectory);

                        string fileName = $"{model.ProductId}.jpg";
                        string filePath = Path.Combine(targetDirectory, fileName);

                        // Convert Base64 string to byte array and save
                        var imageData = model.ImageBase64.Split(',')[1];
                        var imageBytes = Convert.FromBase64String(imageData);
                        await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

                        // Update image filename in product
                        product.Image = fileName;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error handling image: {ex.Message}");
                        return Json(new { success = false, message = "Error updating product image" });
                    }
                }
                var oldValues = $"Name: {product.ProductName}, Price: {product.Price}, Brand: {product.Brand_id}, Type: {product.Type_id}";

                // Update product properties
                product.ProductName = model.ProductName;
                product.Price = model.Price;
                product.Discount = model.Discount;
                product.Brand_id = model.BrandId;
                product.Type_id = model.TypeId;
                product.Rating = model.Rating;
                product.ReviewCount = model.ReviewCount;
                product.Date = DateTime.Now;
                product.Description = model.Description;

                await LogAdminActivity("EDIT_PRODUCT",
        $"Updated product {model.ProductId}. Old values: [{oldValues}]. New values: [Name: {model.ProductName}, Price: {model.Price}, Brand: {model.BrandId}, Type: {model.TypeId}]");

                // Save changes
                await _db.SaveChangesAsync();

                return Json(new { success = true, message = "Product updated successfully" });
            }
            catch (Exception ex)
            {
                await LogAdminActivity("EDIT_PRODUCT_ERROR",
        $"Failed to update product {model.ProductId} - {ex.Message}");
                _logger.LogError($"Error updating product: {ex.Message}");
                return Json(new { success = false, message = "Error updating product" });
            }
        }
        public class ProcessOrderModel
        {
            public string OrderId { get; set; }
        }




        [AuthorizeRoles(UserRole.Admin)]
        public async Task<IActionResult> LoggingView(string query, string actionType, DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                var activitiesQuery = _db.AdminActivity.AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(query))
                {
                    query = query.Trim().ToLower();
                    activitiesQuery = activitiesQuery.Where(a =>
                        a.Username.ToLower().Contains(query) ||
                        a.ActionDetails.ToLower().Contains(query));
                }

                if (!string.IsNullOrEmpty(actionType))
                {
                    activitiesQuery = activitiesQuery.Where(a => a.ActionType == actionType);
                }

                if (fromDate.HasValue)
                {
                    activitiesQuery = activitiesQuery.Where(a => a.ActionTime >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    activitiesQuery = activitiesQuery.Where(a => a.ActionTime <= toDate.Value.AddDays(1));
                }

                // Order by most recent first
                activitiesQuery = activitiesQuery.OrderByDescending(a => a.ActionTime);

                var model = new LoggingSearchViewModel
                {
                    Query = query,
                    ActionType = actionType,
                    FromDate = fromDate,
                    ToDate = toDate,
                    Activities = await activitiesQuery.ToListAsync()
                };
                // Trong phương thức LoggingView của AdminController
                var activities = model.Activities;
                ViewBag.TotalActivities = activities.Count();
                ViewBag.CreateActions = activities.Count(a => a.ActionType.Contains("CREATE"));
                ViewBag.EditActions = activities.Count(a => a.ActionType.Contains("EDIT") || a.ActionType.Contains("UPDATE"));
                ViewBag.DeleteActions = activities.Count(a => a.ActionType.Contains("DELETE") || a.ActionType.Contains("CANCEL"));
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in LoggingView");
                return View(new LoggingSearchViewModel());
            }
        }


        [AuthorizeRoles(UserRole.Admin, UserRole.Staff)]
        public async Task<IActionResult> OrderView(string query, DateTime? fromDate, DateTime? toDate,
       bool? status, string paymentMethod, bool? paymentStatus, double? minTotal, double? maxTotal, bool? deleteStatus)
        {
            try
            {
                var ordersQuery = _db.Bill
                    .Include(b => b.Client)
                    .Include(b => b.Bill_Voucher)
                        .ThenInclude(bv => bv.Voucher)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(query))
                {
                    query = query.Trim().ToLower();
                    ordersQuery = ordersQuery.Where(b =>
                        b.Bill_id.ToLower().Contains(query) ||
                        (b.Client != null && (
                            b.Client.Name.ToLower().Contains(query) ||
                            b.Client.Location.ToLower().Contains(query) ||
                            b.Client.Contact.ToLower().Contains(query)
                        )) ||
                        (b.Bill_Voucher.Any(bv => bv.Voucher.Code.ToLower().Contains(query)))
                    );
                }

                if (fromDate.HasValue)
                    ordersQuery = ordersQuery.Where(b => b.Date >= fromDate.Value);
                if (toDate.HasValue)
                    ordersQuery = ordersQuery.Where(b => b.Date <= toDate.Value.AddDays(1));
                if (status.HasValue)
                    ordersQuery = ordersQuery.Where(b => b.Status == status.Value);
                if (!string.IsNullOrEmpty(paymentMethod))
                    ordersQuery = ordersQuery.Where(b => b.PaymentMethod == paymentMethod);
                if (paymentStatus.HasValue)
                    ordersQuery = ordersQuery.Where(b => b.PaymentStatus == paymentStatus.Value);
                if (minTotal.HasValue)
                    ordersQuery = ordersQuery.Where(b => b.Total >= minTotal.Value);
                if (maxTotal.HasValue)
                    ordersQuery = ordersQuery.Where(b => b.Total <= maxTotal.Value);

                ordersQuery = ordersQuery.Where(b => b.DeleteStatus == false)
                                        .OrderByDescending(b => b.Date);

                var orders = await ordersQuery.ToListAsync();

                // Tính toán tổng giá trị trước và sau khi áp dụng voucher
                foreach (var order in orders)
                {
                    var appliedVoucher = order.Bill_Voucher.FirstOrDefault();
                    if (appliedVoucher != null)
                    {
                        order.SubTotal = order.Total + appliedVoucher.DiscountAmount;
                        order.VoucherDiscount = appliedVoucher.DiscountAmount;
                        order.VoucherCode = appliedVoucher.Voucher?.Code;
                        order.VoucherName = appliedVoucher.Voucher?.Name;
                    }
                    else
                    {
                        order.SubTotal = order.Total;
                        order.VoucherDiscount = 0;
                    }

                    // Chuyển đổi PaymentMethod
                    switch (order.PaymentMethod)
                    {
                        case "Cash":
                            order.PaymentMethod = "Tiền mặt";
                            break;
                        case "Credit Card":
                            order.PaymentMethod = "Thẻ tín dụng";
                            break;
                        case "Bank Transfer":
                            order.PaymentMethod = "Chuyển khoản ngân hàng";
                            break;
                    }
                }

                var model = new OrderSearchViewModel
                {
                    Query = query,
                    FromDate = fromDate,
                    ToDate = toDate,
                    Status = status,
                    PaymentMethod = paymentMethod,
                    PaymentStatus = paymentStatus,
                    Orders = orders
                };
                // Trong phương thức OrderView của AdminController 
                ViewBag.TotalOrders = orders.Count();
                ViewBag.PendingOrders = orders.Count(o => !o.Status);
                ViewBag.CompletedOrders = orders.Count(o => o.Status);
                ViewBag.TotalRevenue = orders.Where(o => o.PaymentStatus).Sum(o => o.Total);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OrderView");
                return View(new OrderSearchViewModel());
            }
        }

        [HttpGet]
        [AuthorizeRoles(UserRole.Admin, UserRole.Staff)]
        public async Task<IActionResult> GetOrderDetails(string id)
        {
            try
            {
                var order = await _db.Bill
                    .Include(b => b.Client)
                    .Include(b => b.Product_Bill)
                        .ThenInclude(pb => pb.Product)
                    .Include(b => b.Bill_Voucher)
                        .ThenInclude(bv => bv.Voucher)
                    .FirstOrDefaultAsync(b => b.Bill_id == id);

                if (order == null)
                    return Json(new { success = false, message = "Order not found" });

                var productBills = order.Product_Bill.ToList();
                var voucherInfo = order.Bill_Voucher.FirstOrDefault();

                // Get IDs for lookup
                var iceIds = productBills.Where(pb => pb.Ice_id != null).Select(pb => pb.Ice_id).Distinct();
                var sugarIds = productBills.Where(pb => pb.Sugar_id != null).Select(pb => pb.Sugar_id).Distinct();
                var sizeIds = productBills.Where(pb => pb.Size_id != null).Select(pb => pb.Size_id).Distinct();

                var ices = await _db.Ice.Where(i => iceIds.Contains(i.Ice_id))
                    .ToDictionaryAsync(i => i.Ice_id, i => i.IceDetail);
                var sugars = await _db.Sugar.Where(s => sugarIds.Contains(s.Sugar_id))
                    .ToDictionaryAsync(s => s.Sugar_id, s => s.SugarDetail);
                var sizes = await _db.Size.Where(s => sizeIds.Contains(s.Size_id))
                    .ToDictionaryAsync(s => s.Size_id, s => s.SizeDetail);

                var subtotal = productBills.Sum(pb => pb.Amount * pb.Quantity);

                var orderDetails = new
                {
                    orderId = order.Bill_id,
                    orderDate = order.Date.ToString("yyyy-MM-dd HH:mm"),
                    customerName = order.Client?.Name ?? "N/A",
                    customerContact = order.Client?.Contact ?? "N/A",
                    customerAddress = order.Client?.Location ?? "N/A",
                    status = order.Status,
                    paymentMethod = order.PaymentMethod,
                    paymentStatus = order.PaymentStatus,
                    items = productBills.Select(pb => new
                    {
                        productName = pb.Product.ProductName,
                        quantity = pb.Quantity,
                        basePrice = pb.Product.Price,
                        discount = pb.Discount ?? pb.Product.Discount,
                        ice = pb.Ice_id != null && ices.ContainsKey(pb.Ice_id) ? ices[pb.Ice_id] : "50%",
                        sugar = pb.Sugar_id != null && sugars.ContainsKey(pb.Sugar_id) ? sugars[pb.Sugar_id] : "100%",
                        size = pb.Size_id != null && sizes.ContainsKey(pb.Size_id) ? sizes[pb.Size_id] : "S",
                        total = pb.Amount,
                        priceAfterDiscount = pb.Amount / pb.Quantity
                    }).ToList(),
                    voucher = voucherInfo != null ? new
                    {
                        code = voucherInfo.Voucher.Code,
                        name = voucherInfo.Voucher.Name,
                        discountAmount = voucherInfo.DiscountAmount,
                        voucherType = voucherInfo.Voucher.VoucherType,
                        detail = voucherInfo.Voucher.Detail
                    } : null,
                    discountAmount = voucherInfo?.DiscountAmount ?? 0,
                    subtotal = order.Total + voucherInfo?.DiscountAmount ?? 0,
                    total = order.Total
                };

                await LogAdminActivity("VIEW_ORDER_DETAILS", $"Viewed details for order: {id}");
                return Json(new { success = true, data = orderDetails });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching order details");
                return Json(new { success = false, message = "Error fetching order details" });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserRole.Admin, UserRole.Staff)]
        public async Task<IActionResult> ProcessOrder([FromBody] ProcessOrderModel model)
        {
            try
            {
                var order = await _db.Bill.FindAsync(model.OrderId);

                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found" });
                }

                order.Status = true;
                order.ProcessDate = DateTime.Now;

                await _db.SaveChangesAsync();
                await LogAdminActivity("PROCESS_ORDER", $"Processed order: {model.OrderId}");

                return Json(new { success = true, message = "Order processed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order");
                return Json(new { success = false, message = "Error processing order" });
            }
        }

        [HttpGet]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<IActionResult> ExportOrders(DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                var ordersQuery = _db.Bill
                    .Include(b => b.Client)
                    .Include(b => b.Product_Bill)
                        .ThenInclude(pb => pb.Product)
                    .AsQueryable();

                if (fromDate.HasValue)
                {
                    ordersQuery = ordersQuery.Where(b => b.Date >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    ordersQuery = ordersQuery.Where(b => b.Date <= toDate.Value.AddDays(1));
                }

                var orders = await ordersQuery.OrderByDescending(b => b.Date).ToListAsync();

                // Create CSV content
                var csvContent = new StringBuilder();
                csvContent.AppendLine("Order ID,Customer,Order Date,Total Amount,Status,Payment Method,Payment Status");

                foreach (var order in orders)
                {
                    csvContent.AppendLine($"{order.Bill_id}," +
                                        $"\"{order.Client?.Name ?? "N/A"}\"," +
                                        $"{order.Date:yyyy-MM-dd HH:mm}," +
                                        $"{order.Total}," +
                                        $"{(order.Status ? "Completed" : "Pending")}," +
                                        $"{order.PaymentMethod ?? "N/A"}," +
                                        $"{(order.PaymentStatus == true ? "Paid" : "Unpaid")}");
                }

                var bytes = Encoding.UTF8.GetBytes(csvContent.ToString());
                await LogAdminActivity("EXPORT_ORDERS", $"Exported orders report from {fromDate} to {toDate}");
                return File(bytes, "text/csv", $"orders_report_{DateTime.Now:yyyyMMdd}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting orders");
                return RedirectToAction("OrderView");
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserRole.Admin, UserRole.Staff)]
        public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateOrderStatusModel model)
        {
            try
            {
                var order = await _db.Bill.FindAsync(model.OrderId);
                if (order == null)
                    return Json(new { success = false, message = "Order not found" });

                order.Status = model.Status; 
                await _db.SaveChangesAsync();

                await LogAdminActivity("UPDATE_ORDER_DETAILS", $"Viewed details for order: {model.OrderId}");

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status");
                return Json(new { success = false, message = "Error updating status" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserRole.Admin, UserRole.Staff)]
        public async Task<IActionResult> UpdatePaymentStatus([FromBody] UpdatePaymentStatusModel model)
        {
            try
            {
                var order = await _db.Bill.FindAsync(model.OrderId);
                if (order == null)
                    return Json(new { success = false, message = "Order not found" });

                order.PaymentStatus = model.PaymentStatus;
                await _db.SaveChangesAsync();
                await LogAdminActivity("UPDATE_PAYMENT_STATUS",
                    $"Updated order {model.OrderId} payment status to {(model.PaymentStatus ? "Paid" : "Unpaid")}");

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment status");
                return Json(new { success = false, message = "Error updating payment status" });
            }
        }

        public class UpdateOrderStatusModel
        {
            public string OrderId { get; set; }
            public bool Status { get; set; }
        }

        public class UpdatePaymentStatusModel
        {
            public string OrderId { get; set; }
            public bool PaymentStatus { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserRole.Admin, UserRole.Staff)]
        public async Task<IActionResult> CancelOrder([FromBody] CancelOrderModel model)
        {
            try
            {
                var order = await _db.Bill.FindAsync(model.OrderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found" });
                }

                // Instead of actually deleting, we set DeleteStatus to true
                order.DeleteStatus = true;
                await _db.SaveChangesAsync();
                await LogAdminActivity("CANCEL_ORDER", $"Marked order {model.OrderId} as canceled");

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order");
                return Json(new { success = false, message = "Error deleting order" });
            }
        }

        public class CancelOrderModel
        {
            public string OrderId { get; set; }
        }


        public class OrderItemUpdateModel
        {
            public string ProductName { get; set; }
            public int Quantity { get; set; }
            public string Ice { get; set; }
            public string Sugar { get; set; }
            public string Size { get; set; }
            public double Price { get; set; }
            public int? Discount { get; set; }  // Added Discount property

        }

        public class OrderUpdateModel
        {
            public string OrderId { get; set; }
            public string CustomerName { get; set; }
            public string CustomerContact { get; set; }
            public string CustomerAddress { get; set; }
            public List<OrderItemUpdateModel> Items { get; set; }
        }

        // Add this controller action to your AdminController.cs
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserRole.Admin, UserRole.Staff)]
        public async Task<IActionResult> UpdateOrder([FromBody] OrderUpdateModel model)
        {
            try
            {
                var order = await _db.Bill
                    .Include(b => b.Client)
                    .Include(b => b.Product_Bill)
                        .ThenInclude(pb => pb.Product)
                    .Include(b => b.Bill_Voucher)
                        .ThenInclude(bv => bv.Voucher)
                    .FirstOrDefaultAsync(b => b.Bill_id == model.OrderId);

                if (order == null)
                    return Json(new { success = false, message = "Order not found" });

                if (order.Client != null)
                {
                    order.Client.Name = model.CustomerName;
                    order.Client.Contact = model.CustomerContact;
                    order.Client.Location = model.CustomerAddress;
                }

                var iceDetails = await _db.Ice.ToListAsync();
                var sugarDetails = await _db.Sugar.ToListAsync();
                var sizeDetails = await _db.Size.ToListAsync();

                foreach (var itemUpdate in model.Items)
                {
                    var productBill = order.Product_Bill
                        .FirstOrDefault(pb => pb.Product.ProductName == itemUpdate.ProductName);
                    if (productBill != null)
                    {
                        productBill.Quantity = itemUpdate.Quantity;
                        productBill.Ice_id = iceDetails.FirstOrDefault(i => i.IceDetail == itemUpdate.Ice)?.Ice_id;
                        productBill.Sugar_id = sugarDetails.FirstOrDefault(s => s.SugarDetail == itemUpdate.Sugar)?.Sugar_id;
                        productBill.Size_id = sizeDetails.FirstOrDefault(s => s.SizeDetail == itemUpdate.Size)?.Size_id;

                        if (itemUpdate.Discount.HasValue)
                        {
                            productBill.Discount = itemUpdate.Discount.Value;
                            productBill.Amount = itemUpdate.Price * itemUpdate.Quantity;
                        }
                        else
                        {
                            productBill.Discount = productBill.Product.Discount;
                            productBill.Amount = itemUpdate.Price * itemUpdate.Quantity;
                        }
                    }
                }

                var subtotal = order.Product_Bill.Sum(pb => pb.Amount);
                var existingVoucher = order.Bill_Voucher.FirstOrDefault();

                if (existingVoucher != null)
                {
                    var voucher = existingVoucher.Voucher;
                    double discountAmount;

                    if (voucher.VoucherType == "PERCENT")
                    {
                        discountAmount = (subtotal * voucher.Detail.Value) / 100;
                        if (voucher.MaximumDiscount.HasValue && discountAmount > voucher.MaximumDiscount.Value)
                        {
                            discountAmount = voucher.MaximumDiscount.Value;
                        }
                    }
                    else
                    {
                        discountAmount = voucher.Detail.Value;
                    }

                    existingVoucher.DiscountAmount = discountAmount;
                    order.Total = subtotal - discountAmount;
                }
                else
                {
                    order.Total = subtotal;
                }

                await _db.SaveChangesAsync();
                await LogAdminActivity("UPDATE_ORDER", $"Updated order: {model.OrderId}");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Update OrderItemUpdateModel


        [AuthorizeRoles(UserRole.Admin, UserRole.Staff)]
        public async Task<IActionResult> OrderCanceledView(string query, DateTime? fromDate, DateTime? toDate,
    bool? status, string paymentMethod, bool? paymentStatus, double? minTotal, double? maxTotal, bool? deleteStatus)
        {
            try
            {
                var ordersQuery = _db.Bill.Include(b => b.Client).AsQueryable();


                if (!string.IsNullOrEmpty(query))
                {
                    query = query.Trim().ToLower();
                    ordersQuery = ordersQuery.Where(b =>
                        b.Bill_id.ToLower().Contains(query) ||
                        (b.Client != null && (
                            b.Client.Name.ToLower().Contains(query) ||
                            b.Client.Location.ToLower().Contains(query) ||
                            b.Client.Contact.ToLower().Contains(query)
                        )));
                }

                if (fromDate.HasValue)
                    ordersQuery = ordersQuery.Where(b => b.Date >= fromDate.Value);

                if (toDate.HasValue)
                    ordersQuery = ordersQuery.Where(b => b.Date <= toDate.Value.AddDays(1));

                if (status.HasValue)
                    ordersQuery = ordersQuery.Where(b => b.Status == status.Value);

                if (!string.IsNullOrEmpty(paymentMethod))
                    ordersQuery = ordersQuery.Where(b => b.PaymentMethod == paymentMethod);

                if (paymentStatus.HasValue)
                    ordersQuery = ordersQuery.Where(b => b.PaymentStatus == paymentStatus.Value);
                if (minTotal.HasValue)
                    ordersQuery = ordersQuery.Where(b => b.Total >= minTotal.Value);

                if (maxTotal.HasValue)
                    ordersQuery = ordersQuery.Where(b => b.Total <= maxTotal.Value);
                ordersQuery = ordersQuery.Where(b => b.DeleteStatus == true);
                ordersQuery = ordersQuery.OrderByDescending(b => b.Date);

                var model = new OrderSearchViewModel
                {
                    Query = query,
                    FromDate = fromDate,
                    ToDate = toDate,
                    Status = status,
                    PaymentMethod = paymentMethod,
                    PaymentStatus = paymentStatus,
                    Orders = await ordersQuery.ToListAsync()
                };
                // Chuyển đổi PaymentMethod
                foreach (var order in model.Orders)
                {
                    switch (order.PaymentMethod)
                    {
                        case "Cash":
                            order.PaymentMethod = "Tiền mặt";
                            break;
                        case "Credit Card":
                            order.PaymentMethod = "Thẻ tín dụng";
                            break;
                        case "Bank Transfer":
                            order.PaymentMethod = "Chuyển khoản ngân hàng";
                            break;
                    }
                }
                ViewBag.TotalOrders = model.Orders.Count();
                ViewBag.PendingOrders = model.Orders.Count(o => !o.Status);
                ViewBag.CompletedOrders = model.Orders.Count(o => o.Status);
                ViewBag.TotalRevenue = model.Orders.Where(o => o.PaymentStatus).Sum(o => o.Total);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OrderView");
                return View(new OrderSearchViewModel());
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserRole.Admin, UserRole.Staff)]
        public async Task<IActionResult> DeleteOrder([FromBody] CancelOrderModel model)
        {
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                await _db.Database.ExecuteSqlRawAsync(
                    @"DELETE FROM Shipping WHERE Bill_id = {0};
              DELETE FROM Product_Bill WHERE Bill_id = {0};  
              DELETE FROM Bill_Voucher WHERE Bill_id = {0};
              DELETE FROM Bill WHERE Bill_id = {0};",
                    model.OrderId);

                await transaction.CommitAsync();
                await LogAdminActivity("DELETE_ORDER", $"Deleted order {model.OrderId}");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting order");
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserRole.Admin, UserRole.Staff)]
        public async Task<IActionResult> RestoreOrder([FromBody] CancelOrderModel model)
        {
            try
            {
                var order = await _db.Bill.FindAsync(model.OrderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found" });
                }

                order.DeleteStatus = false;
                await _db.SaveChangesAsync();
                await LogAdminActivity("RESTORE_ORDER", $"Restored order {model.OrderId}");

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring order");
                return Json(new { success = false, message = "Error restoring order" });
            }
        }
        [AuthorizeRoles(UserRole.Admin, UserRole.Staff)]
        public async Task<IActionResult> FeedbackView(string query, string status, DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                var feedbackQuery = _db.Feedback
                    .Include(f => f.Client)
                    .Include(f => f.Product)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(query))
                {
                    query = query.Trim().ToLower();
                    feedbackQuery = feedbackQuery.Where(f =>
                        f.Client.Name.ToLower().Contains(query) ||
                        f.Product.ProductName.ToLower().Contains(query) ||
                        f.Comment.ToLower().Contains(query));
                }

                if (!string.IsNullOrEmpty(status))
                {
                    bool isResponded = status == "responded";
                    feedbackQuery = feedbackQuery.Where(f =>
                        (isResponded && !string.IsNullOrEmpty(f.AdminResponse)) ||
                        (!isResponded && string.IsNullOrEmpty(f.AdminResponse)));
                }

                if (fromDate.HasValue)
                    feedbackQuery = feedbackQuery.Where(f => f.CreatedDate >= fromDate.Value);

                if (toDate.HasValue)
                    feedbackQuery = feedbackQuery.Where(f => f.CreatedDate <= toDate.Value.AddDays(1));

                feedbackQuery = feedbackQuery.OrderByDescending(f => f.CreatedDate);

                var model = new FeedbackSearchViewModel
                {
                    Query = query,
                    Status = status,
                    FromDate = fromDate,
                    ToDate = toDate,
                    Feedbacks = await feedbackQuery.ToListAsync()
                };
                var feedbacks = model.Feedbacks;
                ViewBag.TotalFeedbacks = feedbacks.Count();
                ViewBag.AwaitingResponse = feedbacks.Count(f => string.IsNullOrEmpty(f.AdminResponse));
                ViewBag.AverageRating = feedbacks.Any() ? Math.Round(feedbacks.Average(f => f.Rating), 1) : 0;
                ViewBag.ResponseRate = feedbacks.Any()
                    ? Math.Round((double)feedbacks.Count(f => !string.IsNullOrEmpty(f.AdminResponse)) / feedbacks.Count() * 100, 1)
                    : 0;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FeedbackView");
                return View(new FeedbackSearchViewModel());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserRole.Admin, UserRole.Staff)]
        public async Task<IActionResult> RespondToFeedback([FromBody] FeedbackResponseModel model)
        {
            try
            {
                var feedback = await _db.Feedback.FindAsync(model.FeedbackId);
                if (feedback == null)
                {
                    return Json(new { success = false, message = "Feedback not found" });
                }

                feedback.AdminResponse = model.Response;
                feedback.ResponseDate = DateTime.Now;
                feedback.RespondedBy = User.Identity.Name;

                await _db.SaveChangesAsync();
                await LogAdminActivity("RESPOND_TO_FEEDBACK",
                    $"Responded to feedback ID: {model.FeedbackId}");

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error responding to feedback");
                return Json(new { success = false, message = "Error saving response" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<IActionResult> DeleteFeedback([FromBody] string feedbackId)
        {
            try
            {
                var feedback = await _db.Feedback.FindAsync(feedbackId);
                if (feedback == null)
                {
                    return Json(new { success = false, message = "Feedback not found" });
                }

                _db.Feedback.Remove(feedback);
                await _db.SaveChangesAsync();
                await LogAdminActivity("DELETE_FEEDBACK",
                    $"Deleted feedback ID: {feedbackId}");

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting feedback");
                return Json(new { success = false, message = "Error deleting feedback" });
            }
        }

        public class FeedbackResponseModel
        {
            public string FeedbackId { get; set; }
            public string Response { get; set; }
        }

        // ----------------------- Quản lý Voucher ---------------------------- 19/12/2024 
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<IActionResult> VoucherView(string query, DateTime? fromDate, DateTime? toDate,
    string voucherType, string brandId, bool? status)
        {
            try
            {
                var vouchersQuery = _db.Voucher
                    .Include(v => v.Brand)
                    .Include(v => v.Bill_Voucher)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(query))
                {
                    query = query.Trim().ToLower();
                    vouchersQuery = vouchersQuery.Where(v =>
                        v.Voucher_id.ToLower().Contains(query) ||
                        v.Code.ToLower().Contains(query) ||
                        v.Name.ToLower().Contains(query));
                }

                if (fromDate.HasValue)
                    vouchersQuery = vouchersQuery.Where(v => v.CreatedDate >= fromDate.Value);

                if (toDate.HasValue)
                    vouchersQuery = vouchersQuery.Where(v => v.CreatedDate <= toDate.Value.AddDays(1));

                if (!string.IsNullOrEmpty(voucherType))
                    vouchersQuery = vouchersQuery.Where(v => v.VoucherType == voucherType);

                if (!string.IsNullOrEmpty(brandId))
                    vouchersQuery = vouchersQuery.Where(v => v.Brand_id == brandId || v.Brand_id == null);
                else
                    vouchersQuery = vouchersQuery.Where(v => v.Brand_id == null || v.Brand_id != null);

                if (status.HasValue)
                    vouchersQuery = vouchersQuery.Where(v => v.Status == status.Value);

                vouchersQuery = vouchersQuery.OrderByDescending(v => v.CreatedDate);

                var model = new VoucherSearchViewModel
                {
                    Query = query,
                    FromDate = fromDate,
                    ToDate = toDate,
                    VoucherType = voucherType,
                    Brand_id = brandId,
                    Status = status,
                    Vouchers = await vouchersQuery.ToListAsync(),
                    Brands = await _db.Brand.Where(b => b.Status == true).ToListAsync()
                };
                var vouchers = model.Vouchers;
                ViewBag.TotalVouchers = vouchers.Count();
                ViewBag.ActiveVouchers = vouchers.Count(v => v.Status == true);
                ViewBag.UsedVouchers = vouchers.Sum(v => v.UsageCount);
                ViewBag.TotalDiscount = vouchers.Sum(v =>
                {
                    if (v.VoucherType == "FIXED")
                        return v.Bill_Voucher?.Sum(bv => bv.DiscountAmount) ?? 0;
                    else
                        return v.Bill_Voucher?.Sum(bv => bv.DiscountAmount) ?? 0;
                });
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in VoucherView");
                return View(new VoucherSearchViewModel
                {
                    Vouchers = new List<Voucher>(),
                    Brands = new List<Brand>()
                });
            }
        }
        [HttpGet]
        [AuthorizeRoles(UserRole.Admin)]
        private async Task<string> GenerateNewVoucherId()
        {
            string maxId = await _db.Voucher
                .Where(v => v.Voucher_id.StartsWith("VC"))
                .Select(v => v.Voucher_id)
                .OrderByDescending(id => id)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(maxId))
            {
                return "VC001";
            }

            if (int.TryParse(maxId.Substring(2), out int currentNumber))
            {
                return $"VC{(currentNumber + 1).ToString("D3")}";
            }

            throw new Exception("Invalid voucher ID format in database");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<IActionResult> CreateVoucher([FromBody] VoucherCreateModel model)
        {
            try
            {
                // Validate that the code is unique
                if (await _db.Voucher.AnyAsync(v => v.Code == model.Code))
                {
                    return Json(new { success = false, message = "Mã voucher đã tồn tại" });
                }

                // Generate new voucher ID
                string newVoucherId = await GenerateNewVoucherId();

                var voucher = new Voucher
                {
                    Voucher_id = newVoucherId,
                    Code = model.Code,
                    Name = model.Name,
                    Detail = model.Detail,
                    ExpirationDate = model.ExpirationDate,
                    Description = model.Description,
                    MinimumSpend = model.MinimumSpend,
                    MaximumDiscount = model.MaximumDiscount,
                    UsageLimit = model.UsageLimit,
                    UsageCount = 0,
                    VoucherType = model.VoucherType,
                    Brand_id = model.Brand_id,
                    Status = true,
                    CreatedDate = DateTime.Now,
                    CreatedBy = User.Identity.Name
                };

                _db.Voucher.Add(voucher);
                await _db.SaveChangesAsync();

                await LogAdminActivity("CREATE_VOUCHER",
                    $"Created voucher: {voucher.Voucher_id} - {voucher.Name}");

                return Json(new
                {
                    success = true,
                    voucherId = voucher.Voucher_id,
                    message = "Voucher được tạo thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating voucher");
                return Json(new { success = false, message = "Lỗi khi tạo voucher" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<IActionResult> UpdateVoucherStatus([FromBody] UpdateVoucherStatusModel model)
        {
            try
            {
                var voucher = await _db.Voucher.FindAsync(model.Id);
                if (voucher == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy voucher" });
                }

                voucher.Status = model.Status;
                await _db.SaveChangesAsync();

                await LogAdminActivity("UPDATE_VOUCHER_STATUS",
                    $"Updated voucher {model.Id} status to {(model.Status ? "active" : "inactive")}");

                return Json(new
                {
                    success = true,
                    message = $"Đã {(model.Status ? "kích hoạt" : "vô hiệu hóa")} voucher thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating voucher status for ID: {model.Id}");
                return Json(new { success = false, message = "Lỗi khi cập nhật trạng thái voucher" });
            }
        }

        public class UpdateVoucherStatusModel
        {
            [Required]
            public string Id { get; set; }

            [Required]
            public bool Status { get; set; }
        }
        [HttpGet]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<IActionResult> GetVoucher(string id)
        {
            try
            {
                var voucher = await _db.Voucher
                    .Include(v => v.Brand)
                    .FirstOrDefaultAsync(v => v.Voucher_id == id);

                if (voucher == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy voucher" });
                }

                // Create anonymous object with properly formatted dates
                var result = new
                {
                    success = true,
                    data = new
                    {
                        voucher_id = voucher.Voucher_id,
                        code = voucher.Code,
                        name = voucher.Name,
                        voucherType = voucher.VoucherType,
                        detail = voucher.Detail,
                        // Format dates to ISO string
                        expirationDate = voucher.ExpirationDate?.ToString("yyyy-MM-dd"),
                        description = voucher.Description,
                        minimumSpend = voucher.MinimumSpend,
                        maximumDiscount = voucher.MaximumDiscount,
                        usageLimit = voucher.UsageLimit,
                        usageCount = voucher.UsageCount,
                        brand_id = voucher.Brand_id,
                        brandName = voucher.Brand?.BrandName,
                        status = voucher.Status,
                        createdDate = voucher.CreatedDate?.ToString("yyyy-MM-dd HH:mm:ss"),
                        createdBy = voucher.CreatedBy
                    }
                };

                // Use JsonResult with specific settings
                return new JsonResult(result, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving voucher details for ID: {id}");
                return Json(new { success = false, message = "Lỗi khi lấy thông tin voucher" });
            }
        }
        public class VoucherCreateModel
        {
            [Required]
            [StringLength(10)]
            public string Voucher_id { get; set; }

            [Required]
            [StringLength(50)]
            public string Code { get; set; }

            [Required]
            [StringLength(100)]
            public string Name { get; set; }

            [Required]
            public double Detail { get; set; }  // Changed from float to double

            [Required]
            public DateTime ExpirationDate { get; set; }

            [StringLength(500)]
            public string Description { get; set; }

            [Required]
            public double MinimumSpend { get; set; }  // Changed from float to double

            public double? MaximumDiscount { get; set; }  // Changed from float? to double?

            public int? UsageLimit { get; set; }

            [Required]
            [StringLength(20)]
            public string VoucherType { get; set; }

            [StringLength(10)]
            public string Brand_id { get; set; }
        }

        public class VoucherUpdateModel
        {
            [Required]
            public string Voucher_id { get; set; }

            [Required]
            [StringLength(50)]
            public string Code { get; set; }

            [Required]
            [StringLength(100)]
            public string Name { get; set; }

            [Required]
            public string VoucherType { get; set; }

            [Required]
            public double Detail { get; set; }

            [Required]
            public DateTime ExpirationDate { get; set; }

            public string Description { get; set; }

            [Required]
            public double MinimumSpend { get; set; }

            public double? MaximumDiscount { get; set; }

            public int? UsageLimit { get; set; }

            public string Brand_id { get; set; }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<IActionResult> EditVoucher([FromBody] VoucherUpdateModel model)
        {
            try
            {
                var voucher = await _db.Voucher.FindAsync(model.Voucher_id);

                if (voucher == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy voucher" });
                }

                // Kiểm tra nếu code đã tồn tại (trừ voucher hiện tại)
                if (await _db.Voucher.AnyAsync(v =>
                    v.Code == model.Code &&
                    v.Voucher_id != model.Voucher_id))
                {
                    return Json(new { success = false, message = "Mã voucher đã tồn tại" });
                }

                // Cập nhật thông tin voucher
                voucher.Code = model.Code;
                voucher.Name = model.Name;
                voucher.VoucherType = model.VoucherType;
                voucher.Detail = model.Detail;
                voucher.ExpirationDate = model.ExpirationDate;
                voucher.Description = model.Description;
                voucher.MinimumSpend = model.MinimumSpend;
                voucher.MaximumDiscount = model.MaximumDiscount;
                voucher.UsageLimit = model.UsageLimit;
                voucher.Brand_id = string.IsNullOrEmpty(model.Brand_id) ? null : model.Brand_id;

                await _db.SaveChangesAsync();
                await LogAdminActivity("UPDATE_VOUCHER", $"Updated voucher: {model.Voucher_id}");

                return Json(new { success = true, message = "Cập nhật voucher thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating voucher");
                return Json(new { success = false, message = "Lỗi khi cập nhật voucher" });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<IActionResult> DeleteVoucher([FromBody] string voucherId)
        {
            try
            {
                var voucher = await _db.Voucher
                    .Include(v => v.Bill_Voucher)
                    .FirstOrDefaultAsync(v => v.Voucher_id == voucherId);

                if (voucher == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy voucher" });
                }

                // Kiểm tra xem voucher đã được sử dụng chưa
                if (voucher.Bill_Voucher != null && voucher.Bill_Voucher.Any())
                {
                    return Json(new
                    {
                        success = false,
                        message = "Không thể xóa voucher đã được sử dụng. Bạn có thể vô hiệu hóa nó thay vì xóa."
                    });
                }

                // Ghi log trước khi xóa
                var voucherInfo = $"{voucher.Voucher_id} - {voucher.Name} ({voucher.Code})";

                _db.Voucher.Remove(voucher);
                await _db.SaveChangesAsync();

                await LogAdminActivity("DELETE_VOUCHER", $"Đã xóa voucher: {voucherInfo}");

                return Json(new { success = true, message = "Xóa voucher thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting voucher: {voucherId}");
                return Json(new { success = false, message = "Lỗi khi xóa voucher" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<IActionResult> BulkDeleteVouchers([FromBody] List<string> voucherIds)
        {
            try
            {
                var vouchers = await _db.Voucher
                    .Include(v => v.Bill_Voucher)
                    .Where(v => voucherIds.Contains(v.Voucher_id))
                    .ToListAsync();

                if (!vouchers.Any())
                {
                    return Json(new { success = false, message = "Không tìm thấy voucher nào" });
                }

                // Kiểm tra các voucher đã sử dụng
                var usedVouchers = vouchers.Where(v => v.Bill_Voucher != null && v.Bill_Voucher.Any()).ToList();
                if (usedVouchers.Any())
                {
                    var usedVoucherCodes = string.Join(", ", usedVouchers.Select(v => v.Code));
                    return Json(new
                    {
                        success = false,
                        message = $"Không thể xóa các voucher đã sử dụng: {usedVoucherCodes}"
                    });
                }

                // Ghi log trước khi xóa
                var vouchersInfo = string.Join(", ", vouchers.Select(v => $"{v.Code} ({v.Name})"));

                _db.Voucher.RemoveRange(vouchers);
                await _db.SaveChangesAsync();

                await LogAdminActivity("BULK_DELETE_VOUCHERS",
                    $"Đã xóa nhiều voucher: {vouchersInfo}");

                return Json(new
                {
                    success = true,
                    message = $"Đã xóa thành công {vouchers.Count} voucher"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk deleting vouchers");
                return Json(new { success = false, message = "Lỗi khi xóa voucher" });
            }
        }

    }


}
