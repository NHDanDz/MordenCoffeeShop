using Microsoft.AspNetCore.Builder;

namespace DemoApp_Test.Services
{
    public static class ServiceExtensions
    {
        public static void ConfigureVisitorCounter(this WebApplicationBuilder builder)
        {
            builder.Services.AddSingleton<IApplicationState, ApplicationState>();
        }
    }
}