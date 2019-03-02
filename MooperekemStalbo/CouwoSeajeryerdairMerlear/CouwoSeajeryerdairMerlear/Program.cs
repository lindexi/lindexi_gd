using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using MooperekemStalbo;
using Newtonsoft.Json;

namespace CouwoSeajeryerdairMerlear
{
    class Program
    {
        static void Main(string[] args)
        {
            SenairjerecisBelnear();

            var url = "http://localhost:5000/api/GairKetemRairsems/Download";

            var kebunerNeefunadrow = new KebunerNeefunadrow()
            {
                Name = "lindexi",
                Version = new Version("5.1.2").ToString()
            };

            var httpClient = new HttpClient();

            var json = JsonConvert.SerializeObject(kebunerNeefunadrow);

            var stringContent = new StringContent(json);
            stringContent.Headers.ContentType.MediaType = "application/json";

            var response = httpClient.PostAsync(url, stringContent).Result;


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


        private static void SenairjerecisBelnear()
        {
            var lirbehereTadriDruwhemLoser = new Package()
            {
                Name = "lindexi",
                Version = "1.0",
                RequirementMaxVersion = new Version(1, 2, 1).ToString(),
                RequirementMinVersion = new Version(1, 1, 0).ToString(),
                Author = "lindexi",
                File = "1.dll"
            };

            var str = new StringBuilder();

            using (var xmlWriter = XmlWriter.Create(str, new XmlWriterSettings()
            {
                Encoding = Encoding.UTF8,
                NewLineChars = "    ",
                NewLineHandling = NewLineHandling.Replace,
                Indent = true,
                IndentChars = "\n",
            }))
            {
                var xmlSerializer = new XmlSerializer(typeof(Package));
                xmlSerializer.Serialize(xmlWriter, lirbehereTadriDruwhemLoser);
            }

            if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "HemfaKarecelRisvenaStishorrorjoo")))
            {
                Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    "HemfaKarecelRisvenaStishorrorjoo"));
            }

            var file = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "HemfaKarecelRisvenaStishorrorjoo", "Package.xml"));
            using (var fileStream = file.Create())
            using (var stream = new StreamWriter(fileStream))
            {
                stream.WriteLine(str.ToString());
            }

            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "ChemtowNalltruTusiwurhel.zip")))
            {
                File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    "ChemtowNalltruTusiwurhel.zip"));
            }

            ZipFile.CreateFromDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "HemfaKarecelRisvenaStishorrorjoo"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "ChemtowNalltruTusiwurhel.zip"));

            Console.WriteLine(str.ToString());

            file = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "ChemtowNalltruTusiwurhel.zip"));

            using (var fileStream = file.OpenRead())
            {
                string fileSha;
                using (var sha = SHA256.Create())
                {
                    fileSha = Convert.ToBase64String(sha.ComputeHash(fileStream));

                    fileStream.Seek(0, SeekOrigin.Begin);
                }

                Upload(fileStream, fileSha, "http://localhost:5000/api/GairKetemRairsems/UploadPackage").Wait();
            }
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