using Google.Cloud.Speech.V1;
using Google.Cloud.Storage.V1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfAppMusicPlayer.Utils
{
    public class CloudStorageHelper
    {
        private readonly StorageClient _storageClient;
        private readonly string _bucketName;

        public CloudStorageHelper()
        {
            _storageClient = StorageClient.Create();
            _bucketName = "storage-client-api";
        }

        public async Task<string> UploadAudioFileAsync(string localFilePath)
        {
            try
            {
                string objectName = Path.GetFileName(localFilePath);
                string storageUri = $"gs://{_bucketName}/{objectName}";

                using (var fileStream = File.OpenRead(localFilePath))
                {
                    await _storageClient.UploadObjectAsync(_bucketName, objectName.Replace(" ", ""), null, fileStream);
                }

                return storageUri;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading file to Cloud Storage: {ex.Message}");
                return null;
            }
        }
    }
}
