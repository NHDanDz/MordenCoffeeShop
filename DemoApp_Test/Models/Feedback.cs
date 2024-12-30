using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DemoApp_Test.Models
{
    public class Feedback
    {
        [Key]
        public string Feedback_id { get; set; }
        [ForeignKey("Client")]

        public string Client_id { get; set; }
        [ForeignKey("Product")]

        public string Product_id { get; set; }
        public virtual Client Client { get; set; }
        public virtual Product Product { get; set; }
        public double Rating { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool Status { get; set; }
        public string? AdminResponse { get; set; }
        public DateTime? ResponseDate { get; set; }
        public string? RespondedBy { get; set; }
    }
}
