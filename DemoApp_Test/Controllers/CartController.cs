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
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using static DemoApp_Test.Controllers.CartController;

namespace DemoApp_Test.Controllers
{
    public class CartController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly ILogger<CartController> _logger;
         
        public IActionResult Index()
        { 
            return View();
        }

        // View Shopping Carts
        public async Task<IActionResult> ShoppingCarts()
        {
            try
            {
                // Lấy username của người dùng
                var username = User.Identity?.Name;
                List<CartItemModel> cartItems;

                if (!string.IsNullOrEmpty(username))
                {
                    // Kiểm tra session cart trước
                    var sessionCart = HttpContext.Session.GetObjectFromJson<List<CartItemModel>>("cart");

                    if (sessionCart != null && sessionCart.Any())
                    {
                        // Nếu có session cart, xóa cart cũ và tạo mới từ session
                        cartItems = await SyncSessionCartToDatabase(username, sessionCart);
                        // Xóa session sau khi đã đồng bộ
                        HttpContext.Session.Remove("cart");
                    }
                    else
                    {
                        // Nếu không có session cart, lấy từ database
                        cartItems = await GetCartFromDatabase(username);
                    }
                }
                else
                {
                    // Nếu chưa đăng nhập, lấy giỏ hàng từ session
                    cartItems = HttpContext.Session.GetObjectFromJson<List<CartItemModel>>("cart") ?? new List<CartItemModel>();

                    if (cartItems.Any())
                    {
                        // Cập nhật thông tin sản phẩm mới nhất
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
                                    var sizePrice = await _dataContext.Product_Size
                                        .AsNoTracking()
                                        .Where(ps => ps.Product_id == item.Product_id && ps.Size_id == item.SizeId)
                                        .Select(ps => ps.Price)
                                        .FirstOrDefaultAsync();

                                    if (sizePrice > 0)
                                    {
                                        item.Price += sizePrice;
                                    }
                                }
                            }
                        }
                    }
                }

                // Khởi tạo ViewModel với danh sách sản phẩm và tổng tiền
                var cartVM = new CartItemViewModel
                {
                    CartItems = cartItems,
                    TongTien = cartItems.Sum(x => x.Price * x.Quantity * (1 - (x.Discount / 100.0)))
                };

                await UpdateCartInfo();
                return View(cartVM);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in ShoppingCarts: {ex.Message}");
                return RedirectToAction("Error", "Home");
            }
        }
        public CartController(DataContext context, ILogger<CartController> logger)
        {
            _dataContext = context;
            _logger = logger;
        }

        // Thêm sản phẩm vào giỏ hàng cơ bản
        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] string productId)
        {
            try
            {
                if (string.IsNullOrEmpty(productId))
                {
                    return BadRequest(new { message = "Invalid product ID" });
                }

                var product = await _dataContext.Product
                    .Include(p => p.Brand)
                    .Include(p => p.TypeCoffee)
                    .FirstOrDefaultAsync(p => p.Product_id == productId);

                if (product == null)
                {
                    return NotFound(new { message = "Sản phẩm không tồn tại." });
                }

                var username = User.Identity?.Name;
                if (!string.IsNullOrEmpty(username))
                {
                    var dbCart = await GetOrCreateCart(username);
                    await AddItemToDbCart(dbCart.Cart_id, product);
                    var cartItems = await GetCartFromDatabase(username);
                    await UpdateCartInfo();

                    return Json(new
                    {
                        success = true,
                        cartCount = cartItems.Count,
                        totalItems = cartItems.Sum(x => x.Quantity),
                        message = "Thêm vào giỏ hàng thành công"
                    });
                }
                else
                {
                    var cartItems = HttpContext.Session.GetObjectFromJson<List<CartItemModel>>("cart") ?? new List<CartItemModel>();
                    var existingItem = cartItems.FirstOrDefault(item => item.Product_id == productId);

                    if (existingItem != null)
                    {
                        existingItem.Quantity++;
                    }
                    else
                    {
                        cartItems.Add(new CartItemModel(product));
                    }

                    HttpContext.Session.SetObjectAsJson("cart", cartItems);
                    await UpdateCartInfo();

                    return Json(new
                    {
                        success = true,
                        cartCount = cartItems.Count,
                        totalItems = cartItems.Sum(x => x.Quantity),
                        message = "Thêm vào giỏ hàng thành công"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in AddToCart: {ex.Message}");
                return StatusCode(500, new { message = "Lỗi khi thêm vào giỏ hàng" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddToCartWithOptions([FromBody] AddToCartRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ProductId))
                {
                    return BadRequest(new { message = "Invalid product ID" });
                }

                var product = await _dataContext.Product
                    .Include(p => p.Brand)
                    .Include(p => p.TypeCoffee)
                    .FirstOrDefaultAsync(p => p.Product_id == request.ProductId);

                if (product == null)
                {
                    return NotFound(new { message = "Sản phẩm không tồn tại." });
                }

                var username = User.Identity?.Name;
                if (!string.IsNullOrEmpty(username))
                {
                    var dbCart = await GetOrCreateCart(username);

                    // Kiểm tra xem sản phẩm với các options tương tự đã tồn tại chưa
                    var existingItem = await _dataContext.ShoppingCartItem
                        .FirstOrDefaultAsync(i =>
                            i.Cart_id == dbCart.Cart_id &&
                            i.Product_id == request.ProductId &&
                            i.Size_id == request.SizeId &&
                            i.Ice_id == request.IceId &&
                            i.Sugar_id == request.SugarId);

                    if (existingItem != null)
                    {
                        // Nếu đã tồn tại, cộng thêm số lượng
                        existingItem.Quantity += request.Quantity;
                        _dataContext.ShoppingCartItem.Update(existingItem);
                    }
                    else
                    {
                        // Nếu chưa tồn tại, tạo mới item
                        var cartItem = new ShoppingCartItem
                        {
                            CartItem_id = Guid.NewGuid().ToString().Substring(0, 10),
                            Cart_id = dbCart.Cart_id,
                            Product_id = request.ProductId,
                            Quantity = request.Quantity,
                            Size_id = request.SizeId,
                            Ice_id = request.IceId,
                            Sugar_id = request.SugarId,
                            AddedDate = DateTime.Now,
                            Discount = product.Discount
                        };
                        _dataContext.ShoppingCartItem.Add(cartItem);
                    }

                    await _dataContext.SaveChangesAsync();
                    var cartItems = await GetCartFromDatabase(username);
                    await UpdateCartInfo();
                    return Json(new
                    {
                        success = true,
                        cartCount = cartItems.Count,
                        totalItems = cartItems.Sum(x => x.Quantity),
                        message = "Thêm vào giỏ hàng thành công"
                    });
                }
                else
                {
                    var cartItems = HttpContext.Session.GetObjectFromJson<List<CartItemModel>>("cart") ?? new List<CartItemModel>();

                    // Kiểm tra sản phẩm có cùng options trong session
                    var existingItem = cartItems.FirstOrDefault(i =>
                        i.Product_id == request.ProductId &&
                        i.SizeId == request.SizeId &&
                        i.IceId == request.IceId &&
                        i.SugarId == request.SugarId);

                    if (existingItem != null)
                    {
                        // Nếu đã tồn tại, cộng thêm số lượng
                        existingItem.Quantity += request.Quantity;
                    }
                    else
                    {
                        // Nếu chưa tồn tại, thêm mới
                        var cartModel = new CartItemModel(product)
                        {
                            Quantity = request.Quantity,
                            SizeId = request.SizeId,
                            IceId = request.IceId,
                            SugarId = request.SugarId
                        };
                        cartItems.Add(cartModel);
                    }

                    HttpContext.Session.SetObjectAsJson("cart", cartItems);
                    await UpdateCartInfo();
                    return Json(new
                    {
                        success = true,
                        cartCount = cartItems.Count,
                        totalItems = cartItems.Sum(x => x.Quantity),
                        message = "Thêm vào giỏ hàng thành công"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in AddToCartWithOptions: {ex.Message}");
                return StatusCode(500, new { message = "Lỗi khi thêm vào giỏ hàng" });
            }
        }


        // Cập nhật số lượng sản phẩm
        [HttpPost]
        public async Task<IActionResult> UpdateCart([FromBody] UpdateCartRequest request)
        {
            try
            {
                var username = User.Identity?.Name;

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (!string.IsNullOrEmpty(username))
                    {
                        var dbCart = await _dataContext.ShoppingCart
                            .Include(sc => sc.ShoppingCartItems)
                            .FirstOrDefaultAsync(sc => sc.Username == username);

                        if (dbCart != null)
                        {
                            var cartItem = dbCart.ShoppingCartItems
                                .FirstOrDefault(sci => sci.Product_id == request.productId);

                            if (cartItem != null)
                            {
                                cartItem.Quantity = request.quantity;
                                dbCart.LastModifiedDate = DateTime.Now;
                                await _dataContext.SaveChangesAsync();

                                var price = await GetUpdatedPrice(cartItem.Product_id, cartItem.Size_id);

                                scope.Complete();

                                return Json(new
                                {
                                    success = true,
                                    newQuantity = cartItem.Quantity,
                                    price = price,
                                    total = price * cartItem.Quantity
                                });
                            }
                        }
                        return Json(new { success = false, message = "Item not found in database" });
                    }
                    else
                    {
                        var cart = HttpContext.Session.GetObjectFromJson<List<CartItemModel>>("cart") ?? new List<CartItemModel>();
                        var cartItem = cart.FirstOrDefault(x => x.Product_id == request.productId);

                        if (cartItem != null)
                        {
                            cartItem.Quantity = request.quantity;
                            var price = await GetUpdatedPrice(cartItem.Product_id, cartItem.SizeId);
                            cartItem.Price = price;

                            HttpContext.Session.SetObjectAsJson("cart", cart);
                            scope.Complete();

                            return Json(new
                            {
                                success = true,
                                newQuantity = cartItem.Quantity,
                                price = price,
                                total = price * cartItem.Quantity
                            });
                        }
                        return Json(new { success = false, message = "Item not found in session" });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UpdateCart: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật giỏ hàng" });
            }
        }



        // Xóa sản phẩm khỏi giỏ
        [HttpPost]
        public async Task<IActionResult> RemoveFromCart([FromBody] string productId)
        {
            try
            {
                _logger.LogInformation($"Attempting to remove product: {productId}");
                var username = User.Identity?.Name;

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (!string.IsNullOrEmpty(username))
                    {
                        // Xóa từ database
                        var dbCart = await _dataContext.ShoppingCart
                            .Include(sc => sc.ShoppingCartItems)
                            .FirstOrDefaultAsync(sc => sc.Username == username);

                        if (dbCart == null)
                        {
                            return Json(new { success = false, message = "Cart not found" });
                        }

                        var cartItem = dbCart.ShoppingCartItems
                            .FirstOrDefault(sci => sci.Product_id == productId);

                        if (cartItem == null)
                        {
                            return Json(new { success = false, message = "Product not found in cart" });
                        }

                        _dataContext.ShoppingCartItem.Remove(cartItem);
                        dbCart.LastModifiedDate = DateTime.Now;
                        await _dataContext.SaveChangesAsync();

                        // Lấy số lượng item còn lại
                        var remainingCount = dbCart.ShoppingCartItems.Count - 1;
                        var totalPrice = await CalculateCartTotal(dbCart.ShoppingCartItems.Where(x => x.Product_id != productId));

                        scope.Complete();

                        return Json(new
                        {
                            success = true,
                            message = "Product removed successfully",
                            cartCount = remainingCount,
                            total = totalPrice
                        });
                    }
                    else
                    {
                        // Xóa từ session
                        var cart = HttpContext.Session.GetObjectFromJson<List<CartItemModel>>("cart");

                        if (cart == null || !cart.Any())
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

                        var totalPrice = cart.Sum(x => x.Price * x.Quantity);

                        scope.Complete();

                        return Json(new
                        {
                            success = true,
                            message = "Product removed successfully",
                            cartCount = cart.Count,
                            total = totalPrice
                        });
                    }
                }

                // Cập nhật thông tin giỏ hàng sau khi xóa
                await UpdateCartInfo();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error removing product: {ex.Message}");
                return Json(new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi xóa sản phẩm khỏi giỏ hàng",
                    error = ex.Message
                });
            }
        }


        // Kiểm tra cart trong session
        public IActionResult GetCart()
        {
            // Lấy giỏ hàng từ Session
            List<CartItemModel> cartItems = HttpContext.Session.GetObjectFromJson<List<CartItemModel>>("cart") ?? new List<CartItemModel>();

            // Trả về giỏ hàng dưới dạng JSON để kiểm tra
            return Json(cartItems); // Trả về đúng biến cartItems
        }


        // Tính giá sản phẩm theo size
        private async Task<double> GetUpdatedPrice(string productId, string sizeId)
        {
            var product = await _dataContext.Product
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Product_id == productId);

            if (!string.IsNullOrEmpty(sizeId))
            {
                var sizePrice = await _dataContext.Product_Size
                    .AsNoTracking()
                    .Where(ps => ps.Product_id == productId && ps.Size_id == sizeId)
                    .Select(ps => ps.Price)
                    .FirstOrDefaultAsync();

                if (sizePrice > 0)
                {
                    return sizePrice;
                }
            }

            return product?.Price ?? 0;
        }
        private async Task<ShoppingCart> GetOrCreateCart(string username)
        {
            var cart = await _dataContext.ShoppingCart
                .FirstOrDefaultAsync(sc => sc.Username == username);

            if (cart == null)
            {
                cart = new ShoppingCart
                {
                    Cart_id = Guid.NewGuid().ToString().Substring(0, 10),
                    Username = username,
                    CreatedDate = DateTime.Now,
                    LastModifiedDate = DateTime.Now
                };
                _dataContext.ShoppingCart.Add(cart);
                await _dataContext.SaveChangesAsync();
            }

            return cart;
        }


        // Thêm item vào giỏ hàng trong database
        private async Task<ShoppingCartItem> AddItemToDbCart(string cartId, Product product)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(cartId) || product == null)
            {
                throw new ArgumentException("Invalid cart ID or product");
            }

            // First check if cart exists
            var cart = await _dataContext.ShoppingCart.FindAsync(cartId);
            if (cart == null)
            {
                throw new ArgumentException("Cart not found");
            }

            try
            {
                // Check for existing item using a separate context


                try
                {
                    var existingItem = await _dataContext.ShoppingCartItem
                        .Where(sci => sci.Cart_id == cartId && sci.Product_id == product.Product_id)
                        .Select(sci => new ShoppingCartItem
                        {
                            CartItem_id = sci.CartItem_id,
                            Cart_id = sci.Cart_id,
                            Product_id = sci.Product_id,
                            Quantity = sci.Quantity,
                            Size_id = sci.Size_id ?? "Size01",
                            Ice_id = sci.Ice_id ?? "Ice01",
                            Sugar_id = sci.Sugar_id ?? "Sugar01",
                            AddedDate = sci.AddedDate,
                            Discount = sci.Discount  // Thiếu Price từ Product
                        })
                        .FirstOrDefaultAsync();
                    // Update quantity
                    existingItem.Quantity++;
                    _dataContext.Update(existingItem);
                    await _dataContext.SaveChangesAsync();
                    return existingItem;
                }
                catch (Exception ex)
                {
                    // Create new item
                    var newItem = new ShoppingCartItem
                    {
                        CartItem_id = Guid.NewGuid().ToString().Substring(0, 10),
                        Cart_id = cartId,
                        Product_id = product.Product_id,
                        Quantity = 1,
                        Size_id = "Size01", // Default size
                        Ice_id = "Ice01",   // Default ice
                        Sugar_id = "Sugar01", // Default sugar
                        AddedDate = DateTime.Now,
                        Discount = product.Discount
                    };

                    _dataContext.ShoppingCartItem.Add(newItem);
                    await _dataContext.SaveChangesAsync();
                    return newItem;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in AddItemToDbCart: {ex.Message}");
                throw;
            }
        }
 

        // Cập nhật thông tin giỏ hàng (số lượng, tổng tiền)  
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



        // Lấy giỏ hàng từ database
        private async Task<List<CartItemModel>> GetCartFromDatabase(string username)
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
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
                    .Include(sc => sc.ShoppingCartItems) // Thêm những dòng này
                        .ThenInclude(sci => sci.Ice)
                    .Include(sc => sc.ShoppingCartItems)
                        .ThenInclude(sci => sci.Sugar)
                    .FirstOrDefaultAsync(sc => sc.Username == username);

                if (dbCart == null)
                {
                    scope.Complete();
                    return new List<CartItemModel>();
                }

                // Log để debug
                foreach (var item in dbCart.ShoppingCartItems)
                {
                    _logger.LogInformation($"Product {item.Product.ProductName} price: {item.Product.Price}");
                }

                var cartItems = dbCart.ShoppingCartItems
                    .Where(item => item.Product != null) // Thêm điều kiện này để lọc các item có product
                    .Select(item => new CartItemModel(item.Product)
                    {
                        Quantity = item.Quantity,
                        SizeId = item.Size_id ?? "Size01",
                        SizeDetail = item.Size?.SizeDetail ?? "S",
                        IceId = item.Ice_id ?? "Ice01",
                        IceDetail = item.Ice?.IceDetail ?? "50%",
                        SugarId = item.Sugar_id ?? "Sugar01",
                        SugarDetail = item.Sugar?.SugarDetail ?? "100%",
                        Price = item.Product.Price,
                        Discount = item.Product.Discount
                    }).ToList();

                scope.Complete();
                return cartItems;
            }
        }

        // Đồng bộ giỏ hàng từ session vào database
        private async Task<List<CartItemModel>> SyncSessionCartToDatabase(string username, List<CartItemModel> sessionCart)
        {
            try
            {
                // Xóa cart cũ nếu tồn tại
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
                var newCart = new ShoppingCart
                {
                    Cart_id = Guid.NewGuid().ToString().Substring(0, 10),
                    Username = username,
                    CreatedDate = DateTime.Now,
                    LastModifiedDate = DateTime.Now,
                    ShoppingCartItems = sessionCart.Select(sessionItem => new ShoppingCartItem
                    {
                        CartItem_id = Guid.NewGuid().ToString().Substring(0, 10),
                        Product_id = sessionItem.Product_id,
                        Quantity = sessionItem.Quantity,
                        Size_id = sessionItem.SizeId ?? "Size01",
                        Ice_id = sessionItem.IceId ?? "Ice01",
                        Sugar_id = sessionItem.SugarId ?? "Sugar01",
                        AddedDate = DateTime.Now
                    }).ToList()
                };

                _dataContext.ShoppingCart.Add(newCart);
                await _dataContext.SaveChangesAsync();

                return await GetCartFromDatabase(username);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in SyncSessionCartToDatabase: {ex.Message}");
                throw;
            }
        }
        private async Task<double> CalculateCartTotal(IEnumerable<ShoppingCartItem> items)
        {
            double total = 0;
            foreach (var item in items)
            {
                var price = await GetUpdatedPrice(item.Product_id, item.Size_id);
                total += price * item.Quantity;
            }
            return total;
        }

 
        public class AddToCartRequest
        {
            public string ProductId { get; set; }
            public int Quantity { get; set; }
            public string SizeId { get; set; }
            public string IceId { get; set; }
            public string SugarId { get; set; }
        }


        // Lớp để update yêu cầu
        public class UpdateCartRequest
        {
            public string productId { get; set; }
            public int quantity { get; set; }
        }


    }
}
