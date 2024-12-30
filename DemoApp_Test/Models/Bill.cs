using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DemoApp_Test.Models
{
    public class Bill
    {
        [Key]
        public string Bill_id { get; set; }
        [Required]
        public double Total { get; set; }
        [Required]
        public DateTime Date { get; set; }
        [Required]
        public bool Status { get; set; }
        [ForeignKey("Client")]
        public string Client_id { get; set; }
        public string OrderNotes { get; set; }
        public string Location { get; set; }  // Thêm trường này
        public string Contact { get; set; }   // Thêm trường này
        public virtual Client Client { get; set; }
        public bool PaymentStatus { get; set; } // Trạng thái thanh toán
        public bool DeleteStatus { get; set; } // Trạng thái thanh toán
         public string PaymentMethod { get; set; }
        public DateTime? ProcessDate { get; set; }
        public virtual ICollection<Product_Bill> Product_Bill { get; set; } = new List<Product_Bill>();

        public virtual ICollection<Bill_Voucher> Bill_Voucher { get; set; } = new List<Bill_Voucher>();
        [NotMapped]
        public double SubTotal { get; set; }

        [NotMapped]
        public double VoucherDiscount { get; set; }

        [NotMapped]
        public string VoucherCode { get; set; }

        [NotMapped]
        public string VoucherName { get; set; }
    }
}
