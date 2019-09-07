using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace Gaaweeealjrdwrebiny.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResourceController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            var folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var file = Path.Combine(folder, "big file");
            const string mime = "application/octet-stream";

            return PhysicalFile(file, mime);
        }
    }
}