using System.ComponentModel.DataAnnotations;

namespace WularItech_solutions.Models
{
    public class Technician
    {
        [Key]
        public Guid TechnicianId { get; set; } = Guid.NewGuid();

        [Required]
        public string FullName { get; set; }

        [Required]
        public string Phone { get; set; }

        [Required]
        public string Skill { get; set; } // e.g. "CCTV", "Plumbing", "Electrical"

        public string? Area { get; set; } // e.g. "Bandipora", "Sopore"

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}