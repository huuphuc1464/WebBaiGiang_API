using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;
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
            var filePath = Path.Combine("wwwroot", "Assignments", submission.SubmitFile);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
            _context.Submits.Remove(submission);
            await _context.SaveChangesAsync();
            return Ok("Xóa bài nộp thành công");
        }

        /*
         "Thống kê theo bài tập: (1)
            - Tổng số học sinh được giao bài
            - Số học sinh đã nộp
            - Số học sinh chưa nộp
            - Tỷ lệ nộp đúng hạn
         */
        [HttpGet("submissions/statistics/{assignmentId}")]
        public async Task<IActionResult> StatisticsByAssignment(int assignmentId)
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
                .Include(a => a.ClassCourse)
                    .ThenInclude(cc => cc.Course)
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
                StudentCode = s.Student.StudentCode,
                StudentEmail = s.Student.Users.UsersEmail
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
                    StudentCode = st.StudentCode,
                    StudentEmail = st.Users.UsersEmail
                });
            var submissionDetails = new
            {
                assignment.AssignmentTitle,
                assignment.AssignmentDescription,
                assignment.AssignmentFilename,
                assignment.ClassCourse.CcDescription,
                assignment.ClassCourse.Classes.ClassTitle,
                assignment.ClassCourse.Course.CourseTitle,
                assignment.AssignmentDeadline,
                assignment.AssignmentCreateAt,
                assignment.AssignmentStart,
                assignment.AssignmentStatus,
                Submissions = submitted.Concat(notSubmitted)
            };
            int totalStudents = allStudents.Count;
            int submittedCount = submitted.Count();
            int notSubmittedCount = notSubmitted.Count();
            double onTimeRate = (double)submitted.Count(s => s.SubmitStatus == "Đúng hạn") / totalStudents * 100;
            return Ok(new
            {
                TotalStudents = totalStudents,
                SubmittedCount = submittedCount,
                NotSubmittedCount = notSubmittedCount,
                OnTimeRate = onTimeRate,
                SubmissionDetails = submissionDetails
            });
        }

        /*
        "Thống kê theo học sinh: (2)
            -Tổng số bài đã được giao
            -Số bài đã nộp
            - Tỷ lệ nộp bài
            - Danh sách bài chưa nộp
         */
        [HttpGet("submissions/statistics/student/{studentId}")]
        public async Task<IActionResult> StatisticsByStudent(int studentId)
        {
            var student = await _context.Students
                .Include(s => s.StudentClasses)
                    .ThenInclude(sc => sc.Classes)
                        .ThenInclude(c => c.ClassCourses)
                            .ThenInclude(cc => cc.Assignments)
                                .ThenInclude(a => a.Submits) // thêm include cho Submits
                .Include(s => s.Users) // thêm include cho Users
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
                return NotFound("Không tìm thấy sinh viên này");

            // Lấy tất cả bài tập được giao thông qua các lớp mà học sinh tham gia
            var assignments = student.StudentClasses
                .SelectMany(sc => sc.Classes.ClassCourses.SelectMany(cc => cc.Assignments))
                .Distinct()
                .ToList();

            int totalAssigned = assignments.Count;
            int totalSubmitted = assignments.Count(a => a.Submits.Any(s => s.SubmitStudentId == studentId));
            double submissionRate = totalAssigned == 0 ? 0 : Math.Round((double)totalSubmitted / totalAssigned * 100, 2);

            var unsubmittedAssignments = assignments
                .Where(a => !a.Submits.Any(s => s.SubmitStudentId == studentId))
                .Select(a => new
                {
                    a.AssignmentId,
                    a.AssignmentTitle,
                    a.AssignmentDescription,
                    a.AssignmentStart,
                    a.AssignmentDeadline,
                    a.AssignmentCreateAt
                })
                .ToList();

            return Ok(new
            {
                StudentId = studentId,
                StudentName = student.Users?.UsersName,
                TotalAssigned = totalAssigned,
                TotalSubmitted = totalSubmitted,
                SubmissionRate = submissionRate,
                UnsubmittedAssignments = unsubmittedAssignments
            });
        }

        // Tải 1 file bài nộp
        [HttpGet("submissions/download/{submitId}")]
        public async Task<IActionResult> DownloadSubmission(int submitId)
        {
            var submission = await _context.Submits.FindAsync(submitId);
            if (submission == null)
            {
                return NotFound("Không tìm thấy bài nộp này");
            }
            var filePath = Path.Combine("wwwroot", "Assignments", submission.SubmitFile);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("File không tồn tại");
            }
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, "application/octet-stream", submission.SubmitFile);
        }

        // Tải toàn bộ bài nộp dưới dạng .zip
        [HttpGet("submissions/download/all/{assignmentId}")]
        public async Task<IActionResult> DownloadAllSubmissions(int assignmentId)
        {
            var assignment = await _context.Assignments
                .Include(a => a.Submits)
                .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId);
            if (assignment == null)
            {
                return NotFound("Không tìm thấy bài tập này");
            }
            var zipFileName = $"{assignment.AssignmentTitle}_{DateTime.Now:yyyyMMddHHmmss}.zip";
            var zipFilePath = Path.Combine("wwwroot", "Assignments", zipFileName);
            using (var zip = new System.IO.Compression.ZipArchive(new FileStream(zipFilePath, FileMode.Create), System.IO.Compression.ZipArchiveMode.Create))
            {
                foreach (var submission in assignment.Submits)
                {
                    var filePath = Path.Combine("wwwroot", "Assignments", submission.SubmitFile);
                    if (System.IO.File.Exists(filePath))
                    {
                        zip.CreateEntryFromFile(filePath, submission.SubmitFile);
                    }
                }
            }
            var fileBytes = await System.IO.File.ReadAllBytesAsync(zipFilePath);
            return File(fileBytes, "application/zip", zipFileName);
        }

        /* Xuất báo cáo thống kê ra file excel:
        Thống kê theo bài tập:
            - Tổng số học sinh được giao bài
            - Số học sinh đã nộp
            - Số học sinh chưa nộp
            - Tỷ lệ nộp đúng hạn
            -  Biểu đồ: số lượng bài nộp theo ngày (line chart hoặc bar chart) 
        */
        [HttpGet("submissions/statistics/export/{assignmentId}")]
        public async Task<IActionResult> ExportAssignmentStatisticsFull(int assignmentId)
        {
            var result = await StatisticsByAssignment(assignmentId) as OkObjectResult;
            if (result == null || result.Value == null)
                return NotFound("Không tìm thấy dữ liệu để export");

            dynamic data = result.Value;

            int totalStudents = data.TotalStudents;
            int submittedCount = data.SubmittedCount;
            int notSubmittedCount = data.NotSubmittedCount;
            double onTimeRate = data.OnTimeRate;
            string assignmentTitle = data.SubmissionDetails.AssignmentTitle;
            string assignmentDescription = data.SubmissionDetails.AssignmentDescription ?? "Không có mô tả";
            string deadline = data.SubmissionDetails.AssignmentDeadline != null
                ? data.SubmissionDetails.AssignmentDeadline.Value.ToString("dd/MM/yyyy HH:mm:ss")
                : "Không có thời gian";
            DateTime createAt = data.SubmissionDetails.AssignmentCreateAt;
            DateTime start = data.SubmissionDetails.AssignmentStart;
            string status = null;
            if (data.SubmissionDetails.AssignmentStatus == 1)
            {
                status = "Đã giao bài";
            }
            else if (data.SubmissionDetails.AssignmentStatus == 2)
            {
                status = "Đã giao bài và cho phép nộp trễ";
            }
            else if (data.SubmissionDetails.AssignmentStatus == 3)
            {
                status = "Đã giao bài và không cho phép nộp trễ";
            }
            else
            {
                status = "Chưa giao bài";
            }
            var submissions = ((IEnumerable<dynamic>)data.SubmissionDetails.Submissions).ToList();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();

            // === Sheet 1: Tổng quan ===
            var overview = package.Workbook.Worksheets.Add("Tổng quan");
            overview.Cells.Style.Font.Name = "Calibri";
            overview.Cells.Style.Font.Size = 11;

            overview.Cells["A1:D1"].Merge = true;
            overview.Cells["A1"].Value = "BÁO CÁO KẾT QUẢ BÀI TẬP";
            overview.Cells["A1"].Style.Font.Size = 16;
            overview.Cells["A1"].Style.Font.Bold = true;
            overview.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            overview.Cells["A1"].Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            overview.Cells["A1:D1"].Style.Border.BorderAround(ExcelBorderStyle.Thin);

            overview.Cells["A3"].Value = "Tên bài tập:";
            overview.Cells["B3"].Value = assignmentTitle;
            overview.Cells["A4"].Value = "Mô tả:";
            overview.Cells["B4"].Value = assignmentDescription;
            overview.Cells["A5"].Value = "File bài tập";
            overview.Cells["B5"].Value = data.SubmissionDetails.AssignmentFilename ?? "Không có file bài tập";
            overview.Cells["A6"].Value = "Lớp học phần:";
            overview.Cells["B6"].Value = data.SubmissionDetails.CcDescription;
            overview.Cells["A7"].Value = "Lớp:";
            overview.Cells["B7"].Value = data.SubmissionDetails.ClassTitle;
            overview.Cells["A8"].Value = "Học phần:";
            overview.Cells["B8"].Value = data.SubmissionDetails.CourseTitle;
            overview.Cells["A9"].Value = "Thời gian tạo bài tập:";
            overview.Cells["B9"].Value = createAt.ToString("dd/MM/yyyy HH:mm:ss");
            overview.Cells["A10"].Value = "Thời gian bắt đầu nộp bài:";
            overview.Cells["B10"].Value = start.ToString("dd/MM/yyyy HH:mm:ss");
            overview.Cells["A11"].Value = "Thời gian kết thúc nộp bài:";
            overview.Cells["B11"].Value = deadline;
            overview.Cells["A12"].Value = "Trạng thái bài tập:";
            overview.Cells["B12"].Value = status;
            overview.Cells["A13"].Value = "Ngày xuất báo cáo:";
            overview.Cells["B13"].Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            overview.Cells["A15"].Value = "Tổng số học sinh:";
            overview.Cells["B15"].Value = totalStudents;
            overview.Cells["A16"].Value = "Đã nộp:";
            overview.Cells["B16"].Value = submittedCount;
            overview.Cells["A17"].Value = "Chưa nộp:";
            overview.Cells["B17"].Value = notSubmittedCount;
            overview.Cells["A18"].Value = "Tỷ lệ nộp đúng hạn:";
            overview.Cells["B18"].Value = $"{onTimeRate:0.##}%";

            overview.Cells["A3:A18"].Style.Font.Bold = true;
            overview.Cells["A3:A18"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            overview.Cells["A3:B18"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            overview.Cells["A3:A13"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            overview.Cells["A3:A13"].Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#E2EFDA"));

            overview.Cells["A15:A18"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            overview.Cells["A15:A18"].Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#DDEBF7"));

            overview.Cells["B3:B18"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            overview.Cells["A3:B13"].Style.Border.Top.Style = ExcelBorderStyle.Thin;
            overview.Cells["A3:B13"].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            overview.Cells["A3:B13"].Style.Border.Left.Style = ExcelBorderStyle.Thin;
            overview.Cells["A3:B13"].Style.Border.Right.Style = ExcelBorderStyle.Thin;
            overview.Cells["A15:B18"].Style.Border.Top.Style = ExcelBorderStyle.Thin;
            overview.Cells["A15:B18"].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            overview.Cells["A15:B18"].Style.Border.Left.Style = ExcelBorderStyle.Thin;
            overview.Cells["A15:B18"].Style.Border.Right.Style = ExcelBorderStyle.Thin;

            overview.Cells["A3:B18"].AutoFitColumns();

            // === Sheet 2: Danh sách học sinh ===
            var sheet2 = package.Workbook.Worksheets.Add("Danh sách nộp bài");

            string[] headers = { "STT", "Mã số sinh viên", "Họ tên sinh viên", "Email", "Ngày nộp", "File nộp", "Trạng thái" };
            for (int i = 0; i < headers.Length; i++)
            {
                sheet2.Cells[1, i + 1].Value = headers[i];
                sheet2.Cells[1, i + 1].Style.Font.Bold = true;
                sheet2.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet2.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#4472C4"));
                sheet2.Cells[1, i + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                sheet2.Cells[1, i + 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                sheet2.Cells[1, i + 1].Style.Font.Color.SetColor(Color.White);
                sheet2.Cells[1, i + 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            for (int i = 0; i < submissions.Count; i++)
            {
                var s = submissions[i];
                int row = i + 2;
                sheet2.Cells[row, 1].Value = i + 1;
                sheet2.Cells[row, 2].Value = s.StudentCode;
                sheet2.Cells[row, 3].Value = s.StudentName;
                sheet2.Cells[row, 4].Value = s.StudentEmail;
                sheet2.Cells[row, 5].Value = s.SubmitDate != null ? DateTime.Parse(s.SubmitDate.ToString()).ToString("dd/MM/yyyy HH:mm:ss") : "";
                sheet2.Cells[row, 6].Value = s.SubmitFile;
                sheet2.Cells[row, 7].Value = s.SubmitStatus;
                var statusCell = sheet2.Cells[row, 7];
                if (s.SubmitStatus == "Đúng hạn")
                {
                    statusCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    statusCell.Style.Fill.BackgroundColor.SetColor(Color.LightGreen);
                }
                else if (s.SubmitStatus == "Trễ hạn")
                {
                    statusCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    statusCell.Style.Fill.BackgroundColor.SetColor(Color.LightGoldenrodYellow); 
                }
                else if (s.SubmitStatus == "Chưa nộp")
                {
                    statusCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    statusCell.Style.Fill.BackgroundColor.SetColor(Color.LightCoral);
                }

                for (int col = 1; col <= 7; col++)
                {
                    sheet2.Cells[row, col].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }
            }
            sheet2.Cells[2, 1, submissions.Count + 1, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            sheet2.Cells[2, 1, submissions.Count + 1, 7].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            sheet2.Cells[sheet2.Dimension.Address].AutoFitColumns();

            // === Sheet 3: Biểu đồ theo ngày nộp ===
            var sheet3 = package.Workbook.Worksheets.Add("Biểu đồ");

            var groupedByDate = submissions
                .Where(s => s.SubmitDate != null)
                .GroupBy(s => DateTime.Parse(s.SubmitDate.ToString()).Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToList();

            sheet3.Cells[1, 1].Value = "Ngày";
            sheet3.Cells[1, 2].Value = "Số lượng nộp";
            sheet3.Cells[1, 1, 1, 2].Style.Font.Bold = true;
            sheet3.Cells[1, 1, 1, 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
            sheet3.Cells[1, 1, 1, 2].Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#4472C4"));
            sheet3.Cells[1, 1, 1, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            sheet3.Cells[1, 1, 1, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            sheet3.Cells[1, 1, 1, 2].Style.Font.Color.SetColor(Color.White);
            sheet3.Cells[1, 1, 1, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            for (int i = 0; i < groupedByDate.Count; i++)
            {
                sheet3.Cells[i + 2, 1].Value = groupedByDate[i].Date;
                sheet3.Cells[i + 2, 1].Style.Numberformat.Format = "dd/MM/yyyy";
                sheet3.Cells[i + 2, 2].Value = groupedByDate[i].Count;
                sheet3.Cells[i + 2, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                sheet3.Cells[i + 2, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                sheet3.Cells[i + 2, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                sheet3.Cells[i + 2, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                sheet3.Cells[i + 2, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                sheet3.Cells[i + 2, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }

            // Tỉ lệ nộp bài đúng hạn/trễ hạn/chưa nộp
            var totalNotSubmitted = submissions.Count(s => s.SubmitStatus == "Chưa nộp");
            var totalOnTime = submissions.Count(s => s.SubmitStatus == "Đúng hạn");
            var totalLate = submissions.Count(s => s.SubmitStatus == "Trễ hạn");

            sheet3.Cells[groupedByDate.Count + 3, 1].Value = "Tình trạng nộp bài";
            sheet3.Cells[groupedByDate.Count + 3, 1].Style.Font.Bold = true;
            sheet3.Cells[groupedByDate.Count + 3, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            sheet3.Cells[groupedByDate.Count + 3, 1].Style.Fill.BackgroundColor.SetColor(Color.Green);
            sheet3.Cells[groupedByDate.Count + 3, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            sheet3.Cells[groupedByDate.Count + 3, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            sheet3.Cells[groupedByDate.Count + 3, 1].Style.Font.Color.SetColor(Color.White);
            sheet3.Cells[groupedByDate.Count + 3, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

            sheet3.Cells[groupedByDate.Count + 3, 2].Value = "Số lượng";
            sheet3.Cells[groupedByDate.Count + 3, 2].Style.Font.Bold = true;
            sheet3.Cells[groupedByDate.Count + 3, 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
            sheet3.Cells[groupedByDate.Count + 3, 2].Style.Fill.BackgroundColor.SetColor(Color.Green);
            sheet3.Cells[groupedByDate.Count + 3, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            sheet3.Cells[groupedByDate.Count + 3, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            sheet3.Cells[groupedByDate.Count + 3, 2].Style.Font.Color.SetColor(Color.White);
            sheet3.Cells[groupedByDate.Count + 3, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);

            sheet3.Cells[groupedByDate.Count + 4, 1].Value = "Đúng hạn";
            sheet3.Cells[groupedByDate.Count + 4, 2].Value = totalOnTime;
            sheet3.Cells[groupedByDate.Count + 4, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            sheet3.Cells[groupedByDate.Count + 4, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            sheet3.Cells[groupedByDate.Count + 4, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            sheet3.Cells[groupedByDate.Count + 4, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            sheet3.Cells[groupedByDate.Count + 4, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            sheet3.Cells[groupedByDate.Count + 4, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            sheet3.Cells[groupedByDate.Count + 5, 1].Value = "Trễ hạn";
            sheet3.Cells[groupedByDate.Count + 5, 2].Value = totalLate;
            sheet3.Cells[groupedByDate.Count + 5, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            sheet3.Cells[groupedByDate.Count + 5, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            sheet3.Cells[groupedByDate.Count + 5, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            sheet3.Cells[groupedByDate.Count + 5, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            sheet3.Cells[groupedByDate.Count + 5, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            sheet3.Cells[groupedByDate.Count + 5, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            sheet3.Cells[groupedByDate.Count + 6, 1].Value = "Chưa nộp";
            sheet3.Cells[groupedByDate.Count + 6, 2].Value = totalNotSubmitted;
            sheet3.Cells[groupedByDate.Count + 6, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            sheet3.Cells[groupedByDate.Count + 6, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            sheet3.Cells[groupedByDate.Count + 6, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            sheet3.Cells[groupedByDate.Count + 6, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            sheet3.Cells[groupedByDate.Count + 6, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            sheet3.Cells[groupedByDate.Count + 6, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            sheet3.Cells[groupedByDate.Count + 3, 1, groupedByDate.Count + 6, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            sheet3.Cells[groupedByDate.Count + 3, 1, groupedByDate.Count + 6, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            sheet3.Cells[groupedByDate.Count + 3, 1, groupedByDate.Count + 6, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            
            sheet3.Cells[groupedByDate.Count + 3, 1, groupedByDate.Count + 6, 2].AutoFitColumns();

            var chart = sheet3.Drawings.AddChart("chart", eChartType.ColumnClustered) as ExcelBarChart;
            chart.Title.Text = "Số lượng bài nộp theo ngày";
            chart.SetPosition(1, 0, 3, 0);
            chart.SetSize(600, 300);
            chart.Series.Add(ExcelRange.GetAddress(2, 2, groupedByDate.Count + 1, 2),
                             ExcelRange.GetAddress(2, 1, groupedByDate.Count + 1, 1));
            chart.Legend.Remove();

            var pie = sheet3.Drawings.AddChart("pie", eChartType.Pie) as ExcelPieChart;
            pie.Title.Text = "Tình trạng nộp bài";
            pie.SetPosition(1, 0, 13, 0);
            pie.SetSize(500, 300);
            pie.Series.Add(ExcelRange.GetAddress(groupedByDate.Count + 4, 2, groupedByDate.Count + 6, 2),
                           ExcelRange.GetAddress(groupedByDate.Count + 4, 1, groupedByDate.Count + 6, 1));
            pie.DataLabel.ShowPercent = true;
            pie.DataLabel.Position = eLabelPosition.Center;
            sheet3.Cells[sheet3.Dimension.Address].AutoFitColumns();

            // === Trả về file ===
            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            string fileName = $"ThongKe_BaiTap_{assignmentId}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

    }
}
