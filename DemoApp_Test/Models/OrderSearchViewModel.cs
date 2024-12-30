namespace DemoApp_Test.Models
{
    public class OrderSearchViewModel
    {
        public string Query { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool? Status { get; set; }
        public string PaymentMethod { get; set; }
        public bool? PaymentStatus { get; set; }
        public decimal? MinTotal { get; set; }
        public decimal? MaxTotal { get; set; }
        public bool? DeleteStatus { get; set; }

        public List<Bill> Orders { get; set; } = new List<Bill>();
    }
}
