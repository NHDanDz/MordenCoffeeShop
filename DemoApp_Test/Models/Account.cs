using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace DemoApp_Test.Models
{
    public class Account
    {
        [Key]
        public string Username { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public DateTime RegistrationDate { get; set; }
        public int Role { get; set; }

        [ForeignKey("Client")]
        public string Client_id { get; set; }
        public virtual Client Client { get; set; }

    }
}
