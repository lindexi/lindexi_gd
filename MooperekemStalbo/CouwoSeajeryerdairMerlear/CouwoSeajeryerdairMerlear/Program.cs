using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace CouwoSeajeryerdairMerlear
{
    class Program
    {
        static void Main(string[] args)
        {
            var fileInfo = new FileInfo("E:\\RevealHighlights.gif");

            var fileStream = fileInfo.OpenRead();
            string fileSha;
            using (var sha = SHA256.Create())
            {
                fileSha = Convert.ToBase64String(sha.ComputeHash(fileStream));

                fileStream.Seek(0, SeekOrigin.Begin);
            }

            Upload(fileStream, fileSha, "http://localhost:52074/api/GairKetemRairsems/UploadPackage").Wait();

            Console.Read();
        }

        private static async Task Upload(FileStream fileStream, string sha, string url)
        {
            var httpClient = new HttpClient();
            var multipartFormDataContent = new MultipartFormDataContent();
            multipartFormDataContent.Add(new StreamContent(fileStream), "File", fileName: "文件名.png");
            multipartFormDataContent.Add(new StringContent(sha), "Sha");

            await httpClient.PostAsync(url, multipartFormDataContent);
        }
    }
}