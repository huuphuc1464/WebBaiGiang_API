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
    public class SchoolYearsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;
        public SchoolYearsController(AppDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpGet("get-school-years")]
        public async Task<ActionResult<IEnumerable<SchoolYear>>> GetSchoolYears()
        {
            var errorResult = KiemTraTokenAdminTeacher();
            if (errorResult != null)
            {
                return errorResult;
            }

            var sy = await _context.SchoolYears.ToListAsync();
            if (sy == null)
            {
                return NotFound(new
                {
                    message = "Hiện tại không có năm học nào"
                });
            }
            return Ok(new
            {
                message = "Danh sách năm học",
                data = sy
            });
        }

        [HttpGet("get-school-year")]
        public async Task<ActionResult<SchoolYear>> GetSchoolYear(int id)
        {
            var errorResult = KiemTraTokenAdminTeacher();
            if (errorResult != null)
            {
                return errorResult;
            }
            var sy = await _context.SchoolYears.Where(s => s.SyearId == id).ToListAsync();
            if (sy == null || !sy.Any())
            {
                return NotFound(new
                {
                    message = "Năm học không tồn tại"
                });
            }
            return Ok(sy);
        }

        [HttpPut("update-school-yaer")]
        public async Task<IActionResult> UpdateSchoolYear(SchoolYear schoolYear)
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

            var sy = await _context.SchoolYears.SingleOrDefaultAsync(s => s.SyearId == schoolYear.SyearId);
            if (sy == null)
            {
                return BadRequest(new { message = "Năm học không tồn tại" });
            }
            if (!KiemTraNamHoc(schoolYear.SyearTitle))
            {
                return BadRequest(new { message = "Năm học không đúng định dạng" });
            }
            sy.SyearTitle = Regex.Replace(schoolYear.SyearTitle.Trim(), @"\s+", " ");
            sy.SyearDescription = Regex.Replace(schoolYear.SyearDescription.Trim(), @"\s+", " ");

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SchoolYearExists(schoolYear.SyearId))
                {
                    return Conflict(new
                    {
                        message = "Năm học không tồn tại"
                    });
                }
                else
                {
                    throw;
                }
            }

            return Ok(new
            {
                message = "Thay đổi thông tin năm học thành công",
                data = sy
            });
        }

        [HttpPost("add-school-year")]
        public async Task<ActionResult<SchoolYear>> AddSchoolYear(SchoolYear schoolYear)
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
            if (!KiemTraNamHoc(schoolYear.SyearTitle))
            {
                return BadRequest(new { message = "Năm học không đúng định dạng", data = schoolYear });
            }
            schoolYear.SyearTitle = Regex.Replace(schoolYear.SyearTitle.Trim(), @"\s+", " ");
            schoolYear.SyearDescription = Regex.Replace(schoolYear.SyearDescription.Trim(), @"\s+", " ");
            _context.SchoolYears.Add(schoolYear);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Thêm năm học thành công",
                data = schoolYear
            });
        }

        [HttpDelete("delete-school-year")]
        public async Task<IActionResult> DeleteSchoolYear(int id)
        {
            var errorResult = KiemTraTokenAdminTeacher();
            if (errorResult != null)
            {
                return errorResult;
            }
            var schoolyear = await _context.SchoolYears.FindAsync(id);
            if (schoolyear == null)
            {
                return NotFound(new { message = "Học kỳ không tồn tại" });
            }

            var result = from s in _context.SchoolYears
                          join c in _context.Classes on s.SyearId equals c.ClassSyearId
                          where s.SyearId == id
                          select new { s, c};

            var data = result.Any();
            if (data)
            {
                return BadRequest(new
                {
                    message = "Học kỳ đang có lớp học và điểm, không thể xóa"
                });
            }

            _context.SchoolYears.Remove(schoolyear);
            await _context.SaveChangesAsync();

            return Conflict(new { message = "Xóa học kỳ thành công" });
        }

        private bool SchoolYearExists(int id)
        {
            return _context.SchoolYears.Any(e => e.SyearId == id);
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
        public static bool KiemTraNamHoc(string namHoc)
        {
            // Regex kiểm tra định dạng "YYYY - YYYY"
            string pattern = @"^(\d{4})\s*-\s*(\d{4})$";
            Match match = Regex.Match(namHoc, pattern);

            if (!match.Success)
            {
                return false; // Định dạng sai
            }

            int namA = int.Parse(match.Groups[1].Value);
            int namB = int.Parse(match.Groups[2].Value);

            return (namB - namA) == 1; // Kiểm tra khoảng cách giữa hai năm
        }
    }
}
