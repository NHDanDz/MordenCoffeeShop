using DemoApp_Test.Repository;
using Microsoft.AspNetCore.Mvc;

namespace DemoApp_Test.Controllers
{
    [Area("Admin")]
    public class AdminController : Controller
    {
        private readonly DataContext context;

        public AdminController(DataContext context)
        {
            this.context = context;
        }

        public IActionResult Index()
        {
            var username = HttpContext.Session.GetString("username");
            if (username is not null)
            {
                // Kiểm tra Role của account
                var account = context.Account.FirstOrDefault(a => a.Username == username);
                if (account == null || account.Role != 0) // Nếu không phải admin (Role = 0)
                {
                    return RedirectToAction("Index", "Login", new { area = "Login" });
                }


                // Đơn hàng chờ xử lý (CXL)
                var cxl = context.Bill.Where(b => !b.Status).Count();
                ViewBag.CXL = cxl;

                // Đơn hàng đã xử lý (DXL)
                var dxl = context.Bill.Where(b => b.Status).Count();
                ViewBag.DXL = dxl;

                // Tổng số đơn đã giao (DG)
                var dg = context.Shipping.Count();
                ViewBag.DG = dg;

                // Tổng số yêu cầu
                var totalRequests = cxl + dxl + dg;
                ViewBag.TB = totalRequests;

                // Số lượng voucher
                var Voucher = context.Voucher.Count();
                ViewBag.S = Voucher;

                // Số lượng khách hàng
                var customers = context.Client.Count();
                ViewBag.SoKH = customers;

                // Sản phẩm đã áp dụng voucher
                var ProductWithDiscount = context.Product.Where(p => p.Discount > 0).Count();
                ViewBag.SP = ProductWithDiscount;

                // Tổng số sản phẩm
                var totalProduct = context.Product.Count();
                ViewBag.SP_ALL = totalProduct;

                // Mục tiêu đơn hàng đã hoàn thành
                var completedOrders = context.Bill.Where(b => b.Status).Count();
                ViewBag.MT = completedOrders;

                // Doanh thu theo tháng
                var currentYear = DateTime.Now.Year;

                // Doanh thu từng tháng
                double t1 = context.Bill
                    .Where(b => b.Status && b.Date.Month == 1 && b.Date.Year == currentYear)
                    .Sum(b => b.Total);

                double t2 = context.Bill
                    .Where(b => b.Status && b.Date.Month == 2 && b.Date.Year == currentYear)
                    .Sum(b => b.Total);

                double t3 = context.Bill
                    .Where(b => b.Status && b.Date.Month == 3 && b.Date.Year == currentYear)
                    .Sum(b => b.Total);

                double t4 = context.Bill
                    .Where(b => b.Status && b.Date.Month == 4 && b.Date.Year == currentYear)
                    .Sum(b => b.Total);

                double t5 = context.Bill
                    .Where(b => b.Status && b.Date.Month == 5 && b.Date.Year == currentYear)
                    .Sum(b => b.Total);

                double t6 = context.Bill
                    .Where(b => b.Status && b.Date.Month == 6 && b.Date.Year == currentYear)
                    .Sum(b => b.Total);

                double t7 = context.Bill
                    .Where(b => b.Status && b.Date.Month == 7 && b.Date.Year == currentYear)
                    .Sum(b => b.Total);

                double t8 = context.Bill
                    .Where(b => b.Status && b.Date.Month == 8 && b.Date.Year == currentYear)
                    .Sum(b => b.Total);

                double t9 = context.Bill
                    .Where(b => b.Status && b.Date.Month == 9 && b.Date.Year == currentYear)
                    .Sum(b => b.Total);

                double t10 = context.Bill
                    .Where(b => b.Status && b.Date.Month == 10 && b.Date.Year == currentYear)
                    .Sum(b => b.Total);

                double t11 = context.Bill
                    .Where(b => b.Status && b.Date.Month == 11 && b.Date.Year == currentYear)
                    .Sum(b => b.Total);

                double t12 = context.Bill
                    .Where(b => b.Status && b.Date.Month == 12 && b.Date.Year == currentYear)
                    .Sum(b => b.Total);

                ViewBag.January = t1;
                ViewBag.February = t2;
                ViewBag.March = t3;
                ViewBag.April = t4;
                ViewBag.May = t5;
                ViewBag.June = t6;
                ViewBag.July = t7;
                ViewBag.August = t8;
                ViewBag.September = t9;
                ViewBag.October = t10;
                ViewBag.November = t11;
                ViewBag.December = t12;

                // Tổng doanh thu
                double totalRevenue = t1 + t2 + t3 + t4 + t5 + t6 + t7 + t8 + t9 + t10 + t11 + t12;
                ViewBag.TongDoanhThu = totalRevenue;

                // Doanh thu trung bình mỗi tháng
                ViewBag.DoanhThuTB = Math.Round((decimal)(totalRevenue / 12), 1);

                // Thống kê hiện tại và tháng trước
                var currentMonth = DateTime.Now.Month;
                var previousMonth = currentMonth == 1 ? 12 : currentMonth - 1;

                double currentMonthRevenue = context.Bill
                    .Where(b => b.Status &&
                           b.Date.Month == currentMonth &&
                           b.Date.Year == currentYear)
                    .Sum(b => b.Total);

                double previousMonthRevenue = context.Bill
                    .Where(b => b.Status &&
                           b.Date.Month == previousMonth &&
                           b.Date.Year == (previousMonth == 12 ? currentYear - 1 : currentYear))
                    .Sum(b => b.Total);

                ViewBag.HienTai = currentMonthRevenue;
                ViewBag.ThangTruoc = previousMonthRevenue;
                ViewBag.TongDoanhThuhs = currentMonthRevenue + previousMonthRevenue;

                // Số đơn tháng này
                var currentMonthOrders = context.Bill
                    .Where(b => b.Status &&
                           b.Date.Month == currentMonth &&
                           b.Date.Year == currentYear)
                    .Count();
                ViewBag.DaBan = currentMonthOrders;

                // Username cho header
                var username1 = HttpContext.Session.GetString("username");
                ViewBag.Username = username1;

                return View();
            }

            return RedirectToAction("Index", "Login", new { area = "Login" });
        }
    }
}