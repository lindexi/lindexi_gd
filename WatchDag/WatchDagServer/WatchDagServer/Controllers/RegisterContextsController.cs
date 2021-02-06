using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WatchDagServer.Data;

namespace WatchDagServer.Model
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegisterContextsController : ControllerBase
    {
        private readonly WatchDagServerContext _context;

        public RegisterContextsController(WatchDagServerContext context)
        {
            _context = context;
        }

        // GET: api/RegisterContexts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RegisterContext>>> GetRegisterContext()
        {
            return await _context.RegisterContext.ToListAsync();
        }

        // GET: api/RegisterContexts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RegisterContext>> GetRegisterContext(ulong id)
        {
            var registerContext = await _context.RegisterContext.FindAsync(id);

            if (registerContext == null)
            {
                return NotFound();
            }

            return registerContext;
        }

        // PUT: api/RegisterContexts/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutRegisterContext(ulong id, RegisterContext registerContext)
        {
            if (id != registerContext.Id)
            {
                return BadRequest();
            }

            _context.Entry(registerContext).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RegisterContextExists(id))
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

        // POST: api/RegisterContexts
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<RegisterContext>> PostRegisterContext(RegisterContext registerContext)
        {
            _context.RegisterContext.Add(registerContext);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetRegisterContext", new { id = registerContext.Id }, registerContext);
        }

        // DELETE: api/RegisterContexts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRegisterContext(ulong id)
        {
            var registerContext = await _context.RegisterContext.FindAsync(id);
            if (registerContext == null)
            {
                return NotFound();
            }

            _context.RegisterContext.Remove(registerContext);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool RegisterContextExists(ulong id)
        {
            return _context.RegisterContext.Any(e => e.Id == id);
        }
    }
}
