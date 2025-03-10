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
    public class CoursesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;

        public CoursesController(AppDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpGet("get-courses")]
        public async Task<ActionResult<IEnumerable<Course>>> GetCourses()
        {

            var errorResult = KiemTraTokenAdmin();
            if (errorResult != null)
            {
                return errorResult;
            }
            var course = await _context.Courses.ToListAsync();
            if (course == null)
            {
                return NotFound(new
                {
                    message = "Hiện tại không có học phần nào"
                });
            }
            return Ok(new
            {
                message = "Danh sách học phần",
                data = course
            });
        }

        [HttpGet("get-course")]
        public async Task<ActionResult<Course>> GetCourse(int id)
        {
            var errorResult = KiemTraTokenAdmin();
            if (errorResult != null)
            {
                return errorResult;
            }
            var result = from c in _context.Courses
                         join d in _context.Departments on c.CourseDepartmentId equals d.DepartmentId
                         where c.CourseId == id
                         select new
                         {
                             c.CourseId,
                             c.CourseTitle,
                             c.CourseTotalSemester,
                             c.CourseImage,
                             c.CourseShortdescription,
                             c.CourseDescription,
                             c.CourseUpdateAt,
                             d.DepartmentTitle,
                             d.DepartmentCode,
                             d.DepartmentDescription,
                         };
            var course = result.ToList();
            if (course == null)
            {
                return NotFound(new
                {
                    message = "Hiện tại không có học phần nào"
                });
            }
            return Ok(new
            {
                message = "Danh sách học phần",
                data = course
            });
        }

        [HttpPut("update-course")]
        public async Task<IActionResult> UpdateCourse([FromForm] CourseDTO courseDTO, [FromForm] IFormFile? image)
        {
            //var errorResult = KiemTraTokenAdmin();
            //if (errorResult != null)
            //{
            //    return errorResult;
            //}
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (!_context.Departments.Any(d => d.DepartmentId == courseDTO.CourseDepartmentId))
            {
                return BadRequest("Phòng ban không tồn tại");
            }
            if (!_context.Courses.Any(c => c.CourseId == courseDTO.CourseId))
            {
                return BadRequest("Học phần không tồn tại");
            }
            var course = new Course();
            course.CourseUpdateAt = DateTime.Now;
            course.CourseDepartmentId = courseDTO.CourseDepartmentId;
            course.CourseTitle = Regex.Replace(courseDTO.CourseTitle.Trim(), @"\s+", " ");
            course.CourseTotalSemester = courseDTO.CourseTotalSemester;
            course.CourseId = courseDTO.CourseId;
            if (courseDTO.CourseShortdescription != null)
            {
                course.CourseShortdescription = Regex.Replace(courseDTO.CourseShortdescription.Trim(), @"\s+", " ");
            }
            if (courseDTO.CourseDescription != null)
            {
                course.CourseDescription = Regex.Replace(courseDTO.CourseDescription.Trim(), @"\s+", " ");
            }
            //Xử lý upload ảnh
            if (image != null && image.Length > 0)
            {
                // Kiểm tra loại file (chỉ chấp nhận ảnh PNG, JPG, JPEG)
                var allowedExtensions = new[] { ".png", ".jpg", ".jpeg" };
                var fileExtension = Path.GetExtension(image.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { message = "Chỉ chấp nhận file ảnh định dạng PNG, JPG, JPEG." });
                }

                // Tạo tên file duy nhất
                string uniqueFileName = $"{course.CourseId}{fileExtension}";

                // Đường dẫn thư mục lưu file
                string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Course");

                // Tạo thư mục nếu chưa tồn tại
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                // Đường dẫn đầy đủ của file
                string filePath = Path.Combine(uploadPath, uniqueFileName);

                // Kiểm tra và xóa ảnh cũ nếu tồn tại
                if (!string.IsNullOrEmpty(course.CourseImage))
                {
                    string oldFilePath = Path.Combine(uploadPath, course.CourseImage);
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        try
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                        catch (Exception ex)
                        {
                            return StatusCode(500, new { message = "Lỗi khi xóa ảnh cũ.", error = ex.Message });
                        }
                    }
                }
                // Lưu file vào local
                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }
                    course.CourseImage = uniqueFileName;
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "Lỗi khi lưu ảnh.", error = ex.Message });
                }
            }
            _context.Courses.Update(course);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }

            return NoContent();
        }

        [HttpPost("add-course")]
        public async Task<ActionResult<Course>> AddCourse([FromForm] CourseDTO courseDTO, [FromForm]IFormFile? image)
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
            if (!_context.Departments.Any(d => d.DepartmentId == courseDTO.CourseDepartmentId))
            {
                return BadRequest("Phòng ban không tồn tại");
            }
            var course = new Course();
            course.CourseUpdateAt = DateTime.Now;
            course.CourseDepartmentId = courseDTO.CourseDepartmentId;
            course.CourseTitle = Regex.Replace(courseDTO.CourseTitle.Trim(), @"\s+", " ");
            course.CourseTotalSemester = courseDTO.CourseTotalSemester; 
            if (courseDTO.CourseShortdescription != null)
            {
                course.CourseShortdescription = Regex.Replace(courseDTO.CourseShortdescription.Trim(), @"\s+", " ");
            }
            if (courseDTO.CourseDescription != null)
            {
                course.CourseDescription = Regex.Replace(courseDTO.CourseDescription.Trim(), @"\s+", " ");
            }
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            //Xử lý upload ảnh
            if (image != null && image.Length > 0)
            {
                // Kiểm tra loại file (chỉ chấp nhận ảnh PNG, JPG, JPEG)
                var allowedExtensions = new[] { ".png", ".jpg", ".jpeg" };
                var fileExtension = Path.GetExtension(image.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { message = "Chỉ chấp nhận file ảnh định dạng PNG, JPG, JPEG." });
                }

                // Tạo tên file duy nhất
                string uniqueFileName = $"{course.CourseId}{fileExtension}";

                // Đường dẫn thư mục lưu file
                string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Course");

                // Tạo thư mục nếu chưa tồn tại
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                // Đường dẫn đầy đủ của file
                string filePath = Path.Combine(uploadPath, uniqueFileName);

                // Lưu file vào local
                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }
                    course.CourseImage = uniqueFileName;
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "Lỗi khi lưu ảnh.", error = ex.Message });
                }
            }
            _context.Courses.Update(course);
            _context.SaveChanges();
            return Ok(new
            {
                message = "Thêm học phần thành công",
                data = course
            });
        }

        [HttpDelete("delete-course")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            try
            {
                var errorResult = KiemTraTokenAdmin();
                if (errorResult != null)
                {
                    return errorResult;
                }
                var course = await _context.Courses.FindAsync(id);
                if (course == null)
                {
                    return NotFound(new { message = "Học phần không tồn tại" });
                }
                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();

                return Conflict(new { message = "Xóa học phần thành công" });
            }
            catch (Exception ex)
            {
                return NotFound(new { message = "Không thể xóa, học phần đang liên kết với bảng khác" });
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
