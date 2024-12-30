namespace DemoApp_Test.Models
{
    public class OrderDetailsViewModel
    {
        public Bill Order { get; set; }
        public List<OrderItemViewModel> Items { get; set; }
        public List<string> AppliedVouchers { get; set; }
        public ShippingInfo ShippingInfo { get; set; }
    }
}
