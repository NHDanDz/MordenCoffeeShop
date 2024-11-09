using Microsoft.AspNetCore.Mvc;

public class LoginStatusViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        var isAuthenticated = User.Identity.IsAuthenticated;
        var userName = isAuthenticated ? User.Identity.Name : string.Empty;

        ViewBag.ConfirmLogin = isAuthenticated;
        ViewBag.UserName = userName;

        return View();
    }
}
