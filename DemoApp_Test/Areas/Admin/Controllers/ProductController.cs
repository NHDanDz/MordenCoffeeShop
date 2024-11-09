using DemoApp_Test.Models;
using DemoApp_Test.Repository;
using Microsoft.AspNetCore.Mvc;
using X.PagedList;

namespace DemoApp_Test.Areas.Admin.Controllers
{
    [Area("Admin")]

    public class ProductController : Controller
    {
        private readonly DataContext _context;
        private const int PageSize = 8; // Thêm hằng số PageSize

        public ProductController(DataContext context)
        {
            _context = context;
        }

        public IActionResult Index(string brandId, string typeId, string size, string priceRange, string sort, int page = 1)
        {
            // Base query với eager loading
            var query = from p in _context.Product
                        join b in _context.Brand on p.Brand_id equals b.Brand_id
                        join t in _context.TypeCoffee on p.Type_id equals t.Type_id
                        select new HienThiSanPham
                        {
                            Ma_Sanpham = p.Product_id,
                            Ten_Sanpham = p.ProductName,
                            Giagoc = p.Price,
                            Rating = p.Rating,
                            ReviewCount = p.ReviewCount,
                            Link1 = p.Image,
                            Discount = p.Discount,
                            Date = p.Date,
                            Ten_Nhasanxuat = b.BrandName,
                            Ten_loai = t.TypeName,
                            Brand_id = p.Brand_id,
                            Type_id = p.Type_id
                        };
            if (!string.IsNullOrEmpty(sort))
            {
                switch (sort.ToLower())
                {
                    case "asc":
                        query = query.OrderBy(p => p.Giagoc);
                        break;
                    case "desc":
                        query = query.OrderByDescending(p => p.Giagoc);
                        break;
                    default:
                        query = query.OrderByDescending(p => p.Date); // Mặc định sắp xếp theo ngày
                        break;
                }
            }
            else
            {
                query = query.OrderByDescending(p => p.Date); // Mặc định sắp xếp theo ngày
            }
            // Apply Brand filter
            if (!string.IsNullOrEmpty(brandId))
            {
                query = query.Where(p => p.Brand_id.Trim() == brandId.Trim());
                ViewBag.SelectedBrand = brandId;
            }

            // Apply Type filter
            if (!string.IsNullOrEmpty(typeId))
            {
                query = query.Where(p => p.Type_id.Trim() == typeId.Trim());
                ViewBag.SelectedType = typeId;
            }

            // Apply Size filter
            if (!string.IsNullOrEmpty(size))
            {
                var productsWithSize = _context.Product_Size
                    .Where(ps => _context.Size
                        .Where(s => s.SizeDetail == size)
                        .Select(s => s.Size_id)
                        .Contains(ps.Size_id))
                    .Select(ps => ps.Product_id);
                query = query.Where(p => productsWithSize.Contains(p.Ma_Sanpham));
                ViewBag.SelectedSize = size;
            }

            // Apply Price Range filter
            if (!string.IsNullOrEmpty(priceRange))
            {
                switch (priceRange)
                {
                    case "1": // 0-5
                        query = query.Where(p => p.Giagoc - (p.Giagoc * p.Discount / 100) < 5);
                        break;
                    case "2": // 5-10
                        query = query.Where(p => p.Giagoc - (p.Giagoc * p.Discount / 100) >= 5 && p.Giagoc - (p.Giagoc * p.Discount / 100) < 10);
                        break;
                    case "3": // 10-15
                        query = query.Where(p => p.Giagoc - (p.Giagoc * p.Discount / 100) >= 10 && p.Giagoc - (p.Giagoc * p.Discount / 100) < 15);
                        break;
                    case "4": // 15-20
                        query = query.Where(p => p.Giagoc - (p.Giagoc * p.Discount / 100) >= 15 && p.Giagoc - (p.Giagoc * p.Discount / 100) < 20);
                        break;
                    case "5": // 20-25
                        query = query.Where(p => p.Giagoc - (p.Giagoc * p.Discount / 100) >= 20 && p.Giagoc - (p.Giagoc * p.Discount / 100) < 25);
                        break;
                    case "6": // 25+
                        query = query.Where(p => p.Giagoc - (p.Giagoc * p.Discount / 100) >= 25);
                        break;
                }
                ViewBag.SelectedPriceRange = priceRange;
            }

            // Execute query and get total count
            var totalItems = query.Count();
            var products = query
                    .OrderByDescending(p => p.Date)
                    .Skip((page - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();
            if (!string.IsNullOrEmpty(sort))
            {
                switch (sort.ToLower())
                {
                    case "asc":
                        products = query.OrderBy(p => p.Giagoc).Skip((page - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();
                        break;
                    case "desc":
                        products = query.OrderByDescending(p => p.Giagoc).Skip((page - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();
                        break;
                    default:
                        products = query.OrderByDescending(p => p.Date).Skip((page - 1) * PageSize)
                    .Take(PageSize)
                    .ToList(); // Mặc định sắp xếp theo ngày
                        break;
                }
            }

            // Create paged list
            var pagedList = new StaticPagedList<HienThiSanPham>(
                products,
                page,
                PageSize,
                totalItems
            );

            // Load filter options
            ViewBag.typeCoffees = _context.TypeCoffee.ToList();
            ViewBag.Brands = _context.Brand.ToList();
            ViewBag.Sizes = _context.Size.ToList();

            // Pagination info
            ViewBag.TotalItems = totalItems;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize);

            // Selected filter values for maintaining state
            ViewBag.SelectedBrandId = brandId;
            ViewBag.SelectedTypeId = typeId;
            ViewBag.SelectedSize = size;
            ViewBag.SelectedPriceRange = priceRange;

            return View(pagedList);
        }
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var product = await _context.Product.FindAsync(id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm." });
                }

                // Xóa các bản ghi liên quan
                var productSizes = _context.Product_Size.Where(ps => ps.Product_id == id);
                _context.Product_Size.RemoveRange(productSizes);

                var productIces = _context.Product_Ice.Where(pi => pi.Product_id == id);
                _context.Product_Ice.RemoveRange(productIces);

                var productSugars = _context.Product_Sugar.Where(ps => ps.Product_id == id);
                _context.Product_Sugar.RemoveRange(productSugars);

                // Xóa sản phẩm
                _context.Product.Remove(product);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetProductDetails(string id)
        {
            try
            {
                var product = _context.Product
                    .Where(p => p.Product_id == id)
                    // Thêm Include nếu bạn có navigation properties
                    // .Include(p => p.Brand)
                    // .Include(p => p.Type)
                    .Select(p => new
                    {
                        p.Product_id,
                        p.ProductName,
                        p.Price,
                        p.Discount,
                        p.Brand_id,
                        p.Type_id,
                        p.Rating,
                        p.ReviewCount,
                        p.Image,
                        // Thêm BrandName và TypeName
                        BrandName = _context.Brand
                            .Where(b => b.Brand_id == p.Brand_id)
                            .Select(b => b.BrandName)
                            .FirstOrDefault(),
                        TypeName = _context.TypeCoffee
                            .Where(t => t.Type_id == p.Type_id)
                            .Select(t => t.TypeName)
                            .FirstOrDefault(),
                        sizes = _context.Size.Select(s => new
                        {
                            size_id = s.Size_id,
                            sizeDetail = s.SizeDetail,
                            price = _context.Product_Size
                                .Where(ps => ps.Size_id == s.Size_id && ps.Product_id == id)
                                .Select(ps => ps.Price)
                                .FirstOrDefault(),
                            isSelected = _context.Product_Size
                                .Any(ps => ps.Size_id == s.Size_id && ps.Product_id == id)
                        }).ToList()
                    })
                    .FirstOrDefault();
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                return Json(new { success = true, data = product });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpPost]
        public IActionResult UpdateProduct([FromBody] ProductUpdateModel model)
        {
            try
            {
                var product = _context.Product.Find(model.Product_id);
                if (product == null)
                    return Json(new { success = false, message = "Product not found" });

                // Cập nhật thông tin sản phẩm
                product.ProductName = model.ProductName;
                product.Price = model.Price;
                product.Discount = model.Discount;
                product.Brand_id = model.Brand_id;
                product.Type_id = model.Type_id;
                product.Rating = model.Rating;
                product.ReviewCount = model.ReviewCount;
                var t = model.NewImage;
                var x = model.RootImage;
                // Xử lý copy file ảnh nếu có
                if (!string.IsNullOrEmpty(model.NewImage) && !string.IsNullOrEmpty(model.RootImage))
                {
                    var brand = _context.Brand.Find(model.Brand_id);
                    var type = _context.TypeCoffee.Find(model.Type_id);

                    if (brand != null && type != null)
                    {
                        string targetDirectory = Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot",
                            "img",
                            brand.BrandName,
                            type.TypeName
                        );

                        if (!Directory.Exists(targetDirectory))
                        {
                            Directory.CreateDirectory(targetDirectory);
                        }

                        string targetFile = Path.Combine(targetDirectory, model.NewImage);

                        // Chuyển data URL thành file
                        if (model.RootImage.Contains(","))
                        {
                            var base64Data = model.RootImage.Split(",")[1];
                            var fileBytes = Convert.FromBase64String(base64Data);
                            System.IO.File.WriteAllBytes(targetFile, fileBytes);
                            product.Image = model.NewImage;
                        }
                    }
                }

                _context.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        // Model cho update
        public class ProductUpdateModel
        {
            public string Product_id { get; set; }
            public string ProductName { get; set; }
            public double Price { get; set; }
            public int Discount { get; set; }
            public string Brand_id { get; set; }
            public string Type_id { get; set; }
            public int Rating { get; set; }
            public int ReviewCount { get; set; }
            public string NewImage { get; set; }    // Tên file mới
            public string RootImage { get; set; }   // Đường dẫn gốc của file
        }
    }
}