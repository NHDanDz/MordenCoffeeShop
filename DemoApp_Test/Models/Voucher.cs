using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DemoApp_Test.Models
{
    public class Voucher
    {
        [Key]
        [Column("Voucher_id")]
        [StringLength(10)]
        public string Voucher_id { get; set; }

        [Column("Detail")]
        public double? Detail { get; set; }  // Changed from float? to double?

        [Column("ExpirationDate")]
        public DateTime? ExpirationDate { get; set; }

        [Column("Code")]
        [StringLength(50)]
        public string Code { get; set; }

        [Column("Name")]
        [StringLength(100)]
        public string Name { get; set; }

        [Column("Description")]
        [StringLength(500)]
        public string Description { get; set; }

        [Column("MinimumSpend")]
        public double? MinimumSpend { get; set; }  // Changed from float? to double?

        [Column("MaximumDiscount")]
        public double? MaximumDiscount { get; set; }  // Changed from float? to double?

        [Column("UsageLimit")]
        public int? UsageLimit { get; set; }

        [Column("UsageCount")]
        public int? UsageCount { get; set; }

        [Column("VoucherType")]
        [StringLength(20)]
        public string VoucherType { get; set; }

        [Column("Brand_id")]
        [StringLength(10)]
        public string? Brand_id { get; set; }

        [Column("Status")]
        public bool? Status { get; set; }

        [Column("CreatedDate")]
        public DateTime? CreatedDate { get; set; }

        [Column("CreatedBy")]
        [StringLength(50)]
        public string CreatedBy { get; set; }

        // Navigation properties
        [ForeignKey("Brand_id")]
        public virtual Brand Brand { get; set; }

        public virtual ICollection<Bill_Voucher> Bill_Voucher { get; set; }
    }
}
