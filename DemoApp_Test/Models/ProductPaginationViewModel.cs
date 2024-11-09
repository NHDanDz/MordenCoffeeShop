namespace DemoApp_Test.Models
{
    public class ProductPaginationViewModel
    {
        public IEnumerable<Product> Products { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
