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
using OfficeOpenXml;
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
            
            var existingCourse = _context.Courses.Join(_context.ClassCourses,
                                                      c => c.CourseId,
                                                      cc => cc.CourseId,
                                                      (c, cc) => new { c.CourseId, cc.CcId })
                                                      .Where(cc => cc.CcId == lesson.LessonClassCourseId)
                                                      .FirstOrDefault();
            if (existingCourse == null) return NotFound("Khóa học không tồn tại");

            var existingClass = _context.Classes.Join(_context.ClassCourses,
                                                    cl => cl.ClassId,
                                                    cc => cc.ClassId,
                                                    (cl, cc) => new { cl.ClassId, cc.CcId })
                                                    .Where(cl => cl.CcId == lesson.LessonClassCourseId)
                                                    .FirstOrDefault();
            if (existingClass == null) return NotFound("Lớp học không tồn tại.");

            if (_context.ClassCourses.Where(cc => cc.CcId == lesson.LessonClassCourseId).FirstOrDefault() == null) 
                return NotFound("Khóa học không thuộc lớp học này.");

            if (_context.Users.Where(u => u.UsersId == lesson.LessonTeacherId && u.UsersRoleId == 2).FirstOrDefault() == null) 
                return NotFound("Giáo viên không tồn tại.");

            if (lesson.LessonWeek < 1 || lesson.LessonWeek > 16) return BadRequest("Tuần học không hợp lệ.");

            bool exists = await _context.TeacherClasses
                .Join(_context.ClassCourses,
                      tc => tc.TcClassCourseId,
                      cc => cc.CcId,
                      (tc, cc) => new { tc.TcUsersId})
                .AnyAsync(t => t.TcUsersId == lesson.LessonTeacherId);
            if (!exists) return NotFound("Giáo viên không thuộc lớp học này.");

            if (lesson.LessonStatus != true && lesson.LessonStatus != false) return BadRequest("Trạng thái bài giảng không hợp lệ.");
            
            lesson.LessonDescription = Regex.Replace(lesson.LessonDescription.Trim(), @"\s+", " ");
            lesson.LessonChapter = lesson.LessonChapter.Trim() ?? "Chương 1";
            lesson.LessonWeek = lesson.LessonWeek ?? 1;
            lesson.LessonName = Regex.Replace(lesson.LessonName.Trim(), @"\s+", " ");
            _context.Lessons.Add(lesson);

            var teacher = await _context.Users.Where(u => u.UsersId == lesson.LessonTeacherId).Select(u => new { u.UsersName, u.UsersEmail }).FirstOrDefaultAsync();

            var announcement = new Announcement
            {
                AnnouncementClassId = lesson.ClassCourse.ClassId,
                AnnouncementTitle = $"📢 Bài giảng mới: {lesson.LessonName} đã được tạo vào {lesson.LessonCreateAt} bởi giáo viên {teacher.UsersName}",
                AnnouncementDescription = $"📚 Mô tả: {lesson.LessonDescription} \n📅 Tuần học: {lesson.LessonWeek} \n 📋 Chương học: {lesson.LessonChapter}",
                AnnouncementDate = DateTime.Now,
                AnnouncementTeacherId = lesson.LessonTeacherId
            };
            _context.Announcements.Add(announcement);

            var students = await _context.StudentClasses
                .Where(sc => sc.ScClassId == lesson.ClassCourse.ClassId && sc.ScStatus == 1)
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
            var courseName = _context.Courses.Find(lesson.ClassCourse.CourseId)?.CourseTitle;
            var className = _context.Classes.Find(lesson.ClassCourse.ClassId)?.ClassTitle;
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
            var classId = await _context.ClassCourses.Where(cc => cc.CcId == lesson.LessonClassCourseId).Select(cc => cc.ClassId).FirstOrDefaultAsync();
            var announcement = new Announcement
            {
                AnnouncementClassId = classId,
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
                .Where(sc => sc.ScClassId == classId && sc.ScStatus == 1)
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
            var courseName = _context.Courses
                .Join(_context.ClassCourses,
                      c => c.CourseId,
                      cc => cc.CourseId,
                      (c, cc) => new { c.CourseTitle })
                .Select(c => c.CourseTitle)
                .FirstOrDefault();
            var className = _context.Classes.Find(classId)?.ClassTitle;
            int emailCount = 0;
            string subject = $"Giáo viên \"{teacher.UsersName}\" đã cập nhật bài giảng!";

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

                // Kiểm tra xem bài giảng có đang được sử dụng ở bảng khác không trước khi xóa
                bool isLinked = await _context.StatusLearns.AnyAsync(sl => sl.SlLessonId == lessonId);
                if (isLinked)
                {
                    return BadRequest("Bài giảng đang được liên kết với dữ liệu khác, không thể xóa!");
                }

                // Lấy thông tin cần thiết trong một truy vấn duy nhất
                var lessonInfo = await (
                    from cc in _context.ClassCourses
                    join c in _context.Courses on cc.CourseId equals c.CourseId
                    join cl in _context.Classes on cc.ClassId equals cl.ClassId
                    where cc.CcId == lesson.LessonClassCourseId
                    select new
                    {
                        c.CourseTitle,
                        cl.ClassId,
                        cl.ClassTitle
                    }
                ).FirstOrDefaultAsync();

                if (lessonInfo == null) return NotFound("Không tìm thấy thông tin khóa học và lớp.");

                var teacher = await _context.Users
                    .Where(u => u.UsersId == lesson.LessonTeacherId)
                    .Select(u => new { u.UsersName, u.UsersEmail })
                    .FirstOrDefaultAsync();

                if (teacher == null) return NotFound("Không tìm thấy thông tin giáo viên.");

                // Tạo thông báo xóa bài giảng
                var announcement = new Announcement
                {
                    AnnouncementClassId = lessonInfo.ClassId,
                    AnnouncementTitle = $"🗑️ Bài giảng {lesson.LessonName} đã bị xóa bởi giáo viên {teacher.UsersName}",
                    AnnouncementDescription = $"❌ **Bài giảng đã bị xóa:** {lesson.LessonName}\n" +
                                              $"📚 **Khóa học:** {lessonInfo.CourseTitle}\n" +
                                              $"🏛️ **Lớp:** {lessonInfo.ClassTitle}\n" +
                                              $"📅 **Tuần học:** {lesson.LessonWeek}\n" +
                                              $"📋 **Chương học:** {lesson.LessonChapter}",
                    AnnouncementDate = DateTime.Now,
                    AnnouncementTeacherId = lesson.LessonTeacherId
                };
                _context.Announcements.Add(announcement);

                // Lấy danh sách sinh viên trong lớp để gửi email
                var studentEmails = await _context.StudentClasses
                    .Where(sc => sc.ScClassId == lessonInfo.ClassId && sc.ScStatus == 1)
                    .Join(_context.Users, sc => sc.ScStudentId, u => u.UsersId, (sc, u) => u.UsersEmail)
                    .ToListAsync();

                // Xóa bài giảng
                _context.Lessons.Remove(lesson);
                await _context.SaveChangesAsync();

                // Gửi email thông báo
                string subject = $"Giáo viên {teacher.UsersName} đã xóa bài giảng!";
                string body = $"<h3>Bài giảng đã bị xóa: {lesson.LessonName}</h3>"
                            + $"<p><strong>📚 Khóa học:</strong> {lessonInfo.CourseTitle}</p>"
                            + $"<p><strong>🏛️ Lớp:</strong> {lessonInfo.ClassTitle}</p>"
                            + $"<p><strong>📅 Tuần học:</strong> {lesson.LessonWeek}</p>"
                            + $"<p><strong>📋 Chương học:</strong> {lesson.LessonChapter}</p>"
                            + "<p>Vui lòng liên hệ giáo viên để biết thêm thông tin.</p>";

                int emailCount = 0;
                foreach (var studentEmail in studentEmails)
                {
                    bool isSent = await _emailService.SendEmail(studentEmail, subject, body);
                    if (isSent) emailCount++;
                }

                // Gửi email xác nhận cho giáo viên
                await _emailService.SendEmail(teacher.UsersEmail,
                    "Thông báo: Bài giảng đã bị xóa",
                    $"Bài giảng {lesson.LessonName} đã bị xóa thành công và thông báo đã được gửi đến {emailCount} sinh viên.");

                return Ok(new { Message = "Xóa bài giảng thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi máy chủ: {ex.Message}");
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
        [HttpGet("lesson/{lessonId}")]
        public async Task<IActionResult> GetLessonById(int lessonId)
        {
            var lesson = await (from l in _context.Lessons
                                join c in _context.Courses on l.ClassCourse.CourseId equals c.CourseId into courses
                                from c in courses.DefaultIfEmpty()
                                join cl in _context.Classes on l.ClassCourse.ClassId equals cl.ClassId into classes
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
        public async Task<IActionResult> DuplicateLesson(int lessonId, [FromQuery] int classCourseId)
        {
            // Kiểm tra bài giảng có tồn tại không
            var lesson = await _context.Lessons.FindAsync(lessonId);
            if (lesson == null) return NotFound("Bài giảng không tồn tại.");

            // Kiểm tra lớp học mới có tồn tại không
            var newClassCourse = await _context.ClassCourses.FindAsync(classCourseId);
            if (newClassCourse == null) return NotFound("Lớp học phần mới không tồn tại.");

            // Kiểm tra lớp học mới có cùng CourseId không
            var courseId = await _context.ClassCourses
                .Where(cc => cc.CcId == lesson.LessonClassCourseId)
                .Select(cc => cc.CourseId)
                .FirstOrDefaultAsync();

            if (courseId == null) return BadRequest("Không tìm thấy học phần của bài giảng.");

            bool exists = await _context.ClassCourses
                .AnyAsync(cc => cc.CcId == classCourseId && cc.CourseId == courseId);

            if (!exists) return BadRequest("Lớp học mới không thuộc cùng học phần với bài giảng.");

            // Kiểm tra giáo viên có thuộc lớp mới không
            bool teacherExists = await _context.TeacherClasses
                .AnyAsync(tc => tc.TcClassCourseId == classCourseId && tc.TcUsersId == lesson.LessonTeacherId);

            if (!teacherExists) return BadRequest("Giáo viên của bài giảng không thuộc lớp mới.");

            // Kiểm tra lớp mới đã có bài giảng trùng tên chưa
            bool lessonExists = await _context.Lessons
                .AnyAsync(l => l.LessonClassCourseId == classCourseId && l.LessonName == lesson.LessonName);

            if (lessonExists) return Conflict("Lớp học mới đã có bài giảng cùng tên.");

            // Nhân bản bài giảng
            var duplicateLesson = new Lesson
            {
                LessonClassCourseId = classCourseId,
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

            // Nhân bản tệp đính kèm
            var lessonFiles = await _context.LessonFiles.Where(f => f.LfLessonId == lessonId).ToListAsync();
            if (lessonFiles.Any())
            {
                string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "LessonFiles");
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                foreach (var file in lessonFiles)
                {
                    string fileExtension = Path.GetExtension(file.LfPath);
                    string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    string newFileName = $"{timestamp}_{Path.GetFileName(file.LfPath)}";
                    string oldPath = Path.Combine(uploadFolder, file.LfPath);
                    string newPath = Path.Combine(uploadFolder, newFileName);

                    try
                    {
                        if (System.IO.File.Exists(oldPath))
                        {
                            System.IO.File.Copy(oldPath, newPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Lỗi khi sao chép file: {ex.Message}");
                        continue;
                    }

                    _context.LessonFiles.Add(new LessonFile
                    {
                        LfLessonId = duplicateLesson.LessonId,
                        LfPath = newFileName,
                        LfType = file.LfType
                    });
                }

                await _context.SaveChangesAsync();
            }

            // Gửi thông báo
            var teacher = await _context.Users
                .Where(u => u.UsersId == duplicateLesson.LessonTeacherId)
                .Select(u => new { u.UsersName, u.UsersEmail })
                .FirstOrDefaultAsync();

            var announcement = new Announcement
            {
                AnnouncementClassId = newClassCourse.ClassId,
                AnnouncementTitle = $"📢 Bài giảng mới: {duplicateLesson.LessonName} đã được tạo vào {duplicateLesson.LessonCreateAt} bởi giáo viên {teacher.UsersName}",
                AnnouncementDescription = $"📚 Mô tả: {duplicateLesson.LessonDescription} \n📅 Tuần học: {duplicateLesson.LessonWeek} \n📋 Chương học: {duplicateLesson.LessonChapter}",
                AnnouncementDate = DateTime.Now,
                AnnouncementTeacherId = duplicateLesson.LessonTeacherId
            };

            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            // Gửi email thông báo đến sinh viên
            var students = await _context.StudentClasses
                .Where(sc => sc.ScClassId == newClassCourse.ClassId && sc.ScStatus == 1)
                .Join(_context.Users, sc => sc.ScStudentId, u => u.UsersId, (sc, u) => u.UsersEmail)
                .ToListAsync();

            string subject = $"Giáo viên {teacher.UsersName} đã thêm bài giảng mới!";
            string body = $"<h3>Bài giảng mới: {duplicateLesson.LessonName}</h3>"
                        + $"<p>Mô tả: {duplicateLesson.LessonDescription}</p>"
                        + $"<p>Tuần: {duplicateLesson.LessonWeek}, Chương: {duplicateLesson.LessonChapter}</p>"
                        + "<p>Vui lòng đăng nhập để xem chi tiết.</p>";

            var emailTasks = students.Select(email => _emailService.SendEmail(email, subject, body)).ToList();
            await Task.WhenAll(emailTasks);

            await _emailService.SendEmail(teacher.UsersEmail, "Thông báo: Bài giảng mới đã được tạo",
                $"Bài giảng {duplicateLesson.LessonName} đã được tạo thành công và đã gửi đến {students.Count} sinh viên.");

            return Ok(new { Message = "Nhân bản bài giảng thành công.", DuplicateLesson = duplicateLesson });
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

        // Xuất danh sách bài giảng theo lớp ra file Excel
        [HttpGet("export-excel-by-class/{classId}")]
        public async Task<IActionResult> ExportExcelByClass(int classId)
        {
            var lessons = _context.Lessons
            .Where(l => l.ClassCourse.ClassId == classId)
            .GroupJoin(_context.LessonFiles,
                lesson => lesson.LessonId,
                file => file.LfLessonId,
                (lesson, files) => new
                {
                    Lesson = lesson,
                    Files = files.ToList()
                })
            .ToList();
            if (lessons == null) return NotFound("Không có bài giảng nào trong lớp này.");
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("DanhSachBaiGiang");

                // Tiêu đề cột
                var headers = new string[]
                {
                    "LessonTeacherId", "LessonDescription", "LessonChapter", "LessonWeek",
                    "LessonName", "LessonStatus", "LessonCreateAt", "LessonUpdateAt",
                    "LfId", "LfPath", "LfType"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                }

                int row = 2;

                foreach (var lesson in lessons)
                {
                    int startRow = row; // Ghi nhớ vị trí dòng bắt đầu của bài giảng

                    // Ghi thông tin bài giảng vào dòng đầu tiên
                    worksheet.Cells[row, 1].Value = lesson.Lesson.LessonTeacherId;
                    worksheet.Cells[row, 2].Value = lesson.Lesson.LessonDescription;
                    worksheet.Cells[row, 3].Value = lesson.Lesson.LessonChapter;
                    worksheet.Cells[row, 4].Value = lesson.Lesson.LessonWeek;
                    worksheet.Cells[row, 5].Value = lesson.Lesson.LessonName;
                    worksheet.Cells[row, 6].Value = lesson.Lesson.LessonStatus;
                    worksheet.Cells[row, 7].Value = lesson.Lesson.LessonCreateAt.ToString("yyyy-MM-dd HH:mm:ss");
                    worksheet.Cells[row, 8].Value = lesson.Lesson.LessonUpdateAt.ToString("yyyy-MM-dd HH:mm:ss");

                    if (lesson.Files.Any())
                    {
                        foreach (var file in lesson.Files)
                        {
                            worksheet.Cells[row, 9].Value = file.LfId;
                            worksheet.Cells[row, 10].Value = file.LfPath;
                            worksheet.Cells[row, 11].Value = file.LfType;
                            row++; // Xuống dòng cho mỗi file
                        }
                    }
                    else
                    {
                        worksheet.Cells[row, 9].Value = "N/A";
                        worksheet.Cells[row, 10].Value = "N/A";
                        worksheet.Cells[row, 11].Value = "N/A";
                        row++;
                    }

                    // Merge các ô thông tin bài giảng
                    if (row > startRow)
                    {
                        for (int col = 1; col <= 8; col++)
                        {
                            worksheet.Cells[startRow, col, row - 1, col].Merge = true;
                            worksheet.Cells[startRow, col, row - 1, col].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                        }
                    }
                }

                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "DanhSachBaiGiang.xlsx");
            }
        }

        [HttpGet("export-excel")]
        public async Task<IActionResult> ExportLessonsToExcel()
        {
            var lessons = _context.Lessons
                .Select(l => new
                {
                    l.LessonId,
                    l.LessonName,
                    l.LessonDescription,
                    l.LessonChapter,
                    l.LessonWeek,
                    l.LessonStatus,
                    l.LessonCreateAt,
                    l.LessonUpdateAt,
                    l.LessonTeacherId
                })
                .ToList();

            var lessonFiles = _context.LessonFiles
                .Select(f => new
                {
                    f.LfLessonId,
                    f.LfId,
                    f.LfPath,
                    f.LfType
                })
                .ToList();

            using (var package = new ExcelPackage())
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Bài giảng");
                worksheet.Cells["A1"].Value = "ID";
                worksheet.Cells["B1"].Value = "Tên bài giảng";
                worksheet.Cells["C1"].Value = "Mô tả";
                worksheet.Cells["D1"].Value = "Chương";
                worksheet.Cells["E1"].Value = "Tuần";
                worksheet.Cells["F1"].Value = "Trạng thái";
                worksheet.Cells["G1"].Value = "Ngày tạo";
                worksheet.Cells["H1"].Value = "Ngày cập nhật";
                worksheet.Cells["I1"].Value = "Giáo viên";

                int row = 2;
                foreach (var lesson in lessons)
                {
                    worksheet.Cells[row, 1].Value = lesson.LessonId;
                    worksheet.Cells[row, 2].Value = lesson.LessonName;
                    worksheet.Cells[row, 3].Value = lesson.LessonDescription;
                    worksheet.Cells[row, 4].Value = lesson.LessonChapter;
                    worksheet.Cells[row, 5].Value = lesson.LessonWeek;
                    worksheet.Cells[row, 6].Value = lesson.LessonStatus ? "Hoạt động" : "Ẩn";
                    worksheet.Cells[row, 7].Value = lesson.LessonCreateAt.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 8].Value = lesson.LessonUpdateAt.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 9].Value = await _context.Users.Where(u => u.UsersId == lesson.LessonTeacherId).Select(u => u.UsersName).FirstOrDefaultAsync();
                    row++;
                }

                // Danh sách file bài giảng
                ExcelWorksheet fileSheet = package.Workbook.Worksheets.Add("File bài giảng");
                fileSheet.Cells["A1"].Value = "LessonId";
                fileSheet.Cells["B1"].Value = "File ID";
                fileSheet.Cells["C1"].Value = "Đường dẫn file";
                fileSheet.Cells["D1"].Value = "Loại file";

                int fileRow = 2;
                foreach (var file in lessonFiles)
                {
                    fileSheet.Cells[fileRow, 1].Value = file.LfLessonId;
                    fileSheet.Cells[fileRow, 2].Value = file.LfId;
                    fileSheet.Cells[fileRow, 3].Value = file.LfPath;
                    fileSheet.Cells[fileRow, 4].Value = file.LfType;
                    fileRow++;
                }

                // Xuất file Excel
                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                string fileName = $"BaiGiang_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        // Thống kê số lượng bài giảng theo khóa học
        [HttpGet("statistics-by-course")]
        public async Task<IActionResult> StatisticsByCourse()
        {
            var statistics = await _context.Courses
                .Select(c => new
                {
                    CourseId = c.CourseId,
                    CourseName = c.CourseTitle,
                    LessonCount = _context.Lessons.Count(l => l.ClassCourse.CourseId == c.CourseId)
                })
                .ToListAsync();
            return Ok(statistics);
        }

        // Thống kê số lượng bài giảng theo lớp học
        [HttpGet("statistics-by-class")]
        public async Task<IActionResult> StatisticsByClass()
        {
            var statistics = await _context.Classes
                .Select(cl => new
                {
                    ClassId = cl.ClassId,
                    ClassName = cl.ClassTitle,
                    LessonCount = _context.Lessons.Count(l => l.ClassCourse.ClassId == cl.ClassId)
                })
                .ToListAsync();
            return Ok(statistics);
        }

        // Thống kê số lượng bài giảng theo giáo viên
        [HttpGet("statistics-by-teacher")]
        public async Task<IActionResult> StatisticsByTeacher()
        {
            var statistics = await _context.Users
                .Where(u => u.UsersRoleId == 2)
                .Select(u => new
                {
                    TeacherId = u.UsersId,
                    TeacherName = u.UsersName,
                    TeacherEmail = u.UsersEmail,
                    LessonCount = _context.Lessons.Count(l => l.LessonTeacherId == u.UsersId)
                })
                .ToListAsync();
            return Ok(statistics);
        }
    }
}
