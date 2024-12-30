namespace DemoApp_Test.Models
{
    public class AccountSearchViewModel
    {
        public string Query { get; set; }
        public string Role { get; set; }
        public List<Account> Accounts { get; set; }
    }
}
