using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace DemoApp_Test.Models
{
    public class Size
    {
        [Key]
        public string Size_id { get; set; }
        [Required]
        public string SizeDetail { get; set; }
    }
}
