using DemoApp_Test.Enums;

namespace DemoApp_Test.Models
{
    public class ProfileViewModel
    {
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public UserRole Role { get; set; }
        public DateTime? LastActivity { get; set; }
    }

}
