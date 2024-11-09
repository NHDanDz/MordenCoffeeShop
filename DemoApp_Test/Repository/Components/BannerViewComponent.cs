using Microsoft.AspNetCore.Mvc;

namespace DemoApp_Test.Repository.Components
{
    public class BannerViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(string backgroundImage)
        {
            return View("Default", backgroundImage);
        }
    }
}
