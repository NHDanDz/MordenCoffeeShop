using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace DemoApp_Test.Models
{
    [PrimaryKey(nameof(Size_id), nameof(Product_id))]

    public class Product_Size
    {
        [Required]
        public string Size_id { get; set; }
        public string Product_id { get; set; }
        public double Price { get; set; }

        public virtual Product Product { get; set; }
        public virtual Size Size { get; set; }
    }
}
