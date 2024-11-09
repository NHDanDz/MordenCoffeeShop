namespace DemoApp_Test.Models.ViewModels
{
    public class CartItemViewModel
    {
        public List<CartItemModel> CartItems { get; set; } = new List<CartItemModel>();
        public double TongTien { get; set; }
    }
}
