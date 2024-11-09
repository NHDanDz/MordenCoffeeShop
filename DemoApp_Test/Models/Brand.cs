using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace DemoApp_Test.Models
{
    public class Brand
    {
        [Key]
        public string Brand_id { get; set; }
        [Required]
        public string BrandName { get; set; }
        [Required]
        public string Location { get; set; }
        [Required]
        public DateTime FoundingDate { get; set; }
    }
}
