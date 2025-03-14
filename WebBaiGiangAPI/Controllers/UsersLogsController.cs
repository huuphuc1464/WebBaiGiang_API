using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
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
        private readonly IJwtService _jwtService;
        public UsersLogsController(AppDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpGet("get-user-logs")]
        public async Task<ActionResult<IEnumerable<UsersLog>>> GetUserLogs()
        {
            var errorResult = KiemTraTokenAdmin();
            if (errorResult != null)
            {
                return errorResult;
            }
            var user = await _context.UserLogs.ToListAsync();
            if (user == null)
            {
                return NotFound(new
                {
                    message = "Hiện tại không có log nào"
                });
            }
            return Ok(new
            {
                message = "Danh sách user log",
                data = user
            });
        }

        [HttpGet("get-user-log")]
        public async Task<ActionResult<UsersLog>> GetUsersLog(int id)
        {
            var errorResult = KiemTraTokenAdmin();
            if (errorResult != null)
            {
                return errorResult;
            }
            var result  = from l in _context.UserLogs
                       join u in _context.Users on l.UlogUsersId equals u.UsersId
                       where l.UlogId == id
                       select new
                       {
                           l.UlogId,
                           l.UlogUsersId,
                           l.UlogUsername,
                           l.UlogLoginDate,
                           l.UlogLogoutDate,
                           u.UsersName,
                           u.UsersEmail,
                           u.UsersMobile,
                           u.UsersDob,
                           u.UsersImage,
                           u.UsersAdd,
                           u.UserGender
                       };
            var user = result.ToList();
            if (user == null || !user.Any())
            {
                return NotFound(new
                {
                    message = "Hiện tại không có log nào"
                });
            }
            return Ok(new
            {
                message = $"Thông tin chi tiết user log {id}",
                data = user
            });
        }

        [HttpGet("get-user-logs-by-teacher")]
        public async Task<ActionResult<IEnumerable<UsersLog>>> GetUserLogsByTeacher(int teacherId)
        {
            var errorResult = KiemTraTokenAdmin();
            if (errorResult != null)
            {
                return errorResult;
            }
            var result = from l in _context.UserLogs
                         join u in _context.Users on l.UlogUsersId equals u.UsersId
                         where l.UlogUsersId == teacherId && u.UsersRoleId == 2
                         select  l ;
            var user = result.ToList();
            if (user == null || !user.Any())
            {
                return NotFound(new
                {
                    message = "Hiện tại không có log nào"
                });
            }
            return Ok(new
            {
                message = "Danh sách user log",
                data = user
            });
        }

        [HttpGet("get-user-log-by-teacher")]
        public async Task<ActionResult<UsersLog>> GetUsersLogByTeacher(int id, int teacherId)
        {
            var errorResult = KiemTraTokenAdmin();
            if (errorResult != null)
            {
                return errorResult;
            }
            var result = from l in _context.UserLogs
                         join u in _context.Users on l.UlogUsersId equals u.UsersId
                         where l.UlogId == id && l.UlogUsersId == teacherId && u.UsersRoleId == 2
                         select new
                         {
                             l.UlogId,
                             l.UlogUsersId,
                             l.UlogUsername,
                             l.UlogLoginDate,
                             l.UlogLogoutDate,
                             u.UsersName,
                             u.UsersEmail,
                             u.UsersMobile,
                             u.UsersDob,
                             u.UsersImage,
                             u.UsersAdd,
                             u.UserGender
                         };
            var user = result.ToList();
            if (user == null || !user.Any())
            {
                return NotFound(new
                {
                    message = "Hiện tại không có log nào"
                });
            }
            return Ok(new
            {
                message = $"Thông tin chi tiết user log {id}",
                data = user
            });
        }

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

        [HttpDelete("delete-user-log")]
        public async Task<IActionResult> DeleteUsersLog(int id)
        {

            var errorResult = KiemTraTokenAdmin();
            if (errorResult != null)
            {
                return errorResult;
            }
            var usersLog = await _context.UserLogs.FindAsync(id);
            if (usersLog == null)
            {
                return NotFound();
            }

            _context.UserLogs.Remove(usersLog);
            await _context.SaveChangesAsync();

            return Ok("Xóa thành công");
        }

        private bool UsersLogExists(int id)
        {
            return _context.UserLogs.Any(e => e.UlogId == id);
        }
        private ActionResult? KiemTraTokenAdmin()
        {
            var authorizationHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (_jwtService.GetToken(authorizationHeader) == null)
            {
                return Unauthorized(new { message = "Token không tồn tại" });
            }

            var token = _jwtService.GetToken(authorizationHeader);
            var tokenInfo = _jwtService.GetTokenInfoFromToken(token);

            if (!tokenInfo.TryGetValue(JwtRegisteredClaimNames.UniqueName, out string username) || string.IsNullOrEmpty(username))
            {
                return Unauthorized(new { message = "Token không hợp lệ hoặc đã hết hạn" });
            }

            tokenInfo.TryGetValue("role", out string role);

            var user_log = _context.UserLogs
                .Where(u => u.UlogUsername == username)
                .OrderByDescending(u => u.UlogId)
                .FirstOrDefault();

            if (user_log == null || user_log.UlogLogoutDate != null)
            {
                return Unauthorized(new { message = "Token không hợp lệ hoặc đã hết hạn" });
            }

            var isUser = _context.Users.SingleOrDefault(u => u.UsersUsername == username);
            if (isUser == null)
            {
                return Unauthorized(new { message = "Tài khoản không tồn tại" });
            }

            if (role != "admin" && role != "1")
            {
                return Unauthorized(new { message = "Bạn không phải là admin" });
            }

            return null; // Không có lỗi
        }

    }
}
