using System.ComponentModel.DataAnnotations;

namespace WularItech_solutions.Models
{
    public class PendingRegistration
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string PasswordHash { get; set; }
        public required string Otp { get; set; }
        public DateTime OtpExpiry { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}