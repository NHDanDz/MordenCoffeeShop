using DemoApp_Test.Extensions;
using DemoApp_Test.Models;
using DemoApp_Test.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Transactions;

namespace DemoApp_Test.Repository.Components
{
    public class ShoppingCartViewComponent : ViewComponent
    {
        private readonly ICartService _cartService;
        private readonly DataContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ShoppingCartViewComponent> _logger;

        public ShoppingCartViewComponent(
            ICartService cartService,
            DataContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<ShoppingCartViewComponent> logger)
        {
            _cartService = cartService;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            try
            {
                List<CartItemModel> cart;
                var username = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
                var session = _httpContextAccessor.HttpContext?.Session;

                if (session == null)
                {
                    _logger.LogWarning("Session is null in ShoppingCartViewComponent");
                    return View(new CartItemViewModel
                    {
                        CartItems = new List<CartItemModel>(),
                        TongTien = 0
                    });
                }

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (!string.IsNullOrEmpty(username))
                    {
                        // Lấy giỏ hàng từ database cho người dùng đã đăng nhập
                        cart = await GetCartFromDatabaseWithTracking(username);

                        // Kiểm tra nếu giỏ hàng database trống và có session cart
                        var sessionCart = session.GetObjectFromJson<List<CartItemModel>>("cart");
                        if (sessionCart != null && sessionCart.Any())
                        {
                            // Đồng bộ giỏ hàng từ session vào database
                            cart = await SyncSessionCartToDatabase(username, sessionCart);
                            // Xóa giỏ hàng trong session sau khi đồng bộ
                            session.Remove("cart");
                        }
                    }
                    else
                    {
                        // Người dùng chưa đăng nhập - lấy giỏ hàng từ session
                        cart = session.GetObjectFromJson<List<CartItemModel>>("cart") ?? new List<CartItemModel>();
                        await UpdateCartItemsInfo(cart); // Cập nhật thông tin sản phẩm
                    }

                    scope.Complete();
                }

                // Tính toán tổng tiền và tạo ViewModel
                var cartViewModel = new CartItemViewModel
                {
                    CartItems = cart,
                    TongTien = CalculateTotal(cart)
                };

                // Lưu số lượng sản phẩm vào ViewData để hiển thị
                ViewData["CartItemCount"] = cart.Sum(x => x.Quantity);

                return View(cartViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in ShoppingCartViewComponent.InvokeAsync: {ex.Message}");
                return View(new CartItemViewModel
                {
                    CartItems = new List<CartItemModel>(),
                    TongTien = 0
                });
            }
        }

        private async Task<List<CartItemModel>> GetCartFromDatabaseWithTracking(string username)
        {
            var dbCart = await _context.ShoppingCart
                .AsNoTracking()
                .Include(sc => sc.ShoppingCartItems)
                    .ThenInclude(sci => sci.Product)
                        .ThenInclude(p => p.Brand)
                .Include(sc => sc.ShoppingCartItems)
                    .ThenInclude(sci => sci.Product)
                        .ThenInclude(p => p.TypeCoffee)
                .Include(sc => sc.ShoppingCartItems)
                    .ThenInclude(sci => sci.Product)
                        .ThenInclude(p => p.Product_Size)  // Thêm dòng này
                .Include(sc => sc.ShoppingCartItems)
                    .ThenInclude(sci => sci.Ice)
                .Include(sc => sc.ShoppingCartItems)
                    .ThenInclude(sci => sci.Sugar)
                .Include(sc => sc.ShoppingCartItems)
                    .ThenInclude(sci => sci.Size)
                .FirstOrDefaultAsync(sc => sc.Username == username);

            // Thêm logging để debug
            _logger.LogInformation($"Found cart for user {username}: {dbCart != null}");
            if (dbCart?.ShoppingCartItems != null)
            {
                foreach (var item in dbCart.ShoppingCartItems)
                {
                    _logger.LogInformation($"Product: {item.Product?.ProductName}, Price: {item.Product?.Price}, Size: {item.Size_id}");
                }
            }

            if (dbCart == null)
            {
                return new List<CartItemModel>();
            }

            return dbCart.ShoppingCartItems
                .Select(item => new CartItemModel(item.Product)
                {
                    Quantity = item.Quantity,
                    SizeId = item.Size_id ?? "Size01",
                    SizeDetail = item.Size?.SizeDetail ?? "S",
                    IceId = item.Ice_id ?? "Ice01",
                    IceDetail = item.Ice?.IceDetail ?? "50%",
                    SugarId = item.Sugar_id ?? "Sugar01",
                    SugarDetail = item.Sugar?.SugarDetail ?? "100%",
                    Price = GetProductPrice(item),
                    Discount = item.Product?.Discount ?? 0
                })
                .ToList();
        }
        private async Task<List<CartItemModel>> SyncSessionCartToDatabase(string username, List<CartItemModel> sessionCart)
        {
            var dbCart = await GetOrCreateCart(username);

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

                _context.ShoppingCartItem.Add(cartItem);
            }

            dbCart.LastModifiedDate = DateTime.Now;
            await _context.SaveChangesAsync();

            return await GetCartFromDatabaseWithTracking(username);
        }

        private async Task<ShoppingCart> GetOrCreateCart(string username)
        {
            var cart = await _context.ShoppingCart
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
                _context.ShoppingCart.Add(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }

        private double GetProductPrice(ShoppingCartItem item)
        {
            // Nếu product null thì return 0
            if (item.Product == null) return 0;

            // Lấy giá mặc định của sản phẩm
            double basePrice = item.Product.Price;
 

            return basePrice;
        }

        private async Task UpdateCartItemsInfo(List<CartItemModel> cart)
        {
            var productIds = cart.Select(x => x.Product_id).Distinct().ToList();
            var products = await _context.Product
                .AsNoTracking()
                .Include(p => p.Brand)
                .Include(p => p.TypeCoffee)
                .Include(p => p.Product_Size)
                .Where(p => productIds.Contains(p.Product_id))
                .ToDictionaryAsync(p => p.Product_id);

            foreach (var item in cart)
            {
                if (products.TryGetValue(item.Product_id, out var product))
                {
                    item.ProductName = product.ProductName;
                    item.Brand = product.Brand?.BrandName ?? "default";
                    item.TypeCoffee = product.TypeCoffee?.TypeName ?? "default";
                    item.Image = product.Image ?? "default.jpg";
                    item.Discount = product.Discount;  // Cập nhật Discount từ Product

                    if (!string.IsNullOrEmpty(item.SizeId))
                    {
                        var sizePrice = product.Product_Size
                            .FirstOrDefault(ps => ps.Size_id == item.SizeId)?.Price;

                        item.Price = sizePrice ?? product.Price;
                    }
                    else
                    {
                        item.Price = product.Price;
                    }
                }
            }
        }

        private double CalculateTotal(List<CartItemModel> cart)
        {
            return cart.Sum(item =>
            {
                double priceAfterDiscount = item.Price * (1 - (item.Discount / 100.0));
                return priceAfterDiscount * item.Quantity;
            });
        }
    }
}