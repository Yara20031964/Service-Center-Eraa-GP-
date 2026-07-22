using Azure.Storage.Blobs;
using KHDMA.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace KHDMA.Infrastructure.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobContainerClient _containerClient;

        public BlobStorageService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureBlobStorage:ConnectionString"];
            var containerName = configuration["AzureBlobStorage:ContainerName"];
            _containerClient = new BlobContainerClient(connectionString, containerName);
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            // TODO: implement
            throw new NotImplementedException();
        }

        public async Task DeleteFileAsync(string fileName)
        {
            // TODO: implement
            throw new NotImplementedException();
        }

        public string GetPresignedUrl(string fileName)
        {
            // TODO: implement
            throw new NotImplementedException();
        }
    }
}
