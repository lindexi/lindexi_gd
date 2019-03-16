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
using MooperekemStalbo.Controllers;
using Newtonsoft.Json;

namespace CouwoSeajeryerdairMerlear
{
    class Program
    {
        static void Main(string[] args)
        {
            SenairjerecisBelnear();

            LaizanadesoDinesebe();


            //var fileInfo = new FileInfo("E:\\RevealHighlights.gif");

            //var fileStream = fileInfo.OpenRead();
            //string fileSha = Shafile.GetFile(fileStream);

            //Upload(fileStream, fileSha, "http://localhost:52074/api/GairKetemRairsems/UploadPackage").Wait();

            Console.Read();
        }

        private static async void LaizanadesoDinesebe()
        {
            var url = "http://localhost:5000/api/GairKetemRairsems/Download";

            var kebunerNeefunadrow = new KebunerNeefunadrow()
            {
                Name = "lindexi",
                Version = new Version("1.3.0").ToString()
            };

            var httpClient = new HttpClient();

            var json = JsonConvert.SerializeObject(kebunerNeefunadrow);

            var stringContent = new StringContent(json);
            stringContent.Headers.ContentType.MediaType = "application/json";

            var response = await httpClient.PostAsync(url, stringContent);

            var drehereposorrasCorxoustesaiyairal = await response.Content.ReadAsStringAsync();
            var chilusterfaVocerjoulel = JsonConvert.DeserializeObject<GemurboostatelnearseRurallnawrear>(drehereposorrasCorxoustesaiyairal);

            url = "http://localhost:5000" + chilusterfaVocerjoulel.File;
            var file = Path.GetTempFileName();
            using (var stream=new FileStream(file,FileMode.Open))
            {
                await (await httpClient.GetStreamAsync(url)).CopyToAsync(stream);
                if (Shafile.GetFile(stream)==chilusterfaVocerjoulel.Sha)
                {
                    stream.Seek(0, SeekOrigin.End);
                }
            }
            
        }

        private static void SenairjerecisBelnear()
        {
            var lirbehereTadriDruwhemLoser = new Package()
            {
                Name = "lindexi",
                Version = "1.0",
                RequirementMaxVersion = new Version(1, 5, 1).ToString(),
                RequirementMinVersion = new Version(1, 3, 0).ToString(),
                Author = "lindexi",
                File = "1.dll"
            };

            if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "HemfaKarecelRisvenaStishorrorjoo", "File")))
            {
                Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    "HemfaKarecelRisvenaStishorrorjoo", "File"));
            }

            var file = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "HemfaKarecelRisvenaStishorrorjoo", "Package.xml"));
            using (var fileStream = file.Create())
            using (var stream = new StreamWriter(fileStream))
            {
                using (var xmlWriter = XmlWriter.Create(stream, new XmlWriterSettings()
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
            }

            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "HemfaKarecelRisvenaStishorrorjoo", "File", "1.dll"), "林德熙逗比");

            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "ChemtowNalltruTusiwurhel.zip")))
            {
                File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    "ChemtowNalltruTusiwurhel.zip"));
            }

            ZipFile.CreateFromDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "HemfaKarecelRisvenaStishorrorjoo"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "ChemtowNalltruTusiwurhel.zip"));

            file = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "ChemtowNalltruTusiwurhel.zip"));

            using (var fileStream = file.OpenRead())
            {
                string fileSha = Shafile.GetFile(fileStream);

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