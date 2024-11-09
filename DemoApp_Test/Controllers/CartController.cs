using DemoApp_Test.Extensions;
using DemoApp_Test.Models.ViewModels;
using DemoApp_Test.Repository;
using Microsoft.AspNetCore.Mvc;

namespace DemoApp_Test.Controllers
{
    public class CartController : Controller
    {
        private readonly DataContext _dataContext;
        public CartController(DataContext _context)
        {
            _dataContext = _context;
        }
        public IActionResult Index()
        {
            List<CartItemModel> cartItems = HttpContext.Session.GetObjectFromJson<List<CartItemModel>>("cart") ?? new List<CartItemModel>();
            CartItemViewModel cartVM = new()
            {
                CartItems = cartItems,
                TongTien = cartItems.Sum(x => x.Quantity * x.Price)
            };
            return View();
        }
    }
}
