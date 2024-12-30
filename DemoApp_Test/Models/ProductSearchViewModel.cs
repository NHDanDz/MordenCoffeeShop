namespace DemoApp_Test.Models
{
    public class ProductSearchViewModel
    {
        // Thuộc tính tìm kiếm
        public string Query { get; set; }
        public float? MinPrice { get; set; }
        public float? MaxPrice { get; set; }
        public float? MinRate { get; set; }
        public float? MaxRate { get; set; }
        public int? MinDiscount { get; set; }
        public int? MaxDiscount { get; set; }
        public string BrandId { get; set; }
        public string BrandName { get; set; }
        public string TypeId { get; set; }

        public string TypeName { get; set; }
        public string Image { get; set; }

        // Danh sách sản phẩm
        public IEnumerable<Product> Product { get; set; }
    }
}
