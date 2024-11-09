using DemoApp_Test.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;

namespace DemoApp_Test.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BillController : Controller
    {
        private readonly DataContext _context;

        public BillController(DataContext context)
        {
            _context = context;
        }

        public IActionResult Index(int page = 1)
        {
            // Kiểm tra đăng nhập
            var username = HttpContext.Session.GetString("username");
            if (username is null)
            {
                return RedirectToAction("Index", "Login", new { area = "Login" });
            }

            // Kiểm tra quyền admin
            var account = _context.Account.FirstOrDefault(a => a.Username == username);
            if (account == null || account.Role != 0)
            {
                return RedirectToAction("Index", "Login", new { area = "Login" });
            }

            // Đảm bảo page không nhỏ hơn 1
            page = page < 1 ? 1 : page;
            int pageSize = 4;

            // Lấy danh sách hóa đơn chưa xử lý
            var unprocessedBills = _context.Bill
                .Include(b => b.Client)
                .Where(b => !b.Status)
                .OrderByDescending(b => b.Date)
                .ToPagedList(page, pageSize);

            // Thống kê
            // Tổng số đơn chưa xử lý
            ViewBag.TotalUnprocessed = _context.Bill.Count(b => !b.Status);

            // Tổng số đơn đã xử lý
            ViewBag.TotalProcessed = _context.Bill.Count(b => b.Status);

            // Tổng số đơn hàng
            ViewBag.TotalBills = _context.Bill.Count();

            // Thống kê theo tháng hiện tại
            var currentDate = DateTime.Now;
            ViewBag.CurrentMonthBills = _context.Bill
                .Count(b => b.Date.Month == currentDate.Month &&
                          b.Date.Year == currentDate.Year);

            // Doanh thu tháng hiện tại
            ViewBag.CurrentMonthRevenue = _context.Bill
                .Where(b => b.Status &&
                          b.Date.Month == currentDate.Month &&
                          b.Date.Year == currentDate.Year)
                .Sum(b => b.Total);

            ViewBag.Username = username;

            return View(unprocessedBills);
        }
        [HttpPost]
        public IActionResult CompleteBill(string id)
        {
            var username = HttpContext.Session.GetString("username");
            if (username is null)
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            var bill = _context.Bill.FirstOrDefault(b => b.Bill_id == id);
            if (bill == null)
            {
                return Json(new { success = false, message = "Đơn hàng không tồn tại" });
            }

            try
            {
                bill.PaymentStatus = true;
                bill.ProcessDate = DateTime.Now;
                _context.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Hoàn thành đơn hàng thành công"
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Lỗi khi hoàn thành đơn hàng" });
            }
        }
        // Hiển thị đơn hàng đã xử lý
        public IActionResult ProcessedBills(int page = 1)
        {
            var username = HttpContext.Session.GetString("username");
            if (username is null)
            {
                return RedirectToAction("Index", "Login", new { area = "Login" });
            }

            var account = _context.Account.FirstOrDefault(a => a.Username == username);
            if (account == null || account.Role != 0)
            {
                return RedirectToAction("Index", "Login", new { area = "Login" });
            }

            page = page < 1 ? 1 : page;
            int pageSize = 4;

            // Lấy danh sách đơn đã xử lý
            var processedBills = _context.Bill
                .Include(b => b.Client)
                .Where(b => b.Status)
                .OrderByDescending(b => b.ProcessDate ?? b.Date)
                .ToPagedList(page, pageSize);

            var currentDate = DateTime.Now;

            // Thống kê cho đơn đã xử lý
            ViewBag.TotalProcessed = _context.Bill.Count(b => b.Status);
            ViewBag.CurrentMonthProcessed = _context.Bill
                .Count(b => b.Status &&
                           b.Date.Month == currentDate.Month &&
                           b.Date.Year == currentDate.Year);
            ViewBag.CurrentMonthRevenue = _context.Bill
                .Where(b => b.Status &&
                           b.Date.Month == currentDate.Month &&
                           b.Date.Year == currentDate.Year)
                .Sum(b => b.Total);

            ViewBag.Username = username;
            return View(processedBills);
        }



        // Action cập nhật trạng thái đơn hàng
        [HttpPost]
        public IActionResult UpdateStatus(string id)
        {
            var username = HttpContext.Session.GetString("username");
            if (username is null)
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            var bill = _context.Bill.FirstOrDefault(b => b.Bill_id == id);
            if (bill == null)
            {
                return Json(new { success = false, message = "Đơn hàng không tồn tại" });
            }

            try
            {
                // Đánh dấu đơn hàng đã xử lý
                bill.Status = true;
                _context.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Cập nhật trạng thái thành công",
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Lỗi khi cập nhật trạng thái" });
            }
        }
        public IActionResult CancelBill(string id)
        {
            var bill = _context.Bill.FirstOrDefault(b => b.Bill_id == id);
            if (bill == null)
            {
                return RedirectToAction(nameof(Index), new { thongbao = "Không tìm thấy hóa đơn" });
            }

            try
            {
                bill.Status = false; // Đánh dấu là đã hủy
                _context.SaveChanges();
                return RedirectToAction(nameof(Index), new { thongbao = "Hủy đơn hàng thành công" });
            }
            catch
            {
                return RedirectToAction(nameof(Index), new { thongbao = "Lỗi khi hủy đơn hàng" });
            }
        }

        public IActionResult BillDetails(string id)
        {
            var bill = _context.Bill
                .Include(b => b.Client)
                .FirstOrDefault(b => b.Bill_id == id);

            if (bill == null)
            {
                return RedirectToAction(nameof(Index), new { thongbao = "Không tìm thấy hóa đơn" });
            }

            return View(bill);
        }

        [HttpPost]
        public IActionResult ProcessBill(string id)
        {
            var username = HttpContext.Session.GetString("username");
            if (username is null)
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            var bill = _context.Bill.FirstOrDefault(b => b.Bill_id == id);
            if (bill == null)
            {
                return Json(new { success = false, message = "Đơn hàng không tồn tại" });
            }

            try
            {
                bill.Status = true; // Đánh dấu là đã xử lý
                _context.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Xử lý đơn hàng thành công"
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Lỗi khi xử lý đơn hàng" });
            }
        }

        // Action xóa đơn hàng
        [HttpPost]
        public IActionResult DeleteBill(string id)
        {
            var username = HttpContext.Session.GetString("username");
            if (username is null)
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            var bill = _context.Bill.FirstOrDefault(b => b.Bill_id == id);
            if (bill == null)
            {
                return Json(new { success = false, message = "Đơn hàng không tồn tại" });
            }

            try
            {
                _context.Bill.Remove(bill);
                _context.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Xóa đơn hàng thành công"
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Lỗi khi xóa đơn hàng" });
            }
        }
    }
}