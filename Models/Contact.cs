using System.ComponentModel.DataAnnotations;

namespace WularItech_solutions.Models
{
    public class Contact
    {
        [Key]
        public int Id { get; set; }  // Primary key

        [Required(ErrorMessage = "Please enter your name")]
        [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Please enter your email")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [StringLength(150)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Please write your requirement or message")]
        [StringLength(500, ErrorMessage = "Message cannot be longer than 500 characters")]
        public string Message { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now; // Timestamp
    }
}