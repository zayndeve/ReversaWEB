using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ReversaWEB.Core.Utils
{
    public static class FileUploader
    {
        /// <summary>
        /// Saves the uploaded file to the given subfolder under /uploads/
        /// and returns the generated filename (UUID + extension).
        /// </summary>
        public static async Task<string> SaveFileAsync(IFormFile file, string address)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or invalid.");

            // Ensure wwwroot/uploads/<address> directory exists (so files are served as static assets)
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", address);
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            // Create unique name (UUID + original extension)
            var extension = Path.GetExtension(file.FileName);
            var randomName = $"{Guid.NewGuid()}{extension}";

            // Full path
            var filePath = Path.Combine(uploadPath, randomName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return randomName; // return only filename (to store in DB)
        }
    }
}
