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
    public class ClassesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;

        public ClassesController(AppDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpGet("get-classes")]
        public async Task<ActionResult<IEnumerable<Class>>> GetClasses()
        {
            var errorResult = KiemTraTokenTeacher();
            if (errorResult != null)
            {
                return errorResult;
            }
            var classes = await _context.Classes.ToListAsync();
            if (classes == null)
            {
                return NotFound(new
                {
                    message = "Hiện tại không có lớp học nào"
                });
            }
            return Ok(new
            {
                message = "Danh sách lớp học",
                data = classes
            });
        }

        [HttpGet("get-class")]
        public async Task<ActionResult<Class>> GetClass(int id)
        {
            var errorResult = KiemTraTokenTeacher();
            if (errorResult != null)
            {
                return errorResult;
            }
            var result = from c in _context.Classes
                         join s in _context.Semesters on c.ClassSemesterId equals s.SemesterId
                         join y in _context.SchoolYears on c.ClassSyearId equals y.SyearId
                         where c.ClassId == id
                         select new {
                            c.ClassId,
                            c.ClassTitle,
                            c.ClassDescription,
                            c.ClassUpdateAt,
                            s.SemesterTitle,
                            s.SemesterDescription,
                            y.SyearDescription,
                            y.SyearTitle,
                         };
            if (result == null || !result.Any())
            {
                return NotFound(new
                {
                    message = "Lớp học không tồn tại"
                });
            }
            return Ok(result);
        }

        [HttpPut("update-class")]
        public async Task<IActionResult> UpdateClass(Class @class)
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
            if (!_context.Semesters.Any(s => s.SemesterId == @class.ClassSemesterId))
            {
                return BadRequest(new
                {
                    message = "Học kỳ không tồn tại",
                    data = @class
                });
            }
            if (!_context.SchoolYears.Any(sy => sy.SyearId == @class.ClassSyearId))
            {
                return BadRequest(new
                {
                    message = "Năm học không tồn tại",
                    data = @class
                });
            }
            var c = await _context.Classes.SingleOrDefaultAsync(c => c.ClassId == @class.ClassId);
            if (c == null)
            {
                return BadRequest(new { message = "Học kỳ không tồn tại" });
            }

            c.ClassTitle = Regex.Replace(@class.ClassTitle.Trim(), @"\s+", " ");
            c.ClassDescription = Regex.Replace(@class.ClassDescription.Trim(), @"\s+", " ");
            c.ClassUpdateAt = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClassExists(@class.ClassId))
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
                message = "Thay đổi thông tin lớp học thành công",
                data = c
            });
        }

        [HttpPost("add-class")]
        public async Task<ActionResult<Class>> AddClass(Class lopHoc)
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
            if (!_context.Semesters.Any(s => s.SemesterId == lopHoc.ClassSemesterId))
            {
                return BadRequest( new
                {
                    message = "Học kỳ không tồn tại",
                    data = lopHoc
                });
            }
            if (!_context.SchoolYears.Any(sy => sy.SyearId == lopHoc.ClassSyearId))
            {
                return BadRequest(new
                {
                    message = "Năm học không tồn tại",
                    data = lopHoc
                });
            }
            lopHoc.ClassTitle = Regex.Replace(lopHoc.ClassTitle.Trim(), @"\s+", " ");
            lopHoc.ClassDescription = Regex.Replace(lopHoc.ClassDescription.Trim(), @"\s+", " ");
            lopHoc.ClassUpdateAt = DateTime.Now;
            _context.Classes.Add(lopHoc);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Thêm lớp học thành công",
                data = lopHoc
            });
        }

        [HttpDelete("delete-class")]
        public async Task<IActionResult> DeleteClass(int id)
        {
            try
            {
                var errorResult = KiemTraTokenTeacher();
                if (errorResult != null)
                {
                    return errorResult;
                }
                var @class = await _context.Classes.FindAsync(id);
                if (@class == null)
                {
                    return NotFound(new { message = "Lớp học không tồn tại" });
                }

                _context.Classes.Remove(@class);
                await _context.SaveChangesAsync();

                return Conflict(new { message = "Xóa lớp học thành công" });
            }
            catch (Exception ex) 
            {
                return NotFound(new { message = "Không thể xóa, lớp học đang liên kết với bảng khác" });
            }
        }

        private bool ClassExists(int id)
        {
            return _context.Classes.Any(e => e.ClassId == id);
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
