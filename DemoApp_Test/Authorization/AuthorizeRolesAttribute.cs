using DemoApp_Test.Enums;
using Microsoft.AspNetCore.Mvc;

namespace DemoApp_Test.Authorization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeRolesAttribute : Attribute
    {
        public UserRole[] Roles { get; set; }

        public AuthorizeRolesAttribute(params UserRole[] roles)
        {
            Roles = roles;
        }
    }
}