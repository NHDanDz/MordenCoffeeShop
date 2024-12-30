using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace DemoApp_Test.Models
{
    [PrimaryKey(nameof(Product_id), nameof(Bill_id), nameof(Size_id), nameof(Ice_id), nameof(Sugar_id))]
    public class Product_Bill
    {
        [Required]
        public string Product_id { get; set; }

        [Required]
        public string Bill_id { get; set; }

        public int Quantity { get; set; }

        public double Amount { get; set; }

        [Required]  // Đổi từ nullable thành required
        public string Ice_id { get; set; } = "Ice01";  // Giá trị mặc định

        [Required]  // Đổi từ nullable thành required
        public string Sugar_id { get; set; } = "Sugar01";  // Giá trị mặc định

        [Required]  // Đổi từ nullable thành required
        public string Size_id { get; set; } = "Size01";  // Giá trị mặc định

        public int? Discount { get; set; }

        // Navigation properties
        public virtual Product Product { get; set; }
        public virtual Bill Bill { get; set; }
        public virtual Ice Ice { get; set; }
        public virtual Sugar Sugar { get; set; }
        public virtual Size Size { get; set; }
    }
}