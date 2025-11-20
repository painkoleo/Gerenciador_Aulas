using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using GerenciadorAulas.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GerenciadorAulas.Services
{
    public class GoogleDriveService : ICloudStorageService
    {
        private static readonly string[] Scopes = { DriveService.Scope.DriveFile };
        private static readonly string ApplicationName = "Gerenciador de Aulas";
        private static readonly string AppFolderName = "GerenciadorDeAulas_Backups";

        private UserCredential? _credential;

        public async Task AuthenticateAsync()
        {
            if (_credential != null && !_credential.Token.IsStale) return;

            using (var stream = new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GerenciadorAulas", "token.json");

                _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true));
            }
        }

        public async Task UploadBackupAsync(string localFilePath, string fileName)
        {
            await AuthenticateAsync();

            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = _credential,
                ApplicationName = ApplicationName,
            });

            string folderId = await GetOrCreateAppFolderAsync(service);

            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = fileName,
                Parents = new[] { folderId }
            };

            FilesResource.CreateMediaUpload request;
            using (var stream = new FileStream(localFilePath, FileMode.Open))
            {
                request = service.Files.Create(fileMetadata, stream, "application/zip");
                request.Fields = "id";
                await request.UploadAsync();
            }
        }

        public async Task<IEnumerable<CloudFile>> ListBackupsAsync()
        {
            await AuthenticateAsync();
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = _credential,
                ApplicationName = ApplicationName,
            });

            string folderId = await GetOrCreateAppFolderAsync(service);

            var listRequest = service.Files.List();
            listRequest.Q = $"'{folderId}' in parents and trashed=false";
            listRequest.Fields = "files(id, name)";
            listRequest.OrderBy = "createdTime desc";
            var files = await listRequest.ExecuteAsync();

            return files.Files.Select(f => new CloudFile { Id = f.Id, Name = f.Name });
        }

        public async Task DownloadBackupAsync(string fileId, string destinationPath)
        {
            await AuthenticateAsync();
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = _credential,
                ApplicationName = ApplicationName,
            });

            var request = service.Files.Get(fileId);
            using (var stream = new FileStream(destinationPath, FileMode.Create))
            {
                await request.DownloadAsync(stream);
            }
        }

        private async Task<string> GetOrCreateAppFolderAsync(DriveService service)
        {
            var listRequest = service.Files.List();
            listRequest.Q = $"mimeType='application/vnd.google-apps.folder' and trashed=false and name='{AppFolderName}'";
            listRequest.Fields = "files(id, name)";
            var files = await listRequest.ExecuteAsync();

            if (files.Files.Any())
            {
                return files.Files.First().Id;
            }
            else
            {
                var folderMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = AppFolderName,
                    MimeType = "application/vnd.google-apps.folder"
                };
                var request = service.Files.Create(folderMetadata);
                request.Fields = "id";
                var folder = await request.ExecuteAsync();
                return folder.Id;
            }
        }
    }
}
