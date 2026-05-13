namespace WularItech_solutions.Models
{
    public class CartViewModel
    {
        public Guid CartItemId { get; set; }
        public string ProductName { get; set; }
        public decimal ProductPrice { get; set; }
        public int Quantity { get; set; }
        public decimal Total { get; set; }

        public string ProductImage { get; set; } // To show image
        public Guid ProductId { get; set; }       // To link + / − buttons
        public int Stock { get; set; }            // To prevent exceeding stock
        public decimal TotalPrice => ProductPrice * Quantity; // auto-calc

    }
}
