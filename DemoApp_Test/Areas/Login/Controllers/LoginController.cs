using DemoApp_Test.Repository;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DemoApp_Test.Areas.Login.Controllers
{
    [Area("Login")]
    public class LoginController : Controller
    {
        private readonly ILogger<LoginController> _logger;
        private readonly DataContext _context;

        public LoginController(ILogger<LoginController> logger, DataContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Access Login Page");

            // Kiểm tra session username
            var username = HttpContext.Session.GetString("username");
            if (!string.IsNullOrEmpty(username))
            {
                // Kiểm tra role từ database
                var account = await _context.Account
                    .FirstOrDefaultAsync(a => a.Username == username);

                if (account != null)
                {
                    // Chuyển hướng dựa vào role
                    if (account.Role == 0)
                    {
                        return RedirectToAction("Index", "Admin", new { area = "Admin" });
                    }
                    else
                    {
                        _logger.LogInformation("User redirecting to Home/Index");
                        return RedirectToAction("Index", "Home", new { area = "" });
                    }
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

                // Thay đổi phần này để check from database
                var account = await _context.Account
                    .Where(a => a.Username == username && a.Password == password)
                    .FirstOrDefaultAsync();

                if (account != null)
                {
                    _logger.LogInformation("Login successful");
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, username),
                        new Claim(ClaimTypes.Role, account.Role == 0 ? "Admin" : "User")
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

                    if (account.Role == 0)
                    {
                        return RedirectToAction("Index", "Admin", new { area = "Admin" });
                    }
                    else
                    {
                        _logger.LogInformation("User redirecting to Home/Index");
                        return RedirectToAction("Index", "Home", new { area = "" });
                    }
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
            // Xóa session
            HttpContext.Session.Clear();

            // Đăng xuất authentication
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Chuyển về trang login
            return RedirectToAction("Index", "Login", new { area = "Login" });
        }
    }
}