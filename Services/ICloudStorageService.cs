using GerenciadorAulas.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GerenciadorAulas.Services
{
    public interface ICloudStorageService
    {
        Task AuthenticateAsync();
        Task UploadBackupAsync(string localFilePath, string fileName);
        Task<IEnumerable<CloudFile>> ListBackupsAsync();
        Task DownloadBackupAsync(string fileId, string destinationPath);
    }

    public class StubCloudStorageService : ICloudStorageService
    {
        public Task AuthenticateAsync() => Task.CompletedTask;
        public Task UploadBackupAsync(string localFilePath, string fileName) => Task.CompletedTask;
        public Task<IEnumerable<CloudFile>> ListBackupsAsync() => Task.FromResult(Enumerable.Empty<CloudFile>());
        public Task DownloadBackupAsync(string fileId, string destinationPath) => Task.CompletedTask;
    }
}
