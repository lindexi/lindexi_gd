using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace MooperekemStalbo.Controllers
{
    public class MedaltraFairjousuFowluNererisMoubeturce
    {
        public Package CheckFile(FileInfo file)
        {
            // 如果文件夹不为空
            if (!Directory.Exists(Folder))
            {
                Folder = Path.GetTempPath();
            }

            var folder = Path.Combine(Folder, "temp", Guid.NewGuid().ToString());
            ZipFile.ExtractToDirectory(file.FullName, folder);

            CheckPackage(folder);

            return _package;
        }

        internal void CheckPackage(string folder)
        {
            var file = Path.Combine(folder, "Package.xml");
            if (!File.Exists(file))
            {
                throw new ArgumentException("找不到 Package.xml 文件");
            }

            var package = ParseFile(file);

            if (string.IsNullOrEmpty(package.Name) || !_regex.IsMatch(package.Name) ||
                (package.Name[0] >= '0' && package.Name[0] <= '9'))
            {
                throw new ArgumentException($"上传 package 的 Name 不符合规范");
            }

            // 判断文件存在
            if (!File.Exists(Path.Combine(folder, "File", package.File)))
            {
                throw new ArgumentException($"找不到文件 package file {package.File}");
            }

            // 判断 Name 是否对的

            if (_regex == null)
            {
                _regex = new Regex(@"^[a-zA-Z0-9]+$", RegexOptions.Compiled);
            }

            _package = package;
        }

        private Package _package;

        private static Regex _regex;

        private Package ParseFile(string file)
        {
            var xmlSerializer = new XmlSerializer(typeof(Package));
            using (var stream = new FileStream(file, FileMode.Open))
            {
                var read = new XmlTextReader(new StreamReader(stream, Encoding.UTF8));

                return (Package) xmlSerializer.Deserialize(read);
            }
        }

        public string Folder { get; set; }
    }
}