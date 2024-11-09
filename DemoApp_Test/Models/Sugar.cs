using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace DemoApp_Test.Models
{
    public class Sugar
    {
        [Key]
        public string Sugar_id { get; set; }
        [Required]
        public string SugarDetail { get; set; }
    }
}
