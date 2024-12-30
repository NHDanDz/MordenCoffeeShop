using Microsoft.EntityFrameworkCore;
using DemoApp_Test.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task<IViewComponentResult> InvokeAsync(
            int currentPage = 1,
            string query = null,
            string brandId = null,
            string typeId = null,
            string priceRange = null,
            string sort = null)
        {
            // Lấy danh sách brands và types cho filter
            ViewBag.Brands = await _dataContext.Brand.ToListAsync();
            ViewBag.TypeCoffees = await _dataContext.TypeCoffee.ToListAsync();
            ViewBag.Sizes = await _dataContext.Size.ToListAsync();

            // Bắt đầu với query cơ bản
            var productsQuery = _dataContext.Product
                .Include(p => p.Brand)
                .Include(p => p.TypeCoffee)
                .AsQueryable();

            // Áp dụng các bộ lọc
            if (!string.IsNullOrEmpty(query))
            {
                productsQuery = productsQuery.Where(p => p.ProductName.Contains(query));
            }

            if (!string.IsNullOrEmpty(brandId))
            {
                productsQuery = productsQuery.Where(p => p.Brand_id == brandId);
            }

            if (!string.IsNullOrEmpty(typeId))
            {
                productsQuery = productsQuery.Where(p => p.Type_id == typeId);
            }

            if (!string.IsNullOrEmpty(priceRange))
            {
                var prices = priceRange.Split('-');
                if (prices.Length == 2)
                {
                    var minPrice = double.Parse(prices[0]);
                    var maxPrice = double.Parse(prices[1]);
                    productsQuery = productsQuery.Where(p => p.Price >= minPrice && p.Price <= maxPrice);
                }
            }

            switch (sort)
            {
                case "price_asc":
                    productsQuery = productsQuery.OrderBy(p => p.Price);
                    break;
                case "price_desc":
                    productsQuery = productsQuery.OrderByDescending(p => p.Price);
                    break;
                case "rating_desc":
                    productsQuery = productsQuery.OrderByDescending(p => p.Rating);
                    break;
                default:
                    productsQuery = productsQuery.OrderByDescending(p => p.Date);
                    break;
            }

            var totalItems = await productsQuery.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / PageSize);

            var products = await productsQuery
                .Skip((currentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = currentPage;
            ViewBag.CurrentSort = sort;
            ViewBag.SelectedBrand = brandId;
            ViewBag.SelectedType = typeId;
            ViewBag.CurrentQuery = query;

            return View(new ProductPaginationViewModel { Products = products });
        } 
    }
}