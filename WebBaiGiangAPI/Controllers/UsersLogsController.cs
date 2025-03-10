using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBaiGiangAPI.Data;
using WebBaiGiangAPI.Models;

namespace WebBaiGiangAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersLogsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersLogsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/UsersLogs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UsersLog>>> GetUserLogs()
        {
            return await _context.UserLogs.ToListAsync();
        }

        // GET: api/UsersLogs/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UsersLog>> GetUsersLog(int id)
        {
            var usersLog = await _context.UserLogs.FindAsync(id);

            if (usersLog == null)
            {
                return NotFound();
            }

            return usersLog;
        }

        // PUT: api/UsersLogs/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUsersLog(int id, UsersLog usersLog)
        {
            if (id != usersLog.UlogId)
            {
                return BadRequest();
            }

            _context.Entry(usersLog).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UsersLogExists(id))
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

        // POST: api/UsersLogs
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<UsersLog>> PostUsersLog(UsersLog usersLog)
        {
            _context.UserLogs.Add(usersLog);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUsersLog", new { id = usersLog.UlogId }, usersLog);
        }

        // DELETE: api/UsersLogs/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUsersLog(int id)
        {
            var usersLog = await _context.UserLogs.FindAsync(id);
            if (usersLog == null)
            {
                return NotFound();
            }

            _context.UserLogs.Remove(usersLog);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UsersLogExists(int id)
        {
            return _context.UserLogs.Any(e => e.UlogId == id);
        }
    }
}
