namespace DemoApp_Test.Models
{
    public class ProductSearchViewModel
    {
        // Thuộc tính tìm kiếm
        public string Query { get; set; }
        public float? MinPrice { get; set; }
        public float? MaxPrice { get; set; }
        public string BrandId { get; set; }

        // Danh sách sản phẩm
        public IEnumerable<Product> Product { get; set; }
    }
}
