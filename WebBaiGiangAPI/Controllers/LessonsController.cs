using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.DependencyResolver;
using WebBaiGiangAPI.Data;
using WebBaiGiangAPI.Models;

namespace WebBaiGiangAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LessonsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;

        public LessonsController(AppDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // Thêm mới bài giảng
        [HttpPost("create-lesson")]
        public async Task<IActionResult> CreateLesson([FromBody] Lesson lesson)
        {
            lesson.LessonCreateAt = DateTime.Now;
            lesson.LessonUpdateAt = DateTime.Now;
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (_context.Courses.Find(lesson.LessonCourseId) == null) return NotFound("Khóa học không tồn tại");
            if (_context.Classes.Find(lesson.LessonClassId) == null) return NotFound("Lớp học không tồn tại.");
            if (_context.Users.Where(u => u.UsersId == lesson.LessonTeacherId && u.UsersRoleId == 2).FirstOrDefault() == null) return NotFound("Giáo viên không tồn tại.");
            if (lesson.LessonWeek < 1 || lesson.LessonWeek > 16) return BadRequest("Tuần học không hợp lệ.");
            if (_context.ClassCourses.Where(cc => cc.ClassId == lesson.LessonClassId && cc.CourseId == lesson.LessonCourseId).FirstOrDefault() == null) return NotFound("Khóa học không thuộc lớp học này.");
            if (_context.TeacherClasses.Where(tc => tc.TcClassId == lesson.LessonClassId && tc.TcUsersId == lesson.LessonTeacherId).FirstOrDefault() == null) return NotFound("Giáo viên không thuộc lớp học này.");
            if (lesson.LessonStatus != true && lesson.LessonStatus != false) return BadRequest("Trạng thái bài giảng không hợp lệ.");
            
            lesson.LessonDescription = Regex.Replace(lesson.LessonDescription.Trim(), @"\s+", " ");
            lesson.LessonChapter = lesson.LessonChapter.Trim() ?? "Chương 1";
            lesson.LessonWeek = lesson.LessonWeek ?? 1;
            lesson.LessonName = Regex.Replace(lesson.LessonName.Trim(), @"\s+", " ");
            _context.Lessons.Add(lesson);

            var teacher = await _context.Users.Where(u => u.UsersId == lesson.LessonTeacherId).Select(u => new { u.UsersName, u.UsersEmail }).FirstOrDefaultAsync();

            var announcement = new Announcement
            {
                AnnouncementClassId = lesson.LessonClassId,
                AnnouncementTitle = $"📢 Bài giảng mới: {lesson.LessonName} đã được tạo vào {lesson.LessonCreateAt} bởi giáo viên {teacher.UsersName}",
                AnnouncementDescription = $"📚 Mô tả: {lesson.LessonDescription} \n📅 Tuần học: {lesson.LessonWeek} \n 📋 Chương học: {lesson.LessonChapter}",
                AnnouncementDate = DateTime.Now,
                AnnouncementTeacherId = lesson.LessonTeacherId
            };
            _context.Announcements.Add(announcement);

            var students = await _context.StudentClasses
                .Where(sc => sc.ScClassId == lesson.LessonClassId && sc.ScStatus == 1)
                .Join(_context.Users,
                      sc => sc.ScStudentId,
                      u => u.UsersId,
                      (sc, u) => new
                      {
                          u.UsersId,
                          u.UsersName,
                          u.UsersEmail,
                      })
                .ToListAsync();
            var courseName = _context.Courses.Find(lesson.LessonCourseId)?.CourseTitle;
            var className = _context.Classes.Find(lesson.LessonClassId)?.ClassTitle;
            int emailCount = 0;
            string subject = $"Giáo viên {teacher.UsersName} đã thêm bài giảng mới!";
                            string body = $"<h3>Bài giảng mới: {lesson.LessonName}</h3>"
                                        + $"<p>Mô tả: {lesson.LessonDescription}</p>"
                                        + $"<p>Khóa học: {courseName}</p>"
                                        + $"<p>Lớp: {className}</p>"
                                        + $"<p>Tuần: {lesson.LessonWeek}, Chương: {lesson.LessonChapter}</p>"
                                        + "<p>Vui lòng đăng nhập để xem chi tiết.</p>";
            foreach (var student in students)
            {
                bool isSent = await _emailService.SendEmail(student.UsersEmail, subject, body);
                if (isSent)
                {
                    emailCount++;
                }
            }
            await _emailService.SendEmail(teacher.UsersEmail, "Thông báo: Bài giảng mới đã được tạo", $"Bài giảng {lesson.LessonName} đã được tạo thành công và đã được gửi đến {emailCount} sinh viên.");
            await _context.SaveChangesAsync();
            return Ok(new
            {
                Message = "Thêm bài giảng thành công.",
                Lesson = lesson
            });
        }

        // Cập nhật bài giảng
        [HttpPut("update-lesson")]
        public async Task<IActionResult> UpdateLesson([FromBody] Lesson updatedLesson)
        {
            var lesson = await _context.Lessons.FindAsync(updatedLesson.LessonId);
            var oldLesson = await _context.Lessons.FindAsync(updatedLesson.LessonId);
            if (lesson == null || oldLesson == null) return NotFound("Bài giảng không tồn tại.");
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (updatedLesson.LessonWeek < 1 || updatedLesson.LessonWeek > 16) return BadRequest("Tuần học không hợp lệ.");
            if (updatedLesson.LessonStatus != true && updatedLesson.LessonStatus != false) return BadRequest("Trạng thái bài giảng không hợp lệ.");

            lesson.LessonDescription = Regex.Replace(updatedLesson.LessonDescription.Trim(), @"\s+", " ");
            lesson.LessonChapter = updatedLesson.LessonChapter.Trim() ?? "Chương 1";
            lesson.LessonWeek = updatedLesson.LessonWeek ?? 1;
            lesson.LessonName = Regex.Replace(updatedLesson.LessonName.Trim(), @"\s+", " ");
            lesson.LessonUpdateAt = DateTime.Now;
            lesson.LessonStatus = updatedLesson.LessonStatus;
            _context.Lessons.Update(lesson);

            var teacher = await _context.Users.Where(u => u.UsersId == lesson.LessonTeacherId).Select(u => new { u.UsersName, u.UsersEmail }).FirstOrDefaultAsync();

            var announcement = new Announcement
            {
                AnnouncementClassId = lesson.LessonClassId,
                AnnouncementTitle = $"✏️ Bài giảng {lesson.LessonName} đã được cập nhật vào {DateTime.Now} bởi giáo viên {teacher.UsersName}",
                AnnouncementDescription = $"🔄 **Cập nhật thông tin bài giảng**:\n\n" +
                                        $"📚 **Tên bài giảng:** {oldLesson.LessonName} ➝ {lesson.LessonName}\n" +
                                        $"📝 **Mô tả:** {oldLesson.LessonDescription} ➝ {lesson.LessonDescription}\n" +
                                        $"📅 **Tuần học:** {oldLesson.LessonWeek} ➝ {lesson.LessonWeek}\n" +
                                        $"📋 **Chương học:** {oldLesson.LessonChapter} ➝ {lesson.LessonChapter}",
                AnnouncementDate = DateTime.Now,
                AnnouncementTeacherId = lesson.LessonTeacherId
            };
            _context.Announcements.Add(announcement);

            var students = await _context.StudentClasses
                .Where(sc => sc.ScClassId == lesson.LessonClassId && sc.ScStatus == 1)
                .Join(_context.Users,
                      sc => sc.ScStudentId,
                      u => u.UsersId,
                      (sc, u) => new
                      {
                          u.UsersId,
                          u.UsersName,
                          u.UsersEmail,
                      })
                .ToListAsync();
            if (students == null || students.Count == 0) return NotFound("Không có sinh viên nào trong lớp này.");
            var courseName = _context.Courses.Find(lesson.LessonCourseId)?.CourseTitle;
            var className = _context.Classes.Find(lesson.LessonClassId)?.ClassTitle;
            int emailCount = 0;
            string subject = $"Giáo viên {teacher.UsersName} đã cập nhật bài giảng!";

            string body = $"<h3>Bài giảng đã được cập nhật: {lesson.LessonName}</h3>"
                        + $"<p><strong>🔄 Thông tin cập nhật:</strong></p>"
                        + $"<p><strong>📚 Tên bài giảng:</strong> {oldLesson.LessonName} ➝ {lesson.LessonName}</p>"
                        + $"<p><strong>📝 Mô tả:</strong> {oldLesson.LessonDescription} ➝ {lesson.LessonDescription}</p>"
                        + $"<p><strong>📋 Khóa học:</strong> {courseName}</p>"
                        + $"<p><strong>🏛️ Lớp:</strong> {className}</p>"
                        + $"<p><strong>📅 Tuần học:</strong> {oldLesson.LessonWeek} ➝ {lesson.LessonWeek}</p>"
                        + $"<p><strong>📋 Chương học:</strong> {oldLesson.LessonChapter} ➝ {lesson.LessonChapter}</p>"
                        + "<p>Vui lòng đăng nhập để xem chi tiết.</p>";

            foreach (var student in students)
            {
                bool isSent = await _emailService.SendEmail(student.UsersEmail, subject, body);
                if (isSent)
                {
                    emailCount++;
                }
            }

            // Gửi email cho giáo viên xác nhận bài giảng đã được cập nhật thành công
            await _emailService.SendEmail(teacher.UsersEmail,
                "Thông báo: Bài giảng đã được cập nhật",
                $"Bài giảng {lesson.LessonName} đã được cập nhật thành công và thông báo đã được gửi đến {emailCount} sinh viên.");

            await _context.SaveChangesAsync();
            return Ok(new
            {
                Message = "Sửa bài giảng thành công.",
                Lesson = lesson
            });
        }

        // Xóa bài giảng
        [HttpDelete("delete-lesson/{lessonId}")]
        public async Task<IActionResult> DeleteLesson(int lessonId, int teacherId)
        {
            try
            {
                var lesson = await _context.Lessons.FindAsync(lessonId);
                if (lesson == null) return NotFound("Bài giảng không tồn tại.");
                if (lesson.LessonTeacherId != teacherId) return Unauthorized("Bạn không có quyền xóa bài giảng này.");
                _context.Lessons.Remove(lesson);

                var teacher = await _context.Users
                .Where(u => u.UsersId == lesson.LessonTeacherId)
                .Select(u => new { u.UsersName, u.UsersEmail })
                .FirstOrDefaultAsync();

                var courseName = _context.Courses.Find(lesson.LessonCourseId)?.CourseTitle;
                var className = _context.Classes.Find(lesson.LessonClassId)?.ClassTitle;

                var announcement = new Announcement
                {
                    AnnouncementClassId = lesson.LessonClassId,
                    AnnouncementTitle = $"🗑️ Bài giảng {lesson.LessonName} đã bị xóa vào {DateTime.Now} bởi giáo viên {teacher.UsersName}",
                    AnnouncementDescription = $"❌ **Bài giảng đã bị xóa:** {lesson.LessonName}\n" +
                                            $"📚 **Khóa học:** {courseName}\n" +
                                            $"🏛️ **Lớp:** {className}\n" +
                                            $"📅 **Tuần học:** {lesson.LessonWeek}\n" +
                                            $"📋 **Chương học:** {lesson.LessonChapter}",
                    AnnouncementDate = DateTime.Now,
                    AnnouncementTeacherId = lesson.LessonTeacherId
                };
                _context.Announcements.Add(announcement);

                // Lấy danh sách sinh viên trong lớp
                var students = await _context.StudentClasses
                    .Where(sc => sc.ScClassId == lesson.LessonClassId && sc.ScStatus == 1)
                    .Join(_context.Users,
                          sc => sc.ScStudentId,
                          u => u.UsersId,
                          (sc, u) => new { u.UsersEmail })
                    .ToListAsync();

                int emailCount = 0;
                string subject = $"Giáo viên {teacher.UsersName} đã xóa bài giảng!";

                string body = $"<h3>Bài giảng đã bị xóa: {lesson.LessonName}</h3>"
                            + $"<p><strong>📚 Khóa học:</strong> {courseName}</p>"
                            + $"<p><strong>🏛️ Lớp:</strong> {className}</p>"
                            + $"<p><strong>📅 Tuần học:</strong> {lesson.LessonWeek}</p>"
                            + $"<p><strong>📋 Chương học:</strong> {lesson.LessonChapter}</p>"
                            + "<p>Vui lòng liên hệ giáo viên để biết thêm thông tin.</p>";

                foreach (var student in students)
                {
                    bool isSent = await _emailService.SendEmail(student.UsersEmail, subject, body);
                    if (isSent)
                    {
                        emailCount++;
                    }
                }

                // Gửi email cho giáo viên xác nhận bài giảng đã bị xóa
                await _emailService.SendEmail(teacher.UsersEmail,
                    "Thông báo: Bài giảng đã bị xóa",
                    $"Bài giảng {lesson.LessonName} đã bị xóa thành công và thông báo đã được gửi đến {emailCount} sinh viên.");

                await _context.SaveChangesAsync();
                return Ok(new {Message = "Xóa bài giảng thành công."});
            }
            catch (Exception)
            {
                return BadRequest("Bài giảng đang được liên kết bảng khác, không thể xóa!");
            }
        }

        // Tìm kiếm bài giảng
        [HttpGet("search-lesson")]
        public async Task<IActionResult> SearchLesson([FromQuery] string? keyword)
        {
            var keywords = keyword?.ToLower().Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? new string[0];

            var query = from l in _context.Lessons
                        where keywords.Length == 0 ||  
                              keywords.Any(kw =>
                                (l.LessonName != null && l.LessonName.ToLower().Contains(kw)) || 
                                (l.LessonDescription != null && l.LessonDescription.ToLower().Contains(kw)) ||  
                                (l.LessonWeek.ToString().Contains(kw)) ||  
                                (l.LessonChapter != null && l.LessonChapter.ToLower().Contains(kw)) 
                              )
                        orderby l.LessonCreateAt descending
                        select l;

            var lessons = await query.ToListAsync();
            return Ok(lessons);
        }

        // Xem chi tiết bài giảng
        [HttpGet("{lessonId}")]
        public async Task<IActionResult> GetLessonById(int lessonId)
        {
            var lesson = await (from l in _context.Lessons
                                join c in _context.Courses on l.LessonCourseId equals c.CourseId into courses
                                from c in courses.DefaultIfEmpty()
                                join cl in _context.Classes on l.LessonClassId equals cl.ClassId into classes
                                from cl in classes.DefaultIfEmpty()
                                join u in _context.Users on l.LessonTeacherId equals u.UsersId into teachers
                                from u in teachers.DefaultIfEmpty()
                                where l.LessonId == lessonId
                                select new
                                {
                                    l.LessonId,
                                    l.LessonName,
                                    l.LessonDescription,
                                    l.LessonWeek,
                                    l.LessonChapter,
                                    CourseTitle = c != null ? c.CourseTitle : "N/A",
                                    ClassTitle = cl != null ? cl.ClassTitle : "N/A",
                                    TeacherName = u != null ? u.UsersName : "N/A",
                                    TeacherEmail = u != null ? u.UsersEmail : "N/A",
                                    l.LessonCreateAt,
                                    l.LessonUpdateAt
                                }).FirstOrDefaultAsync();

            if (lesson == null) return NotFound("Bài giảng không tồn tại.");

            return Ok(lesson);
        }
        
        [HttpPost("duplicate/{lessonId}")]
        public async Task<IActionResult> DuplicateLesson(int lessonId, [FromQuery] int newClassId)
        {
            // Kiểm tra bài giảng có tồn tại không
            var lesson = await _context.Lessons.FindAsync(lessonId);
            if (lesson == null) return NotFound("Bài giảng không tồn tại.");

            // Kiểm tra lớp mới có tồn tại không
            var newClass = await _context.Classes.FindAsync(newClassId);
            if (newClass == null) return NotFound("Lớp học mới không tồn tại.");
            
            // Kiểm tra lớp mới có thuộc cùng học phần không
            var classCourse = await _context.ClassCourses
                .FirstOrDefaultAsync(cc => cc.ClassId == newClassId && cc.CourseId == lesson.LessonCourseId);
            if (classCourse == null) return BadRequest("Lớp học mới không thuộc cùng học phần với bài giảng.");

            // Kiểm tra giáo viên có thuộc lớp mới không
            var teacherClass = await _context.TeacherClasses
                .FirstOrDefaultAsync(tc => tc.TcClassId == newClassId && tc.TcUsersId == lesson.LessonTeacherId);
            if (teacherClass == null) return BadRequest("Giáo viên của bài giảng không thuộc lớp mới.");

            // Kiểm tra lớp mới đã có bài giảng trùng tên chưa
            var existingLesson = await _context.Lessons
                .FirstOrDefaultAsync(l => l.LessonClassId == newClassId && l.LessonName == lesson.LessonName);
            if (existingLesson != null) return Conflict("Lớp học mới đã có bài giảng cùng tên.");

            // Nhân bản bài giảng
            var duplicateLesson = new Lesson
            {
                LessonClassId = newClassId,
                LessonCourseId = lesson.LessonCourseId,
                LessonTeacherId = lesson.LessonTeacherId,
                LessonDescription = lesson.LessonDescription,
                LessonChapter = lesson.LessonChapter,
                LessonWeek = lesson.LessonWeek,
                LessonName = lesson.LessonName,
                LessonStatus = lesson.LessonStatus,
                LessonCreateAt = DateTime.Now,
                LessonUpdateAt = DateTime.Now
            };

            _context.Lessons.Add(duplicateLesson);
            await _context.SaveChangesAsync();
            
            var lessonFiles = await _context.LessonFiles.Where(f => f.LfLessonId == lessonId).ToListAsync();

            // Sao chép file đính kèm của bài giảng
            if (lessonFiles.Any())
            {
                string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "LessonFiles");

                // Kiểm tra thư mục tồn tại
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                foreach (var file in lessonFiles)
                {
                    string originalFileName = Path.GetFileNameWithoutExtension(file.LfPath);
                    string fileExtension = Path.GetExtension(file.LfPath);

                    string pattern = @"^\d{14}_"; // Regex cho timestamp: YYYYMMDDHHMMSS_
                    originalFileName = System.Text.RegularExpressions.Regex.Replace(originalFileName, pattern, "");

                    // Tạo tên file mới: thời gian + tên gốc (không có timestamp cũ)
                    string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    string newFileName = $"{timestamp}_{originalFileName}{fileExtension}";

                    // Xác định đường dẫn đầy đủ
                    string oldPath = Path.Combine(uploadFolder, file.LfPath);
                    string newPath = Path.Combine(uploadFolder, newFileName);

                    try
                    {
                        // Kiểm tra file gốc tồn tại
                        if (System.IO.File.Exists(oldPath))
                        {
                            // Sao chép file sang đường dẫn mới
                            System.IO.File.Copy(oldPath, newPath);
                        }
                        else
                        {
                            Console.WriteLine($"File không tồn tại: {oldPath}");
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Lỗi khi sao chép file: {ex.Message}");
                        continue;
                    }

                    var copiedFile = new LessonFile
                    {
                        LfLessonId = duplicateLesson.LessonId,
                        LfPath = newFileName, 
                        LfType = file.LfType
                    };
                    _context.LessonFiles.Add(copiedFile);
                    await _context.SaveChangesAsync();
                }
            }

            var teacher = await _context.Users.Where(u => u.UsersId == duplicateLesson.LessonTeacherId).Select(u => new { u.UsersName, u.UsersEmail }).FirstOrDefaultAsync();
            var announcement = new Announcement
            {
                AnnouncementClassId = duplicateLesson.LessonClassId,
                AnnouncementTitle = $"📢 Bài giảng mới: {duplicateLesson.LessonName} đã được tạo vào {duplicateLesson.LessonCreateAt} bởi giáo viên {teacher.UsersName}",
                AnnouncementDescription = $"📚 Mô tả: {duplicateLesson.LessonDescription} \n📅 Tuần học: {duplicateLesson.LessonWeek} \n 📋 Chương học: {duplicateLesson.LessonChapter}",
                AnnouncementDate = DateTime.Now,
                AnnouncementTeacherId = duplicateLesson.LessonTeacherId
            };
            _context.Announcements.Add(announcement);

            var students = await _context.StudentClasses
                .Where(sc => sc.ScClassId == duplicateLesson.LessonClassId && sc.ScStatus == 1)
                .Join(_context.Users,
                      sc => sc.ScStudentId,
                      u => u.UsersId,
                      (sc, u) => new
                      {
                          u.UsersId,
                          u.UsersName,
                          u.UsersEmail,
                      })
                .ToListAsync();
            var courseName = _context.Courses.Find(duplicateLesson.LessonCourseId)?.CourseTitle;
            var className = _context.Classes.Find(duplicateLesson.LessonClassId)?.ClassTitle;
            int emailCount = 0;
            string subject = $"Giáo viên {teacher.UsersName} đã thêm bài giảng mới!";
            string body = $"<h3>Bài giảng mới: {duplicateLesson.LessonName}</h3>"
                        + $"<p>Mô tả: {duplicateLesson.LessonDescription}</p>"
                        + $"<p>Khóa học: {courseName}</p>"
                        + $"<p>Lớp: {className}</p>"
                        + $"<p>Tuần: {duplicateLesson.LessonWeek}, Chương: {duplicateLesson.LessonChapter}</p>"
                        + "<p>Vui lòng đăng nhập để xem chi tiết.</p>";
            foreach (var student in students)
            {
                bool isSent = await _emailService.SendEmail(student.UsersEmail, subject, body);
                if (isSent)
                {
                    emailCount++;
                }
            }
            await _emailService.SendEmail(teacher.UsersEmail, "Thông báo: Bài giảng mới đã được tạo", $"Bài giảng {duplicateLesson.LessonName} đã được tạo thành công và đã được gửi đến {emailCount} sinh viên.");
            await _context.SaveChangesAsync();
            return Ok(new
            {
                Message = "Nhân bản bài giảng thành công.",
                DuplicateLesson = duplicateLesson
            });
        }

        // Ẩn/Hiện bài giảng
        [HttpPut("visibility/{lessonId}")]
        public async Task<IActionResult> ToggleLessonVisibility(int lessonId)
        {
            var lesson = await _context.Lessons.FindAsync(lessonId);
            if (lesson == null) return NotFound("Bài giảng không tồn tại.");

            lesson.LessonStatus = !lesson.LessonStatus;
            await _context.SaveChangesAsync();

            return Ok($"Trạng thái bài giảng đã được thay đổi thành {(lesson.LessonStatus ? "Hiện" : "Ẩn")}.");
        }


        // Upload file cho bài giảng
        [HttpPost("{lessonId}/upload")]
        public async Task<IActionResult> UploadFile(int lessonId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File không hợp lệ.");

            var lesson = await _context.Lessons.FindAsync(lessonId);
            if (lesson == null)
                return NotFound("Bài giảng không tồn tại.");

            string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "LessonFiles");

            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            // Tạo tên file mới: thời gian + tên gốc
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string fileExtension = Path.GetExtension(file.FileName).ToLower();
            string newFileName = $"{timestamp}_{Path.GetFileNameWithoutExtension(file.FileName)}{fileExtension}";

            string filePath = Path.Combine(uploadFolder, newFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Xác định loại file
            string fileType = "File"; // Mặc định
            if (new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp" }.Contains(fileExtension))
            {
                fileType = "Hình ảnh";
            }
            else if (new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".mkv" }.Contains(fileExtension))
            {
                fileType = "Video";
            }
            else if (new[] { ".mp3", ".wav", ".aac", ".ogg", ".flac" }.Contains(fileExtension))
            {
                fileType = "Âm thanh";
            }
            else if (new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt" }.Contains(fileExtension))
            {
                fileType = "Tài liệu";
            }

            var lessonFile = new LessonFile
            {
                LfLessonId = lessonId,
                LfPath = newFileName,
                LfType = fileType
            };

            _context.LessonFiles.Add(lessonFile);
            await _context.SaveChangesAsync();

            return Ok(lessonFile);
        }

    }
}
