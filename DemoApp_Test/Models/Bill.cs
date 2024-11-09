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

        public virtual Client Client { get; set; }
        public bool PaymentStatus { get; set; } // Trạng thái thanh toán
        public string PaymentMethod { get; set; }
        public DateTime? ProcessDate { get; set; }

    }
}
