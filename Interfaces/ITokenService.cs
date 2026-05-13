using WularItech_solutions.Models; 
namespace WularItech_solutions.Interfaces
{
    public interface ITokenService
    {
        public  string CreateToken(User user);
        Guid VerifyTokenAndGetId(string token);

         bool IsAdmin(string token);
    }
}
