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
    public class DepartmentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;

        public DepartmentsController(AppDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;   
        }

        [HttpGet("get-departments")]
        public async Task<ActionResult<Department>> GetListDepartments()
        {
            var errorResult = KiemTraTokenAdmin();
            if (errorResult != null)
            {
                return errorResult;
            }
            var de = await _context.Departments.ToListAsync();
            if (de == null)
            {
                return NotFound(new
                {
                    message = "Hiện tại không có phòng ban nào"
                });
            }
            return Ok(new
            {
                message = "Danh sách phòng ban",
                data = de
            });
        }

        [HttpGet("get-department")]
        public async Task<ActionResult<Department>> GetDepartment(int id)
        {
            var errorResult = KiemTraTokenAdmin();
            if (errorResult != null)
            {
                return errorResult;
            }
            var de = await _context.Departments.Where(d => d.DepartmentId == id).ToListAsync();
            if (de == null || !de.Any())
            {
                return NotFound(new
                {
                    message = "Phòng ban không tồn tại"
                });
            }
            return Ok(de);
        }

        [HttpGet("get-department-by-teacher")]
        public async Task<ActionResult<Department>> GetDepartmentByTeacher(int id)
        {
            var errorResult = KiemTraTokenTeacher();
            if (errorResult != null)
            {
                return errorResult;
            }

            var de = (from d in _context.Departments
                              from u in _context.Users
                              where d.DepartmentId == u.UsersDepartmentId && u.UsersId == id
                              select new { d , u});


            if (de == null || !de.Any())
            {
                return NotFound(new
                {
                    message = "Giáo viên chưa thuộc phòng ban nào"
                });
            }
            return Ok(de);
        }

        [HttpPut("update-department")]
        public async Task<IActionResult> UpdateDepartment(Department department)
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

            var de = await _context.Departments.SingleOrDefaultAsync(u => u.DepartmentId == department.DepartmentId);
            if (de == null)
            {
                return BadRequest(new { message = "Phòng ban không tồn tại" });
            }

            // Kiểm tra mã phòng ban đã tồn tại, nhưng không tính chính nó
            if (_context.Departments.Any(d => d.DepartmentCode == department.DepartmentCode && d.DepartmentId != department.DepartmentId))
            {
                return BadRequest(new
                {
                    message = "Mã phòng ban đã tồn tại",
                    data = department
                });
            }

            de.DepartmentTitle = Regex.Replace(department.DepartmentTitle.Trim(), @"\s+", " ");
            de.DepartmentDescription = Regex.Replace(department.DepartmentDescription.Trim(), @"\s+", " ");
            de.DepartmentCode = department.DepartmentCode.Replace(" ", "").ToUpper();

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DepartmentExists(department.DepartmentId))
                {
                    return Conflict(new
                    {
                        message = "Phòng ban không tồn tại"
                    });
                }
                else
                {
                    throw;
                }
            }

            return Ok(new
            {
                message = "Thay đổi thông tin phòng ban thành công",
                data = de
            });
        }

        [HttpPost("add-department")]
        public async Task<ActionResult<Department>> AddDepartment(Department department)
        {
            var errorResult = KiemTraTokenAdmin();
            if (errorResult != null)
            {
                return errorResult;
            }
            if (_context.Departments.Any(d => d.DepartmentCode == department.DepartmentCode))
            {
                return BadRequest(new
                {
                    message = "Mã phòng ban đã tồn tại",
                    data = department
                });
            }
            department.DepartmentTitle = Regex.Replace(department.DepartmentTitle.Trim(), @"\s+", " ");
            department.DepartmentDescription = Regex.Replace(department.DepartmentDescription.Trim(), @"\s+", " ");
            department.DepartmentCode = department.DepartmentCode.Replace(" ", "").ToUpper();
            _context.Departments.Add(department);
            await _context.SaveChangesAsync();

            return Ok( new
            {
                message =  "Thêm phòng ban thành công",
                data = department
            });
        }

        [HttpDelete("delete-department")]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            var errorResult = KiemTraTokenAdmin();
            if (errorResult != null)
            {
                return errorResult;
            }
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
            {
                return NotFound( new { message = "Phòng ban không tồn tại" });
            }

            var hasCourses = (from d in _context.Departments
                              from c in _context.Courses
                              where d.DepartmentId == c.CourseDepartmentId && c.CourseDepartmentId == id
                              select c).Any();

            if (hasCourses)
            {
                return BadRequest(new
                {
                    message = "Phòng ban đang có khóa học, vui lòng thử lại"
                });
            }

            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();

            return Conflict( new { message = "Xóa phòng ban thành công" });
        }

        private bool DepartmentExists(int id)
        {
            return _context.Departments.Any(e => e.DepartmentId == id);
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
