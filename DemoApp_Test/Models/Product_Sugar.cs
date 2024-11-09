using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
namespace DemoApp_Test.Models
{
    [PrimaryKey(nameof(Sugar_id), nameof(Product_id))]

    public class Product_Sugar
    {
        [Required]
        public string Sugar_id { get; set; }
        public string Product_id { get; set; }
        public virtual Product Product { get; set; }
        public virtual Sugar Sugar { get; set; }
    }
}
