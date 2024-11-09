using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
namespace DemoApp_Test.Models
{
    public class Ice
    {
        [Key]
        public string Ice_id { get; set; }
        [Required]
        public string IceDetail { get; set; }
    }
}
