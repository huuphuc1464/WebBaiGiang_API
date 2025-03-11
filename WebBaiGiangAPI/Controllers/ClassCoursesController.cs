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
    public class ClassCoursesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;

        public ClassCoursesController(AppDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpGet("get-class-courses")]
        public async Task<ActionResult<IEnumerable<ClassCourse>>> GetClassCourses()
        {
            var errorResult = KiemTraTokenAdmin();
            if (errorResult != null)
            {
                return errorResult;
            }
            var result = from cc in _context.ClassCourses
                         join cl in _context.Classes on cc.ClassId equals cl.ClassId
                         join co in _context.Courses on cc.CourseId equals co.CourseId
                         select new {
                            cc.CcId,
                            cc.CcDescription,
                            cl.ClassTitle,
                            cl.ClassDescription,
                            cl.ClassSemesterId,
                            cl.ClassSyearId,
                            cl.ClassUpdateAt,
                            co.CourseDepartmentId,
                            co.CourseTitle,
                            co.CourseTotalSemester,
                            co.CourseImage,
                            co.CourseShortdescription,
                            co.CourseDescription,
                            co.CourseUpdateAt,
                         };
            var classCourse = result.ToList();
            if (classCourse == null)
            {
                return NotFound(new
                {
                    message = "Hiện tại không có lớp học phần nào"
                });
            }
            return Ok(new
            {
                message = "Danh sách lớp học phần",
                data = classCourse
            });
        }

        [HttpGet("get-class-course")]
        public async Task<ActionResult<ClassCourse>> GetClassCourse(int id)
        {
            var errorResult = KiemTraTokenAdmin();
            if (errorResult != null)
            {
                return errorResult;
            }
            var result = from cc in _context.ClassCourses
                         join cl in _context.Classes on cc.ClassId equals cl.ClassId
                         join co in _context.Courses on cc.CourseId equals co.CourseId
                         join de in _context.Departments on co.CourseDepartmentId equals de.DepartmentId
                         join sy in _context.SchoolYears on cl.ClassSyearId equals sy.SyearId
                         join se in _context.Semesters on cl.ClassSemesterId equals se.SemesterId
                         where cc.CcId == id
                         select new
                         {
                             cc.CcId,
                             cc.CcDescription,
                             cl.ClassTitle,
                             cl.ClassDescription,
                             se.SemesterTitle,
                             sy.SyearTitle,
                             cl.ClassUpdateAt,
                             de.DepartmentTitle,
                             co.CourseTitle,
                             co.CourseTotalSemester,
                             co.CourseImage,
                             co.CourseShortdescription,
                             co.CourseDescription,
                             co.CourseUpdateAt,
                         };
            var classCourse = await result.FirstOrDefaultAsync();
            if (classCourse == null)
            {
                return NotFound("Lớp học phần không tồn tại");
            }

            return Ok(classCourse);
        }

        [HttpPut("update-class-course")]
        public async Task<IActionResult> UpdateClassCourse(ClassCourse classCourse)
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
            if (!_context.ClassCourses.Any(cc => cc.CcId == classCourse.CcId)) 
            {
                return Unauthorized(new { message = "Lớp học phần không tồn tại" });
            }
            if (!_context.Classes.Any(s => s.ClassId == classCourse.ClassId))
            {
                return BadRequest(new
                {
                    message = "Lớp học không tồn tại",
                    data = classCourse
                });
            }
            if (!_context.Courses.Any(s => s.CourseId == classCourse.CourseId))
            {
                return BadRequest(new
                {
                    message = "Học phần không tồn tại",
                    data = classCourse
                });
            }
            if (classCourse.CcDescription != null)
            {
                classCourse.CcDescription = Regex.Replace(classCourse.CcDescription.Trim(), @"\s+", " ");
            }
            _context.ClassCourses.Update(classCourse);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {

                throw;
                
            }

            return Ok("Cập nhật lớp học phần thành công");
        }

        [HttpPost("add-class-course")]
        public async Task<ActionResult<ClassCourse>> AddClassCourse(ClassCourse classCourse)
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
            if (!_context.Classes.Any(s => s.ClassId == classCourse.ClassId))
            {
                return BadRequest(new
                {
                    message = "Lớp học không tồn tại",
                    data = classCourse
                });
            }
            if (!_context.Courses.Any(s => s.CourseId == classCourse.CourseId ))
            {
                return BadRequest(new
                {
                    message = "Học phần không tồn tại",
                    data = classCourse
                });
            }
            if (classCourse.CcDescription != null)
            {
                classCourse.CcDescription = Regex.Replace(classCourse.CcDescription.Trim(), @"\s+", " ");
            }
            _context.ClassCourses.Add(classCourse);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Thêm lớp học phần thành công",
                data = classCourse
            });
        }

        [HttpDelete("delete-course")]
        public async Task<IActionResult> DeleteClassCourse(int id)
        {
            var errorResult = KiemTraTokenAdmin();
            if (errorResult != null)
            {
                return errorResult;
            }
            try
            {
                var classCourse = await _context.ClassCourses.FindAsync(id);
                if (classCourse == null)
                {
                    return NotFound("Lớp học phần không tồn tại");
                }

                _context.ClassCourses.Remove(classCourse);
                await _context.SaveChangesAsync();

                return Ok("Xóa thành công lớp học phần");
            }
            catch (Exception)
            {
                return BadRequest("Không thể xóa lớp học phần hiện tại");
            }
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
