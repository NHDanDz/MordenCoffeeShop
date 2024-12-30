using DemoApp_Test.Extensions;
using DemoApp_Test.Models;
using DemoApp_Test.Models.ViewModels;
using DemoApp_Test.Repository;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;
using System.Transactions;
using X.PagedList; 
 using Microsoft.Data.SqlClient;
using Microsoft.Identity.Client;
using DemoApp_Test.Services;
using System.Text.Json;
namespace DemoApp_Test.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DataContext _dataContext;

        // Lấy dữ liệu từ database và logger

        private readonly IEmailService _emailService;
         
        public HomeController(DataContext context,
            ILogger<HomeController> logger,
            IEmailService emailService)
        {
            _dataContext = context;
            _logger = logger;
            _emailService = emailService;
        }

        /*----------------------------------------------Update Cart BEGIN------------------------------------------------------------*/


        // Hàm Update Giá trị của cart trong session
        // Cập nhật phương thức UpdateCartInfo
        private async Task UpdateCartInfo()
        {
            try
            {
                List<CartItemModel> cart;
                var username = User.Identity.Name;

                if (!string.IsNullOrEmpty(username))
                {
                    using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                    {
                        cart = await GetCartFromDatabase(username);
                        if (!cart.Any())
                        {
                            var sessionCart = HttpContext.Session.GetObjectFromJson<List<CartItemModel>>("cart");
                            if (sessionCart != null && sessionCart.Any())
                            {
                                cart = await SyncSessionCartToDatabase(username, sessionCart);
                                HttpContext.Session.Remove("cart");
                            }
                        }
                        scope.Complete();
                    }
                }
                else
                {
                    cart = HttpContext.Session.GetObjectFromJson<List<CartItemModel>>("cart") ?? new List<CartItemModel>();
                }

                ViewData["CartItemCount"] = cart.Sum(x => x.Quantity);
                ViewData["CartTotal"] = cart.Sum(x => x.Price * x.Quantity).ToString("N2");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UpdateCartInfo: {ex.Message}");
            }
        }

        private async Task<List<CartItemModel>> GetCartFromDatabase(string username)
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var dbCart = await _dataContext.ShoppingCart
                    .AsNoTracking()  // Thêm AsNoTracking để tối ưu hiệu suất
                    .Include(sc => sc.ShoppingCartItems)
                        .ThenInclude(sci => sci.Product)
                            .ThenInclude(p => p.Brand)
                    .Include(sc => sc.ShoppingCartItems)
                        .ThenInclude(sci => sci.Product)
                            .ThenInclude(p => p.TypeCoffee)
                    .Include(sc => sc.ShoppingCartItems)
                        .ThenInclude(sci => sci.Size)
                    .FirstOrDefaultAsync(sc => sc.Username == username);

                if (dbCart == null)
                {
                    scope.Complete();
                    return new List<CartItemModel>();
                }

                var cartItems = dbCart.ShoppingCartItems.Select(item => new CartItemModel(item.Product)
                {
                    Quantity = item.Quantity,
                    SizeId = item.Size_id ?? "Size01",
                    SizeDetail = item.Size?.SizeDetail ?? "S",
                    IceId = item.Ice_id ?? "Ice01",
                    IceDetail = item.Ice?.IceDetail ?? "50%",
                    SugarId = item.Sugar_id ?? "Sugar01",
                    SugarDetail = item.Sugar?.SugarDetail ?? "100%"
                }).ToList();

                scope.Complete();
                return cartItems;
            }
        }


        private async Task<List<CartItemModel>> SyncSessionCartToDatabase(string username, List<CartItemModel> sessionCart)
        {
            // Tạo shopping cart mới nếu chưa có
            var dbCart = await _dataContext.ShoppingCart.FirstOrDefaultAsync(sc => sc.Username == username);
            if (dbCart == null)
            {
                dbCart = new ShoppingCart
                {
                    Cart_id = Guid.NewGuid().ToString().Substring(0, 10),
                    Username = username,
                    CreatedDate = DateTime.Now,
                    LastModifiedDate = DateTime.Now
                };
                _dataContext.ShoppingCart.Add(dbCart);
                await _dataContext.SaveChangesAsync();
            }

            // Thêm từng item từ session vào database
            foreach (var sessionItem in sessionCart)
            {
                var cartItem = new ShoppingCartItem
                {
                    CartItem_id = Guid.NewGuid().ToString().Substring(0, 10),
                    Cart_id = dbCart.Cart_id,
                    Product_id = sessionItem.Product_id,
                    Quantity = sessionItem.Quantity,
                    Size_id = sessionItem.SizeId,
                    Ice_id = sessionItem.IceId,
                    Sugar_id = sessionItem.SugarId,
                    AddedDate = DateTime.Now
                };

                _dataContext.ShoppingCartItem.Add(cartItem);
            }

            dbCart.LastModifiedDate = DateTime.Now;
            await _dataContext.SaveChangesAsync();

            return await GetCartFromDatabase(username);
        }

        /*----------------------------------------------Update Cart END------------------------------------------------------------*/



        /*-------------------------------------------------HOME BEGIN-----------------------------------------------------------------*/

        public IActionResult Home()
        {
            UpdateCartInfo();

            return View();
        }

        // Menu Index
        public async Task<IActionResult> Index()
        {
            await UpdateCartInfo();
            ViewBag.ConfirmLogin = User.Identity.IsAuthenticated;
            return View();
        }
        public IActionResult Privacy()
        {
            UpdateCartInfo();

            return View();
        }

        /*-------------------------------------------------HOME END-----------------------------------------------------------------*/



        /*-------------------------------------------------MENU BEGIN-----------------------------------------------------------------*/

        // Thêm constant cho phân trang
        private const int PageSize = 8;

        // Sửa lại phương thức Menu để hỗ trợ filter và phân trang
        public async Task<IActionResult> Menu(string searchTerm, string brandId, string typeId, string size,
            string priceRange, double? minPrice, double? maxPrice, int? rating,
            string sort, int page = 1)
        {
            try
            {
                // Load all necessary data first to avoid multiple concurrent DB operations
                var typeCoffees = await _dataContext.TypeCoffee.ToListAsync();
                var brands = await _dataContext.Brand.ToListAsync();
                var sizes = await _dataContext.Size.ToListAsync();

                // Base query with eager loading
                var query = from p in _dataContext.Product
                            join b in _dataContext.Brand on p.Brand_id equals b.Brand_id
                            join t in _dataContext.TypeCoffee on p.Type_id equals t.Type_id
                            // Tính tổng số lượng đã bán từ Product_Bill
                            let soldQuantity = (_dataContext.Product_Bill
                                .Where(pb => pb.Product_id == p.Product_id)
                                .Select(pb => (int?)pb.Quantity)  // Cast to nullable int
                                .Sum()) ?? 0  // If sum is null, use 0
                            select new HienThiSanPham
                            {
                                Ma_Sanpham = p.Product_id,
                                Ten_Sanpham = p.ProductName,
                                Giagoc = p.Price,
                                Rating = p.Rating,
                                ReviewCount = p.ReviewCount,
                                Link1 = p.Image,
                                Discount = p.Discount,
                                Date = p.Date,
                                Ten_Nhasanxuat = b.BrandName,
                                Ten_loai = t.TypeName,
                                Brand_id = p.Brand_id,
                                Type_id = p.Type_id,
                                SoldQuantity = soldQuantity,  // This is now a non-nullable int
                                status = p.Status
                            };
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(p => p.Ten_Sanpham.ToLower().Contains(searchTerm));
                }
                // Apply filters
                if (!string.IsNullOrEmpty(brandId))
                {
                    query = query.Where(p => p.Brand_id.Trim() == brandId.Trim());
                }

                if (!string.IsNullOrEmpty(typeId))
                {
                    query = query.Where(p => p.Type_id.Trim() == typeId.Trim());
                }

                // Apply custom price range filter
                if (minPrice.HasValue)
                {
                    query = query.Where(p => (p.Giagoc - (p.Giagoc * p.Discount / 100)) >= minPrice.Value);
                }

                if (maxPrice.HasValue)
                {
                    query = query.Where(p => (p.Giagoc - (p.Giagoc * p.Discount / 100)) <= maxPrice.Value);
                }

                // Apply predefined price range filter
                if (!string.IsNullOrEmpty(priceRange))
                {
                    switch (priceRange)
                    {
                        case "1": // 0-5
                            query = query.Where(p => p.Giagoc - (p.Giagoc * p.Discount / 100) < 20000);
                            break;
                        case "2": // 5-10
                            query = query.Where(p => p.Giagoc - (p.Giagoc * p.Discount / 100) >= 20000 && p.Giagoc - (p.Giagoc * p.Discount / 100) < 40000);
                            break;
                        case "3": // 10-15
                            query = query.Where(p => p.Giagoc - (p.Giagoc * p.Discount / 100) >= 40000 && p.Giagoc - (p.Giagoc * p.Discount / 100) < 60000);
                            break;
                        case "4": // 15-20
                            query = query.Where(p => p.Giagoc - (p.Giagoc * p.Discount / 100) >= 60000 && p.Giagoc - (p.Giagoc * p.Discount / 100) < 80000);
                            break;
                        case "5": // 20-25
                            query = query.Where(p => p.Giagoc - (p.Giagoc * p.Discount / 100) >= 80000 && p.Giagoc - (p.Giagoc * p.Discount / 100) < 100000);
                            break;
                        case "6": // 25+
                            query = query.Where(p => p.Giagoc - (p.Giagoc * p.Discount / 100) >= 100000);
                            break;
                    }
                }

                // Apply rating filter
                if (rating.HasValue)
                {
                    var minRating = rating.Value * 20;
                    var maxRating = 100;
                    query = query.Where(p => p.Rating >= minRating && p.Rating <= maxRating);
                }

                if (!string.IsNullOrEmpty(sort))
                {
                    switch (sort.ToLower())
                    {
                        case "newest":
                            query = query.OrderByDescending(p => p.Date);
                            break;
                        case "oldest":
                            query = query.OrderBy(p => p.Date);
                            break;
                        case "price_asc":
                            query = query.OrderBy(p => p.Giagoc - (p.Giagoc * p.Discount / 100));
                            break;
                        case "price_desc":
                            query = query.OrderByDescending(p => p.Giagoc - (p.Giagoc * p.Discount / 100));
                            break;
                        case "bestseller":
                            query = query.OrderByDescending(p => p.SoldQuantity);
                            break;
                        default:
                            query = query.OrderByDescending(p => p.Date);
                            break;
                    }
                }
                else
                {
                    query = query.OrderByDescending(p => p.Date);
                }
                query = query.Where(p => p.status == true);
                // Make sure to set the current sort in ViewBag
                ViewBag.CurrentSort = sort;

                // Execute the query once and materialize the results
                var items = await query.ToListAsync();

                // Create PagedList
                var pageSize = 8;
                var pagedList = new PagedList<HienThiSanPham>(items, page, pageSize);
                ViewBag.CurrentSearch = searchTerm;

                // Set ViewBag data
                ViewBag.typeCoffees = typeCoffees;
                ViewBag.Brands = brands;
                ViewBag.Sizes = sizes;
                ViewBag.SelectedBrandId = brandId;
                ViewBag.SelectedTypeId = typeId;
                ViewBag.SelectedSize = size;
                ViewBag.SelectedPriceRange = priceRange;
                ViewBag.SelectedMinPrice = minPrice;
                ViewBag.SelectedMaxPrice = maxPrice;
                ViewBag.SelectedRating = rating;
                ViewBag.TotalItems = pagedList.TotalItemCount;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = pagedList.PageCount;

                await UpdateCartInfo(); // Make sure this is awaited

                return View(pagedList);
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError(ex, "Error in Menu action");
                throw; // Re-throw the exception after logging
            }
        }
        // Model class for product updates
        public class ProductUpdateModel
        {
            public string Product_id { get; set; }
            public string ProductName { get; set; }
            public double Price { get; set; }
            public int Discount { get; set; }
            public string Brand_id { get; set; }
            public string Type_id { get; set; }
            public int Rating { get; set; }
            public int ReviewCount { get; set; }
            public string NewImage { get; set; }
            public string RootImage { get; set; }
        }
 
   
        public IActionResult GetProductDetails(string id)
        {
            var product = _dataContext.Product
                .Include(p => p.Brand)
                .Include(p => p.TypeCoffee)
                .Include(p => p.Product_Size).ThenInclude(ps => ps.Size)
                .Include(p => p.Product_Ice).ThenInclude(pi => pi.Ice)
                .Include(p => p.Product_Sugar).ThenInclude(ps => ps.Sugar)
                .FirstOrDefault(p => p.Product_id == id);

            if (product == null)
                return NotFound();

            // Tính toán lại giá sau khi áp dụng giảm giá
            var discountedPrice = product.Price - (product.Price * product.Discount / 100);

            return Json(new
            {
                productId = product.Product_id,
                productName = product.ProductName,
                price = discountedPrice,
                originalPrice = product.Price,
                imageUrl = $"/img/{product.Brand?.BrandName ?? "default"}/{product.TypeCoffee?.TypeName ?? "default"}/{product.Image ?? "default.jpg"}",
                rating = product.Rating,
                reviewCount = product.ReviewCount,
                sizes = product.Product_Size.Select(ps => new
                {
                    sizeId = ps.Size_id,
                    sizeDetail = ps.Size.SizeDetail,
                    price = ps.Price - (ps.Price * product.Discount / 100) // Áp dụng giảm giá cho từng size
                }),
                iceOptions = product.Product_Ice.Select(pi => new
                {
                    iceId = pi.Ice_id,
                    iceDetail = pi.Ice.IceDetail
                }),
                sugarOptions = product.Product_Sugar.Select(ps => new
                {
                    sugarId = ps.Sugar_id,
                    sugarDetail = ps.Sugar.SugarDetail
                })
            });
        }

        /*-------------------------------------------------MENU END-----------------------------------------------------------------*/


        public async Task<IActionResult> GetClientInfo()
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    return Json(new { success = false, message = "User not logged in" });
                }

                var client = await _dataContext.Account
                    .Include(a => a.Client)
                    .Where(a => a.Username == username)
                    .Select(a => new {
                        name = a.Client.Name,
                        gmail = a.Client.Gmail,
                        contact = a.Client.Contact,
                        location = a.Client.Location
                    })
                    .FirstOrDefaultAsync();

                if (client == null)
                {
                    return Json(new { success = false, message = "Client not found" });
                }

                return Json(new
                {
                    success = true,
                    client = client
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetClientInfo: {ex.Message}");
                return Json(new { success = false, message = "Error retrieving client information" });
            }
        }

        public async Task<IActionResult> Checkout()
        {
            try
            {
                var username = User.Identity?.Name;
                List<CartItemModel> cartItems;

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (!string.IsNullOrEmpty(username))
                    {
                        // Lấy session cart trước
                        var sessionCart = HttpContext.Session.GetObjectFromJson<List<CartItemModel>>("cart");
                        if (sessionCart != null && sessionCart.Any())
                        {
                            // Xóa cart cũ trong database nếu có
                            var existingCart = await _dataContext.ShoppingCart
                                .Include(sc => sc.ShoppingCartItems)
                                .FirstOrDefaultAsync(sc => sc.Username == username);

                            if (existingCart != null)
                            {
                                // Xóa cart cũ và các items của nó
                                _dataContext.ShoppingCart.Remove(existingCart);
                                await _dataContext.SaveChangesAsync();
                            }

                            // Tạo cart mới
                            var cartId = Guid.NewGuid().ToString().Substring(0, 10);

                            // Lấy thông tin sản phẩm mới nhất
                            var productIds = sessionCart.Select(x => x.Product_id).Distinct().ToList();
                            var products = await _dataContext.Product
                                .AsNoTracking()
                                .Where(p => productIds.Contains(p.Product_id))
                                .ToDictionaryAsync(p => p.Product_id);

                            // Tạo cart mới từ session
                            var newCart = new ShoppingCart
                            {
                                Cart_id = cartId,
                                Username = username,
                                CreatedDate = DateTime.Now,
                                LastModifiedDate = DateTime.Now,
                                ShoppingCartItems = sessionCart.Select(item => new ShoppingCartItem
                                {
                                    CartItem_id = Guid.NewGuid().ToString().Substring(0, 10),
                                    Cart_id = cartId, // Thêm Cart_id
                                    Product_id = item.Product_id,
                                    Quantity = item.Quantity,
                                    Discount = products.TryGetValue(item.Product_id, out var product) ? product.Discount : 0, // Lấy Discount từ product
                                    Size_id = item.SizeId ?? "Size01",
                                    Ice_id = item.IceId ?? "Ice01",
                                    Sugar_id = item.SugarId ?? "Sugar01",
                                    AddedDate = DateTime.Now
                                }).ToList()
                            };

                            _dataContext.ShoppingCart.Add(newCart);
                            await _dataContext.SaveChangesAsync();

                            // Xóa session sau khi đã đồng bộ
                            HttpContext.Session.Remove("cart");
                        }

                        // Lấy cart từ database để hiển thị
                        var dbCart = await _dataContext.ShoppingCart
                            .AsNoTracking()
                            .Include(sc => sc.ShoppingCartItems)
                                .ThenInclude(sci => sci.Product)
                                    .ThenInclude(p => p.Brand)
                            .Include(sc => sc.ShoppingCartItems)
                                .ThenInclude(sci => sci.Product)
                                    .ThenInclude(p => p.TypeCoffee)
                            .Include(sc => sc.ShoppingCartItems)
                                .ThenInclude(sci => sci.Size)
                            .FirstOrDefaultAsync(sc => sc.Username == username);

                        if (dbCart == null || !dbCart.ShoppingCartItems.Any())
                        {
                            return RedirectToAction("Menu", "Home");
                        }

                        cartItems = dbCart.ShoppingCartItems.Select(item => new CartItemModel(item.Product)
                        {
                            Quantity = item.Quantity,
                            SizeId = item.Size_id ?? "Size01",
                            SizeDetail = item.Size?.SizeDetail ?? "S",
                            IceId = item.Ice_id ?? "Ice01",
                            SugarId = item.Sugar_id ?? "Sugar01",
                            Discount = item.Discount
                        }).ToList();
                    }
                    else
                    {
                        // Get cart from session for non-logged-in users
                        cartItems = HttpContext.Session.GetObjectFromJson<List<CartItemModel>>("cart") ?? new List<CartItemModel>();

                        if (!cartItems.Any())
                        {
                            return RedirectToAction("Menu", "Home");
                        }

                        // Update cart items with latest product information
                        var productIds = cartItems.Select(x => x.Product_id).Distinct().ToList();
                        var products = await _dataContext.Product
                            .AsNoTracking()
                            .Include(p => p.Brand)
                            .Include(p => p.TypeCoffee)
                            .Include(p => p.Product_Size)
                            .Where(p => productIds.Contains(p.Product_id))
                            .ToDictionaryAsync(p => p.Product_id);

                        foreach (var item in cartItems)
                        {
                            if (products.TryGetValue(item.Product_id, out var product))
                            {
                                item.Price = product.Price;
                                item.Discount = product.Discount;
                                if (!string.IsNullOrEmpty(item.SizeId))
                                {
                                    var sizePrice = _dataContext.Product_Size
                                        .AsNoTracking()
                                        .FirstOrDefault(ps =>
                                            ps.Product_id == item.Product_id &&
                                            ps.Size_id == item.SizeId)?.Price;
                                    if (sizePrice.HasValue)
                                    {
                                        item.Price += sizePrice.Value;
                                    }
                                }
                            }
                        }
                    }

                    // Create view model
                    var viewModel = new CartItemViewModel
                    {
                        CartItems = cartItems,
                        TongTien = cartItems.Sum(item =>
                            item.Price * item.Quantity * (1 - (item.Discount / 100.0)))
                    };

                    await UpdateCartInfo();
                    scope.Complete();
                    ViewBag.IsAuthenticated = !string.IsNullOrEmpty(User.Identity?.Name);

                    return View(viewModel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Checkout: {ex.Message}");
                return RedirectToAction("Error", "Home");
            }
        }
        [HttpPost]
        public async Task<IActionResult> ApplyVoucher([FromBody] VoucherApplyRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Code))
                {
                    return Json(new { success = false, message = "Vui lòng nhập mã giảm giá" });
                }

                var voucher = await _dataContext.Voucher
                    .FirstOrDefaultAsync(v => v.Code == request.Code && v.Status == true);

                if (voucher == null)
                {
                    return Json(new { success = false, message = "Mã giảm giá không tồn tại" });
                }

                if (voucher.ExpirationDate < DateTime.Now)
                {
                    return Json(new { success = false, message = "Mã giảm giá đã hết hạn" });
                }

                if (voucher.UsageLimit.HasValue && voucher.UsageCount >= voucher.UsageLimit)
                {
                    return Json(new { success = false, message = "Mã giảm giá đã hết lượt sử dụng" });
                }

                if (voucher.MinimumSpend.HasValue && request.Total < voucher.MinimumSpend.Value)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Đơn hàng tối thiểu {voucher.MinimumSpend.Value.ToString("#,0")}₫ để sử dụng mã này"
                    });
                }

                // Calculate discount amount
                double discountAmount;
                if (voucher.VoucherType == "PERCENT")
                {
                    discountAmount = (request.Total * voucher.Detail.Value) / 100;
                    if (voucher.MaximumDiscount.HasValue && discountAmount > voucher.MaximumDiscount.Value)
                    {
                        discountAmount = voucher.MaximumDiscount.Value;
                    }
                }
                else // Fixed amount
                {
                    discountAmount = voucher.Detail.Value;
                }

                // Format the discount amount with thousand separators
                string formattedDiscountAmount = discountAmount.ToString("#,0").Replace(",", ".");
                double newTotal = request.Total - discountAmount;
                string formattedNewTotal = newTotal.ToString("#,0").Replace(",", ".");

                var response = new
                {
                    success = true,
                    voucher = new
                    {
                        code = voucher.Code,
                        name = voucher.Name,
                        description = voucher.Description,
                        discountAmount = formattedDiscountAmount,
                        type = voucher.VoucherType,
                        detail = voucher.Detail
                    },
                    newTotal = formattedNewTotal,
                    discountAmount = formattedDiscountAmount
                };

                return Json(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in ApplyVoucher: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi áp dụng mã giảm giá" });
            }
        }

        public class VoucherApplyRequest
        {
            public string Code { get; set; }
            public double Total { get; set; }
        }
        [HttpGet]
        public async Task<IActionResult> GetAvailableVouchers()
        {
            try
            {
                var username = User.Identity?.Name;
                List<CartItemModel> cartItems;
                string commonBrandId = null;
                bool allSameBrand = true;

                // Lấy cart items
                if (!string.IsNullOrEmpty(username))
                {
                    cartItems = await GetCartFromDatabase(username);
                }
                else
                {
                    cartItems = HttpContext.Session.GetObjectFromJson<List<CartItemModel>>("cart") ?? new List<CartItemModel>();
                }

                if (!cartItems.Any())
                {
                    return Json(new { success = false, message = "Giỏ hàng trống" });
                }

                // Kiểm tra xem tất cả sản phẩm có cùng brand không
                var productIds = cartItems.Select(x => x.Product_id).Distinct().ToList();
                var products = await _dataContext.Product
                    .Where(p => productIds.Contains(p.Product_id))
                    .Select(p => new { p.Product_id, p.Brand_id })
                    .ToListAsync();

                if (products.Any())
                {
                    commonBrandId = products.First().Brand_id;
                    allSameBrand = products.All(p => p.Brand_id == commonBrandId);
                }

                // Tính tổng giá trị giỏ hàng
                double cartTotal = cartItems.Sum(item => item.Price * item.Quantity);

                // Lấy các voucher còn hiệu lực và phù hợp với điều kiện
                var availableVouchers = await _dataContext.Voucher
                    .Where(v =>
                        v.Status == true &&
                        v.ExpirationDate > DateTime.Now &&
                        (!v.UsageLimit.HasValue || v.UsageCount < v.UsageLimit) &&
                        (!v.MinimumSpend.HasValue || v.MinimumSpend <= cartTotal) &&
                        (
                            v.Brand_id == null || // Voucher áp dụng cho tất cả brand
                            (allSameBrand && v.Brand_id == commonBrandId) // Voucher specific brand khi tất cả sản phẩm cùng brand
                        )
                    )
                    .Select(v => new {
                        v.Code,
                        v.Name,
                        v.Description,
                        v.Detail,
                        v.VoucherType,
                        v.MinimumSpend,
                        v.MaximumDiscount,
                        v.ExpirationDate,
                        BrandName = v.Brand.BrandName,
                        Discount = v.VoucherType == "PERCENT"
                            ? $"{v.Detail}% giảm" + (v.MaximumDiscount.HasValue ? $" (tối đa {v.MaximumDiscount:N0}₫)" : "")
                            : $"{v.Detail:N0}₫ giảm"
                    })
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    vouchers = availableVouchers,
                    cartTotal = cartTotal
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetAvailableVouchers: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi lấy danh sách voucher" });
            }
        }


        /* Xác nhận đặt hàng qua email */
        public class EmailVerificationRequest
        {
            public string Email { get; set; }
            public OrderCreateModel OrderData { get; set; }
            public string Code { get; set; }
        }
        public class ApiResponse<T>
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public T Data { get; set; }
        }
        [HttpPost]
        public async Task<IActionResult> SendVerificationCode([FromBody] EmailVerificationRequest request)
        {
            try
            {
                // Validate input
                if (string.IsNullOrEmpty(request.Email))
                {
                    return Json(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Email không được để trống"
                    });
                }

                // Generate code
                var code = new Random().Next(100000, 999999).ToString();
                var timestamp = DateTime.Now;

                // Store verification data
                var verificationData = new
                {
                    Code = code,
                    Email = request.Email,
                    Timestamp = timestamp,
                    OrderData = request.OrderData
                };

                TempData["VerificationData"] = JsonSerializer.Serialize(verificationData);

                // Send verification email
                var subject = "Xác nhận đơn hàng - NHDanDz Coffee Shop";
                var body = $@"
            <h2>Xác nhận đơn hàng của bạn</h2>
            <p>Mã xác nhận của bạn là: <strong>{code}</strong></p>
            <p>Mã này sẽ hết hạn sau 1 phút.</p>
            <p>Vui lòng không chia sẻ mã này với người khác.</p>
            <br>
            <p>Trân trọng,</p>
            <p>NHDanDz Coffee Shop</p>";

                await _emailService.SendEmailAsync(request.Email, subject, body);

                return Ok(new ApiResponse<object> { Success = true });  // Thay Json() bằng Ok()
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending verification email: {ex.Message}");
                return Ok(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Có lỗi khi gửi mã xác nhận"
                });
            }
        }



        public class PlaceOrderResult
        {
            public bool Success { get; set; }
            public string BillId { get; set; }
            public string Message { get; set; }
        }
        [HttpPost]
        public async Task<IActionResult> VerifyOrderCode([FromBody] EmailVerificationRequest request)
        {
            try
            {
                // Validate input
                if (string.IsNullOrEmpty(request.Code) || string.IsNullOrEmpty(request.Email))
                {
                    return Ok(new ApiResponse<object>  // Thay Json() bằng Ok()
                    {
                        Success = false,
                        Message = "Vui lòng nhập đầy đủ thông tin"
                    });
                }

                // Get verification data
                var verificationDataJson = TempData["VerificationData"]?.ToString();
                if (string.IsNullOrEmpty(verificationDataJson))
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Mã xác nhận không đúng"
                    });
                }

                // Parse verification data
                var verificationData = JsonSerializer.Deserialize<dynamic>(verificationDataJson);
                var storedCode = verificationData.GetProperty("Code").GetString();
                var storedEmail = verificationData.GetProperty("Email").GetString();
                var timestamp = verificationData.GetProperty("Timestamp").GetDateTime();

                // Check expiration
                if (DateTime.Now.Subtract(timestamp).TotalMinutes > 1)
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Mã xác nhận đã hết hạn"
                    });
                }

                // Verify code and email
                if (request.Code != storedCode)
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Mã xác nhận không đúng"
                    });
                }

                // Get order data and place order
                var orderData = JsonSerializer.Deserialize<OrderCreateModel>(
                    verificationData.GetProperty("OrderData").GetRawText()
                );

                var placeOrderResult = await PlaceOrder(orderData); 
                var confirmationSubject = "Đặt hàng thành công - NHDanDz Coffee Shop";
                var confirmationBody = $@"
            <h2>Cảm ơn bạn đã đặt hàng!</h2>
            <p>Đơn hàng của bạn đã được xác nhận và đang được xử lý.</p> 
            <p>Chúng tôi sẽ sớm liên hệ với bạn để xác nhận chi tiết giao hàng.</p>
            <br>
            <p>Trân trọng,</p>
            <p>NHDanDz Coffee Shop</p>";

                // Send confirmation email...
                await _emailService.SendEmailAsync(request.Email, confirmationSubject, confirmationBody);

                // Return success response
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new { billId = placeOrderResult.BillId }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in VerifyOrderCode: {ex.Message}");
                return Ok(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi xác nhận mã"
                });
            }
        }

        /*
         [HttpPost]
public async Task<IActionResult> SendVerificationCode()
{
    try
    {
        // Đọc dữ liệu từ HttpContext.Request.Body
        string requestBody;
        using (var reader = new StreamReader(HttpContext.Request.Body))
        {
            requestBody = await reader.ReadToEndAsync();
        }

        // Chuyển đổi dữ liệu từ JSON sang EmailVerificationRequest
        var request = JsonSerializer.Deserialize<EmailVerificationRequest>(requestBody);

        // Validate input
        if (string.IsNullOrEmpty(request?.Email))
        {
            return Json(new ApiResponse<object>
            {
                Success = false,
                Message = "Email không được để trống"
            });
        }

        // Generate code
        var code = new Random().Next(100000, 999999).ToString();
        var timestamp = DateTime.Now;

        // Store verification data
        var verificationData = new
        {
            Code = code,
            Email = request.Email,
            Timestamp = timestamp,
            OrderData = request.OrderData
        };

        TempData["VerificationData"] = JsonSerializer.Serialize(verificationData);

        // Send verification email
        var subject = "Xác nhận đơn hàng - NHDanDz Coffee Shop";
        var body = $@"
    <h2>Xác nhận đơn hàng của bạn</h2>
    <p>Mã xác nhận của bạn là: <strong>{code}</strong></p>
    <p>Mã này sẽ hết hạn sau 1 phút.</p>
    <p>Vui lòng không chia sẻ mã này với người khác.</p>
    <br>
    <p>Trân trọng,</p>
    <p>NHDanDz Coffee Shop</p>";

        await _emailService.SendEmailAsync(request.Email, subject, body);

        return Ok(new ApiResponse<object> { Success = true });
    }
    catch (Exception ex)
    {
        _logger.LogError($"Error sending verification email: {ex.Message}");
        return Ok(new ApiResponse<object>
        {
            Success = false,
            Message = "Có lỗi khi gửi mã xác nhận"
        });
    }
}

         */


        /* -------------Xác nhận đặt hàng qua email END-------------- */ 
//public async Task<Client> GetClientByGmailAsync(string gmail)
//    {
//        Client client = null;

//        // Chuỗi kết nối tới cơ sở dữ liệu
//        var connectionString = "YourConnectionString";

//        // Truy vấn SQL
//        var sqlQuery = "SELECT TOP 1 * FROM Client WHERE Gmail = @Gmail";

//        // Sử dụng ADO.NET
//        using (var connection = new SqlConnection(connectionString))
//        {
//            using (var command = new SqlCommand(sqlQuery, connection))
//            {
//                // Thêm tham số để tránh SQL Injection
//                command.Parameters.AddWithValue("@Gmail", gmail);

//                // Mở kết nối
//                await connection.OpenAsync();

//                // Thực thi truy vấn và đọc dữ liệu
//                using (var reader = await command.ExecuteReaderAsync())
//                {
//                    if (reader.Read())
//                    {
//                        // Gán dữ liệu từ SQL vào đối tượng Client
//                        client = new Client
//                        {
//                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
//                            Name = reader.GetString(reader.GetOrdinal("Name")),
//                            Gmail = reader.GetString(reader.GetOrdinal("Gmail")),
//                            // Thêm các cột khác tương ứng với bảng Client
//                        };
//                    }
//                }
//            }
//        }

//        return client;
//    }
     

        [HttpPost]
        public async Task<PlaceOrderResult> PlaceOrder([FromBody] OrderCreateModel model)
        {
            try
            {
                var client = await _dataContext.Client
                    .FirstOrDefaultAsync(c => c.Gmail == model.clientInfo.gmail);

                if (client == null)
                {
                    var newClientId = await GenerateNewClientId();
                    client = new Client
                    {
                        Client_id = newClientId,
                        Name = model.clientInfo.name,
                        Gmail = model.clientInfo.gmail,
                        Contact = model.clientInfo.contact,
                        Location = model.clientInfo.location
                    };
                    _dataContext.Client.Add(client);
                }
                else
                {
                    client.Name = model.clientInfo.name;
                    client.Location = model.clientInfo.location;
                    client.Contact = model.clientInfo.contact;
                    _dataContext.Client.Update(client);
                }

                string billId = await GenerateNewBillId();
                var billExists = await _dataContext.Bill.AnyAsync(b => b.Bill_id == billId);
                if (billExists)
                {
                    return new PlaceOrderResult
                    {
                        Success = false,
                        Message = "Bill ID đã tồn tại"
                    };
                }


                var bill = new Bill
                {
                    Bill_id = billId,
                    Client_id = client.Client_id,
                    Date = DateTime.Now,
                    Status = false,
                    PaymentStatus = false,
                    PaymentMethod = model.paymentMethod,
                    DeleteStatus = false,
                    OrderNotes = model.orderNotes,
                    Location = model.clientInfo.location,
                    Contact = model.clientInfo.contact
                };

                List<CartItemModel> cartItems;
                var username = User.Identity?.Name;

                if (!string.IsNullOrEmpty(username))
                {
                    cartItems = await GetCartFromDatabase(username);
                }
                else
                {
                    cartItems = HttpContext.Session.GetObjectFromJson<List<CartItemModel>>("cart") ?? new List<CartItemModel>();
                }

                Voucher voucherToUpdate = null;
                double discountAmount = 0;
                if (!string.IsNullOrEmpty(model.voucherCode))
                {
                    var voucher = await _dataContext.Voucher
                        .FirstOrDefaultAsync(v => v.Code == model.voucherCode && v.Status == true);

                    if (voucher != null)
                    {
                        voucherToUpdate = voucher;
                        var cartTotal = await CalculateCartTotalFromModel(cartItems);

                        if (voucher.VoucherType == "PERCENT")
                        {
                            discountAmount = (cartTotal * voucher.Detail.Value) / 100;
                            if (voucher.MaximumDiscount.HasValue && discountAmount > voucher.MaximumDiscount.Value)
                            {
                                discountAmount = voucher.MaximumDiscount.Value;
                            }
                        }
                        else
                        {
                            discountAmount = voucher.Detail.Value;
                        }

                        var billVoucher = new Bill_Voucher
                        {
                            Bill_id = billId,
                            Voucher_id = voucher.Voucher_id,
                            AppliedDate = DateTime.Now,
                            DiscountAmount = discountAmount
                        };

                        voucher.UsageCount = (voucher.UsageCount ?? 0) + 1;

                        _dataContext.Voucher.Update(voucher);
                        _dataContext.Bill_Voucher.Add(billVoucher);

                        bill.Total = cartTotal - discountAmount;
                    }
                    else
                    {
                        bill.Total = await CalculateCartTotalFromModel(cartItems);
                    }
                }
                else
                {
                    bill.Total = await CalculateCartTotalFromModel(cartItems);
                }
  

                _dataContext.Bill.Add(bill);
                await _dataContext.SaveChangesAsync();

 
                var productBills = new Dictionary<string, Product_Bill>();
                double total = 0;

                foreach (var item in cartItems)
                {
                    var compositeKey = $"{item.Product_id}_{billId}_{item.SizeId ?? "Size01"}_{item.IceId ?? "Ice01"}_{item.SugarId ?? "Sugar01"}";

                    if (!productBills.ContainsKey(compositeKey))
                    {
                        var itemTotal = item.Price * item.Quantity * (1 - (item.Discount / 100.0));
                        var productBill = new Product_Bill
                        {
                            Bill_id = billId,
                            Product_id = item.Product_id,
                            Size_id = item.SizeId ?? "Size01",
                            Ice_id = item.IceId ?? "Ice01",
                            Sugar_id = item.SugarId ?? "Sugar01",
                            Quantity = item.Quantity,
                            Amount = itemTotal,
                            Discount = item.Discount
                        };

                        productBills.Add(compositeKey, productBill);
                        total += itemTotal;
                    }
                    else
                    {
                        var existingBill = productBills[compositeKey];
                        var itemTotal = item.Price * item.Quantity * (1 - (item.Discount / 100.0));
                        existingBill.Quantity += item.Quantity;
                        existingBill.Amount += itemTotal;
                        total += itemTotal;
                    }
                }

                using (var transaction = await _dataContext.Database.BeginTransactionAsync())
                {
                    try
                    {
                        _dataContext.ChangeTracker.Clear();

                        if (client != null)
                        {
                            _dataContext.Client.Attach(client);
                        }

                        await _dataContext.Product_Bill.AddRangeAsync(productBills.Values);

                        if (!string.IsNullOrEmpty(username))
                        {
                            var dbCart = await _dataContext.ShoppingCart
                                .Include(sc => sc.ShoppingCartItems)
                                .FirstOrDefaultAsync(sc => sc.Username == username);

                            if (dbCart != null)
                            {
                                _dataContext.ShoppingCartItem.RemoveRange(dbCart.ShoppingCartItems);
                            }
                        }
                        else
                        {
                            HttpContext.Session.Remove("cart");
                        }

                        await _dataContext.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return new PlaceOrderResult
                        {
                            Success = true,
                            BillId = billId,
                            Message = "Đặt hàng thành công"
                        };
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        return new PlaceOrderResult
                        {
                            Success = false,
                            Message = "Lỗi khi xử lý giao dịch"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new PlaceOrderResult
                {
                    Success = false,
                    Message = "Có lỗi xảy ra: " + ex.Message
                };
            }
        }



        private async Task<double> CalculateCartTotalFromModel(List<CartItemModel> cartItems)
        {
            double total = 0;
            foreach (var item in cartItems)
            {
                var price = item.Price;
                if (!string.IsNullOrEmpty(item.SizeId))
                {
                    var sizePrice = await _dataContext.Product_Size
                        .AsNoTracking()
                        .Where(ps => ps.Product_id == item.Product_id && ps.Size_id == item.SizeId)
                        .Select(ps => ps.Price)
                        .FirstOrDefaultAsync();
                    if (sizePrice > 0)
                    {
                        price = sizePrice;
                    }
                }
                total += price * item.Quantity * (1 - (item.Discount / 100.0));
            }
            return total;
        }

        private async Task<string> GenerateNewBillId()
        {
            while (true) // Thêm vòng lặp để đảm bảo tìm được ID duy nhất
            {
                var lastId = await _dataContext.Bill
                    .Where(b => b.Bill_id.StartsWith("BILL"))
                    .Select(b => b.Bill_id)
                    .OrderByDescending(id => id)
                    .FirstOrDefaultAsync();

                string newId;
                if (string.IsNullOrEmpty(lastId))
                {
                    newId = "BILL001";
                }
                else
                {
                    // Đảm bảo lấy đúng phần số và xử lý padding
                    if (int.TryParse(lastId.Substring(4).Trim(), out int lastNumber))
                    {
                        newId = $"BILL{(lastNumber + 1):D3}";
                    }
                    else
                    {
                        throw new Exception("Invalid bill ID format in database");
                    }
                }

                // Kiểm tra xem ID mới có tồn tại không
                var exists = await _dataContext.Bill.AnyAsync(b => b.Bill_id == newId);
                if (!exists)
                {
                    return newId;
                }
                // Nếu ID đã tồn tại, vòng lặp sẽ tiếp tục để tạo ID mới
            }
        }

        private async Task<List<CartItemModel>> GetCartItems(string username)
        {
            if (!string.IsNullOrEmpty(username))
            {
                return await GetCartFromDatabase(username);
            }
            return HttpContext.Session.GetObjectFromJson<List<CartItemModel>>("cart") ?? new List<CartItemModel>();
        }
        private async Task<string> GenerateNewClientId()
        {
            while (true)
            {
                // Lấy Client_id cuối cùng
                var lastId = await _dataContext.Client
                    .OrderByDescending(c => c.Client_id)
                    .Select(c => c.Client_id)
                    .FirstOrDefaultAsync();

                string newId;
                if (string.IsNullOrEmpty(lastId))
                {
                    newId = "KH001";
                }
                else
                {
                    // Lấy số từ ID cuối cùng
                    var lastNumber = int.Parse(lastId.Substring(2).Trim());
                    newId = $"KH{(lastNumber + 1):D3}";
                }

                // Kiểm tra xem ID mới đã tồn tại chưa
                var exists = await _dataContext.Client.AnyAsync(c => c.Client_id == newId);
                if (!exists)
                {
                    return newId;
                }
            }
        }

        public class OrderCreateModel
        {
            public ClientInfo clientInfo { get; set; }
            public string orderNotes { get; set; }
            public string paymentMethod { get; set; }
            public string voucherCode { get; set; }
        }
        public class ClientInfo
        {
            public string name { get; set; }
            public string gmail { get; set; }
            public string contact { get; set; }
            public string location { get; set; }
        }
        public async Task<IActionResult> Confirmation(string id)
        {
            try
            {
                var order = await _dataContext.Bill
                    .Include(b => b.Client)
                    .Include(b => b.Product_Bill)
                        .ThenInclude(pb => pb.Product)
                    .Include(b => b.Product_Bill)
                        .ThenInclude(pb => pb.Size)
                    .Include(b => b.Product_Bill)
                        .ThenInclude(pb => pb.Ice)
                    .Include(b => b.Product_Bill)
                        .ThenInclude(pb => pb.Sugar)
                    .Include(b => b.Bill_Voucher)
                        .ThenInclude(bv => bv.Voucher)
                    .FirstOrDefaultAsync(b => b.Bill_id == id);

                if (order == null)
                {
                    return RedirectToAction("Index");
                }

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Confirmation: {ex.Message}");
                return RedirectToAction("Error");
            }
        }
        public class FeedbackViewModel
        {
            public string ClientName { get; set; }
            public double Rating { get; set; }
            public string Comment { get; set; }
            public DateTime CreatedDate { get; set; }
            public string AdminResponse { get; set; }
            public DateTime? ResponseTime { get; set; }
        }
        public async Task<IActionResult> ProductDetailView(string id)
        {
            var product = await _dataContext.Product
                .Include(p => p.Brand)
                .Include(p => p.TypeCoffee)
                .Include(p => p.Feedback)
                    .ThenInclude(f => f.Client)
                .Where(p => p.Product_id == id)
                .Select(p => new HienThiSanPham
                {
                    Ma_Sanpham = p.Product_id,
                    Ten_Sanpham = p.ProductName,
                    Giagoc = p.Price,
                    Rating = p.Rating,
                    ReviewCount = p.ReviewCount,
                    Link1 = p.Image,
                    Discount = p.Discount,
                    Ten_Nhasanxuat = p.Brand.BrandName,
                    Ten_loai = p.TypeCoffee.TypeName,
                    SoldQuantity = _dataContext.Product_Bill
                        .Where(pb => pb.Product_id == p.Product_id)
                        .Sum(pb => pb.Quantity),
                    Description = p.Description,
                    Feedbacks = p.Feedback.Select(f => new FeedbackViewModel
                    {
                        ClientName = f.Client.Name,
                        Rating = f.Rating,
                        Comment = f.Comment,
                        CreatedDate = f.CreatedDate,
                        AdminResponse = f.AdminResponse,
                        ResponseTime = f.ResponseDate  // Xử lý null
                    })
                        .OrderByDescending(f => f.CreatedDate)
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (product == null)
            {
                return RedirectToAction("Menu");
            }

            var username = User.Identity?.Name;
            if (!string.IsNullOrEmpty(username))
            {
                // Đầu tiên lấy Client_id từ Account
                var clientId = await _dataContext.Account
                    .Where(a => a.Username == username)
                    .Select(a => a.Client_id)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrEmpty(clientId))
                {
                    // Sau đó kiểm tra Bill với Client_id
                    var hasPurchased = await _dataContext.Bill
                        .Include(b => b.Product_Bill)
                        .AnyAsync(b => b.Client_id == clientId &&
                                      b.Product_Bill.Any(pb => pb.Product_id == id));
                    if (hasPurchased != null)
                    {
                        ViewBag.HasPurchased = hasPurchased;
                    }
                    else
                    {
                        ViewBag.HasPurchased = false;
                    }
                }
                else
                {
                    ViewBag.HasPurchased = false;
                }
            }

            return View(product);
        }
        [HttpPost]
        public async Task<IActionResult> SubmitFeedback([FromBody] FeedbackSubmitModel model)
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để đánh giá" });
                }

                // Tìm client_id từ username
                var clientId = await _dataContext.Account
                    .Where(a => a.Username == username)
                    .Select(a => a.Client_id)
                    .FirstOrDefaultAsync();

                if (clientId == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin khách hàng" });
                }

                var feedback = new Feedback
                {
                    Feedback_id = Guid.NewGuid().ToString().Substring(0, 10),
                    Client_id = clientId,
                    Product_id = model.ProductId,
                    Rating = model.Rating,
                    Comment = model.Comment,
                    CreatedDate = DateTime.Now,
                    Status = false
                    // Loại bỏ ResponseBy nếu không cần
                };

                _dataContext.Feedback.Add(feedback);
                await _dataContext.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in SubmitFeedback: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi gửi đánh giá" });
            }
        }
        public class FeedbackSubmitModel
        {
            public string ProductId { get; set; }
            public double Rating { get; set; }
            public string Comment { get; set; }
        } 
        [HttpGet]
        public async Task<IActionResult> GetRelatedProducts(string productId, int limit = 10)
        {
            try
            {
                // First get the current product's brand_id and type_id
                var currentProduct = await _dataContext.Product
                    .Where(p => p.Product_id == productId)
                    .Select(p => new { p.Brand_id, p.Type_id })
                    .FirstOrDefaultAsync();

                if (currentProduct == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                // Get products from the same brand (excluding current product)
                var brandProducts = await _dataContext.Product
                    .Include(p => p.Brand)
                    .Include(p => p.TypeCoffee)
                    .Where(p => p.Brand_id == currentProduct.Brand_id && p.Product_id != productId)
                    .Select(p => new {
                        id = p.Product_id,
                        name = p.ProductName,
                        price = p.Price,
                        discount = p.Discount,
                        rating = p.Rating,
                        reviewCount = p.ReviewCount,
                        image = p.Image,
                        brandName = p.Brand.BrandName,
                        typeName = p.TypeCoffee.TypeName
                    })
                    .Take(limit)
                    .ToListAsync();

                // Get products of the same type (excluding current product and brand products)
                var typeProducts = await _dataContext.Product
                    .Include(p => p.Brand)
                    .Include(p => p.TypeCoffee)
                    .Where(p => p.Type_id == currentProduct.Type_id &&
                               p.Product_id != productId  )
                    .Select(p => new {
                        id = p.Product_id,
                        name = p.ProductName,
                        price = p.Price,
                        discount = p.Discount,
                        rating = p.Rating,
                        reviewCount = p.ReviewCount,
                        image = p.Image,
                        brandName = p.Brand.BrandName,
                        typeName = p.TypeCoffee.TypeName
                    })
                    .Take(limit)
                    .ToListAsync();

                // Get brand name
                var brandName = await _dataContext.Brand
                    .Where(b => b.Brand_id == currentProduct.Brand_id)
                    .Select(b => b.BrandName)
                    .FirstOrDefaultAsync();

                return Json(new
                {
                    success = true,
                    brandName = brandName,
                    brandProducts = brandProducts,
                    typeProducts = typeProducts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetRelatedProducts: {ex.Message}");
                return Json(new { success = false, message = "Error fetching related products" });
            }
        }
        public async Task<IActionResult> About()
        {
            return View();
        }

        public async Task<IActionResult> FAQ()
        {
            return View();
        }
        public async Task<IActionResult> Contact()
        {
            return View();
        }

    }
}

