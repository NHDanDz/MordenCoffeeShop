using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace DemoApp_Test.Models
{
    public class Client
    {
        [Key]
        public string Client_id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Gmail { get; set; }
        [Required]
        public string Contact { get; set; }
        [Required]
        public string Location { get; set; }

    }
}
