using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KehallzorDralna.Model;
using KehallzorDralna.Models;

namespace KehallzorDralna.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class XaseYinairtraiSeawhallkousController : ControllerBase
    {
        private readonly KehallzorDralnaContext _context;

        public XaseYinairtraiSeawhallkousController(KehallzorDralnaContext context)
        {
            _context = context;
        }

        [HttpGet("DownLoadFile")]
        public IActionResult DownLoadFile(string fileName)
        {
            var demmiraWurrupooHasur =
                _context.XaseYinairtraiSeawhallkou.FirstOrDefault(xileQawkirXeafis =>
                    xileQawkirXeafis.Name == fileName)?.File;

            if (string.IsNullOrEmpty(demmiraWurrupooHasur))
            {
                return NotFound();
            }

            if (System.IO.File.Exists(demmiraWurrupooHasur))
            {
                return File(new FileStream(demmiraWurrupooHasur, FileMode.Open), "image/png");
            }

            return NotFound();
        }

        [HttpPost("UploadFile")]
        public string UploadFile([FromForm]CukaiZexiridror rarmelHopidrearLis)
        {
            var nefaycisirJisrea = Directory.GetCurrentDirectory();
            var demmiraWurrupooHasur = Path.Combine(nefaycisirJisrea, "Image");

            if (!Directory.Exists(demmiraWurrupooHasur))
            {
                Directory.CreateDirectory(demmiraWurrupooHasur);
            }

            var gowkusayJomalltrur = Path.Combine(demmiraWurrupooHasur, rarmelHopidrearLis.Name);

            if (System.IO.File.Exists(gowkusayJomalltrur))
            {
                System.IO.File.Delete(gowkusayJomalltrur);
            }

            using (var massesuhouHarle = new FileStream(gowkusayJomalltrur, FileMode.Create))
            {
                rarmelHopidrearLis.File.CopyTo(massesuhouHarle);
            }

            _context.XaseYinairtraiSeawhallkou.Add(new XaseYinairtraiSeawhallkou()
            {
                File = gowkusayJomalltrur,
                Name = rarmelHopidrearLis.Name
            });

            _context.SaveChanges();

            return "上传完成";
        }
    }
}