using System.ComponentModel.DataAnnotations;

namespace WularItech_solutions.Models
{
    public class Cart
    {
        [Key]
        public Guid CartItemId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }   //

        [Required]
        public Guid ProductId { get; set; }

        [Required]
        public int Quantity { get; set; } = 1;
    }
}


