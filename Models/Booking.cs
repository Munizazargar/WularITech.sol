using System.ComponentModel.DataAnnotations;

namespace WularItech_solutions.Models
{
    public class Booking
    {
        [Key]
        public Guid BookingId { get; set; } = Guid.NewGuid();

        [Required]
        public required string CustomerName { get; set; }

        [Required]
        public required string CustomerEmail { get; set; }

        [Required]
        public required string CustomerPhone { get; set; }

        [Required]
        public required string ServiceType { get; set; }

        [Required]
        public required string Address { get; set; }

        public string? Notes { get; set; }

        public DateTime PreferredDate { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}