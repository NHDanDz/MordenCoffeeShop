namespace DemoApp_Test.Models
{
    public class OrderItemViewModel
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public double UnitPrice { get; set; }
        public double Discount { get; set; }
        public double Subtotal { get; set; }
        public string IceOption { get; set; }
        public string SugarOption { get; set; }
        public string SizeOption { get; set; }
    }
}
