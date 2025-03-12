using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBaiGiangAPI.Data;
using WebBaiGiangAPI.Models;
using System.IdentityModel.Tokens.Jwt;

namespace WebBaiGiangAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentClassesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;
        private readonly IJwtService _jwtService;
        public StudentClassesController(AppDbContext context, EmailService emailService, IJwtService jwtService)
        {
            _context = context;
            _emailService = emailService;
            _jwtService = jwtService;
        }

        [HttpPost("invite-students")]
        public async Task<IActionResult> InviteStudents(int classId, [FromBody] List<string> studentEmail)
        {
            // Kiểm tra quyền giáo viên
            var errorResult = KiemTraTokenTeacher();
            if (errorResult != null)
                return errorResult;

            if (studentEmail == null || !studentEmail.Any())
                return BadRequest("Danh sách email không hợp lệ.");

            // Lấy danh sách sinh viên từ email
            var students = await _context.Users
                .Where(s => studentEmail.Contains(s.UsersEmail) && s.UsersRoleId == 3)
                .ToListAsync();

            // Xác định các email không hợp lệ
            var validEmails = students.Select(s => s.UsersEmail).ToHashSet();
            var invalidEmails = studentEmail.Where(email => !validEmails.Contains(email)).ToList();

            // Lấy danh sách `StudentClasses` một lần
            var existingRecords = await _context.StudentClasses
                .Where(sc => sc.ScClassId == classId && students.Select(s => s.UsersId).Contains(sc.ScStudentId))
                .ToListAsync();

            var studentClassesToAdd = new List<StudentClass>();
            var studentsToUpdate = new List<StudentClass>(); // Danh sách cần cập nhật token

            foreach (var student in students)
            {
                var existingRecord = existingRecords.FirstOrDefault(sc => sc.ScStudentId == student.UsersId);
                var token = Guid.NewGuid().ToString(); // Tạo token mới

                if (existingRecord == null)
                {
                    // Thêm mới sinh viên vào lớp học
                    studentClassesToAdd.Add(new StudentClass
                    {
                        ScStudentId = student.UsersId,
                        ScClassId = classId,
                        ScToken = token,
                        ScCreateAt = DateTime.UtcNow,
                        ScStatus = 0 // Chưa xác nhận
                    });
                }
                else if (existingRecord.ScStatus == 0)
                {
                    // Cập nhật token mới nếu chưa xác nhận
                    existingRecord.ScToken = token;
                    existingRecord.ScCreateAt = DateTime.UtcNow;
                    studentsToUpdate.Add(existingRecord);
                }

                // Gửi email mời tham gia lớp học
                await _emailService.SendClassInvitationEmail(student.UsersEmail, student.UsersName, token);
            }

            // Cập nhật database nếu có thay đổi
            if (studentClassesToAdd.Any())
            {
                await _context.StudentClasses.AddRangeAsync(studentClassesToAdd);
            }

            if (studentsToUpdate.Any())
            {
                _context.StudentClasses.UpdateRange(studentsToUpdate);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Lời mời đã được gửi.",
                InvalidEmails = invalidEmails
            });
        }

        [HttpGet("confirm-invitation")]
        public async Task<IActionResult> ConfirmInvitation([FromQuery] string? tokenConfirm)
        {
            // Kiểm tra Authorization Token
            var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader))
                return Unauthorized(new { message = "Token không tồn tại" });

            var token = _jwtService.GetToken(authHeader);
            if (token == null)
                return Unauthorized(new { message = "Token không hợp lệ hoặc đã hết hạn" });

            // Trích xuất thông tin từ JWT
            var tokenInfo = _jwtService.GetTokenInfoFromToken(token);
            if (!tokenInfo.TryGetValue(JwtRegisteredClaimNames.UniqueName, out string username) ||
                !tokenInfo.TryGetValue("role", out string role))
                return Unauthorized(new { message = "Token thiếu thông tin cần thiết" });

            // Kiểm tra tài khoản người dùng
            var user = await _context.Users.SingleOrDefaultAsync(u => u.UsersUsername == username);
            if (user == null)
                return Unauthorized(new { message = "Tài khoản không tồn tại" });

            // Kiểm tra user có đăng xuất không
            var lastUserLog = await _context.UserLogs
                .Where(u => u.UlogUsername == username)
                .OrderByDescending(u => u.UlogId)
                .FirstOrDefaultAsync();

            if (lastUserLog == null || lastUserLog.UlogLogoutDate != null)
                return Unauthorized(new { message = "Token không hợp lệ hoặc đã hết hạn" });

            // Kiểm tra quyền truy cập (chỉ sinh viên mới được xác nhận)
            if (role != "student" && role != "3")
                return Unauthorized(new { message = "Bạn không có quyền xác nhận lớp học" });

            // Kiểm tra token lời mời hợp lệ
            var invitation = await _context.StudentClasses
                .FirstOrDefaultAsync(sc => sc.ScToken == tokenConfirm);

            if (invitation == null)
                return BadRequest(new { message = "Lời mời không hợp lệ hoặc đã hết hạn" });

            // Kiểm tra sinh viên có tồn tại và có đúng email không
            var student = await _context.Students
                .Include(s => s.Users)
                .SingleOrDefaultAsync(s => s.StudentId == invitation.ScStudentId);

            if (student?.Users == null)
                return Unauthorized(new { message = "Sinh viên không tồn tại hoặc thông tin tài khoản không hợp lệ" });

            if (student.Users.UsersUsername != user.UsersUsername)
                return Unauthorized(new { message = "Email đăng nhập không khớp với email được mời" });

            // Xác nhận tham gia lớp học
            invitation.ScStatus = 1;   // Đánh dấu đã xác nhận
            invitation.ScToken = null; // Xóa token lời mời

            await _context.SaveChangesAsync();
            return Ok(new { message = "Bạn đã xác nhận tham gia lớp học thành công." });
        }

        [HttpGet("get-confirmed-students")] 
        public async Task<IActionResult> GetConfirmedStudents(int classId)
        {
            var students = await _context.StudentClasses
                .Where(sc => sc.ScClassId == classId && sc.ScStatus == 1)
                .Select(sc => new
                {
                    sc.ScId,
                    sc.Student.Users.UsersName,
                    sc.Student.Users.UsersEmail,
                    sc.Classes.ClassTitle
                })
                .ToListAsync();

            return Ok(students);
        }

        [HttpDelete("remove-student")]
        public async Task<IActionResult> RemoveStudent(int classId, int studentId)
        {
            var studentClass = await _context.StudentClasses.FirstOrDefaultAsync(sc => sc.ScStudentId == studentId && sc.ScClassId == classId);
            if (studentClass == null)
            {
                return NotFound("Lớp học hoặc sinh viên không tồn tại");
            }

            _context.StudentClasses.Remove(studentClass);
            await _context.SaveChangesAsync();

            return Ok("Thành công");
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
