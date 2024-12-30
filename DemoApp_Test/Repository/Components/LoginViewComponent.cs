using Microsoft.AspNetCore.Mvc;
using DemoApp_Test.Repository;
using DemoApp_Test.Models;

namespace DemoApp_Test.ViewComponents
{
    public class LoginViewComponent : ViewComponent
    {
        private readonly DataContext _context;

        public LoginViewComponent(DataContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var username = HttpContext.Session.GetString("username");
            if (!string.IsNullOrEmpty(username))
            {
                ViewBag.Username = username;
            }
            return View();
        }
    }
}
