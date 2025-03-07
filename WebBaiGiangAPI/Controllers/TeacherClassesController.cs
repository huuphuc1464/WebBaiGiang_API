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
    public class TeacherClassesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;
        public TeacherClassesController(AppDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpGet("get-teacher-classes")]
        public async Task<ActionResult<IEnumerable<TeacherClass>>> GetTeacherClasses()
        {
            var errorResult = KiemTraTokenAdmin();
            if (errorResult != null)
            {
                return errorResult;
            }
            var teacherClasses = from tc in _context.TeacherClasses
                                 join t in _context.Users on tc.TcUsersId equals t.UsersId
                                 join c in _context.Classes on tc.TcClassId equals c.ClassId
                                 select new { 
                                    tc.TcId,
                                    tc.TcDescription,
                                    c.ClassTitle,
                                    t.UsersName,
                                 };

            if (teacherClasses == null)
            {
                return NotFound(new
                {
                    message = "Hiện tại không có danh sách phân công giáo viên nào"
                });
            }
            return Ok(new
            {
                message = "Danh sách phân công giáo viên",
                data = teacherClasses
            });
        }

        [HttpGet("get-teacher-class")]
        public async Task<ActionResult<TeacherClass>> GetTeacherClass(int id)
        {
            var errorResult = KiemTraTokenAdmin();
            if (errorResult != null)
            {
                return errorResult;
            }
            var result = from tc in _context.TeacherClasses
                         join t in _context.Users on tc.TcUsersId equals t.UsersId
                         join c in _context.Classes on tc.TcClassId equals c.ClassId
                         where tc.TcId == id
                         select new
                         {
                             tc.TcId,
                             tc.TcDescription,
                             c.ClassTitle,
                             c.ClassDescription,
                             t.UsersName,
                             t.UsersEmail,
                             t.UsersMobile,
                             t.UsersDob,
                             t.UsersImage,
                         };

            if (result == null || !result.Any())
            {
                return NotFound(new
                {
                    message = "Phân công không tồn tại"
                });
            }
            return Ok(result);
        }

        [HttpGet("get-teacher-class-by-teacher")]
        public async Task<ActionResult<TeacherClass>> GetTeacherClassByTeacher(int id)
        {
            var errorResult = KiemTraTokenAdminTeacher();
            if (errorResult != null)
            {
                return errorResult;
            }
            var result = from tc in _context.TeacherClasses
                         join t in _context.Users on tc.TcUsersId equals t.UsersId
                         join c in _context.Classes on tc.TcClassId equals c.ClassId
                         where tc.TcUsersId == id 
                         select new
                         {
                             tc.TcId,
                             tc.TcUsersId,
                             tc.TcDescription,
                             c.ClassTitle,
                             c.ClassDescription,
                             t.UsersName,
                             t.UsersEmail,
                             t.UsersMobile,
                             t.UsersDob,
                             t.UsersImage,
                         };

            if (result == null || !result.Any())
            {
                return NotFound(new
                {
                    message = "Phân công không tồn tại"
                });
            }
            return Ok(result);
        }
        
        [HttpGet("get-teacher-class-by-class")]
        public async Task<ActionResult<TeacherClass>> GetTeacherClassByClass(int id)
        {
            var errorResult = KiemTraTokenAdminTeacher();
            if (errorResult != null)
            {
                return errorResult;
            }
            var result = from tc in _context.TeacherClasses
                         join t in _context.Users on tc.TcUsersId equals t.UsersId
                         join c in _context.Classes on tc.TcClassId equals c.ClassId
                         where tc.TcClassId == id
                         select new
                         {
                             tc.TcId,
                             tc.TcClassId,
                             tc.TcDescription,
                             c.ClassTitle,
                             c.ClassDescription,
                             t.UsersName,
                             t.UsersEmail,
                             t.UsersMobile,
                             t.UsersDob,
                             t.UsersImage,
                         };

            if (result == null || !result.Any())
            {
                return NotFound(new
                {
                    message = "Phân công không tồn tại"
                });
            }
            return Ok(result);
        }

        [HttpPut("update-teacher-class")]
        public async Task<IActionResult> UpdateTeacherClass(TeacherClass teacherClass)
        {
            var errorResult = KiemTraTokenAdmin();
            if (errorResult != null)
            {
                return errorResult;
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (!_context.Classes.Any(c => c.ClassId == teacherClass.TcClassId))
            {
                return BadRequest(new
                {
                    message = "Lớp học không tồn tại",
                    data = teacherClass
                });
            }

            if (!_context.Users.Any(t => t.UsersId == teacherClass.TcUsersId && t.UsersRoleId == 2))
            {
                return BadRequest(new
                {
                    message = "Giáo viên không tồn tại",
                    data = teacherClass
                });
            }

            var tc = await _context.TeacherClasses.SingleOrDefaultAsync(t => t.TcId == teacherClass.TcId);
            if (tc == null)
            {
                return BadRequest(new { message = "Phân công không tồn tại" });
            }

            tc.TcDescription = Regex.Replace(teacherClass.TcDescription.Trim(), @"\s+", " ");

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TeacherClassExists(teacherClass.TcId))
                {
                    return Conflict(new
                    {
                        message = "Phân công không tồn tại"
                    });
                }
                else
                {
                    throw;
                }
            }

            return Ok(new
            {
                message = "Thay đổi thông tin phân công thành công",
                data = tc
            });
        }

        [HttpPost("add-teacher-class")]
        public async Task<ActionResult<TeacherClass>> AddTeacherClass(TeacherClass teacherClass)
        {
            var errorResult = KiemTraTokenAdmin();
            if (errorResult != null)
            {
                return errorResult;
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (!_context.Classes.Any(c => c.ClassId == teacherClass.TcClassId))
            {
                return BadRequest(new
                {
                    message = "Lớp học không tồn tại",
                    data = teacherClass
                });
            }

            if (!_context.Users.Any(t => t.UsersId == teacherClass.TcUsersId && t.UsersRoleId == 2))
            {
                return BadRequest(new
                {
                    message = "Giáo viên không tồn tại",
                    data = teacherClass
                });
            }
            teacherClass.TcDescription = Regex.Replace(teacherClass.TcDescription.Trim(), @"\s+", " ");
            _context.TeacherClasses.Add(teacherClass);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Thêm phân công thành công",
                data = teacherClass
            });
        }

        [HttpDelete("delete-teacher-class")]
        public async Task<IActionResult> DeleteTeacherClass(int id)
        {
            try
            {
                var errorResult = KiemTraTokenAdmin();
                if (errorResult != null)
                {
                    return errorResult;
                }
                var tc = await _context.TeacherClasses.FindAsync(id);
                if (tc == null)
                {
                    return NotFound(new { message = "Phân công không tồn tại" });
                }

                _context.TeacherClasses.Remove(tc);
                await _context.SaveChangesAsync();

                return Conflict(new { message = "Xóa phân công thành công" });
            }
            catch (Exception)
            {
                return NotFound(new { message = "Không thể xóa, phân công đang liên kết với bảng khác" });
            }
        }

        private bool TeacherClassExists(int id)
        {
            return _context.TeacherClasses.Any(e => e.TcId == id);
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
