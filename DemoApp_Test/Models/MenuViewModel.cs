namespace DemoApp_Test.Models
{
    public class MenuViewModel
    {
        public IEnumerable<Product> Product { get; set; }
        public IEnumerable<TypeCoffee> TypeCoffee { get; set; }
        public IEnumerable<Brand> Brand { get; set; }
        public IEnumerable<Size> Size { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string CurrentSort { get; set; }
        public string CurrentBrand { get; set; }
        public string CurrentType { get; set; }
        public string CurrentSize { get; set; }
        public string CurrentPriceRange { get; set; }
        public string SearchQuery { get; set; }
    }
}
