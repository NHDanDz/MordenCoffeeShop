using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DemoApp_Test.Models
{ 
        public class Brand
        {
            [Key]
            [Column("Brand_id")]
            [StringLength(10)]
            public string Brand_id { get; set; }

            [Column("BrandName")]
            [StringLength(50)]
            public string BrandName { get; set; }

            [Column("Location")]
            [StringLength(50)]
            public string Location { get; set; }

            [Column("FoundingDate")]
            public DateTime FoundingDate { get; set; }

            [Column("Email")]
            [StringLength(100)]
            public string? Email { get; set; }

            [Column("PhoneNumber")]
            [StringLength(20)]
            public string? PhoneNumber { get; set; }

            [Column("Description")]
            [StringLength(500)]
            public string? Description { get; set; }

            [Column("Logo")]
            [StringLength(200)]
            public string? Logo { get; set; }

            [Column("Status")]
            public bool? Status { get; set; }

            [Column("RegisterDate")]
            public DateTime? RegisterDate { get; set; }

            [Column("BusinessLicense")]
            [StringLength(50)]
            public string? BusinessLicense { get; set; }

            [Column("TaxNumber")]
            [StringLength(50)]
            public string? TaxNumber { get; set; }

            // Navigation properties
            public virtual ICollection<Product> Products { get; set; }
            public virtual ICollection<Voucher> Vouchers { get; set; }
        }
    }
 