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
    public class AssignmentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;
        public AssignmentsController(AppDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpGet("get-assignments")]
        public async Task<ActionResult<IEnumerable<Assignment>>> GetAssignments(int tID)
        {
            var errorResult = KiemTraTokenTeacher();
            if (errorResult != null)
            {
                return errorResult;
            }
            var ass = await _context.Assignments.Where(a => a.AssignmentTeacherId == tID).ToListAsync();
            if (ass == null || !ass.Any())
            {
                return NotFound(new
                {
                    message = "Hiện tại không có bài tập nào"
                });
            }
            return Ok(new
            {
                message = "Danh sách bài tập",
                data = ass
            });
        }

        [HttpGet("get-assignment")]
        public async Task<ActionResult<Assignment>> GetAssignment(int teacherID, int assignmentID)
        {
            var errorResult = KiemTraTokenTeacher();
            if (errorResult != null)
            {
                return errorResult;
            }
            var ass = from a in _context.Assignments
                         join u in _context.Users on a.AssignmentTeacherId equals u.UsersId
                         join c in _context.Classes on a.AssignmentClassId equals c.ClassId
                         where a.AssignmentId == assignmentID && a.AssignmentTeacherId == teacherID
                         select new
                         {
                             a.AssignmentId,
                             a.AssignmentTitle,
                             a.AssignmentDescription,
                             a.AssignmentFilename,
                             a.AssignmentDeadline,
                             a.AssignmentCreateAt,
                             a.AssignmentStart,
                             a.AssignmentStatus,
                             c.ClassTitle,
                             c.ClassDescription,
                             u.UsersName,
                             u.UsersEmail,
                             u.UsersMobile,
                             u.UsersDob,
                             u.UsersImage,
                         };
            if (ass == null || !ass.Any())
            {
                return NotFound(new
                {
                    message = "Bài tập không tồn tại"
                });
            }
            return Ok(ass);
        }

        [HttpGet("get-all-assignments")]
        public async Task<ActionResult<IEnumerable<Assignment>>> GetAllAssignments()
        {
            var errorResult = KiemTraTokenAdmin();
            if (errorResult != null)
            {
                return errorResult;
            }
            var ass = await _context.Assignments.ToListAsync();
            if (ass == null || !ass.Any())
            {
                return NotFound(new
                {
                    message = "Hiện tại không có bài tập nào"
                });
            }
            return Ok(new
            {
                message = "Danh sách bài tập",
                data = ass
            });
        }

        [HttpGet("get-all-assignment")]
        public async Task<ActionResult<Assignment>> GetAllAssignment(int assignmentID)
        {
            var errorResult = KiemTraTokenAdmin();
            if (errorResult != null)
            {
                return errorResult;
            }
            var ass = from a in _context.Assignments
                      join u in _context.Users on a.AssignmentTeacherId equals u.UsersId
                      join c in _context.Classes on a.AssignmentClassId equals c.ClassId
                      where a.AssignmentId == assignmentID
                      select new
                      {
                          a.AssignmentId,
                          a.AssignmentTitle,
                          a.AssignmentDescription,
                          a.AssignmentFilename,
                          a.AssignmentDeadline,
                          a.AssignmentCreateAt,
                          a.AssignmentStart,
                          a.AssignmentStatus,
                          c.ClassTitle,
                          c.ClassDescription,
                          u.UsersName,
                          u.UsersEmail,
                          u.UsersMobile,
                          u.UsersDob,
                          u.UsersImage,
                      };
            if (ass == null || !ass.Any())
            {
                return NotFound(new
                {
                    message = "Bài tập không tồn tại"
                });
            }
            return Ok(ass);
        }


        [HttpPut("update-assignment")]
        public async Task<IActionResult> UpdateAssignment([FromForm]AssignmentDTO assignmentDTO, [FromForm]IFormFile? file)
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

            var assignment = await _context.Assignments.SingleOrDefaultAsync(a => a.AssignmentId == assignmentDTO.AssignmentId);

            if (!_context.Classes.Any(c => c.ClassId == assignmentDTO.AssignmentClassId))
            {
                return BadRequest(new
                {
                    message = "Lớp học không tồn tại",
                    data = assignmentDTO
                });
            }
            assignment.AssignmentClassId = assignmentDTO.AssignmentClassId;

            if (!_context.Users.Any(t => t.UsersId == assignmentDTO.AssignmentTeacherId && t.UsersRoleId == 2))
            {
                return BadRequest(new
                {
                    message = "Giáo viên không tồn tại",
                    data = assignmentDTO
                });
            }
            assignment.AssignmentTeacherId = assignmentDTO.AssignmentTeacherId;
            assignment.AssignmentCreateAt = DateTime.Now;

            if (assignmentDTO.AssignmentStart == null)
            {
                assignment.AssignmentStart = DateTime.Now;
            }
            else
            {
                assignment.AssignmentStart = assignmentDTO.AssignmentStart;
            }
            if (assignmentDTO.AssignmentStart <= DateTime.Now)
            {
                return Conflict("Ngày bắt đầu không thể nhỏ hơn ngày hiện tại.");
            }
            if (assignmentDTO.AssignmentDeadline <= DateTime.Now)
            {
                return Conflict("Ngày hết hạn không thể nhỏ hơn ngày hiện tại.");
            }
            if (assignmentDTO.AssignmentDeadline != null && assignmentDTO.AssignmentStart > assignmentDTO.AssignmentDeadline)
            {
                return BadRequest(new
                {
                    message = "Thời gian bắt đầu phải nhỏ hơn thời gian hết hạn",
                    data = assignmentDTO
                });
            }
            assignment.AssignmentDeadline = assignmentDTO.AssignmentDeadline;
            assignment.AssignmentTitle = Regex.Replace(assignmentDTO.AssignmentTitle.Trim(), @"\s+", " ");
            assignment.AssignmentDescription = Regex.Replace(assignmentDTO.AssignmentDescription.Trim(), @"\s+", " ");
            assignment.AssignmentStatus = assignmentDTO.AssignmentStatus;
            // Xử lý file nếu có
            if (file != null && file.Length > 0)
            {
                var fileExtension = Path.GetExtension(file.FileName).ToLower();

                // Tạo tên file duy nhất
                string timestamp = assignment.AssignmentCreateAt.ToString("yyyyMMddHHmmss") ?? DateTime.Now.ToString("yyyyMMddHHmmss");
                string uniqueFileName = $"{assignment.AssignmentTeacherId}_{assignment.AssignmentClassId}_{timestamp}{fileExtension}";
                // Đường dẫn thư mục lưu file
                string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Assignments");

                // Tạo thư mục nếu chưa tồn tại
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                // Đường dẫn đầy đủ của file
                string filePath = Path.Combine(uploadPath, uniqueFileName);

                if (!string.IsNullOrEmpty(assignment.AssignmentFilename))
                {
                    string oldFilePath = Path.Combine(uploadPath, assignment.AssignmentFilename);
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
                        await file.CopyToAsync(stream);
                    }
                    assignment.AssignmentFilename = uniqueFileName;
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "Lỗi khi lưu file.", error = ex.Message });
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AssignmentExists(assignment.AssignmentId))
                {
                    return NotFound("Bài tập không tồn tại");
                }
                else
                {
                    throw;
                }
            }

            return Ok( new { message = "Thay đổi thông tin bài tập thành công."});
        }

        [HttpPost("add-assignment")]
        public async Task<ActionResult<Assignment>> AddAssignment([FromForm]AssignmentDTO assignmentDTO, [FromForm]IFormFile? file)
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
            
            Assignment assignment = new Assignment();

            if (!_context.Classes.Any(c => c.ClassId == assignmentDTO.AssignmentClassId))
            {
                return BadRequest(new
                {
                    message = "Lớp học không tồn tại",
                    data = assignmentDTO
                });
            }
            assignment.AssignmentClassId = assignmentDTO.AssignmentClassId;

            if (!_context.Users.Any(t => t.UsersId == assignmentDTO.AssignmentTeacherId && t.UsersRoleId == 2))
            {
                return BadRequest(new
                {
                    message = "Giáo viên không tồn tại",
                    data = assignmentDTO
                });
            }
            assignment.AssignmentTeacherId = assignmentDTO.AssignmentTeacherId;
            assignment.AssignmentCreateAt = DateTime.Now;

            if (assignmentDTO.AssignmentStart == null)
            {
                assignment.AssignmentStart = DateTime.Now;
            }
            else
            {
                assignment.AssignmentStart = assignmentDTO.AssignmentStart;
            }
            if (assignmentDTO.AssignmentStart <= DateTime.Now)
            {
                return Conflict("Ngày bắt đầu không thể nhỏ hơn ngày hiện tại.");
            }
            if (assignmentDTO.AssignmentDeadline <= DateTime.Now)
            {
                return Conflict("Ngày hết hạn không thể nhỏ hơn ngày hiện tại.");
            }
            if (assignmentDTO.AssignmentDeadline != null && assignmentDTO.AssignmentStart > assignmentDTO.AssignmentDeadline)
            {
                return BadRequest(new
                {
                    message = "Thời gian bắt đầu phải nhỏ hơn thời gian hết hạn",
                    data = assignmentDTO
                });
            }
            assignment.AssignmentDeadline = assignmentDTO.AssignmentDeadline;
            assignment.AssignmentTitle = Regex.Replace(assignmentDTO.AssignmentTitle.Trim(), @"\s+", " ");
            assignment.AssignmentDescription = Regex.Replace(assignmentDTO.AssignmentDescription.Trim(), @"\s+", " ");
            assignment.AssignmentStatus = assignmentDTO.AssignmentStatus;
            // Xử lý file nếu có
            if (file != null && file.Length > 0)
            {
                var fileExtension = Path.GetExtension(file.FileName).ToLower();

                // Tạo tên file duy nhất
                string timestamp = assignment.AssignmentCreateAt.ToString("yyyyMMddHHmmss") ?? DateTime.Now.ToString("yyyyMMddHHmmss");
                string uniqueFileName = $"{assignment.AssignmentTeacherId}_{assignment.AssignmentClassId}_{timestamp}{fileExtension}";
                // Đường dẫn thư mục lưu file
                string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Assignments");

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
                        await file.CopyToAsync(stream);
                    }
                    assignment.AssignmentFilename = uniqueFileName;
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "Lỗi khi lưu file.", error = ex.Message });
                }
            }

            _context.Assignments.Add(assignment);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Thêm bài tập thành công",
                data = assignment
            });
        }

        [HttpDelete("delete-assignment")]
        public async Task<IActionResult> DeleteAssignment(int id)
        {
            try
            {
                var errorResult = KiemTraTokenTeacher();
                if (errorResult != null)
                {
                    return errorResult;
                }
                var assignment = await _context.Assignments.FindAsync(id);
                if (assignment == null)
                {
                    return NotFound(new { message = "Bài tập không tồn tại" });
                }

                _context.Assignments.Remove(assignment);
                await _context.SaveChangesAsync();

                return Conflict(new { message = "Xóa bài tập thành công" });
            }
            catch (Exception)
            {
                return NotFound(new { message = "Không thể xóa, bài tập đang liên kết với bảng khác" });
            }
        }

        private bool AssignmentExists(int id)
        {
            return _context.Assignments.Any(e => e.AssignmentId == id);
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
