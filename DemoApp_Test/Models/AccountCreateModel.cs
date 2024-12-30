using DemoApp_Test.Enums;

namespace DemoApp_Test.Models
{
    public class AccountCreateModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Contact { get; set; }
        public string Location { get; set; }
        public string Role { get; set; }  // Đổi từ UserRole sang string
    }
}
