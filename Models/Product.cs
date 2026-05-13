using System.ComponentModel.DataAnnotations;

namespace WularItech_solutions.Models
{
    public class Product
    {
        [Key]
        public Guid ProductId { get; set; } = Guid.NewGuid(); //autogenerate
        public required string ProductName { get; set; }
        public required string ProductDescription { get; set; }
        public string ProductImage { get; set; }
        public required decimal ProductPrice { get; set; }
        public required int ProductStock { get; set; }
    }
}