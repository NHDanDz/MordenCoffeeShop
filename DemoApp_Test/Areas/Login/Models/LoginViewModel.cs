namespace DemoApp_Test.Areas.Login.Models
{
    public class LoginViewModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public bool RememberMe { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        public string Username { get; set; }
        public string Email { get; set; }
    }
}
