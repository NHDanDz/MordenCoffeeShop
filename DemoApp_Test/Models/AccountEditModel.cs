using DemoApp_Test.Enums;
using System.ComponentModel.DataAnnotations;

namespace DemoApp_Test.Models
{
    public class AccountEditModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Contact { get; set; }
        public string Location { get; set; }
        public string Role { get; set; }  // Đổi thành string để match với dữ liệu gửi lên
    }
}
