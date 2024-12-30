namespace DemoApp_Test.Models
{
    public class VoucherSearchViewModel
    {
        public string Query { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string VoucherType { get; set; }
        public string Brand_id { get; set; }
        public bool? Status { get; set; }
        public List<Voucher> Vouchers { get; set; }
        public List<Brand> Brands { get; set; } // For brand dropdown
    }
}
