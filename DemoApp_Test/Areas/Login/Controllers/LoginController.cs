using DemoApp_Test.Repository;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DemoApp_Test.Enums;
using DemoApp_Test.Services;
using System.Security.Cryptography;
using DemoApp_Test.Areas.Login.Models;
using DemoApp_Test.Models;

namespace DemoApp_Test.Areas.Login.Controllers
{
    [Area("Login")]
    public class LoginController : Controller
    {
        private readonly ILogger<LoginController> _logger;
        private readonly DataContext _context;
        private readonly IEmailService _emailService;

        public LoginController(
            ILogger<LoginController> logger,
            DataContext context,
            IEmailService emailService)
        {
            _logger = logger;
            _context = context;
            _emailService = emailService;
        }


        public async Task<IActionResult> Index(string returnUrl = null)
        {
            _logger.LogInformation("Access Login Page"); 

            // Get visitor stats
 

            // Lưu returnUrl vào TempData để sử dụng sau này
            if (!string.IsNullOrEmpty(returnUrl))
            {
                TempData["ReturnUrl"] = returnUrl;
            }

            var username = HttpContext.Session.GetString("username");
            if (!string.IsNullOrEmpty(username))
            {
                var account = await _context.Account
                    .FirstOrDefaultAsync(a => a.Username == username);

                if (account != null)
                {
                    // Nếu có returnUrl, redirect về đó
                    if (!string.IsNullOrEmpty(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    // Increment visitor count
                    VisitorCounter.IncrementVisitorCount(HttpContext);
                    // Nếu không có returnUrl, redirect theo role
                    return account.Role switch
                    {
                        UserRole.Admin => RedirectToAction("Index", "Admin", new { area = "Admin" }),
                        UserRole.Staff => RedirectToAction("Index", "Admin", new { area = "Admin" }),
                        UserRole.Customer => RedirectToAction("Index", "Home", new { area = "" }),
                        _ => RedirectToAction("Index", "Home", new { area = "" })
                    };
                }
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            try
            {
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    _logger.LogWarning("Username or password is empty");
                    ViewBag.Error = "Username and password are required";
                    return View("Index");
                }

                var account = await _context.Account
                    .Where(a => a.Username == username && a.Password == password)
                    .FirstOrDefaultAsync();

                if (account != null)
                {
                    _logger.LogInformation("Login successful");

                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, account.Role.ToString())
            };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    HttpContext.Session.SetString("username", username);

                    // Kiểm tra xem có returnUrl không
                    string returnUrl = TempData["ReturnUrl"]?.ToString();
                    if (!string.IsNullOrEmpty(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    // Nếu không có returnUrl, redirect theo role
                    return account.Role switch
                    {
                        UserRole.Admin => RedirectToAction("Index", "Admin", new { area = "Admin" }),
                        UserRole.Staff => RedirectToAction("Index", "Admin", new { area = "Admin" }),
                        UserRole.Customer => RedirectToAction("Index", "Home", new { area = "" }),
                        _ => RedirectToAction("Index", "Home", new { area = "" })
                    };
                }

                _logger.LogWarning($"Login failed for username: {username}");
                ViewBag.Error = "Invalid username or password";
                ViewBag.LastUsername = username;
                return View("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login process");
                ViewBag.Error = "An error occurred during login";
                return View("Index");
            }
        }
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            VisitorCounter.RemoveOnlineUser(HttpContext);

            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Login", new { area = "Login" });
        }


        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string username)
        {
            try
            {
                var account = await _context.Account
                    .Include(a => a.Client)
                    .FirstOrDefaultAsync(a => a.Username == username);

                if (account == null)
                {
                    _logger.LogWarning($"Forgot password attempt for non-existent username: {username}");
                    ViewBag.Error = "Username not found";
                    return View();
                }

                if (account.Client == null || string.IsNullOrEmpty(account.Client.Gmail))
                {
                    _logger.LogWarning($"No email address found for username: {username}");
                    ViewBag.Error = "No email address associated with this account";
                    return View();
                }

                // Generate reset token
                var token = GenerateResetToken();
                account.ResetPasswordToken = token;
                account.ResetPasswordExpiry = DateTime.UtcNow.AddHours(24);

                await _context.SaveChangesAsync();

                // Create reset password link
                var resetLink = Url.Action("ResetPassword", "Login",
                    new { area = "Login", token = token },
                    Request.Scheme);

                // Send email using your email service
                await _emailService.SendResetPasswordEmailAsync(account.Client.Gmail, resetLink);

                ViewBag.Success = "Password reset instructions have been sent to your email";
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot password process");
                ViewBag.Error = "An error occurred while processing your request";
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token)
        {
            var account = await _context.Account
                .FirstOrDefaultAsync(a => a.ResetPasswordToken == token);

            if (account == null ||
                !account.ResetPasswordExpiry.HasValue ||
                account.ResetPasswordExpiry.Value < DateTime.UtcNow)
            {
                ViewBag.Error = "Invalid or expired reset token";
                return RedirectToAction("Index");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string token, string newPassword)
        {
            try
            {
                var account = await _context.Account
                    .FirstOrDefaultAsync(a => a.ResetPasswordToken == token);

                if (account == null ||
                    !account.ResetPasswordExpiry.HasValue ||
                    account.ResetPasswordExpiry.Value < DateTime.UtcNow)
                {
                    ViewBag.Error = "Invalid or expired reset token";
                    return View();
                }

                // Update password
                account.Password = newPassword; // Consider adding password hashing here
                account.ResetPasswordToken = null;
                account.ResetPasswordExpiry = null;

                await _context.SaveChangesAsync();

                ViewBag.Success = "Your password has been successfully reset";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset");
                ViewBag.Error = "An error occurred while resetting your password";
                return View();
            }
        }

        private string GenerateResetToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        [HttpPost]
        public async Task<IActionResult> TestEmail()
        {
            try
            {
                await _emailService.SendEmailAsync(
                    "nhdandz@gmail.com", // email nhận test
                    "Test Email",
                    "<h1>This is a test email</h1><p>If you receive this, the email service is working correctly.</p>"
                );
                return Json(new { success = true, message = "Test email sent successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        public IActionResult Register()
        {
            return View();
        }
        private async Task<string> GenerateClientId()
        {
            // Lấy Client_id cuối cùng từ database
            var lastClient = await _context.Client
                .OrderByDescending(c => c.Client_id)
                .FirstOrDefaultAsync();

            if (lastClient == null)
            {
                // Nếu chưa có client nào, bắt đầu từ KH001
                return "KH001";
            }

            // Lấy số từ Client_id cuối cùng
            string numberPart = lastClient.Client_id.Substring(2); // Bỏ "KH"
            int lastNumber = int.Parse(numberPart);

            // Tăng số lên 1 và format lại thành chuỗi 3 chữ số
            string newNumber = (lastNumber + 1).ToString("D3");

            return $"KH{newNumber}";
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // Check username exists
                var existingAccount = await _context.Account
                    .FirstOrDefaultAsync(a => a.Username == model.Username);
                if (existingAccount != null)
                {
                    ModelState.AddModelError("Username", "Username already exists");
                    return View(model);
                }

                // Find existing client by Gmail
                var existingClient = await _context.Client
                    .FirstOrDefaultAsync(c => c.Gmail == model.Gmail);

                string clientId;
                Client client;

                if (existingClient != null)
                {
                    // Use existing client
                    clientId = existingClient.Client_id;
                    client = existingClient;
                }
                else
                {
                    // Create new client
                    clientId = await GenerateClientId();
                    client = new Client
                    {
                        Client_id = clientId,
                        Name = model.Name,
                        Gmail = model.Gmail,
                        Contact = model.Contact,
                        Location = model.Location
                    };
                    _context.Client.Add(client);
                }

                // Create new account
                var account = new Account
                {
                    Username = model.Username,
                    Password = model.Password,
                    RegistrationDate = DateTime.UtcNow,
                    Status = true,
                    Role = UserRole.Customer,
                    Client_id = clientId,
                    Client = client
                };

                _context.Account.Add(account);
                await _context.SaveChangesAsync();


                // Gửi email chào mừng
                await _emailService.SendEmailAsync(
                    model.Gmail,
                    "Welcome to NHDanDz Coffee Shop",
                    $@"<h2>Welcome to NHDanDz Coffee Shop!</h2>
               <p>Dear {model.Name},</p>
               <p>Thank you for registering with us. Your account has been successfully created.</p>
               <p>Your Client ID: {clientId}</p>
               <p>Username: {model.Username}</p>
               <p>You can now log in and start ordering your favorite coffee!</p>
               <br>
               <p>Best regards,</p>
               <p>NHDanDz Coffee Shop Team</p>"
                );

                TempData["SuccessMessage"] = "Registration successful! Please login with your credentials.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                ModelState.AddModelError("", "An error occurred during registration. Please try again.");
                return View(model);
            }
        }
    }
}