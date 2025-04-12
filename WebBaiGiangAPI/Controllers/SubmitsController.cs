using System;
using System.Collections.Generic;
using System.Linq;
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
    public class SubmitsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;

        public SubmitsController(AppDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // Nộp bài (upload file, nội dung text hoặc link)
        [HttpPost("submissions/{assignmentId}")]
        public async Task<IActionResult> Submissions (int assignmentId, int studentId, [FromForm] IFormFile fileNop)
        {
            DateTime currentDate = DateTime.Now;
            var assignment = await _context.Assignments.FindAsync(assignmentId);
            if (assignment == null)
            {
                return NotFound("Bài tập không tồn tại");
            }
            var isStudent = (from s in _context.Students
                            join sc in _context.StudentClasses on s.StudentId equals sc.ScStudentId
                            join cc in _context.ClassCourses on sc.ScClassId equals cc.ClassId
                            join a in _context.Assignments on cc.CcId equals a.AssignmentClassCourseId
                            where s.StudentId == studentId && a.AssignmentId == assignmentId && sc.ScStatus == 1
                             select s).FirstOrDefault();
            if (isStudent == null)
            {
                return NotFound("Sinh viên không thuộc lớp này");
            }
            if (assignment.AssignmentStart > currentDate)
            {
                return BadRequest("Bài tập này chưa được mở. Bạn không được phép nộp bài");
            }
            if (assignment.AssignmentDeadline < currentDate && assignment.AssignmentStatus == 3) // 3: ko cho nộp bài trễ
            {
                return BadRequest("Hết hạn nộp bài. Bạn không được phép nộp trễ");
            }
            if (assignment.AssignmentDeadline < currentDate && assignment.AssignmentStatus == 0) // 0: ẩn 
            {
                return BadRequest("Bài tập này đã bị ẩn. Bạn không được phép nộp bài");
            }
            int status = 0;
            if (assignment.AssignmentDeadline < currentDate && assignment.AssignmentStatus == 2) // 2: cho phép nộp bài trễ
            {
                status = 0;
            }
            else if (assignment.AssignmentDeadline == null && assignment.AssignmentStatus == 1) // 1: cho phép và ko có dealine
            {
                status = 1;
            }
            else
            {
                status = 1;
            }

            // Kiểm tra xem sinh viên đã nộp bài hay chưa
            var existingSubmit = await _context.Submits
                .FirstOrDefaultAsync(s => s.SubmitAssignmentId == assignmentId && s.SubmitStudentId == studentId);
            if (existingSubmit != null)
            {
                return BadRequest("Bạn đã nộp bài này rồi");
            }

            // Kiểm tra xem file có tồn tại không
            if (fileNop == null || fileNop.Length == 0)
            {
                return BadRequest("File không tồn tại");
            }
            // Kiểm tra định dạng file
            var allowedExtensions = new[] { ".rar", ".zip", ".pdf", ".doc", ".docx", ".txt" };
            var fileExtension = Path.GetExtension(fileNop.FileName);
            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest("Định dạng file không hợp lệ. Chỉ cho phép nộp file .rar, .zip, .pdf, .doc, .docx, .txt");
            }
            // Kiểm tra kích thước file
            var maxFileSize = 10 * 1024 * 1024; // 10 MB
            if (fileNop.Length > maxFileSize)
            {
                return BadRequest("Kích thước file không được vượt quá 10 MB");
            }

            // Chuẩn hóa tên file
            var fileName = Path.GetFileNameWithoutExtension(fileNop.FileName);
            var fileExtensionWithoutDot = fileExtension.TrimStart('.');
            var newFileName = $"{assignmentId}_{isStudent.StudentCode}_{currentDate:yyyyMMddHHmmss}.{fileExtensionWithoutDot}";

            // lưu vào thư mục wwwroot/assignments
            var filePath = Path.Combine("wwwroot", "Assignments", newFileName);
            // Kiểm tra xem thư mục đã tồn tại chưa, nếu chưa thì tạo mới
            var directoryPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await fileNop.CopyToAsync(stream);
            }

            // Lưu thông tin nộp bài vào cơ sở dữ liệu
            var submit = new Submit
            {
                SubmitAssignmentId = assignmentId,
                SubmitStudentId = studentId,
                SubmitFile = newFileName,
                SubmitDate = currentDate,
                SubmitStatus = status
            };
            _context.Submits.Add(submit);

            var studentInfo = _context.Users
                .Where(u => u.UsersId == isStudent.StudentId)
                .Select(u => new { u.UsersName, u.UsersEmail })
                .FirstOrDefault();

            var classCourse = await _context.ClassCourses.FindAsync(assignment.AssignmentClassCourseId);
            var teacherInfo = await _context.TeacherClasses
                .Include(tc => tc.User)
                .Where(tc => tc.TcClassCourseId == assignment.AssignmentClassCourseId)
                .Select(tc => new { tc.User.UsersName, tc.User.UsersEmail })
                .FirstOrDefaultAsync();
            var className = await _context.Classes
                .Where(c => c.ClassId == classCourse.ClassId)
                .Select(c => new { c.ClassTitle })
                .FirstOrDefaultAsync();

            string subject = $"Sinh viên {studentInfo?.UsersName} đã nộp bài tập!";
            string body = $"<h3>Bài tập: {assignment.AssignmentTitle}</h3>"
                        + $"<p>Lớp: {className.ClassTitle}</p>"
                        + $"<p>Sinh viên {studentInfo?.UsersName} đã hoàn thành và nộp bài tập.</p>"
                        + "<p>Vui lòng tập kết quả bài làm của sinh viên.</p>";
            var emailTeacher = await _emailService.SendEmail(teacherInfo.UsersEmail, subject, body);

            var emailStudent = await _emailService.SendEmail(
                 studentInfo?.UsersEmail,
                 "Cảm ơn bạn đã nộp bài tập",
                 $"Chúng tôi xin cảm ơn bạn, {studentInfo?.UsersName}, đã nộp bài tập: \"{assignment.AssignmentTitle}\" thành công. "
                 + "Chúng tôi sẽ sớm kiểm tra kết quả và thông báo cho bạn."
             );
            await _context.SaveChangesAsync();
            return Ok("Nộp bài thành công");
        }

        // Danh sách bài nộp của học sinh cho bài tập đó (bao gồm trạng thái: đã nộp/chưa nộp/trễ hạn) (giáo viên)
        [HttpGet("submissions/detail/{assignmentId}")]
        public async Task<IActionResult> GetSubmissionDetail(int assignmentId)
        {
            var assignment = await _context.Assignments
                .Include(a => a.Submits)
                    .ThenInclude(s => s.Student)
                        .ThenInclude(st => st.Users)
                .Include(a => a.ClassCourse)
                    .ThenInclude(cc => cc.Classes) // để lấy danh sách học sinh của lớp
                        .ThenInclude(c => c.StudentClasses)
                            .ThenInclude(sc => sc.Student)
                                .ThenInclude(st => st.Users)
                .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId);

            if (assignment == null)
                return NotFound("Không tìm thấy bài tập này");

            // Danh sách học sinh trong lớp
            var allStudents = assignment.ClassCourse.Classes.StudentClasses
                .Select(sc => sc.Student)
                .Distinct()
                .ToList();

            // Danh sách học sinh đã nộp
            var submittedStudentIds = assignment.Submits
                .Select(s => s.SubmitStudentId)
                .ToHashSet();

            // Tạo danh sách submissions đã nộp
            var submitted = assignment.Submits.Select(s => new
            {
                s.SubmitFile,
                SubmitDate = (DateTime?)s.SubmitDate,
                SubmitStatus = s.SubmitStatus == 1 ? "Đúng hạn" : "Trễ hạn",
                StudentName = s.Student.Users.UsersName,
                StudentCode = s.Student.StudentCode
            });

            // Tạo danh sách học sinh chưa nộp
            var notSubmitted = allStudents
                .Where(st => !submittedStudentIds.Contains(st.StudentId))
                .Select(st => new
                {
                    SubmitFile = (string?)null,
                    SubmitDate = (DateTime?)null,
                    SubmitStatus = "Chưa nộp",
                    StudentName = st.Users.UsersName,
                    StudentCode = st.StudentCode
                });

            var submissionDetails = new
            {
                assignment.AssignmentTitle,
                assignment.AssignmentDescription,
                assignment.AssignmentDeadline,
                assignment.AssignmentCreateAt,
                assignment.AssignmentStart,
                Submissions = submitted.Concat(notSubmitted)
            };

            return Ok(submissionDetails);
        }

        // Danh sách bài sv đã nộp (kèm trạng thái: đúng hạn/trễ hạn/chưa nộp) (sinh viên)
        [HttpGet("submissions/{studentId}")]
        public async Task<IActionResult> GetStudentSubmissions(int studentId)
        {
            // Lấy danh sách bài tập từ các lớp mà sinh viên này tham gia
            var assignments = await _context.Assignments
                .Include(a => a.ClassCourse)
                    .ThenInclude(cc => cc.Classes)
                        .ThenInclude(c => c.StudentClasses)
                .Include(a => a.Submits)
                .Where(a => a.ClassCourse.Classes.StudentClasses
                            .Any(sc => sc.ScStudentId == studentId))
                .ToListAsync();

            var result = assignments.Select(a =>
            {
                var submit = a.Submits.FirstOrDefault(s => s.SubmitStudentId == studentId);

                string status;
                DateTime? submitDate = null;
                string? file = null;

                if (submit != null)
                {
                    submitDate = submit.SubmitDate;
                    file = submit.SubmitFile;
                    status = submit.SubmitStatus == 1 ? "Đúng hạn" : "Trễ hạn";
                }
                else
                {
                    status = "Chưa nộp";
                }

                return new
                {
                    a.ClassCourse.CcDescription,
                    a.AssignmentId,
                    a.AssignmentTitle,
                    a.AssignmentDescription,
                    a.AssignmentStart,
                    a.AssignmentDeadline,
                    SubmitFile = file,
                    SubmitDate = submitDate,
                    SubmitStatus = status
                };
            });

            return Ok(result);
        }

        // Sửa bài đã nộp (chỉ trước deadline, nếu cho phép)
        [HttpPut("submissions/{submitId}")]
        public async Task<IActionResult> UpdateSubmission(int submitId, [FromForm] IFormFile fileNop)
        {
            DateTime currentDate = DateTime.Now;
            var submission = await _context.Submits.FindAsync(submitId);
            if (submission == null)
            {
                return NotFound("Không tìm thấy bài nộp này");
            }
            var assignment = await _context.Assignments.FindAsync(submission.SubmitAssignmentId);
            if (assignment.AssignmentDeadline < currentDate && assignment.AssignmentStatus == 3) // 3: ko cho nộp bài trễ
            {
                return BadRequest("Bài nộp đã quá hạn. Không thể sửa bài nộp");
            }
            if (assignment.AssignmentDeadline < currentDate && assignment.AssignmentStatus == 0) // 0: ẩn 
            {
                return BadRequest("Bài tập này đã bị ẩn. Bạn không được phép nộp bài");
            }
            if (assignment.AssignmentDeadline < currentDate && assignment.AssignmentStatus == 2) // 2: cho phép nộp bài trễ
            {
                submission.SubmitStatus = 0;
            }
            else
            {
                submission.SubmitStatus = 1;
            }
            if (fileNop == null || fileNop.Length == 0)
            {
                return BadRequest("File không tồn tại");
            }
            // Kiểm tra định dạng file
            var allowedExtensions = new[] { ".rar", ".zip", ".pdf", ".doc", ".docx", ".txt" };
            var fileExtension = Path.GetExtension(fileNop.FileName);
            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest("Định dạng file không hợp lệ. Chỉ cho phép nộp file .rar, .zip, .pdf, .doc, .docx, .txt");
            }
            // Kiểm tra kích thước file
            var maxFileSize = 10 * 1024 * 1024; // 10 MB
            if (fileNop.Length > maxFileSize)
            {
                return BadRequest("Kích thước file không được vượt quá 10 MB");
            }
            // Kiểm tra xem file đã tồn tại chưa
            var existingFilePath = Path.Combine("wwwroot", "Assignments", submission.SubmitFile);
            if (System.IO.File.Exists(existingFilePath))
            {
                // Xóa file cũ
                System.IO.File.Delete(existingFilePath);
            }
            // Kiểm tra xem thư mục đã tồn tại chưa, nếu chưa thì tạo mới
            var directoryPath = Path.GetDirectoryName(existingFilePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            var studentInfo = _context.Users
                .Join(_context.Students, u => u.UsersId, s => s.StudentId, (u, s) => new { u, s })
                .Where(u => u.u.UsersId == submission.SubmitStudentId)
                .Select(u => new { u.u.UsersName, u.u.UsersEmail, u.s.StudentCode })
                .FirstOrDefault();
            // Chuẩn hóa tên file
            var fileName = Path.GetFileNameWithoutExtension(fileNop.FileName);
            var fileExtensionWithoutDot = fileExtension.TrimStart('.');
            var newFileName = $"{assignment.AssignmentId}_{studentInfo.StudentCode}_{currentDate:yyyyMMddHHmmss}.{fileExtensionWithoutDot}";
            // lưu vào thư mục wwwroot/assignments
            var filePath = Path.Combine("wwwroot", "Assignments", newFileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await fileNop.CopyToAsync(stream);
            }
            submission.SubmitFile = newFileName;
            submission.SubmitDate = currentDate;

            // Gui email thông báo cho giảng viên
            var classCourse = await _context.ClassCourses.FindAsync(assignment.AssignmentClassCourseId);
            var teacherInfo = await _context.TeacherClasses
                .Include(tc => tc.User)
                .Where(tc => tc.TcClassCourseId == assignment.AssignmentClassCourseId)
                .Select(tc => new { tc.User.UsersName, tc.User.UsersEmail })
                .FirstOrDefaultAsync();
            var className = await _context.Classes
                .Where(c => c.ClassId == classCourse.ClassId)
                .Select(c => new { c.ClassTitle })
                .FirstOrDefaultAsync();
            string subject = $"Sinh viên {studentInfo?.UsersName} đã cập nhật và hoàn thành bài tập!";
            string body = $"<h3>Bài tập: {assignment.AssignmentTitle}</h3>"
                        + $"<p>Lớp: {className.ClassTitle}</p>"
                        + $"<p>Sinh viên {studentInfo?.UsersName} đã cập nhật và hoàn thành bài tập.</p>"
                        + "<p>Vui lòng tập kết quả bài làm của sinh viên.</p>";
            var emailTeacher = await _emailService.SendEmail(teacherInfo.UsersEmail, subject, body);

            var emailStudent = await _emailService.SendEmail(
                 studentInfo?.UsersEmail,
                 "Cảm ơn bạn đã cập nhật và hoàn thành bài tập",
                 $"Chúng tôi xin cảm ơn bạn, {studentInfo?.UsersName}, đã nộp bài tập: \"{assignment.AssignmentTitle}\" thành công. "
                 + "Chúng tôi sẽ sớm kiểm tra kết quả và thông báo cho bạn."
             );

            await _context.SaveChangesAsync();
            return Ok("Cập nhật bài nộp thành công");
        }

        // Xoá bài đã nộp (nếu cho phép nộp sau dealine thì có thể xóa, ko thì ko thể xoá)
        [HttpDelete("submissions/{submitId}")]
        public async Task<IActionResult> DeleteSubmission(int submitId)
        {
            var submission = await _context.Submits.FindAsync(submitId);
            if (submission == null)
            {
                return NotFound("Không tìm thấy bài nộp này");
            }
            var assignment = await _context.Assignments.FindAsync(submission.SubmitAssignmentId);
            if (assignment.AssignmentDeadline < DateTime.Now && assignment.AssignmentStatus == 3) // 3: ko cho nộp bài trễ
            {
                return BadRequest("Bài nộp đã quá hạn. Không thể xóa bài nộp");
            }
            if (assignment.AssignmentDeadline < DateTime.Now && assignment.AssignmentStatus == 0) // 0: ẩn 
            {
                return BadRequest("Bài tập này đã bị ẩn. Bạn không được phép xóa bài nộp");
            }
            if (assignment.AssignmentDeadline < DateTime.Now && assignment.AssignmentStatus == 2) // 2: cho phép nộp bài trễ
            {
                submission.SubmitStatus = 0;
            }
            else
            {
                submission.SubmitStatus = 1;
            }
            // Xóa file trên server
            var filePath = Path.Combine("wwwroot", "Assignments", submission.SubmitFile);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
            _context.Submits.Remove(submission);
            await _context.SaveChangesAsync();
            return Ok("Xóa bài nộp thành công");
        }
    }
}
