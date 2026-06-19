using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WularItech_solutions.Models
{
    public class Booking
    {
        [Key]
        public Guid BookingId { get; set; } = Guid.NewGuid();

        // Add inside Booking.cs
        public Guid? TechnicianId { get; set; }  // nullable — not always assigned

        [ForeignKey("TechnicianId")]
        public Technician? Technician { get; set; }

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