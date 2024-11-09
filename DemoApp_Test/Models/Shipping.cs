using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DemoApp_Test.Models
{
    public class Shipping
    {
        [Key]

        public string Shipping_id { get; set; }
        public DateTime Date { get; set; }
        public string Location { get; set; }
        [ForeignKey("Bill_id")]

        public string Bill_id { get; set; }
        public virtual Bill Bill { get; set; }
    }
}
