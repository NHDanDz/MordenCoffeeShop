namespace DemoApp_Test.Models
{
    public class LoggingSearchViewModel
    {
        public string Query { get; set; }
        public string ActionType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<AdminActivity> Activities { get; set; }
    }
}
