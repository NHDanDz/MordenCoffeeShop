using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace DemoApp_Test.Models
{
    public class Voucher
    {
        [Key]
        public string Voucher_id { get; set; }
        [Required]
        public bool VoucherType { get; set; }
        [Required]
        public double Detail { get; set; }
        [Required]
        public string ExpirationDate { get; set; }

    }
}
