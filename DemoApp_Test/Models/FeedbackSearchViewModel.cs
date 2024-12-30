namespace DemoApp_Test.Models
{
    public class FeedbackSearchViewModel
    {
        public string Query { get; set; }
        public string Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<Feedback> Feedbacks { get; set; }
    }
}
