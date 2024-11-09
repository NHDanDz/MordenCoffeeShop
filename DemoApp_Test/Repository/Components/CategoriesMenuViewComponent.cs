using DemoApp_Test.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DemoApp_Test.Repository.Components
{
    public class CategoriesMenuViewComponent : ViewComponent
    {
        private readonly DataContext _dataContext;
        private const int PageSize = 12;

        public CategoriesMenuViewComponent(DataContext context)
        {
            _dataContext = context;
        }
        public async Task<IViewComponentResult> InvokeAsync(int currentPage = 1)
        {
            var query = _dataContext.Product
                                  .Include(sp => sp.Brand)
                                  .Include(sp => sp.TypeCoffee)
                                  .OrderByDescending(sp => sp.Date);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / PageSize);

            var products = await query.Skip((currentPage - 1) * PageSize)
                                    .Take(PageSize)
                                    .ToListAsync();

            var model = new ProductPaginationViewModel
            {
                Products = products,
                CurrentPage = currentPage,
                TotalPages = totalPages
            };

            return View(model);
        }

    }
}
