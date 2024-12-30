using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;


namespace DemoApp_Test.Models
{
    [PrimaryKey(nameof(Voucher_id), nameof(Bill_id))]

    public class Bill_Voucher
    {
        [Required]
        public string Voucher_id { get; set; }
        public string Bill_id { get; set; }
        public DateTime AppliedDate { get; set; }
        public double DiscountAmount { get; set; }
        public virtual Voucher Voucher { get; set; }
        public virtual Bill Bill { get; set; }
    }
}
