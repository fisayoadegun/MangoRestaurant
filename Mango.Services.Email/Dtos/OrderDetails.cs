using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mango.Services.Email.Models
{
    public class OrderDetails
    {
        [Key]
        public int OrderDetailsId { get; set; }
        public int OrderHeaderId { get; set; }
        [ForeignKey("OrderHeaderId")]
        public virtual EmailOrderHeader OrderHeader { get; set; }
        public int ProductId { get; set; }                
        public int Count { get; set; }
        public string ProductName { get; set; }
        public double Price { get; set; }
        public string ProductImage { get; set; }
    }
}
