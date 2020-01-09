using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace AzureBlobImageHelper
{

    public class Agent
    {
        private Microsoft.WindowsAzure.Storage.CloudStorageAccount CloudStorage;
        private Microsoft.WindowsAzure.Storage.Blob.CloudBlobClient CloudBlobClient;
        private Microsoft.WindowsAzure.Storage.Blob.CloudBlobContainer CloudBlobContainer;

        private static string SwapPath { get; set; }

        public Agent(ContainerInfo containerInfo)
        {
            if (containerInfo == null)
            {
                throw new ArgumentNullException("containerInfo");
            }

            CloudStorage = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(containerInfo.AzureStorageConnectionString);
            CloudBlobClient = CloudStorage.CreateCloudBlobClient();
            CloudBlobContainer = CloudBlobClient.GetContainerReference(containerInfo.BlobContainerName);
            var res = CloudBlobContainer.CreateIfNotExistsAsync().Result;

            SwapPath = AppDomain.CurrentDomain.BaseDirectory + "AzureBlobImageHelper_SwapData" + Path.DirectorySeparatorChar;
            System.IO.Directory.CreateDirectory(SwapPath);

        }

        public Agent(ContainerInfo containerInfo, string swapPath)
        {
            if (containerInfo == null)
            {
                throw new ArgumentNullException("containerInfo");
            }

            CloudStorage = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(containerInfo.AzureStorageConnectionString);
            CloudBlobClient = CloudStorage.CreateCloudBlobClient();
            CloudBlobContainer = CloudBlobClient.GetContainerReference(containerInfo.BlobContainerName);
            var res = CloudBlobContainer.CreateIfNotExistsAsync().Result;

            if (string.IsNullOrEmpty(swapPath))
            {
                SwapPath = AppDomain.CurrentDomain.BaseDirectory + "AzureBlobImageHelper_SwapData" + Path.DirectorySeparatorChar;
                System.IO.Directory.CreateDirectory(SwapPath);
            }
            else
            {
                if (!swapPath.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()))
                {
                    swapPath = swapPath + System.IO.Path.DirectorySeparatorChar.ToString();
                }
                SwapPath = swapPath + "AzureBlobImageHelper_SwapData" + Path.DirectorySeparatorChar;
                System.IO.Directory.CreateDirectory(SwapPath);
            }

        }


        /// <summary>
        /// 取得 blob image uri and permission parameter 
        /// 這邊沒有檢查檔案是不是存在，您得先自己檢查
        /// projecName/blobPath
        /// </summary>
        /// <param name="fileName">檔名</param>
        /// <param name="fileUri">out for image uri</param>
        /// <param name="signUriPara">out for image uri parameter</param>
        /// <param name="expireDateTime"></param>
        /// <param name="projectName"></param>
        /// <param name="blobPath"></param>
        public void GetUriAndPermission(string fileName, out string fileUri, out string signUriPara, DateTime expireDateTime, string projectName, string blobPath = "source/images")
        {

            Microsoft.WindowsAzure.Storage.Blob.CloudBlobDirectory cloudBlobDirectory =
                CloudBlobContainer.GetDirectoryReference(projectName + "/" + blobPath);


            var res = CloudBlobContainer.GetPermissionsAsync().Result;

            //    expireDateTime = DateTime.UtcNow.Date.AddDays(1).AddSeconds(-1);
            var sharedPolicy = new Microsoft.WindowsAzure.Storage.Blob.SharedAccessBlobPolicy()
            {
                SharedAccessExpiryTime = DateTime.SpecifyKind(expireDateTime, DateTimeKind.Unspecified),
                Permissions = Microsoft.WindowsAzure.Storage.Blob.SharedAccessBlobPermissions.Read,

            };
            signUriPara = CloudBlobContainer.GetSharedAccessSignature(sharedPolicy, null);

            fileUri = cloudBlobDirectory.GetBlockBlobReference(fileName).Uri.ToString();
        }


        /// <summary>
        /// 取的 某一張圖片得縮圖，如果檔案不存在，會自動產生新的縮圖且上傳，所以這呼叫時間會比較長
        /// </summary>
        /// <param name="id"></param>
        /// <param name="projectName"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="expireDateTime">if null for 2099/12/31</param>
        /// <returns></returns>
        public ImageBlobResult GetImageThumbFromSource(string id, string projectName, int w, int h, DateTime? expireDateTime)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException("id");
            if (string.IsNullOrEmpty(projectName)) throw new ArgumentNullException("projectName");
            if (!expireDateTime.HasValue)
            {
                expireDateTime = new DateTime(2099, 12, 31);
            }

            string url = "";
            string para = "";
            if (w == 0 && h == 0)
            {
                GetUriAndPermission(id + ".gif", out url, out para, expireDateTime.Value, projectName);
                return new ImageBlobResult { ExpireTime = expireDateTime.Value, Para = para, Url = url };
            }

            if (IsImageThumbExisted(id, projectName, w, h))
            {
                GetUriAndPermission(id + ".gif", out url, out para, expireDateTime.Value, projectName, "thumbs/" + w + "_" + h);
                return new ImageBlobResult { ExpireTime = expireDateTime.Value, Para = para, Url = url };
            }

            //Get Image From Source 
            var sourceImage = DownloadImageFromBlob(id + ".gif", projectName);

            var thumbHandler = new ImageUtility();
            //按照寬度，高度隨意
            if (w > 0 && h == 0)
            {
                var source2 = thumbHandler.MakeThumbnail(sourceImage, w, h, "W");
                var memStream = new MemoryStream();
                source2.Save(memStream, sourceImage.RawFormat);
                UpoloadImageToSource(id, memStream.ToArray(), projectName, "thumbs/" + w + "_" + h);
                sourceImage.Dispose();
                source2.Dispose();
                GetUriAndPermission(id + ".gif", out url, out para, expireDateTime.Value, projectName, "thumbs/" + w + "_" + h);
                return new ImageBlobResult { ExpireTime = expireDateTime.Value, Para = para, Url = url };
            }

            //按照高度，寬度隨意
            if (h > 0 && w == 0)
            {
                var source2 = thumbHandler.MakeThumbnail(sourceImage, w, h, "H");
                var memStream = new MemoryStream();
                source2.Save(memStream, sourceImage.RawFormat);
                UpoloadImageToSource(id, memStream.ToArray(), projectName, "thumbs/" + w + "_" + h);
                sourceImage.Dispose();
                source2.Dispose();
                GetUriAndPermission(id + ".gif", out url, out para, expireDateTime.Value, projectName, "thumbs/" + w + "_" + h);
                return new ImageBlobResult { ExpireTime = expireDateTime.Value, Para = para, Url = url };
            }

            //強制任性
            if (h > 0 && w > 0)
            {
                var source2 = thumbHandler.MakeThumbnail(sourceImage, w, h, "WH");
                var memStream = new MemoryStream();
                source2.Save(memStream, sourceImage.RawFormat);
                UpoloadImageToSource(id, memStream.ToArray(), projectName, "thumbs/" + w + "_" + h);
                sourceImage.Dispose();
                source2.Dispose();
                GetUriAndPermission(id + ".gif", out url, out para, expireDateTime.Value, projectName, "thumbs/" + w + "_" + h);
                return new ImageBlobResult { ExpireTime = expireDateTime.Value, Para = para, Url = url };
            }




            return null;
        }


        /// <summary>
        /// 從 blob 上面下載圖片 轉成 Image 物件後給你
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="projectName"></param>
        /// <param name="blobPath"></param>
        /// <returns></returns>
        public Image DownloadImageFromBlob(string fileName, string projectName, string blobPath = "source/images")
        {
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException("fileName");
            if (string.IsNullOrEmpty(projectName)) throw new ArgumentNullException("projectName");


            Microsoft.WindowsAzure.Storage.Blob.CloudBlobDirectory cloudBlobDirectory =
                CloudBlobContainer.GetDirectoryReference(projectName + "/" + blobPath);

            Directory.CreateDirectory(SwapPath + projectName + Path.DirectorySeparatorChar);

            var memStream = new MemoryStream();
            cloudBlobDirectory.GetBlockBlobReference(fileName).DownloadToStreamAsync(memStream).GetAwaiter().GetResult();
            return Image.FromStream(memStream);

        }


        /// <summary>
        /// 檢查 blob 上面檔案是不是存在
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="projectName"></param>
        /// <param name="blobPath"></param>
        /// <returns></returns>
        protected bool IsFileExisted(string fileName, string projectName, string blobPath = "source/images")
        {
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException("fileName");
            if (string.IsNullOrEmpty(projectName)) throw new ArgumentNullException("projectName");

            Microsoft.WindowsAzure.Storage.Blob.CloudBlobDirectory cloudBlobDirectory =
                CloudBlobContainer.GetDirectoryReference(projectName + "/" + blobPath);
            return cloudBlobDirectory.GetBlockBlobReference(fileName).ExistsAsync().Result;
        }


        /// <summary>
        ///  檢查圖片 source 是否存在
        /// </summary>
        /// <param name="id"></param>
        /// <param name="projectName"></param>
        /// <returns></returns>
        public bool IsImageSourceExisted(string id, string projectName)
        {
            return IsFileExisted(id + ".gif", projectName);
        }


        public bool IsImageInfoExisted(string id, string projectName)
        {
            return IsFileExisted(id + ".json", projectName, "source/info");
        }

        /// <summary>
        /// 檢查該尺寸縮圖是不是存在
        /// </summary>
        /// <param name="id"></param>
        /// <param name="projectName"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        public bool IsImageThumbExisted(string id, string projectName, int w, int h)
        {
            if (w == 0 && h == 0)
            {
                return IsFileExisted(id + ".gif", projectName);
            }
            return IsFileExisted(id + ".gif", projectName, "thumbs/" + w + "_" + h);
        }

        /// <summary>
        /// 上傳圖片 info 去 blob
        /// </summary>
        /// <param name="imageInfo"></param>
        /// <param name="projectName"></param>
        /// <param name="filePath"></param>
        public void UpoloadImageInfo(ImageInfo imageInfo, string projectName, string filePath = "source/info/")
        {
            if (string.IsNullOrEmpty(projectName)) throw new ArgumentNullException("projectName");
            if (imageInfo == null) throw new ArgumentNullException("imageInfo");
            if (string.IsNullOrEmpty(imageInfo.Id)) throw new ArgumentNullException("imageInfo.Id");

            Microsoft.WindowsAzure.Storage.Blob.CloudBlobDirectory cloudBlobDirectory =
               CloudBlobContainer.GetDirectoryReference(projectName + "/" + filePath);

            var bFileInfo = cloudBlobDirectory.GetBlockBlobReference(imageInfo.Id + ".json");
            bFileInfo.Properties.ContentType = "application/json; charset=utf-8";
            bFileInfo.UploadTextAsync(JsonConvert.SerializeObject(imageInfo)).GetAwaiter().GetResult();

        }


        /// <summary>
        /// 上傳圖片至 blob 上面的 source 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="localImagePath">本機檔案圖片位置</param>
        /// <param name="projectName"></param>
        /// <param name="filePath"></param>
        public void UpoloadImageToSource(string id, string localImagePath, string projectName, string filePath = "source/images/")
        {
            if (string.IsNullOrEmpty(localImagePath)) throw new ArgumentNullException("localImagePath");
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException("id");
            if (string.IsNullOrEmpty(projectName)) throw new ArgumentNullException("projectName");

            Microsoft.WindowsAzure.Storage.Blob.CloudBlobDirectory cloudBlobDirectory =
                 CloudBlobContainer.GetDirectoryReference(projectName + "/" + filePath);

            var bFileInfo = cloudBlobDirectory.GetBlockBlobReference(id + ".gif");

            bFileInfo.Properties.ContentType = "image/gif";
            bFileInfo.UploadFromFileAsync(localImagePath).GetAwaiter().GetResult();

        }

        /// <summary>
        /// 上傳圖片至 blob 上面的 source 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="imageBytes">圖片檔案的 byte[]</param>
        /// <param name="projectName"></param>
        /// <param name="filePath"></param>
        public void UpoloadImageToSource(string id, byte[] imageBytes, string projectName, string filePath = "source/images/")
        {
            if (imageBytes == null) throw new ArgumentNullException("imageBytes");
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException("id");
            if (string.IsNullOrEmpty(projectName)) throw new ArgumentNullException("projectName");


            Microsoft.WindowsAzure.Storage.Blob.CloudBlobDirectory cloudBlobDirectory =
                 CloudBlobContainer.GetDirectoryReference(projectName + "/" + filePath);

            var bFileInfo = cloudBlobDirectory.GetBlockBlobReference(id + ".gif");

            bFileInfo.Properties.ContentType = "image/gif";
            bFileInfo.UploadFromByteArrayAsync(imageBytes, 0, imageBytes.Length).GetAwaiter().GetResult();

        }


        /// <summary>
        /// 刪除 blob 上面的 image source
        /// </summary>
        /// <param name="id"></param>
        /// <param name="projectName"></param>
        /// <param name="isDeleteAllThumbs"></param>
        /// <param name="isDeleteInfo"></param>
        public void DeleteImageFromSource(string id, string projectName, bool isDeleteAllThumbs = true, bool isDeleteInfo = true)
        {
            CloudBlobContainer.GetDirectoryReference(projectName + "/source/images/").GetBlockBlobReference(id + ".gif").DeleteIfExistsAsync().GetAwaiter().GetResult();

            if (isDeleteAllThumbs)
            {
                DeleteAllThumbById(id, projectName);
            }

            if (isDeleteInfo)
            {
                DeleteInfoById(id, projectName);
            }


        }


        /// <summary>
        /// 刪掉 blob 上面的 image info.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="projectName"></param>
        public void DeleteInfoById(string id, string projectName)
        {
            CloudBlobContainer.GetDirectoryReference(projectName + "/source/info/").GetBlockBlobReference(id + ".json").DeleteIfExistsAsync().GetAwaiter().GetResult();

        }

        /// <summary>
        /// 透過 image id  刪除所有縮圖 可能時間會比較長
        /// </summary>
        /// <param name="id"></param>
        /// <param name="projectName"></param>
        public void DeleteAllThumbById(string id, string projectName)
        {

            Microsoft.WindowsAzure.Storage.Blob.CloudBlobDirectory cloudBlobDirectory =
                        CloudBlobContainer.GetDirectoryReference(projectName + "/thumbs");

            var dirs = new List<string>();

            Microsoft.WindowsAzure.Storage.Blob.BlobContinuationToken continuationToken = null;
            do
            {

                var listingResult = cloudBlobDirectory.ListBlobsSegmentedAsync(continuationToken).Result;
                continuationToken = listingResult.ContinuationToken;
                dirs.AddRange(listingResult.Results.Where(x => x as Microsoft.WindowsAzure.Storage.Blob.CloudBlobDirectory != null).Select(x => x.Uri.Segments.Last()).ToList());
            }
            while (continuationToken != null);


            foreach (var dir in dirs)
            {
                try
                {
                    CloudBlobContainer.GetDirectoryReference(projectName + "/thumbs/" + dir).GetBlockBlobReference(id + ".gif").DeleteIfExistsAsync().GetAwaiter().GetResult();
                }
                catch
                {
                    continue;
                }
            }

            GC.Collect();
        }

    }
}
