using DemoApp_Test.Repository;
using Microsoft.AspNetCore.Mvc;

namespace DemoApp_Test.Components
{
    public class AdminBarViewComponent : ViewComponent
    {
        private readonly DataContext context;
        public AdminBarViewComponent(DataContext _context)
        {
            context = _context;
        }
        public IViewComponentResult Invoke()
        {
            try
            {
                // Chỉ đếm số lượng đơn hàng chưa xử lý
                ViewBag.PendingCount = context.Bill.Count(b => !b.Status);

                // Chỉ đếm số lượng đơn hàng đang giao
                ViewBag.DeliveringCount = context.Shipping.Count();

                // Chỉ đếm số lượng đơn hàng đã hoàn thành
                ViewBag.CompletedCount = context.Bill.Count(b => b.Status);
                ViewBag.ProductCount = context.Product.Count();
                var check = context.Product.Count();
                // Đếm số voucher còn hạn
                var currentDate = DateTime.Now;
                //var activeVouchers = context.Voucher
                //    .Where(v => !string.IsNullOrEmpty(v.ExpirationDate))
                //    .AsEnumerable()
                //    .Where(v => DateTime.TryParse(v.ExpirationDate, out DateTime expDate) &&
                //           expDate > currentDate)
                //    .ToList();
                //ViewBag.VoucherCount = activeVouchers.Count;

                // Username
                ViewBag.Username = HttpContext.Session.GetString("username");

                return View();
            }
            catch (Exception ex)
            {
                ViewBag.ProductCount = 0;
                ViewBag.PendingCount = 0;
                ViewBag.DeliveringCount = 0;
                ViewBag.CompletedCount = 0;
                ViewBag.VoucherCount = 0;
                ViewBag.Username = HttpContext.Session.GetString("username");
                return View();
            }
        }
    }
}