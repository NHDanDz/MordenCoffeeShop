using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DemoApp_Test.Enums;
using Newtonsoft.Json;

namespace DemoApp_Test.Models
{
    public class Account
    {
        [Key]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public DateTime? RegistrationDate { get; set; }

        [Required]
        public bool Status { get; set; } 
        public string? ResetPasswordToken { get; set; }
        public DateTime? ResetPasswordExpiry { get; set; }

        [Required]
        public UserRole Role { get; set; }  // Thay đổi kiểu từ int sang UserRole

        [ForeignKey("Client")]
        public string? Client_id { get; set; }

        public DateTime? LastActivity { get; set; }
        public virtual Client Client { get; set; }
    }
}   