using Azure.Storage.Blobs;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Web;

namespace FarmTrack.Services
{
    public class BlobService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName = "farmblob";

        public BlobService(string connectionString)
        {
            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        public async Task<string> UploadFileAsync(HttpPostedFileBase file)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient(Path.GetFileName(file.FileName));
            using (var stream = file.InputStream)
            {
                await blobClient.UploadAsync(stream, overwrite: true);
            }

            return blobClient.Uri.ToString();
        }

        public async Task<string> UploadQrCodeAsync(Bitmap qrBitmap, string fileName)
        {
            using (var stream = new MemoryStream())
            {
                qrBitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Position = 0;
                return await UploadStreamAsync(stream, fileName);
            }
        }

        private async Task<string> UploadStreamAsync(Stream stream, string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient(fileName);
            await blobClient.UploadAsync(stream, overwrite: true);

            return blobClient.Uri.ToString();
        }

    }
}
