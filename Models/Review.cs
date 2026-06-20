using System.ComponentModel.DataAnnotations;

namespace WularItech_solutions.Models
{
    public class Review
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid BookingId { get; set; }
        public Guid UserId { get; set; }

        public required string CustomerName { get; set; }
        public required string ServiceType { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        public required string Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}