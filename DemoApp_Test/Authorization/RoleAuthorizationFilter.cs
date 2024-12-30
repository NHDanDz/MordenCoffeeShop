using DemoApp_Test.Authorization;
using DemoApp_Test.Repository;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

public class RoleAuthorizationFilter : IAuthorizationFilter
{
    private readonly DataContext _context;

    public RoleAuthorizationFilter(DataContext context)
    {
        _context = context;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var authorizeAttribute = context.ActionDescriptor.EndpointMetadata
            .OfType<AuthorizeRolesAttribute>()
            .FirstOrDefault();

        if (authorizeAttribute == null)
            return;

        var username = context.HttpContext.Session.GetString("username");
        if (string.IsNullOrEmpty(username))
        {
            context.Result = new RedirectToActionResult("Index", "Login", new { area = "Login" });
            return;
        }

        var account = _context.Account.FirstOrDefault(a => a.Username == username);
        if (account == null || !authorizeAttribute.Roles.Contains(account.Role))
        {
            // Thay đổi từ ForbidResult sang ViewResult
            context.Result = new ViewResult
            {
                ViewName = "~/Views/Shared/AccessDenied.cshtml"
            };
        }
    }
}