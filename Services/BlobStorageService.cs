using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Web;

namespace FarmTrack.Services
{
    public class BlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName = "product-images";

        public BlobStorageService(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                System.Diagnostics.Debug.WriteLine("⚠️ WARNING: Azure Blob Storage connection string is NULL!");
                // Don't throw error - let it fail gracefully
            }
            else
            {
                _blobServiceClient = new BlobServiceClient(connectionString);
                System.Diagnostics.Debug.WriteLine("✅ Blob Storage Service initialized");
            }
        }

        public async Task<string> UploadImageAsync(HttpPostedFileBase imageFile, string productName)
        {
            if (imageFile == null || imageFile.ContentLength == 0)
            {
                System.Diagnostics.Debug.WriteLine("❌ No image file provided");
                return null;
            }

            if (_blobServiceClient == null)
            {
                System.Diagnostics.Debug.WriteLine("❌ Blob service client is NULL - check connection string!");
                return null;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"📤 Uploading image: {imageFile.FileName} ({imageFile.ContentLength} bytes)");

                // Get container client and create if not exists
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

                System.Diagnostics.Debug.WriteLine($"✅ Container '{_containerName}' ready");

                // Generate unique blob name
                var fileName = GenerateFileName(productName, Path.GetExtension(imageFile.FileName));
                var blobClient = containerClient.GetBlobClient(fileName);

                System.Diagnostics.Debug.WriteLine($"📝 Generated filename: {fileName}");

                // Reset stream position
                imageFile.InputStream.Position = 0;

                // Upload the file
                using (var stream = imageFile.InputStream)
                {
                    await blobClient.UploadAsync(stream, new BlobHttpHeaders
                    {
                        ContentType = imageFile.ContentType
                    });
                }

                var imageUrl = blobClient.Uri.ToString();
                System.Diagnostics.Debug.WriteLine($"✅ Image uploaded successfully: {imageUrl}");

                // Return the blob URL
                return imageUrl;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Blob storage upload error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        public async Task<bool> DeleteImageAsync(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return true;

            if (_blobServiceClient == null)
            {
                System.Diagnostics.Debug.WriteLine("❌ Cannot delete - Blob service client is NULL");
                return false;
            }

            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobName = Path.GetFileName(new Uri(imageUrl).LocalPath);
                var blobClient = containerClient.GetBlobClient(blobName);

                bool deleted = await blobClient.DeleteIfExistsAsync();

                if (deleted)
                    System.Diagnostics.Debug.WriteLine($"✅ Deleted image: {blobName}");
                else
                    System.Diagnostics.Debug.WriteLine($"⚠️ Image not found: {blobName}");

                return deleted;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Blob storage delete error: {ex.Message}");
                return false;
            }
        }

        private string GenerateFileName(string productName, string extension)
        {
            var cleanName = System.Text.RegularExpressions.Regex.Replace(productName ?? "product", @"[^a-zA-Z0-9_-]", "");
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            return $"{cleanName.ToLower()}-{timestamp}-{random}{extension}";
        }

        public async Task<string> UpdateImageAsync(HttpPostedFileBase newImageFile, string oldImageUrl, string productName)
        {
            // Delete old image if exists
            if (!string.IsNullOrEmpty(oldImageUrl) && oldImageUrl.Contains("blob.core.windows.net"))
            {
                await DeleteImageAsync(oldImageUrl);
            }

            // Upload new image
            return await UploadImageAsync(newImageFile, productName);
        }
    }
}