using System;

namespace AzureBlobImageHelper
{
    public class ContainerInfo
    {
      
        public string AzureStorageConnectionString { get; set; }
        public string BlobContainerName { get; set; }
        public ContainerInfo(string azureStorageConnectionString, string blobContainer)
        {
            if (string.IsNullOrEmpty(azureStorageConnectionString)) throw new ArgumentNullException("azureStorageConnectionString");
            if (string.IsNullOrEmpty(blobContainer)) throw new ArgumentNullException("blobContainer");
            AzureStorageConnectionString = azureStorageConnectionString;
            BlobContainerName = blobContainer;
        }

        
    }
}
