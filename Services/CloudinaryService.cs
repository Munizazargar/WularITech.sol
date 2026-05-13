using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using WularItech_solutions.Configuration;
using WularItech_solutions.Interfaces;

namespace WularItech_solutions.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IOptions<CloudinarySettings> options)
        {

            var settings = options.Value
                           ?? throw new ArgumentNullException("Cloudinary settings are not configured");


            if (string.IsNullOrEmpty(settings.CloudName) ||
                string.IsNullOrEmpty(settings.ApiKey) ||
                string.IsNullOrEmpty(settings.ApiSecret))
                throw new ArgumentException("Cloudinary settings are missing required fields");
            Console.WriteLine("===== CLOUDINARY DEBUG =====");
            Console.WriteLine(settings.CloudName);
            Console.WriteLine(settings.ApiKey);
            Console.WriteLine(settings.ApiSecret);
            Console.WriteLine("SECRET LENGTH: " + settings.ApiSecret?.Length);
            Console.WriteLine("============================");

            var account = new Account(
                settings.CloudName,
                settings.ApiKey,
                settings.ApiSecret
            );

            _cloudinary = new Cloudinary(account)
            {
                Api = { Secure = true }
            };
        }

        public async Task<string> UploadImageAsync(IFormFile image, string folder = "")
        {
            if (image == null || image.Length == 0)
                throw new ArgumentNullException(nameof(image), "Image file is null or empty");

            if (_cloudinary == null)
                throw new Exception("Cloudinary instance is not initialized");

            using var stream = image.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(image.FileName, stream),
                Folder = string.IsNullOrEmpty(folder) ? null : folder,
                //   UseFilename = true,
                // UniqueFilename = true,
                Overwrite = false
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult == null || uploadResult.Error != null)
                throw new Exception(uploadResult?.Error?.Message ?? "Unknown Cloudinary error");

            return uploadResult.SecureUrl.ToString();
        }
    }
}
