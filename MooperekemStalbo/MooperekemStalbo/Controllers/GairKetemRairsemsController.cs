using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooperekemStalbo;
using MooperekemStalbo.Models;

namespace MooperekemStalbo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GairKetemRairsemsController : ControllerBase
    {
        private readonly MooperekemStalboContext _context;

        public GairKetemRairsemsController(MooperekemStalboContext context)
        {
            _context = context;
        }

        // GET: api/GairKetemRairsems
        [HttpGet]
        public IEnumerable<GairKetemRairsem> GetGairKetemRairsem()
        {
            return _context.GairKetemRairsem;
        }

        // GET: api/GairKetemRairsems/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetGairKetemRairsem([FromRoute] long id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var gairKetemRairsem = await _context.GairKetemRairsem.FindAsync(id);

            if (gairKetemRairsem == null)
            {
                return NotFound();
            }

            return Ok(gairKetemRairsem);
        }

        // PUT: api/GairKetemRairsems/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutGairKetemRairsem([FromRoute] long id, [FromBody] GairKetemRairsem gairKetemRairsem)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != gairKetemRairsem.Id)
            {
                return BadRequest();
            }

            _context.Entry(gairKetemRairsem).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GairKetemRairsemExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/GairKetemRairsems
        [HttpPost]
        public async Task<IActionResult> PostGairKetemRairsem([FromBody] GairKetemRairsem gairKetemRairsem)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.GairKetemRairsem.Add(gairKetemRairsem);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetGairKetemRairsem", new { id = gairKetemRairsem.Id }, gairKetemRairsem);
        }

        [HttpPost("UploadPackage")]
        public async Task<StatusCodeResult> UploadPackage([FromForm]KanajeaLolowge file)
        {
            var fileInfo = new FileInfo("E:\\1.png");

            var fileStream = fileInfo.Open(FileMode.Create, FileAccess.ReadWrite);

            await file.File.CopyToAsync(fileStream);

            fileStream.Seek(0, SeekOrigin.Begin);

            string fileSha;
            using (var sha = SHA256.Create())
            {
                fileSha = Convert.ToBase64String(sha.ComputeHash(fileStream));

                fileStream.Seek(0, SeekOrigin.Begin);
            }

            if (fileSha == file.Sha)
            {
                return Ok();
            }

            return BadRequest();
        }

        // DELETE: api/GairKetemRairsems/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGairKetemRairsem([FromRoute] long id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var gairKetemRairsem = await _context.GairKetemRairsem.FindAsync(id);
            if (gairKetemRairsem == null)
            {
                return NotFound();
            }

            _context.GairKetemRairsem.Remove(gairKetemRairsem);
            await _context.SaveChangesAsync();

            return Ok(gairKetemRairsem);
        }

        private bool GairKetemRairsemExists(long id)
        {
            return _context.GairKetemRairsem.Any(e => e.Id == id);
        }
    }
}