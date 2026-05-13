using System.ComponentModel.DataAnnotations;

namespace WularItech_solutions.Models
{
    public class User
    {
        [Key]
        public Guid Id { get; set; }=Guid.NewGuid(); //autogenerate 
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }

        public bool IsAdmin { get; set; }  //by default false
    }
}
