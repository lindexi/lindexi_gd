using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace MooperekemStalbo.Controllers
{
    public class MedaltraFairjousuFowluNererisMoubeturce
    {
        /// <inheritdoc />
        public MedaltraFairjousuFowluNererisMoubeturce(FileInfo file, string folder, string fileSha)
        {
            _file = file;
            Folder = folder;
            _fileSha = fileSha;
            if (!Directory.Exists(folder))
            {
                throw new ArgumentException("不能传入不存在文件夹");
            }
        }

        public bool CheckFile()
        {
            var file = _file;

            var folder = Path.Combine(Folder, "temp", Guid.NewGuid().ToString());
            ZipFile.ExtractToDirectory(file.FullName, folder);

            CheckPackage(folder);

            Directory.Delete(folder, true);

            return true;
        }


        public string MoveFile()
        {
            var fileSha = _fileSha;
            var folder = Path.Combine(Folder, fileSha.Substring(0, 2), fileSha.Substring(2, 2),
                fileSha.Substring(2 + 2));
            Directory.CreateDirectory(folder);
            var file = Path.Combine(folder, _package.Name + ".zip");
            _file.MoveTo(file);
            return GetRelativePath(file, Folder);
        }

        private string GetRelativePath(string filespec, string folder)
        {
            // 计算相对路径
            var file = filespec.Replace(folder, "");
            if (file.StartsWith("\\") || file.StartsWith("/"))
            {
                return file.Substring(1);
            }

            return file;
        }

        internal void CheckPackage(string folder)
        {
            var file = Path.Combine(folder, "Package.xml");
            if (!File.Exists(file))
            {
                throw new ArgumentException("找不到 Package.xml 文件");
            }

            var package = ParseFile(file);

            // 判断 Name 是否对的

            if (_regex == null)
            {
                _regex = new Regex(@"^[a-zA-Z0-9]+$", RegexOptions.Compiled);
            }

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

            _package = package;
        }

        public Package Package => _package;

        private Package _package;

        private static Regex _regex;
        private readonly FileInfo _file;
        private string _fileSha;

        public static Package ParseFile(string file)
        {
            var xmlSerializer = new XmlSerializer(typeof(Package));
            using (var stream = new FileStream(file, FileMode.Open))
            {
                var read = new XmlTextReader(new StreamReader(stream, Encoding.UTF8));

                return (Package) xmlSerializer.Deserialize(read);
            }
        }

        private string Folder { get; }
    }
}