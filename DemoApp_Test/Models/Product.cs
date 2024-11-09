using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DemoApp_Test.Models
{
    public class Product
    {
        [Key]
        public string Product_id { get; set; }
        [Required]
        public string ProductName { get; set; }
        [Required]
        public double Price { get; set; }
        [Required]
        public double Rating { get; set; }
        [Required]
        public int ReviewCount { get; set; }
        [Required]
        public string Image { get; set; }
        [Required]
        public DateTime Date { get; set; }
        [Required]

        public int Discount { get; set; }
        [ForeignKey("Brand")]
        [Required]
        public string Brand_id { get; set; }
        public virtual Brand Brand { get; set; }
        [ForeignKey("TypeCoffee")]
        [Required]
        public string Type_id { get; set; }
        public virtual TypeCoffee TypeCoffee { get; set; }
        public ICollection<Product_Size> Product_Size { get; set; }
        public ICollection<Product_Ice> Product_Ice { get; set; }
        public ICollection<Product_Sugar> Product_Sugar { get; set; }

    }
}
