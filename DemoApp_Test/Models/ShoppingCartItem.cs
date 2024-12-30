using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;


namespace DemoApp_Test.Models
{
    public class ShoppingCartItem
    {
        [Key]
        public string CartItem_id { get; set; }

        [Required]
        [ForeignKey("ShoppingCart")]
        public string Cart_id { get; set; }

        [Required]
        [ForeignKey("Product")]
        public string Product_id { get; set; }

        [Required]
        public int Quantity { get; set; }
        [Required]
        public int Discount { get; set; }

        [ForeignKey("Ice")]
        public string Ice_id { get; set; }

        [ForeignKey("Sugar")]
        public string Sugar_id { get; set; }

        [ForeignKey("Size")]
        public string Size_id { get; set; }

        [Required]
        public DateTime AddedDate { get; set; }

        // Navigation properties
        public virtual ShoppingCart ShoppingCart { get; set; }
        public virtual Product Product { get; set; }
        public virtual Ice Ice { get; set; }
        public virtual Sugar Sugar { get; set; }
        public virtual Size Size { get; set; }
    }
}
