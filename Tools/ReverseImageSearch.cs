﻿using ImgComparer.Model;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

namespace ImgComparer.Tools
{
    public static class ReverseImageSearch
    {
        public static string[] GetSearchUrl(Image image)
        {
            string connectionString = Properties.Settings.Default.ImageBlob;
            if (connectionString == string.Empty)
                return null;

            CloudStorageAccount account = CloudStorageAccount.Parse(connectionString);
            CloudBlobClient blobClient = account.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("images");
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(image.Filename);
            if (!blockBlob.Exists())
                blockBlob.UploadFromFile(image.path);
            string url = blockBlob.Uri.AbsoluteUri;
            return new string[]
            {
                $"https://www.google.com/searchbyimage?image_url={url}",
                $"https://yandex.com/images/search?source=collections&&url={url}&rpt=imageview"
            };
        }

        public static bool Test(string connectionString)
        {
            try
            {
                CloudStorageAccount account = CloudStorageAccount.Parse(connectionString);
                CloudBlobClient blobClient = account.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference("images");
                return container != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
