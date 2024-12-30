using DemoApp_Test.Models;

public class ProductPaginationViewModel
{
    public List<Product> Products { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public string Query { get; set; }
    public string SelectedBrand { get; set; }
    public string SelectedType { get; set; }
    public string CurrentSort { get; set; }
}