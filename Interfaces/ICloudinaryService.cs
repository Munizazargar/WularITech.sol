using Microsoft.AspNetCore.Http;

namespace WularItech_solutions.Interfaces
{
    public interface ICloudinaryService
    {
        Task<string> UploadImageAsync(IFormFile image, string folder);
     
    }
}
