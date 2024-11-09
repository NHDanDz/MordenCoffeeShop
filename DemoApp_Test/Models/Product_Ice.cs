using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace DemoApp_Test.Models
{
    [PrimaryKey(nameof(Ice_id), nameof(Product_id))]

    public class Product_Ice
    {
        [Required]
        public string Ice_id { get; set; }
        public string Product_id { get; set; }
        public virtual Product Product { get; set; }
        public virtual Ice Ice { get; set; }
    }
}
