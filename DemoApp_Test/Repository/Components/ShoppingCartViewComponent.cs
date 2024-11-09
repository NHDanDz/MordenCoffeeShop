using DemoApp_Test.Extensions;
using DemoApp_Test.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DemoApp_Test.Repository.Components
{
    public class ShoppingCartViewComponent : ViewComponent
    {
        private readonly ICartService _cartService;
        private readonly DataContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ShoppingCartViewComponent(
            ICartService cartService,
            DataContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _cartService = cartService;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Lấy giỏ hàng từ session
            var cart = _httpContextAccessor.HttpContext.Session.GetObjectFromJson<List<CartItemModel>>("cart") ?? new List<CartItemModel>();

            // Cập nhật thông tin sản phẩm từ database
            foreach (var item in cart)
            {
                var product = await _context.Product
                    .Include(p => p.Brand)
                    .Include(p => p.TypeCoffee)
                    .FirstOrDefaultAsync(p => p.Product_id == item.Product_id);

                if (product != null)
                {
                    // Cập nhật thông tin mới nhất từ database
                    item.ProductName = product.ProductName;
                    item.Price = product.Price;
                    item.Brand = product.Brand?.BrandName ?? "default";
                    item.TypeCoffee = product.TypeCoffee?.TypeName ?? "default";
                    item.Image = product.Image ?? "default.jpg";
                }
            }

            // Tạo view model
            var cartViewModel = new CartItemViewModel
            {
                CartItems = cart,
                TongTien = cart.Sum(item => item.Price * item.Quantity)
            };

            // Lưu số lượng item vào ViewData
            ViewData["CartItemCount"] = cart.Sum(x => x.Quantity);

            return View(cartViewModel);
        }
    }
}