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
namespace DemoApp_Test.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DataContext _dataContext;

        // Lấy dữ liệu từ database và logger

        public HomeController(DataContext context, ILogger<HomeController> logger)
        {
            _dataContext = context;
            _logger = logger;
        }



        // Hàm Update Giá trị của cart trong session
        private void UpdateCartInfo()
        {

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItemModel>>("cart") ?? new List<CartItemModel>();
            ViewData["CartItemCount"] = cart.Sum(x => x.Quantity);
            ViewData["CartTotal"] = cart.Sum(x => x.Price * x.Quantity).ToString("N2");
        }

        // Thêm dữ liệu vào session khi nhấn vào AddToCart

        [HttpPost]
        public IActionResult AddToCart([FromBody] string productId)
        {
            UpdateCartInfo();

            try
            {
                // Lấy giỏ hàng từ Session
                var cartItems = HttpContext.Session.GetObjectFromJson<List<CartItemModel>>("cart");

                if (cartItems == null)
                {
                    cartItems = new List<CartItemModel>();
                }

                // Log product search 
                var product = _dataContext.Product.FirstOrDefault(sp => sp.Product_id == productId);

                if (product == null)
                {
                    return NotFound(new
                    {
                        message = "Sản phẩm không tồn tại.",
                        productId = productId,
                        timestamp = DateTime.Now
                    });
                }

                var existingItem = cartItems.FirstOrDefault(item => item.Product_id == productId);

                if (existingItem != null)
                {
                    existingItem.Quantity++;
                }
                else
                {
                    cartItems.Add(new CartItemModel(product));
                }

                // Save to session
                HttpContext.Session.SetObjectAsJson("cart", cartItems);

                var response = new
                {
                    success = true,
                    cartCount = cartItems.Count,
                    totalItems = cartItems.Sum(x => x.Quantity),
                    message = "Thêm vào giỏ hàng thành công",
                    cartContents = cartItems.Select(x => new
                    {
                        x.Product_id,
                        x.ProductName,
                        x.Quantity,
                        x.Price
                    })
                };

                return Json(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in AddToCart: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new
                {
                    message = "Lỗi khi thêm vào giỏ hàng",
                    error = ex.Message,
                    timestamp = DateTime.Now
                });
            }
        }

        // Update giá trị của cart (Thêm hoặc bớt số lượng sản phẩm)

        [HttpPost]
        public IActionResult UpdateCart([FromBody] UpdateCartRequest request)
        {
            UpdateCartInfo();

            try
            {
                var cart = HttpContext.Session.GetObjectFromJson<List<CartItemModel>>("cart");
                var cartItem = cart.FirstOrDefault(x => x.Product_id == request.productId);

                if (cartItem != null)
                {
                    cartItem.Quantity = request.quantity;
                    HttpContext.Session.SetObjectAsJson("cart", cart);
                    return Json(new { success = true });
                }

                return Json(new { success = false, message = "Item not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Remove giá trị của sản phẩm trong cart


        [HttpPost]
        public IActionResult RemoveFromCart([FromBody] string productId)
        {
            UpdateCartInfo();

            try
            {
                // Log để kiểm tra
                _logger.LogInformation($"Removing product: {productId}");

                var cart = HttpContext.Session.GetObjectFromJson<List<CartItemModel>>("cart");

                if (cart == null)
                {
                    return Json(new { success = false, message = "Cart is empty" });
                }

                var item = cart.FirstOrDefault(x => x.Product_id == productId);

                if (item == null)
                {
                    return Json(new { success = false, message = "Product not found in cart" });
                }

                cart.Remove(item);
                HttpContext.Session.SetObjectAsJson("cart", cart);

                return Json(new
                {
                    success = true,
                    message = "Product removed successfully",
                    cartCount = cart.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error removing product: {ex.Message}");
                return Json(new
                {
                    success = false,
                    message = "Error removing product from cart",
                    error = ex.Message
                });
            }
        }

        // Lớp để update yêu cầu
        public class UpdateCartRequest
        {
            public string productId { get; set; }
            public int quantity { get; set; }
        }

        // Login

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            // Giả sử chúng ta kiểm tra username và password với dữ liệu mẫu
            if (username == "admin" && password == "admin123")
            {
                // Điều hướng đến controller của admin
                return RedirectToAction("Index", "Admin");
            }
            else if (username == "user" && password == "user123")
            {
                // Tạo danh sách các claim của người dùng
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username)
        };

                // Tạo identity và principal cho người dùng
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                // Đăng nhập người dùng
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

                // Điều hướng đến trang chủ sau khi đăng nhập thành công
                return RedirectToAction("Index", "Home");
            }
            else
            {
                // Nếu đăng nhập không thành công, quay lại trang đăng nhập với thông báo lỗi
                ViewBag.Error = "Invalid username or password";
                return View();
            }
        }


        public IActionResult Home()
        {
            UpdateCartInfo();

            return View();
        }

        // Menu View
        public async Task<IActionResult> Menu()
        {
            // Lấy danh sách sản phẩm
            var products = await _dataContext.Product
                                             .Include(sp => sp.Brand)
                                             .Include(sp => sp.TypeCoffee)
                                             .OrderByDescending(sp => sp.Date)
                                             .ToListAsync();

            // Lấy tất cả các loại cà phê từ bảng TypeCoffee
            var typeCoffees = await _dataContext.TypeCoffee.ToListAsync();
            var Brands = await _dataContext.Brand.ToListAsync();
            var Sizes = await _dataContext.Size.ToListAsync();

            // Truyền cả danh sách sản phẩm và TypeCoffee vào View
            var model = new MenuViewModel
            {
                Product = products,
                TypeCoffee = typeCoffees,
                Brand = Brands,
                Size = Sizes
            };
            UpdateCartInfo();

            return View(model);
        }

        // Menu Index
        public IActionResult Index()
        {
            UpdateCartInfo();

            ViewBag.ConfirmLogin = User.Identity.IsAuthenticated;
            return View();
        }

        public IActionResult Privacy()
        {
            UpdateCartInfo();

            return View();
        }

        // Load MenuComponent (Dùng để chuyển giữa các trang)

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> LoadProducts(int page = 1)
        {
            UpdateCartInfo();

            return ViewComponent("CategoriesMenu", new { currentPage = page });
        }
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


        // Kiểm tra cart trong session
        public IActionResult GetCart()
        {
            // Lấy giỏ hàng từ Session
            List<CartItemModel> cartItems = HttpContext.Session.GetObjectFromJson<List<CartItemModel>>("cart") ?? new List<CartItemModel>();

            // Trả về giỏ hàng dưới dạng JSON để kiểm tra
            return Json(cartItems); // Trả về đúng biến cartItems
        }

        // View Shopping Carts
        public IActionResult ShoppingCarts()
        {
            UpdateCartInfo();

            // Lấy giỏ hàng từ Session
            List<CartItemModel> cartItems = HttpContext.Session.GetObjectFromJson<List<CartItemModel>>("cart") ?? new List<CartItemModel>();

            // Khởi tạo ViewModel với danh sách sản phẩm và tổng tiền
            CartItemViewModel cartVM = new()
            {
                CartItems = cartItems,
                TongTien = cartItems.Sum(x => x.Quantity * x.Price)
            };

            // Trả về View với dữ liệu ViewModel
            return View(cartVM); // Truyền cartVM vào View
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
    }
}

