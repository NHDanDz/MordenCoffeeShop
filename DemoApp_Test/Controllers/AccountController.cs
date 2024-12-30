using DemoApp_Test.Models;
using DemoApp_Test.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DemoApp_Test.Services;
using DemoApp_Test.Enums;
using Microsoft.AspNetCore.Authorization;
using DemoApp_Test.Repository;

namespace DemoApp_Test.Controllers
{
    [Authorize] // Yêu cầu đăng nhập cho tất cả các action trong controller
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;
        private readonly DataContext _dataContext;
        private readonly IEmailService _emailService;

        public AccountController(
            DataContext context,
            ILogger<AccountController> logger,
            IEmailService emailService)
        {
            _dataContext = context;
            _logger = logger;
            _emailService = emailService;
        }

        // GET: /Account/Profile
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    return RedirectToAction("Index", "Login", new { area = "Login" });
                }

                var account = await _dataContext.Account
                    .Include(a => a.Client)
                    .FirstOrDefaultAsync(a => a.Username == username);

                if (account == null)
                {
                    _logger.LogWarning($"Account not found for username: {username}");
                    return NotFound();
                }

                // Tạo view model với null checking
                var profileViewModel = new ProfileViewModel
                {
                    Username = account.Username,
                    FullName = account.Client?.Name ?? string.Empty,
                    Email = account.Client?.Gmail ?? string.Empty,
                    PhoneNumber = account.Client?.Contact ?? string.Empty,
                    Address = account.Client?.Location ?? string.Empty,
                    RegistrationDate = account.RegistrationDate ?? DateTime.Now,
                    Role = account.Role,
                    LastActivity = account.LastActivity
                };

                return View(profileViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving profile");
                TempData["Error"] = "Có lỗi xảy ra khi tải thông tin cá nhân";
                return RedirectToAction("Error", "Home");
            }
        }
        // POST: /Account/UpdateProfile
        [HttpPost]
        public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View("Profile", model);
                }

                var username = User.Identity?.Name;
                var account = await _dataContext.Account
                    .Include(a => a.Client)
                    .FirstOrDefaultAsync(a => a.Username == username);

                if (account == null)
                {
                    return NotFound();
                }

                // Kiểm tra email có bị trùng không (trừ email hiện tại)
                var existingEmail = await _dataContext.Client
                    .AnyAsync(c => c.Gmail == model.Email && c.Client_id != account.Client_id);

                if (existingEmail)
                {
                    ModelState.AddModelError("Email", "Email đã được sử dụng bởi tài khoản khác");
                    return View("Profile", model);
                }

                // Cập nhật thông tin
                account.Client.Name = model.FullName;
                account.Client.Gmail = model.Email;
                account.Client.Contact = model.PhoneNumber;
                account.Client.Location = model.Address;
                account.LastActivity = DateTime.Now;

                await _dataContext.SaveChangesAsync();

                // Gửi email xác nhận cập nhật
                await _emailService.SendEmailAsync(
                    model.Email,
                    "Profile Update Confirmation",
                    $@"<h2>Profile Update Confirmation</h2>
                    <p>Dear {model.FullName},</p>
                    <p>Your profile has been successfully updated.</p>
                    <p>If you did not make this change, please contact us immediately.</p>"
                );

                TempData["Success"] = "Cập nhật thông tin thành công";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile");
                TempData["Error"] = "Đã xảy ra lỗi khi cập nhật thông tin";
                return View("Profile", model);
            }
        }

        // GET: /Account/ChangePassword
        public IActionResult ChangePassword()
        {
            return View();
        }

        // POST: /Account/ChangePassword
        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var username = User.Identity?.Name;
                var account = await _dataContext.Account
                    .Include(a => a.Client)
                    .FirstOrDefaultAsync(a => a.Username == username);

                if (account == null)
                {
                    return NotFound();
                }

                if (account.Password != model.CurrentPassword)
                {
                    ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không chính xác");
                    return View(model);
                }

                if (!IsPasswordValid(model.NewPassword))
                {
                    ModelState.AddModelError("NewPassword", "Mật khẩu mới phải có ít nhất 8 ký tự, chứa chữ hoa, chữ thường và số");
                    return View(model);
                }

                // Cập nhật mật khẩu
                account.Password = model.NewPassword;
                account.LastActivity = DateTime.Now;

                await _dataContext.SaveChangesAsync();

                // Gửi email thông báo
                await _emailService.SendEmailAsync(
                    account.Client.Gmail,
                    "Password Change Notification",
                    $@"<h2>Password Change Notification</h2>
                    <p>Dear {account.Client.Name},</p>
                    <p>Your password has been successfully changed.</p>
                    <p>If you did not make this change, please contact us immediately.</p>"
                );

                TempData["Success"] = "Đổi mật khẩu thành công";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                TempData["Error"] = "Đã xảy ra lỗi khi đổi mật khẩu";
                return View(model);
            }
        }

        // GET: /Account/OrderHistory
        public async Task<IActionResult> OrderHistory()
        {
            try
            {
                var username = User.Identity?.Name;
                var account = await _dataContext.Account
                    .Include(a => a.Client)
                    .FirstOrDefaultAsync(a => a.Username == username);

                if (account == null)
                {
                    return NotFound();
                }

                var orders = await _dataContext.Bill
                    .Include(b => b.Product_Bill)
                        .ThenInclude(pb => pb.Product)
                    .Include(b => b.Bill_Voucher)
                        .ThenInclude(bv => bv.Voucher)
                    .Where(b => b.Client_id == account.Client_id)
                    .OrderByDescending(b => b.Date)
                    .ToListAsync();

                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order history");
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: /Account/OrderDetail/{id}
        public async Task<IActionResult> OrderDetail(string id)
        {
            try
            {
                var username = User.Identity?.Name;
                var account = await _dataContext.Account
                    .FirstOrDefaultAsync(a => a.Username == username);

                var order = await _dataContext.Bill
                    .Include(b => b.Client)
                    .Include(b => b.Product_Bill)
                        .ThenInclude(pb => pb.Product)
                    .Include(b => b.Product_Bill)
                        .ThenInclude(pb => pb.Size)
                    .Include(b => b.Bill_Voucher)
                        .ThenInclude(bv => bv.Voucher)
                    .FirstOrDefaultAsync(b => b.Bill_id == id && b.Client_id == account.Client_id);

                if (order == null)
                {
                    return NotFound();
                }

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order detail");
                return RedirectToAction("Error", "Home");
            }
        }

        // POST: /Account/CancelOrder
        [HttpPost]
        public async Task<IActionResult> CancelOrder(string id)
        {
            try
            {
                var username = User.Identity?.Name;
                var account = await _dataContext.Account
                    .FirstOrDefaultAsync(a => a.Username == username);

                var order = await _dataContext.Bill
                    .Include(b => b.Client)
                    .FirstOrDefaultAsync(b => b.Bill_id == id && b.Client_id == account.Client_id);

                if (order == null)
                {
                    return NotFound();
                }

                if (order.Status)
                {
                    TempData["Error"] = "Không thể hủy đơn hàng đã được xử lý";
                    return RedirectToAction("OrderDetail", new { id = id });
                }

                order.DeleteStatus = true;
                await _dataContext.SaveChangesAsync();

                // Gửi email thông báo
                await _emailService.SendEmailAsync(
                    order.Client.Gmail,
                    "Order Cancellation Confirmation",
                    $@"<h2>Order Cancellation Confirmation</h2>
                    <p>Dear {order.Client.Name},</p>
                    <p>Your order {order.Bill_id} has been cancelled successfully.</p>
                    <p>If you did not request this cancellation, please contact us immediately.</p>"
                );

                TempData["Success"] = "Hủy đơn hàng thành công";
                return RedirectToAction("OrderHistory");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order");
                TempData["Error"] = "Đã xảy ra lỗi khi hủy đơn hàng";
                return RedirectToAction("OrderHistory");
            }
        }

        private bool IsPasswordValid(string password)
        {
            if (string.IsNullOrEmpty(password)) return false;
            if (password.Length < 8) return false;

            bool hasUppercase = password.Any(char.IsUpper);
            bool hasLowercase = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);

            return hasUppercase && hasLowercase && hasDigit;
        }
        [HttpPost]
        public async Task<IActionResult> CompleteOrder(string id)
        {
            try
            {
                var username = User.Identity?.Name;
                var account = await _dataContext.Account
                    .FirstOrDefaultAsync(a => a.Username == username);

                var order = await _dataContext.Bill
                    .FirstOrDefaultAsync(b => b.Bill_id == id && b.Client_id == account.Client_id);

                if (order == null)
                {
                    return NotFound();
                }

                // Chỉ cho phép chuyển trạng thái từ "Đang giao hàng" sang "Hoàn thành"
                if (order.Status && !order.PaymentStatus)  // Đang giao hàng
                {
                    order.PaymentStatus = true;  // Chuyển thành Hoàn thành
                    await _dataContext.SaveChangesAsync();

                    TempData["Success"] = "Đã xác nhận hoàn thành đơn hàng";
                }
                else
                {
                    TempData["Error"] = "Không thể thay đổi trạng thái đơn hàng này";
                }

                return RedirectToAction("OrderHistory");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing order");
                TempData["Error"] = "Đã xảy ra lỗi khi xác nhận hoàn thành đơn hàng";
                return RedirectToAction("OrderHistory");
            }
        }
        public async Task<IActionResult> ShowVouchers()
        {
            try
            {
                var now = DateTime.Now;
                var vouchers = await _dataContext.Voucher
                    .Where(v => v.Status == true  // Thay vì chỉ v.Status 
                        && v.ExpirationDate > now
                        && (!v.UsageLimit.HasValue || v.UsageCount < v.UsageLimit.Value))
                    .OrderBy(v => v.ExpirationDate)
                    .ToListAsync();

                return View(vouchers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vouchers");
                return RedirectToAction("Error", "Home");
            }
        }
    }

   
 
}