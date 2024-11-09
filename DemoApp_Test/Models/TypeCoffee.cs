using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace DemoApp_Test.Models
{
    public class TypeCoffee
    {
        [Key]
        public string Type_id { get; set; }
        [Required]
        public string TypeName { get; set; }
    }
}
