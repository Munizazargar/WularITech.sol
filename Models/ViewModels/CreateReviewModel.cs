using System.ComponentModel.DataAnnotations;

namespace WularItech_solutions.ViewModels
{
    public class CreateReviewViewModel
    {
        [Required]
        public Guid BookingId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Please select a rating between 1 and 5")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Please write a short comment")]
        [MaxLength(500)]
        public required string Comment { get; set; }
    }
}