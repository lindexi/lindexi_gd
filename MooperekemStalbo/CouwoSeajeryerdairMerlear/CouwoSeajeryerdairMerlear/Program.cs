using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using CommandLine;
using MooperekemStalbo;
using MooperekemStalbo.Controllers;
using Newtonsoft.Json;

namespace CouwoSeajeryerdairMerlear
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<SpecOption>(args).MapResult(RunSpecOption, errorList => { return -1; });

            //SenairjerecisBelnear();

            //LaizanadesoDinesebe();


            //var fileInfo = new FileInfo("E:\\RevealHighlights.gif");

            //var fileStream = fileInfo.OpenRead();
            //string fileSha = Shafile.GetFile(fileStream);

            //Upload(fileStream, fileSha, "http://localhost:52074/api/GairKetemRairsems/UploadPackage").Wait();

            Console.Read();
        }

        private static int RunSpecOption(SpecOption specOption)
        {
            string packageId;
            if (string.IsNullOrEmpty(specOption.PackageID))
            {
                packageId = "lindexi";
                Console.WriteLine("没有找到 PackageID 使用默认");
            }
            else
            {
                packageId = specOption.PackageID;
                Console.WriteLine("PackageID " + specOption.PackageID);
            }

            Console.WriteLine("开始创建文件");
            Console.WriteLine("工作文件夹" + Environment.CurrentDirectory);

            var str = $@"<?xml version=""1.0"" encoding=""utf-8""?>    
<Package xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">  
    <!-- 当前插件名，每个插件的名都需要唯一  -->
    <Name>{packageId}</Name>    
    <!-- 插件的版本 -->
    <Version>1.0</Version>    
    <!-- 插件的作者 -->
    <Author>lindexi</Author>    
    <!-- 插件的入口 dll 文件 -->
    <File>{packageId}.dll</File>    
    <!-- 要求的客户端最低版本 -->
    <RequirementMinVersion>5.1.12.63002</RequirementMinVersion>    
    <!-- 要求的客户端最高版本，要求大于等于最低版本，小于最高版本 -->
    <RequirementMaxVersion>5.1.12.70000</RequirementMaxVersion>    
</Package>";

            File.WriteAllText("Package.xml", str, Encoding.UTF8);

            Console.WriteLine("创建完成");

            return 0;
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
            var chilusterfaVocerjoulel =
                JsonConvert.DeserializeObject<GemurboostatelnearseRurallnawrear>(drehereposorrasCorxoustesaiyairal);

            url = "http://localhost:5000" + chilusterfaVocerjoulel.File;
            var file = Path.GetTempFileName();
            using (var stream = new FileStream(file, FileMode.Open))
            {
                await (await httpClient.GetStreamAsync(url)).CopyToAsync(stream);
                if (Shafile.GetFile(stream) == chilusterfaVocerjoulel.Sha)
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