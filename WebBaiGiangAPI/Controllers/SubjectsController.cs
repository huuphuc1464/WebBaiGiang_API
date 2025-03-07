using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
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
    public class SubjectsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;
        public SubjectsController(AppDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;   
        }

        [HttpGet("get-subjects")]
        public async Task<ActionResult<IEnumerable<Subject>>> GetSubjects()
        {
            var errorResult = KiemTraTokenTeacher();
            if (errorResult != null)
            {
                return errorResult;
            }
            var subjects = await _context.Subjects.ToListAsync();
            if (subjects == null)
            {
                return NotFound(new
                {
                    message = "Hiện tại không có môn học nào"
                });
            }
            return Ok(new
            {
                message = "Danh sách môn học",
                data = subjects
            });
        }

        [HttpGet("get-subject")]
        public async Task<ActionResult<Subject>> GetSubject(int id)
        {
            var errorResult = KiemTraTokenTeacher();
            if (errorResult != null)
            {
                return errorResult;
            }
            var result = await _context.Subjects.FindAsync(id);
            if (result == null)
            {
                return NotFound(new
                {
                    message = "Môn học không tồn tại"
                });
            }
            return Ok(result);
        }

        [HttpPut("update-subject")]
        public async Task<IActionResult> UpdateSubject(Subject subject)
        {
            var errorResult = KiemTraTokenTeacher();
            if (errorResult != null)
            {
                return errorResult;
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (!_context.Classes.Any(s => s.ClassId == subject.SubjectClassId))
            {
                return BadRequest(new
                {
                    message = "Lớp học không tồn tại",
                    data = subject
                });
            }
            if (_context.Subjects.Any(s => s.SubjectTitle == subject.SubjectTitle && s.SubjectId != subject.SubjectId))
            {
                return BadRequest(new
                {
                    message = "Môn học đã tồn tại",
                    data = subject,
                });
            }
            var s = await _context.Subjects.SingleOrDefaultAsync(s => s.SubjectId == subject.SubjectId);
            if (s == null)
            {
                return BadRequest(new { message = "Môn học không tồn tại" });
            }

            s.SubjectTitle = Regex.Replace(subject.SubjectTitle.Trim(), @"\s+", " ");
            s.SubjectDescription = Regex.Replace(subject.SubjectDescription.Trim(), @"\s+", " ");

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SubjectExists(subject.SubjectId))
                {
                    return Conflict(new
                    {
                        message = "Môn học không tồn tại"
                    });
                }
                else
                {
                    throw;
                }
            }

            return Ok(new
            {
                message = "Thay đổi thông tin môn học thành công",
                data = s
            });
        }

        [HttpPost("add-subject")]
        public async Task<ActionResult<Subject>> PostSubject(Subject subject)
        {
            var errorResult = KiemTraTokenTeacher();
            if (errorResult != null)
            {
                return errorResult;
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (!_context.Subjects.Any(s => s.SubjectClassId == subject.SubjectClassId))
            {
                return BadRequest(new
                {
                    message = "Lớp học không tồn tại",
                    data = subject
                });
            }
            
            subject.SubjectTitle = Regex.Replace(subject.SubjectTitle.Trim(), @"\s+", " ");

            if (_context.Subjects.Any(s => s.SubjectTitle == subject.SubjectTitle))
            {
                return BadRequest(new
                {
                    message = "Môn học đã tồn tại",
                    data = subject
                });
            }
            subject.SubjectDescription = Regex.Replace(subject.SubjectDescription.Trim(), @"\s+", " ");
            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Thêm môn học thành công",
                data = subject
            });
        }

        [HttpDelete("delete-subject")]
        public async Task<IActionResult> DeleteSubject(int id)
        {
            try
            {
                var errorResult = KiemTraTokenTeacher();
                if (errorResult != null)
                {
                    return errorResult;
                }
                var subject = await _context.Subjects.FindAsync(id);
                if (subject == null)
                {
                    return NotFound(new { message = "Môn học không tồn tại" });
                }

                _context.Subjects.Remove(subject);
                await _context.SaveChangesAsync();

                return Conflict(new { message = "Xóa môn học thành công" });
            }
            catch (Exception)
            {
                return NotFound(new { message = "Không thể xóa, môn học đang liên kết với bảng khác" });
            }
        }
        private bool SubjectExists(int id)
        {
            return _context.Subjects.Any(e => e.SubjectId == id);
        }
        private ActionResult? KiemTraTokenTeacher()
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

            if (role != "teacher" && role != "2")
            {
                return Unauthorized(new { message = "Bạn không phải là giáo viên" });
            }

            return null; // Không có lỗi
        }

    }
}
