using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
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
            //RunVerifyOption(new VerifyOption()
            //{
            //    File = "HemfaKarecelRisvenaStishorrorjoo\\lindexi1.0.zip"
            //});

            //RunUploadOption(new UploadOption()
            //{
            //    File = "HemfaKarecelRisvenaStishorrorjoo\\lindexi1.0.zip",
            //});

            //RunDownloadOption(new DownloadOption()
            //{
            //    Package = "lindexi",
            //    Version = "5.1.12.63002",
            //    Output = Path.GetTempPath()
            //});

            Parser.Default.ParseArguments<SpecOption, PackOption, VerifyOption, DownloadOption, UploadOption>(args)
                .MapResult<SpecOption, PackOption, VerifyOption, DownloadOption, UploadOption, int>(RunSpecOption,
                    RunPackOption, RunVerifyOption, RunDownloadOption, RunUploadOption,
                    errorList => { return -1; });

            //SenairjerecisBelnear();

            //LaizanadesoDinesebe();


            //var fileInfo = new FileInfo("E:\\RevealHighlights.gif");

            //var fileStream = fileInfo.OpenRead();
            //string fileSha = Shafile.GetFile(fileStream);

            //Upload(fileStream, fileSha, "http://localhost:52074/api/GairKetemRairsems/UploadPackage").Wait();

            Console.Read();
        }

        private static int RunUploadOption(UploadOption upload)
        {
            Console.WriteLine("开始上传");

            if (string.IsNullOrEmpty(upload.File))
            {
                Console.WriteLine("上传的文件不能为空");
            }

            if (!File.Exists(upload.File))
            {
                Console.WriteLine("上传的文件不存在" + Path.GetFullPath(upload.File));
            }

            try
            {
                using (var fileStream = new FileStream(Path.GetFullPath(upload.File), FileMode.Open))
                {
                    string fileSha = Shafile.GetFile(fileStream);

                    Upload(fileStream, fileSha, Url + "/api/GairKetemRairsems/UploadPackage").Wait();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return 0;
        }

        private const string Url = "http://localhost:5000";

        private static int RunDownloadOption(DownloadOption downloadOption)
        {
            var package = downloadOption.Package;

            if (!Directory.Exists(downloadOption.Output))
            {
                Console.WriteLine("输出的文件夹" + downloadOption.Output + "不存在");
                return -1;
            }

            if (Version.TryParse(downloadOption.Version, out var _))
            {
                Console.WriteLine("开始下载");

                Console.WriteLine("下载" + package + "客户端版本" + downloadOption.Version);

                try
                {
                    Donwload(downloadOption).Wait();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            else
            {
                Console.WriteLine("传入的版本号无法解析");
                return -1;
            }

            return 0;
        }

        private static async Task Donwload(DownloadOption downloadOption)
        {
            var url = Url + "/api/GairKetemRairsems/Download";
            var httpClient = new HttpClient();

            var kebunerNeefunadrow = new KebunerNeefunadrow()
            {
                Name = downloadOption.Package,
                Version = downloadOption.Version
            };

            var json = JsonConvert.SerializeObject(kebunerNeefunadrow);

            var stringContent = new StringContent(json);
            stringContent.Headers.ContentType.MediaType = "application/json";

            var response = await httpClient.PostAsync(url, stringContent);

            var drehereposorrasCorxoustesaiyairal = await response.Content.ReadAsStringAsync();
            var chilusterfaVocerjoulel =
                JsonConvert.DeserializeObject<GemurboostatelnearseRurallnawrear>(drehereposorrasCorxoustesaiyairal);

            if (chilusterfaVocerjoulel == null)
            {
                Console.WriteLine("找不到可以下载的文件");
                return ;
            }

            url = Url + chilusterfaVocerjoulel.File;
            var file = Path.GetTempFileName();
            bool hawqelciyaihearKemladairheejaywer = false;
            using (var stream = new FileStream(file, FileMode.Open))
            {
                await (await httpClient.GetStreamAsync(url)).CopyToAsync(stream);
                if (Shafile.GetFile(stream) == chilusterfaVocerjoulel.Sha)
                {
                    stream.Seek(0, SeekOrigin.End);
                    hawqelciyaihearKemladairheejaywer = true;
                }
                else
                {
                    Console.WriteLine("校验不通过");
                    return;
                }
            }

            if (hawqelciyaihearKemladairheejaywer)
            {
                Console.WriteLine("下载成功");

                Console.WriteLine("开始移动文件");

                var fileName = downloadOption.Package + chilusterfaVocerjoulel.Version + ".zip";
                File.Copy(file, Path.Combine(downloadOption.Output, fileName));
                Console.WriteLine("下载完成");
            }
        }

        private static int RunVerifyOption(VerifyOption verifyOption)
        {
            if (string.IsNullOrEmpty(verifyOption.File))
            {
                Console.WriteLine("传入的文件不能为空");
                return -1;
            }

            if (!File.Exists(verifyOption.File))
            {
                Console.WriteLine("找不到" + verifyOption.File);
                return -1;
            }

            var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(folder);

            var medaltraFairjousuFowluNererisMoubeturce = new MedaltraFairjousuFowluNererisMoubeturce(
                new FileInfo(verifyOption.File), folder, "");

            try
            {
                if (medaltraFairjousuFowluNererisMoubeturce.CheckFile())
                {
                    Console.WriteLine("符合标准");
                }
                else
                {
                    Console.WriteLine("不符合标准");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return 0;
        }

        private static int RunPackOption(PackOption packOption)
        {
            Console.WriteLine("开始打包");
            var packageFile = new FileInfo("Package.xml");
            if (!packageFile.Exists)
            {
                Console.WriteLine("打包文件" + packageFile.FullName + "不存在");
                return -1;
            }

            Console.WriteLine("开始解析");

            try
            {
                var package = MedaltraFairjousuFowluNererisMoubeturce.ParseFile(packageFile.FullName);

                var regex = new Regex(@"^[a-zA-Z0-9]+$");

                if (string.IsNullOrEmpty(package.Name) || !regex.IsMatch(package.Name) ||
                    (package.Name[0] >= '0' && package.Name[0] <= '9'))
                {
                    throw new ArgumentException($"上传 package 的 Name 不符合规范");
                }

                if (!File.Exists("File\\" + package.File))
                {
                    Console.WriteLine("入口文件" + Path.GetFullPath("File\\" + package.File) + "不存在");
                    return -1;
                }

                var file = Path.GetTempFileName();
                File.Delete(file);
                ZipFile.CreateFromDirectory(Environment.CurrentDirectory, file);
                var moveFile = Path.Combine(Environment.CurrentDirectory, package.Name + package.Version + ".zip");
                if (File.Exists(moveFile))
                {
                    File.Delete(moveFile);
                }

                File.Move(file, moveFile);

                Console.WriteLine("完成");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return -1;
            }

            return 0;
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