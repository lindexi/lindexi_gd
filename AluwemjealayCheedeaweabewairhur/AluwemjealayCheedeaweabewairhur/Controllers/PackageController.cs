using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AluwemjealayCheedeaweabewairhur.Model;

namespace AluwemjealayCheedeaweabewairhur.Controllers
{
    [ApiController]
    [Route("api/v2/[controller]")]
    public class PackageController : ControllerBase
    {
        [HttpPut]
        public async Task<IActionResult> Push([FromForm] FilePackage package)
        {
            var packageFile = package.Package;
            if (packageFile == null)
            {
                packageFile = HttpContext.Request.Form.Files.FirstOrDefault();
            }

            if (packageFile == null)
            {
                return BadRequest();
            }

            var file = Path.GetTempFileName();
            using (var stream = new FileStream(file, FileMode.OpenOrCreate))
            {
                await packageFile.CopyToAsync(stream);
            }

            return Ok();
        }
    }
}