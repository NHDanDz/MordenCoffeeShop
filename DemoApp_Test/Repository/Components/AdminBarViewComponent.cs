using DemoApp_Test.Repository;
using Microsoft.AspNetCore.Mvc;

namespace DemoApp_Test.Components
{
    public class AdminBarViewComponent : ViewComponent
    {
        private readonly DataContext context;
        public AdminBarViewComponent(DataContext _context)
        {
            context = _context;
        }
        public IViewComponentResult Invoke()
        {
            try
            { 
                return View();
            }
            catch (Exception ex)
            { 
                return View();
            }
        }
    }
}