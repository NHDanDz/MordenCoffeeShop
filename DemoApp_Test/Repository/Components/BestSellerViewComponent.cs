using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DemoApp_Test.Repository.Components
{
    public class BestSellerViewComponent : ViewComponent
    {
        private readonly DataContext _dataContext;
        public BestSellerViewComponent(DataContext context)
        {
            _dataContext = context;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var products = await _dataContext.Product
                                             .Include(sp => sp.Brand)
                                             .Include(sp => sp.TypeCoffee)
                                             .OrderByDescending(sp => sp.Discount)
                                             .Take(1)
                                             .ToListAsync();
            return View(products);
        }

    }
}
