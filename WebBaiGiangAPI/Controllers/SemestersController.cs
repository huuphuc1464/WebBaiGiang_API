using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text.RegularExpressions;
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
    public class SemestersController : ControllerBase
    {
        private readonly AppDbContext _context;
        public readonly IJwtService _jwtService;

        public SemestersController(AppDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;   
        }

        [HttpGet("get-semesters")]
        public async Task<ActionResult<IEnumerable<Semester>>> GetSemesters()
        {
            var errorResult = KiemTraTokenAdminTeacher();
            if (errorResult != null)
            {
                return errorResult;
            }

            var se = await _context.Semesters.ToListAsync();
            if (se == null)
            {
                return NotFound(new
                {
                    message = "Hiện tại không có học kỳ nào"
                });
            }
            return Ok(new
            {
                message = "Danh sách học kỳ",
                data = se
            });
        }

        [HttpGet("get-semester")]
        public async Task<ActionResult<Semester>> GetSemester(int id)
        {
            var errorResult = KiemTraTokenAdminTeacher();
            if (errorResult != null)
            {
                return errorResult;
            }
            var se = await _context.Semesters.Where(s => s.SemesterId == id).ToListAsync();
            if (se == null || !se.Any())
            {
                return NotFound(new
                {
                    message = "Học kỳ không tồn tại"
                });
            }
            return Ok(se);
        }

        [HttpPut("update-semester")]
        public async Task<IActionResult> UpadteSemester (Semester semester)
        {
            var errorResult = KiemTraTokenAdminTeacher();
            if (errorResult != null)
            {
                return errorResult;
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var se = await _context.Semesters.SingleOrDefaultAsync(s => s.SemesterId == semester.SemesterId);
            if (se == null)
            {
                return BadRequest(new { message = "Học kỳ không tồn tại" });
            }

            se.SemesterTitle = Regex.Replace(semester.SemesterTitle.Trim(), @"\s+", " ");
            se.SemesterDescription = Regex.Replace(semester.SemesterDescription.Trim(), @"\s+", " ");

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SemesterExists(semester.SemesterId))
                {
                    return Conflict(new
                    {
                        message = "Học kỳ không tồn tại"
                    });
                }
                else
                {
                    throw;
                }
            }

            return Ok(new
            {
                message = "Thay đổi thông tin học kỳ thành công",
                data = se
            });
        }

        [HttpPost("add-semester")]
        public async Task<ActionResult<Semester>> AddSemester(Semester semester)
        {
            var errorResult = KiemTraTokenAdminTeacher();
            if (errorResult != null)
            {
                return errorResult;
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            semester.SemesterTitle = Regex.Replace(semester.SemesterTitle.Trim(), @"\s+", " ");
            semester.SemesterDescription = Regex.Replace(semester.SemesterDescription.Trim(), @"\s+", " ");
            _context.Semesters.Add(semester);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Thêm học kỳ thành công",
                data = semester
            });
        }

        [HttpDelete("delete-semester")]
        public async Task<IActionResult> DeleteSemester(int id)
        {
            var errorResult = KiemTraTokenAdminTeacher();
            if (errorResult != null)
            {
                return errorResult;
            }
            var semester = await _context.Semesters.FindAsync(id);
            if (semester == null)
            {
                return NotFound(new { message = "Học kỳ không tồn tại" });
            }

            var result1 = from s in _context.Semesters
                         join c in _context.Classes on s.SemesterId equals c.ClassSemesterId
                         join m in _context.Marks on s.SemesterId equals m.MarksSemesterId into markGroup
                         from m in markGroup.DefaultIfEmpty()
                         where s.SemesterId == id
                         select new { s, c, m };

            var data1 = result1.Any();
            var result2 = from s in _context.Semesters
                         join c in _context.Classes on s.SemesterId equals c.ClassSemesterId into classGroup
                         from c in classGroup.DefaultIfEmpty()
                         join m in _context.Marks on s.SemesterId equals m.MarksSemesterId 
                         where s.SemesterId == id
                         select new { s, c, m };

            var data2 = result2.Any();
            if (data1 || data2)
            {
                return BadRequest(new
                {
                    message = "Học kỳ đang có lớp học và điểm, không thể xóa"
                });
            }

            _context.Semesters.Remove(semester);
            await _context.SaveChangesAsync();

            return Conflict(new { message = "Xóa học kỳ thành công" });
        }

        private bool SemesterExists(int id)
        {
            return _context.Semesters.Any(e => e.SemesterId == id);
        }
        private ActionResult? KiemTraTokenAdminTeacher()
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

            if (role != "admin" && role != "1" && role != "teacher" && role != "2")
            {
                return Unauthorized(new { message = "Bạn không phải là admin hoặc giáo viên" });
            }

            return null; // Không có lỗi
        }

    }
}
