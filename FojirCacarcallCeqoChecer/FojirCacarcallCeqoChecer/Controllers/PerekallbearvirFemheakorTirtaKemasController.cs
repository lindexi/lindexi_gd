using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FojirCacarcallCeqoChecer.Model;
using FojirCacarcallCeqoChecer.Models;

namespace FojirCacarcallCeqoChecer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PerekallbearvirFemheakorTirtaKemasController : ControllerBase
    {
        private readonly RasasCootrorvouContext _context;

        public PerekallbearvirFemheakorTirtaKemasController(RasasCootrorvouContext context)
        {
            _context = context;
        }

        // GET: api/PerekallbearvirFemheakorTirtaKemas
        [HttpGet]
        public IEnumerable<PerekallbearvirFemheakorTirtaKema> GetPerekallbearvirFemheakorTirtaKema()
        {
            return _context.PerekallbearvirFemheakorTirtaKema;
        }

        // GET: api/PerekallbearvirFemheakorTirtaKemas/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPerekallbearvirFemheakorTirtaKema([FromRoute] long id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var perekallbearvirFemheakorTirtaKema = await _context.PerekallbearvirFemheakorTirtaKema.FindAsync(id);

            if (perekallbearvirFemheakorTirtaKema == null)
            {
                return NotFound();
            }

            return Ok(perekallbearvirFemheakorTirtaKema);
        }

        // PUT: api/PerekallbearvirFemheakorTirtaKemas/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPerekallbearvirFemheakorTirtaKema([FromRoute] long id, [FromBody] PerekallbearvirFemheakorTirtaKema perekallbearvirFemheakorTirtaKema)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != perekallbearvirFemheakorTirtaKema.Id)
            {
                return BadRequest();
            }

            _context.Entry(perekallbearvirFemheakorTirtaKema).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PerekallbearvirFemheakorTirtaKemaExists(id))
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

        // POST: api/PerekallbearvirFemheakorTirtaKemas
        [HttpPost]
        public async Task<IActionResult> PostPerekallbearvirFemheakorTirtaKema([FromBody] PerekallbearvirFemheakorTirtaKema perekallbearvirFemheakorTirtaKema)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.PerekallbearvirFemheakorTirtaKema.Add(perekallbearvirFemheakorTirtaKema);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetPerekallbearvirFemheakorTirtaKema", new { id = perekallbearvirFemheakorTirtaKema.Id }, perekallbearvirFemheakorTirtaKema);
        }

        // DELETE: api/PerekallbearvirFemheakorTirtaKemas/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePerekallbearvirFemheakorTirtaKema([FromRoute] long id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var perekallbearvirFemheakorTirtaKema = await _context.PerekallbearvirFemheakorTirtaKema.FindAsync(id);
            if (perekallbearvirFemheakorTirtaKema == null)
            {
                return NotFound();
            }

            _context.PerekallbearvirFemheakorTirtaKema.Remove(perekallbearvirFemheakorTirtaKema);
            await _context.SaveChangesAsync();

            return Ok(perekallbearvirFemheakorTirtaKema);
        }

        private bool PerekallbearvirFemheakorTirtaKemaExists(long id)
        {
            return _context.PerekallbearvirFemheakorTirtaKema.Any(e => e.Id == id);
        }
    }
}