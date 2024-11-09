using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
namespace DemoApp_Test.Models
{
    [PrimaryKey(nameof(Product_id), nameof(Bill_id))]
    public class Product_Bill
    {
        [Required]
        public string Product_id { get; set; }
        public string Bill_id { get; set; }
        [Required]
        public int Quantity { get; set; }
        [Required]
        public double Amount { get; set; }

        public virtual Product Product { get; set; }
        public virtual Bill Bill { get; set; }
    }
}
