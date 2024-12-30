using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace DemoApp_Test.Models
{
    public class ShoppingCart
    {
        [Key]
        public string Cart_id { get; set; }

        [Required]
        [ForeignKey("Account")]
        public string Username { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; }

        [Required]
        public DateTime LastModifiedDate { get; set; }

        // Navigation properties
        public virtual Account Account { get; set; }
        public virtual ICollection<ShoppingCartItem> ShoppingCartItems { get; set; }
    }
}
