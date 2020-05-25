using System;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.IO.Compression;
using System.Security.AccessControl;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace LarnaceakemLachonanafemhejal.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FileController : ControllerBase
    {
        [HttpGet]
        [Route("{id}")]
        public async Task Get([FromRoute] string id)
        {
            var folder = @"f:\lindexi\test\";
            HttpContext.Response.StatusCode = StatusCodes.Status200OK;

            using var stream = HttpContext.Response.BodyWriter.AsStream();

            await ReadDirectoryToZipStreamAsync(new DirectoryInfo(folder), stream);
        }

        /// <summary>
        /// 将一个文件夹的内容读取为 Stream 的压缩包
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="stream"></param>
        public static async Task ReadDirectoryToZipStreamAsync(DirectoryInfo directory, Stream stream)
        {
            var fileList = directory.GetFiles();

            using var zipArchive = new ZipArchive(stream, ZipArchiveMode.Create);
            foreach (var file in fileList)
            {
                var relativePath = file.FullName.Replace(directory.FullName, "");
                if (relativePath.StartsWith("\\") || relativePath.StartsWith("//"))
                {
                    relativePath = relativePath.Substring(1);
                }

                var zipArchiveEntry = zipArchive.CreateEntry(relativePath, CompressionLevel.NoCompression);

                using (var entryStream = zipArchiveEntry.Open())
                {
                    using var toZipStream = file.OpenRead();
                    await toZipStream.CopyToAsync(stream);
                }

                await stream.FlushAsync();
            }
        }
    }
}