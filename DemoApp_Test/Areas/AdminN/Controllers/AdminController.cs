using DemoApp_Test.Models;
using DemoApp_Test.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DemoApp_Test.Areas.AdminN.Controllers
{
    [Area("AdminN")]
    public class AdminController : Controller
    { 
        private readonly DataContext _db;
        private readonly IWebHostEnvironment _webHostEnvironment;


        public AdminController(DataContext context, IWebHostEnvironment webHostEnvironment)
        {
            _db = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Home()
        {

            int totalCount = _db.Account.Count();
            Console.WriteLine(totalCount);
            return View();
        }
        public IActionResult ShowProduct(string query, float? minPrice, float? maxPrice, string brandId)
        {
            var model = new ProductSearchViewModel
            {
                Query = query,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                BrandId = brandId,

                Product = _db.Product.Include(p => p.Brand).Include(p => p.TypeCoffee).AsQueryable()
                    .Where(p => (string.IsNullOrEmpty(query) || p.ProductName.Contains(query) || p.Brand.BrandName.Contains(query))
                        && (!minPrice.HasValue || p.Price >= minPrice)
                        && (!maxPrice.HasValue || p.Price <= maxPrice)
                        && (string.IsNullOrEmpty(brandId) || p.Brand_id == brandId))
                    .ToList()
            };

            ViewBag.Brands = new SelectList(_db.Brand, "Brand_id", "BrandName");

            return View(model);
        }

        [HttpGet]
        public IActionResult Create()
        {
            // Kiểm tra xem có thương hiệu hay không
            ViewBag.Brands = _db.Brand.ToList();

            // Truyền danh sách loại sản phẩm (ProductType) vào ViewBag
            ViewBag.ProductTypes = _db.TypeCoffee.ToList();  // Đảm bảo rằng bạn lấy đúng danh sách từ cơ sở dữ liệu

            // Kiểm tra nếu không có loại sản phẩm trong cơ sở dữ liệu
            if (ViewBag.TypeCoffees == null || !ViewBag.TypeCoffees.Any())
            {
                ViewBag.TypeCoffees = new List<TypeCoffee>(); // Nếu không có dữ liệu, trả về danh sách rỗng
            }

            return View();
        }




        // POST: Admin/Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Brands = _db.Brand.ToList();
                ViewBag.ProductTypes = _db.TypeCoffee.ToList();
                return View(model);
            }
              
            await _db.SaveChangesAsync();

            return RedirectToAction("ShowProduct");  // Sau khi thêm sản phẩm, chuyển hướng đến danh sách sản phẩm
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(string id)
        {
            // Tìm sản phẩm theo ID (đồng bộ)
            var product = _db.Product.Find(id);
            Console.WriteLine(product);

            if (product != null)
            {
                // Kiểm tra xem sản phẩm có hình ảnh không và nếu có thì xóa hình ảnh
                if (!string.IsNullOrEmpty(product.Image))
                {
                    string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "productImages", product.Image);
                    Console.WriteLine(imagePath);
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath); // Xóa file hình ảnh
                    }
                }

                _db.Product.Remove(product); // Xóa sản phẩm
                _db.SaveChanges(); // Lưu thay đổi vào cơ sở dữ liệu
            }

            return RedirectToAction("ShowProduct"); // Chuyển hướng đến action ShowProduct
        }

    }
} 