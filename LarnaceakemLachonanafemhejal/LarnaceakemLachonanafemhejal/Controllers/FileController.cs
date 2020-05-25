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
        public async Task Get([FromRoute]string id)
        {
            var folder = @"f:\lindexi\test\";
            var fileList = Directory.GetFiles(folder);
            HttpContext.Response.StatusCode = StatusCodes.Status200OK;

            using var fileStream = HttpContext.Response.BodyWriter.AsStream();

            using var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create);

            foreach (var file in fileList)
            {
                var zipArchiveEntry = zipArchive.CreateEntry(Path.GetFileName(file), CompressionLevel.NoCompression);

                using (var stream = zipArchiveEntry.Open())
                {
                    using var toZipStream = new FileStream(file, FileMode.Open, FileAccess.Read);
                    await toZipStream.CopyToAsync(stream);
                }

                await fileStream.FlushAsync();
            }
        }
    }
}