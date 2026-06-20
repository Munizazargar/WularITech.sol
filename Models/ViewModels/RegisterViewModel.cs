using System.ComponentModel.DataAnnotations;

namespace WularItech_solutions.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        public required string Username { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        public required string Password { get; set; }
    }

    public class VerifyOtpViewModel
    {
        [Required]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Please enter the OTP")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits")]
        public required string Otp { get; set; }
    }
}