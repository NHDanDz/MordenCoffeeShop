using DemoApp_Test.Extensions;
using Microsoft.AspNetCore.Mvc;

public class SessionViewerController : Controller
{
    public IActionResult Index()
    {
        var sessionItems = new Dictionary<string, object>();

        // Lấy giỏ hàng từ session
        var cart = HttpContext.Session.GetObjectFromJson<List<CartItemModel>>("cart");
        if (cart != null)
        {
            sessionItems.Add("cart", new
            {
                ItemCount = cart.Count,
                TotalQuantity = cart.Sum(x => x.Quantity),
                Items = cart.Select(x => new
                {
                    x.Product_id,
                    x.ProductName,
                    x.Quantity,
                    x.Price,
                    Subtotal = x.Price * x.Quantity
                })
            });
        }

        foreach (var key in HttpContext.Session.Keys)
        {
            if (key != "cart")
            {
                var value = HttpContext.Session.GetString(key);
                if (!string.IsNullOrEmpty(value))
                {
                    sessionItems.Add(key, value);
                }
            }
        }

        return View(sessionItems);
    }

    [HttpPost]
    public IActionResult ClearSession(string key = null)
    {
        if (string.IsNullOrEmpty(key))
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        HttpContext.Session.Remove(key);
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult ClearCart()
    {
        HttpContext.Session.Remove("cart");
        return RedirectToAction("Index");
    }
}