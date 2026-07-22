using KHDMA.Application.Common;
using KHDMA.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KHDMA.Infrastructure.Services
{
    /// <summary>
    /// Stores uploads under <c>wwwroot/uploads</c> and serves them as static files.
    /// </summary>
    /// <remarks>
    /// Registered in place of <see cref="BlobStorageService"/>, whose methods all
    /// throw <c>NotImplementedException</c> while still being wired into DI - so
    /// every upload was a 500 with no Azure account configured.
    ///
    /// This adds the validation SRS 10.3 requires and that none of the three
    /// hand-rolled SaveFileAsync copies in AuthService / ProfileService /
    /// AdminServiceService performed: an extension whitelist and a size cap.
    /// Without them, uploading a .aspx or .js file into a statically-served
    /// directory is an obvious attack path.
    /// </remarks>
    public class LocalFileStorageService : IBlobStorageService
    {
        private const string UploadsFolder = "uploads";

        private readonly string _webRootPath;
        private readonly FileUploadSettings _settings;
        private readonly ILogger<LocalFileStorageService> _logger;

        public LocalFileStorageService(
            IOptions<FileUploadSettings> settings,
            ILogger<LocalFileStorageService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
            _webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            if (fileStream is null || fileStream.Length == 0)
                throw new InvalidOperationException("The uploaded file is empty.");

            if (fileStream.Length > _settings.MaxSizeBytes)
                throw new InvalidOperationException(
                    $"File exceeds the {_settings.MaxSizeBytes / (1024 * 1024)} MB limit.");

            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            if (string.IsNullOrEmpty(extension) ||
                !_settings.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"File type '{extension}' is not allowed. Permitted: {string.Join(", ", _settings.AllowedExtensions)}");
            }

            // The stored name is generated, never taken from the request: a client
            // filename of "../../appsettings.json" would otherwise escape the folder.
            var storedName = $"{Guid.NewGuid():N}{extension}";

            var directory = Path.Combine(_webRootPath, UploadsFolder);
            Directory.CreateDirectory(directory);

            var fullPath = Path.Combine(directory, storedName);

            await using (var target = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write))
            {
                if (fileStream.CanSeek) fileStream.Position = 0;
                await fileStream.CopyToAsync(target);
            }

            _logger.LogInformation("Stored upload {StoredName} ({Bytes} bytes)", storedName, fileStream.Length);

            // Root-relative so it works across environments without a hardcoded host.
            return $"/{UploadsFolder}/{storedName}";
        }

        public Task DeleteFileAsync(string fileName)
        {
            var safeName = Path.GetFileName(fileName);   // strips any traversal
            var fullPath = Path.Combine(_webRootPath, UploadsFolder, safeName);

            if (File.Exists(fullPath)) File.Delete(fullPath);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Local files are served directly by the static-file middleware, so the
        /// stored path is already the URL. Nothing is signed - this is the tradeoff
        /// of local storage versus blob storage with SAS tokens.
        /// </summary>
        public string GetPresignedUrl(string fileName)
            => $"/{UploadsFolder}/{Path.GetFileName(fileName)}";
    }
}
