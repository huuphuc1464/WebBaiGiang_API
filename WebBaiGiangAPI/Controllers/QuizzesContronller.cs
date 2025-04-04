using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit.Tnef;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using WebBaiGiangAPI.Data;
using WebBaiGiangAPI.Models;

namespace WebBaiGiangAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuizzesContronller : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;

        public QuizzesContronller(AppDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }
        public class QuizRequest
        {
            public Quiz Quiz { get; set; }
            public List<QuizQuestion> QuizQuestions { get; set; }
        }

        // Thêm bài kiểm tra quiz mới
        [HttpPost("create-quiz")]
        public async Task<IActionResult> CreateQuiz(int teacherId, [FromBody] QuizRequest request)
        {
            request.Quiz.QuizCreateAt = DateTime.Now;
            request.Quiz.QuizUpdateAt = DateTime.Now;
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (_context.Users.Find(teacherId) == null) return NotFound("Giáo viên không tồn tại.");
            var existClassCourse = _context.ClassCourses
                .Join(_context.TeacherClasses, cc => cc.CcId, tc => tc.TcClassCourseId, (cc, tc) => new { cc, tc.TcUsersId })
                .Where(x => x.TcUsersId == teacherId && x.cc.CcId == request.Quiz.QuizClassCourseId)
                .FirstOrDefault();
            if (existClassCourse == null) return Unauthorized("Khóa học không tồn tại hoặc không thuộc quyền quản lý của giáo viên.");
            if (request.Quiz.QuizStartAt < DateTime.Now)
            {
                return BadRequest("Thời gian bắt đầu không hợp lệ.");
            }
            if (request.Quiz.QuizEndAt < DateTime.Now)
            {
                return BadRequest("Thời gian kết thúc không hợp lệ.");
            }
            if (request.Quiz.QuizStartAt > request.Quiz.QuizEndAt)
            {
                return BadRequest("Thời gian kết thúc phải sau thời gian bắt đầu.");
            }
            request.Quiz.QuizTitle = Regex.Replace(request.Quiz.QuizTitle.Trim(), @"\s+", " ");
            request.Quiz.QuizDescription = request.Quiz.QuizDescription != null ? Regex.Replace(request.Quiz.QuizDescription.Trim(), @"\s+", " ") : null;
            _context.Quizzes.Add(request.Quiz);
            await _context.SaveChangesAsync();

            if (request.Quiz.QuizStatus == true)
            {

                int quizId = request.Quiz.QuizId;

                foreach (var question in request.QuizQuestions)
                {
                    if (question.QqCorrect != question.QqOption1
                        && question.QqCorrect != question.QqOption2
                        && question.QqCorrect != question.QqOption3
                        && question.QqCorrect != question.QqOption4)
                    {
                        return BadRequest($"Câu trả lời đúng không hợp lệ.\n Câu hỏi: \"{question.QqQuestion}\"");
                    }
                }

                foreach (var question in request.QuizQuestions)
                {
                    question.QqQuizId = quizId;
                    question.QqQuestion = Regex.Replace(question.QqQuestion.Trim(), @"\s+", " ");
                    question.QqOption1 = Regex.Replace(question.QqOption1.Trim(), @"\s+", " ");
                    question.QqOption2 = Regex.Replace(question.QqOption2.Trim(), @"\s+", " ");
                    question.QqOption3 = Regex.Replace(question.QqOption3.Trim(), @"\s+", " ");
                    question.QqOption4 = Regex.Replace(question.QqOption4.Trim(), @"\s+", " ");
                    question.QqCorrect = Regex.Replace(question.QqCorrect.Trim(), @"\s+", " ");
                    _context.QuizQuestions.Add(question);
                }
                _context.SaveChanges();

                var teacher = await _context.Users.Where(u => u.UsersId == teacherId).Select(u => new { u.UsersName, u.UsersEmail }).FirstOrDefaultAsync();
                var className = await _context.Classes.Where(c => c.ClassId == existClassCourse.cc.ClassId).Select(c => c.ClassTitle).FirstOrDefaultAsync();

                var announcement = new Announcement
                {
                    AnnouncementClassId = existClassCourse.cc.ClassId,
                    AnnouncementTitle = $"📝 Bài kiểm tra mới: {request.Quiz.QuizTitle} đã được tạo vào {request.Quiz.QuizCreateAt} bởi giáo viên {teacher.UsersName}",
                    AnnouncementDescription = $"📖 Mô tả: {request.Quiz.QuizDescription} \n🏫 Lớp học: {className} \n⏳ Thời gian làm bài: {(request.Quiz.QuizStartAt - request.Quiz.QuizEndAt).TotalMinutes} phút \n📅 Ngày mở: {request.Quiz.QuizStartAt} - Ngày đóng: {request.Quiz.QuizEndAt}",
                    AnnouncementDate = DateTime.Now,
                    AnnouncementTeacherId = teacherId
                };

                var students = await _context.StudentClasses
                   .Where(sc => sc.ScClassId == existClassCourse.cc.ClassId && sc.ScStatus == 1)
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
                var courseName = _context.Courses.Find(existClassCourse.cc.CourseId)?.CourseTitle;
                int emailCount = 0;
                string subject = $"Giáo viên {teacher.UsersName} đã thêm bài kiểm tra mới!";
                string body = $"<h3>Bài kiểm tra mới: {request.Quiz.QuizTitle}</h3>"
                            + $"<p>Mô tả: {request.Quiz.QuizDescription}</p>"
                            + $"<p>Khóa học: {courseName}</p>"
                            + $"<p>Lớp: {className}</p>"
                            + $"<p>Thời gian làm bài: {(request.Quiz.QuizEndAt - request.Quiz.QuizStartAt).TotalMinutes} phút</p>"
                            + $"<p>Thời gian mở: {request.Quiz.QuizStartAt} - Thời gian đóng: {request.Quiz.QuizEndAt}</p>"
                            + "<p>Vui lòng đăng nhập để làm bài kiểm tra.</p>";

                foreach (var student in students)
                {
                    bool isSent = await _emailService.SendEmail(student.UsersEmail, subject, body);
                    if (isSent)
                    {
                        emailCount++;
                    }
                }
                await _emailService.SendEmail(
                    teacher.UsersEmail,
                    "Thông báo: Bài kiểm tra mới đã được tạo",
                    $"Bài kiểm tra: \"{request.Quiz.QuizTitle}\" đã được tạo thành công và đã được gửi đến {emailCount} sinh viên."
                );
                _context.Announcements.Add(announcement);
                await _context.SaveChangesAsync();
            }
            return Ok(new { Message = " Thêm bài kiểm tra thành công" });
        }

        // Upload file Excel chứa câu hỏi cho bài kiểm tra quiz
        [HttpPost("import-excel")]
        public async Task<IActionResult> ImportExcel(int teacherId, int classCourse, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File không hợp lệ.");

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);

                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets[0];

                // Đọc thông tin bài kiểm tra
                var quiz = new Quiz
                {
                    QuizClassCourseId = classCourse,
                    QuizTitle = worksheet.Cells["B1"].Text.Trim(),
                    QuizStartAt = DateTime.Parse(worksheet.Cells["B2"].Text.Trim()),
                    QuizEndAt = DateTime.Parse(worksheet.Cells["B3"].Text.Trim()),
                    QuizDescription = worksheet.Cells["B4"].Text.Trim(),
                    QuizStatus = worksheet.Cells["B5"].Text.Trim().ToLower() == "hiện"
                };

                var questions = new List<QuizQuestion>();

                int rowCount = worksheet.Dimension.Rows;
                for (int row = 7; row <= rowCount; row++)
                {
                    if (string.IsNullOrWhiteSpace(worksheet.Cells[row, 1].Text)) continue;

                    var question = new QuizQuestion
                    {
                        QqQuestion = worksheet.Cells[row, 1].Text.Trim(),
                        QqOption1 = worksheet.Cells[row, 2].Text.Trim(),
                        QqOption2 = worksheet.Cells[row, 3].Text.Trim(),
                        QqOption3 = worksheet.Cells[row, 4].Text.Trim(),
                        QqOption4 = worksheet.Cells[row, 5].Text.Trim(),
                        QqCorrect = worksheet.Cells[row, 6].Text.Trim(),
                        QqDescription = worksheet.Cells[row, 7].Text.Trim()
                    };
                    questions.Add(question);
                }

                // Gọi lại CreateQuiz để lưu vào DB
                return await CreateQuiz(teacherId, new QuizRequest { Quiz = quiz, QuizQuestions = questions });
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new { message = "Lỗi khi lưu dữ liệu", error = ex.InnerException?.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi hệ thống", error = ex.Message });
            }
        }

        // Thêm môt câu hỏi cho bài kiểm tra quiz
        [HttpPost("add-question")]
        public async Task<IActionResult> AddQuestion(QuizQuestion question)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (_context.Quizzes.Find(question.QqQuizId) == null) return NotFound("Bài kiểm tra không tồn tại.");
            question.QqQuestion = Regex.Replace(question.QqQuestion.Trim(), @"\s+", " ");
            question.QqOption1 = Regex.Replace(question.QqOption1.Trim(), @"\s+", " ");
            question.QqOption2 = Regex.Replace(question.QqOption2.Trim(), @"\s+", " ");
            question.QqOption3 = Regex.Replace(question.QqOption3.Trim(), @"\s+", " ");
            question.QqOption4 = Regex.Replace(question.QqOption4.Trim(), @"\s+", " ");
            question.QqCorrect = Regex.Replace(question.QqCorrect.Trim(), @"\s+", " ");
            _context.QuizQuestions.Add(question);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Thêm câu hỏi thành công" });
        }

        // Lấy danh sách tất cả bài kiểm tra quiz
        [HttpGet("get-all-quiz")]
        public async Task<IActionResult> GetAllQuiz()
        {
            var quizzes = await _context.Quizzes.ToListAsync();
            return Ok(quizzes);
        }

        // Lấy chi tiết một bài kiểm tra của giáo viên theo mã bài kiểm tra
        [HttpGet("get-quiz/{id}/{teacherId}")]
        public async Task<IActionResult> GetQuiz(int id, int teacherId)
        {
            var quizData = await _context.Quizzes
                .Where(q => q.QuizId == id) // Chỉ lấy bài kiểm tra có id cần tìm
                .Join(_context.ClassCourses,
                    q => q.QuizClassCourseId,
                    cc => cc.CcId,
                    (q, cc) => new { q, cc })
                .Join(_context.TeacherClasses,
                    qcc => qcc.cc.CcId,
                    tc => tc.TcClassCourseId,
                    (qcc, tc) => new { qcc.q, qcc.cc, tc })
                .Where(result => result.tc.TcUsersId == teacherId) // Kiểm tra giáo viên có quyền xem
                .Select(result => new
                {
                    QuizId = result.q.QuizId,
                    QuizTitle = result.q.QuizTitle,
                    ClassName = result.cc.Classes.ClassTitle,
                    CourseName = result.cc.Course.CourseTitle,
                    TeacherName = result.tc.User.UsersName,
                    QuizCreateAt = result.q.QuizCreateAt,
                    QuizUpdateAt = result.q.QuizUpdateAt,
                    QuizStartAt = result.q.QuizStartAt,
                    QuizEndAt = result.q.QuizEndAt,
                    QuizDescription = result.q.QuizDescription,
                    QuizStatus = result.q.QuizStatus,
                    Questions = _context.QuizQuestions
                        .Where(qq => qq.QqQuizId == result.q.QuizId)
                        .Select(qq => new
                        {
                            QuestionId = qq.QqId,
                            QuestionText = qq.QqQuestion,
                            Options = new string[] { qq.QqOption1, qq.QqOption2, qq.QqOption3, qq.QqOption4 },
                            CorrectAnswer = qq.QqCorrect,
                            Description = qq.QqDescription
                        }).ToList() // Chuyển thành danh sách câu hỏi
                })
                .FirstOrDefaultAsync();

            if (quizData == null)
                return NotFound("Bài kiểm tra không tồn tại hoặc bạn không có quyền xem.");

            return Ok(quizData);
        }

        // Lấy danh sách bài kiểm tra của giáo viên theo mã giáo viên
        [HttpGet("get-quizzes-by-teacherId/{teacherId}")]
        public async Task<IActionResult> GetQuizzesByTeacherId(int teacherId)
        {
            var quizzes = await _context.Quizzes
                .Join(_context.ClassCourses,
                    q => q.QuizClassCourseId,
                    cc => cc.CcId,
                    (q, cc) => new { q, cc })
                .Join(_context.TeacherClasses,
                    qcc => qcc.cc.CcId,
                    tc => tc.TcClassCourseId,
                    (qcc, tc) => new { qcc.q, qcc.cc, tc })
                .Where(result => result.tc.TcUsersId == teacherId) // Lọc theo giáo viên
                .GroupJoin(_context.QuizQuestions,
                    quiz => quiz.q.QuizId,
                    question => question.QqQuizId,
                    (quiz, questions) => new
                    {
                        QuizId = quiz.q.QuizId,
                        QuizTitle = quiz.q.QuizTitle,
                        ClassName = quiz.cc.Classes.ClassTitle,
                        CourseName = quiz.cc.Course.CourseTitle,
                        TeacherName = quiz.tc.User.UsersName,
                        QuizCreateAt = quiz.q.QuizCreateAt,
                        QuizUpdateAt = quiz.q.QuizUpdateAt,
                        QuizStartAt = quiz.q.QuizStartAt,
                        QuizEndAt = quiz.q.QuizEndAt,
                        QuizDescription = quiz.q.QuizDescription,
                        QuizStatus = quiz.q.QuizStatus,
                        Questions = questions.Select(q => new
                        {
                            QuestionId = q.QqId,
                            QuestionText = q.QqQuestion,
                            Options = new string[] { q.QqOption1, q.QqOption2, q.QqOption3, q.QqOption4 },
                            CorrectAnswer = q.QqCorrect,
                            Description = q.QqDescription
                        }).ToList() // Lấy danh sách câu hỏi
                    })
                .ToListAsync(); // Lấy tất cả bài kiểm tra của giáo viên

            if (quizzes == null || !quizzes.Any())
                return NotFound("Không có bài kiểm tra nào cho giáo viên này.");

            return Ok(quizzes);
        }

        // Lấy danh sách bài kiểm tra theo mã lớp
        [HttpGet("get-quizzes-by-classId/{classId}")]
        public async Task<IActionResult> GetQuizzesByClassId(int classId)
        {
            var quizzes = await _context.Quizzes
                .Join(_context.ClassCourses,
                    q => q.QuizClassCourseId,
                    cc => cc.CcId,
                    (q, cc) => new { q, cc })
                .Join(_context.TeacherClasses,
                    qcc => qcc.cc.CcId,
                    tc => tc.TcClassCourseId,
                    (qcc, tc) => new { qcc.q, qcc.cc, tc })
                .Where(result => result.cc.ClassId == classId)
                .GroupJoin(_context.QuizQuestions,
                    quiz => quiz.q.QuizId,
                    question => question.QqQuizId,
                    (quiz, questions) => new
                    {
                        QuizId = quiz.q.QuizId,
                        QuizTitle = quiz.q.QuizTitle,
                        ClassName = quiz.cc.Classes.ClassTitle,
                        CourseName = quiz.cc.Course.CourseTitle,
                        TeacherName = quiz.tc.User.UsersName,
                        QuizCreateAt = quiz.q.QuizCreateAt,
                        QuizUpdateAt = quiz.q.QuizUpdateAt,
                        QuizStartAt = quiz.q.QuizStartAt,
                        QuizEndAt = quiz.q.QuizEndAt,
                        QuizDescription = quiz.q.QuizDescription,
                        QuizStatus = quiz.q.QuizStatus,
                        Questions = questions.Select(q => new
                        {
                            QuestionId = q.QqId,
                            QuestionText = q.QqQuestion,
                            Options = new string[] { q.QqOption1, q.QqOption2, q.QqOption3, q.QqOption4 },
                            CorrectAnswer = q.QqCorrect,
                            Description = q.QqDescription
                        }).ToList() // Lấy danh sách câu hỏi
                    })
                .ToListAsync(); // Lấy tất cả bài kiểm tra của lớp học

            if (quizzes == null || !quizzes.Any())
                return NotFound("Không có bài kiểm tra nào cho lớp này.");

            return Ok(quizzes);
        }

        // Lấy danh sách bài kiểm tra của giáo viên theo mã lớp
        [HttpGet("get-quizzes-by-class-teacher/{classId}/{teacherId}")]
        public async Task<IActionResult> GetQuizzesByClassTeacher(int classId, int teacherId)
        {
            var quizzes = await _context.Quizzes
                .Join(_context.ClassCourses,
                    q => q.QuizClassCourseId,
                    cc => cc.CcId,
                    (q, cc) => new { q, cc })
                .Join(_context.TeacherClasses,
                    qcc => qcc.cc.CcId,
                    tc => tc.TcClassCourseId,
                    (qcc, tc) => new { qcc.q, qcc.cc, tc })
                .Where(result => result.cc.ClassId == classId && result.tc.TcUsersId == teacherId)
                .GroupJoin(_context.QuizQuestions,
                    quiz => quiz.q.QuizId,
                    question => question.QqQuizId,
                    (quiz, questions) => new
                    {
                        QuizId = quiz.q.QuizId,
                        QuizTitle = quiz.q.QuizTitle,
                        ClassName = quiz.cc.Classes.ClassTitle,
                        CourseName = quiz.cc.Course.CourseTitle,
                        TeacherName = quiz.tc.User.UsersName,
                        QuizCreateAt = quiz.q.QuizCreateAt,
                        QuizUpdateAt = quiz.q.QuizUpdateAt,
                        QuizStartAt = quiz.q.QuizStartAt,
                        QuizEndAt = quiz.q.QuizEndAt,
                        QuizDescription = quiz.q.QuizDescription,
                        QuizStatus = quiz.q.QuizStatus,
                        Questions = questions.Select(q => new
                        {
                            QuestionId = q.QqId,
                            QuestionText = q.QqQuestion,
                            Options = new string[] { q.QqOption1, q.QqOption2, q.QqOption3, q.QqOption4 },
                            CorrectAnswer = q.QqCorrect,
                            Description = q.QqDescription
                        }).ToList()
                    })
                .ToListAsync();

            if (quizzes == null || !quizzes.Any())
                return NotFound("Không có bài kiểm tra nào cho giáo viên trong lớp này.");

            return Ok(quizzes);
        }

        // Cập nhật bài Quiz
        [HttpPut("update-quiz/{id}")]
        public async Task<IActionResult> UpdateQuiz(int teacherId, int id, [FromBody] QuizRequest request)
        {
            request.Quiz.QuizUpdateAt = DateTime.Now;
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var quiz = await _context.Quizzes.FindAsync(id);
            var oldQuiz = await _context.Quizzes.FindAsync(id);
            if (quiz == null) return NotFound("Bài kiểm tra không tồn tại.");
            if (_context.Users.Find(teacherId) == null) return NotFound("Giáo viên không tồn tại.");
            var existClassCourse = _context.ClassCourses
                .Join(_context.TeacherClasses, cc => cc.CcId, tc => tc.TcClassCourseId, (cc, tc) => new { cc, tc.TcUsersId })
                .Where(x => x.TcUsersId == teacherId && x.cc.CcId == request.Quiz.QuizClassCourseId)
                .FirstOrDefault();
            if (existClassCourse == null) return Unauthorized("Khóa học không tồn tại hoặc không thuộc quyền quản lý của giáo viên.");
            if (request.Quiz.QuizStartAt < DateTime.Now)
            {
                return BadRequest("Thời gian bắt đầu không hợp lệ.");
            }
            if (request.Quiz.QuizEndAt < DateTime.Now)
            {
                return BadRequest("Thời gian kết thúc không hợp lệ.");
            }
            if (request.Quiz.QuizStartAt > request.Quiz.QuizEndAt)
            {
                return BadRequest("Thời gian kết thúc phải sau thời gian bắt đầu.");
            }

            quiz.QuizTitle = Regex.Replace(request.Quiz.QuizTitle.Trim(), @"\s+", " ");
            quiz.QuizDescription = request.Quiz.QuizDescription != null ? Regex.Replace(request.Quiz.QuizDescription.Trim(), @"\s+", " ") : null;
            quiz.QuizStartAt = request.Quiz.QuizStartAt;
            quiz.QuizEndAt = request.Quiz.QuizEndAt;
            quiz.QuizStatus = request.Quiz.QuizStatus;
            _context.Entry(quiz).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            var questions = await _context.QuizQuestions.Where(q => q.QqQuizId == id).ToListAsync();
            foreach (var question in request.QuizQuestions)
            {
                if (question.QqCorrect != question.QqOption1
                    && question.QqCorrect != question.QqOption2
                    && question.QqCorrect != question.QqOption3
                    && question.QqCorrect != question.QqOption4)
                {
                    return BadRequest($"Câu trả lời đúng không hợp lệ.\n Câu hỏi: \"{question.QqQuestion}\"");
                }
            }

            // Xóa tất cả câu hỏi cũ
            foreach (var question in questions)
            {
                _context.QuizQuestions.Remove(question);
            }
            await _context.SaveChangesAsync();
            foreach (var question in request.QuizQuestions)
            {
                question.QqQuizId = id;
                question.QqQuestion = Regex.Replace(question.QqQuestion.Trim(), @"\s+", " ");
                question.QqOption1 = Regex.Replace(question.QqOption1.Trim(), @"\s+", " ");
                question.QqOption2 = Regex.Replace(question.QqOption2.Trim(), @"\s+", " ");
                question.QqOption3 = Regex.Replace(question.QqOption3.Trim(), @"\s+", " ");
                question.QqOption4 = Regex.Replace(question.QqOption4.Trim(), @"\s+", " ");
                question.QqCorrect = Regex.Replace(question.QqCorrect.Trim(), @"\s+", " ");
                _context.QuizQuestions.Add(question);
            }
            await _context.SaveChangesAsync();
            if (quiz.QuizStatus == true)
            {
                var teacher = await _context.Users.Where(u => u.UsersId == teacherId).Select(u => new { u.UsersName, u.UsersEmail }).FirstOrDefaultAsync();
                var className = await _context.Classes.Where(c => c.ClassId == existClassCourse.cc.ClassId).Select(c => c.ClassTitle).FirstOrDefaultAsync();
                var announcement = new Announcement
                {
                    AnnouncementClassId = existClassCourse.cc.ClassId,
                    AnnouncementTitle = $"✏️ Bài kiểm tra {quiz.QuizTitle} đã được cập nhật vào {DateTime.Now} bởi giáo viên {teacher.UsersName}",
                    AnnouncementDescription = $"🔄 **Cập nhật thông tin bài kiểm tra**:\n\n" +
                                $"🏫 **Lớp học:** {className}\n" +
                                $"📚 **Tên bài kiểm tra:** {oldQuiz.QuizTitle} ➝ {quiz.QuizTitle}\n" +
                                $"📝 **Mô tả:** {oldQuiz.QuizDescription} ➝ {quiz.QuizDescription}\n" +
                                $"📅 **Ngày bắt đầu:** {oldQuiz.QuizStartAt} ➝ {quiz.QuizStartAt}\n" +
                                $"⏳ **Ngày kết thúc:** {oldQuiz.QuizEndAt} ➝ {quiz.QuizEndAt}" +
                                $"⏳ **Thời gian làm bài:** {(oldQuiz.QuizEndAt - oldQuiz.QuizStartAt).TotalMinutes} ➝ {(quiz.QuizEndAt - quiz.QuizStartAt).TotalMinutes} phút\n" +
                                $"📅 **Ngày cập nhật:** {DateTime.Now}\n",
                    AnnouncementDate = DateTime.Now,
                    AnnouncementTeacherId = teacherId
                };

                var students = await _context.StudentClasses
                   .Where(sc => sc.ScClassId == existClassCourse.cc.ClassId && sc.ScStatus == 1)
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
                var courseName = _context.Courses.Find(existClassCourse.cc.CourseId)?.CourseTitle;
                int emailCount = 0;
                string subject = $"Giáo viên \"{teacher.UsersName}\" đã cập nhật bài kiểm tra!";
                string body = $"<h3>🔄 <strong>Cập nhật thông tin bài kiểm tra</strong></h3>" +
                              $"<p>✏️ <strong>Bài kiểm tra</strong> \"{oldQuiz.QuizTitle}\" đã được cập nhật vào {DateTime.Now} bởi giáo viên \"{teacher.UsersName}\"</p>" +
                              $"<p>🏫 <strong>Lớp học:</strong> {className}</p>" +
                              $"<p>📚 <strong>Tên bài kiểm tra:</strong> {oldQuiz.QuizTitle} ➝ {quiz.QuizTitle}</p>" +
                              $"<p>📝 <strong>Mô tả:</strong> {oldQuiz.QuizDescription} ➝ {quiz.QuizDescription}</p>" +
                              $"<p>📅 <strong>Ngày bắt đầu:</strong> {oldQuiz.QuizStartAt} ➝ {quiz.QuizStartAt}</p>" +
                              $"<p>⏳ <strong>Ngày kết thúc:</strong> {oldQuiz.QuizEndAt} ➝ {quiz.QuizEndAt}</p>" +
                              $"<p>⏳ <strong>Thời gian làm bài:</strong> {(oldQuiz.QuizEndAt - oldQuiz.QuizStartAt).TotalMinutes} phút ➝ {(quiz.QuizEndAt - quiz.QuizStartAt).TotalMinutes} phút</p>" +
                              $"<p>📅 <strong>Ngày cập nhật:</strong> {DateTime.Now}</p>" +
                              $"<p>📌 Vui lòng đăng nhập để xem thông tin chi tiết và làm bài kiểm tra.</p>";

                foreach (var student in students)
                {
                    bool isSent = await _emailService.SendEmail(student.UsersEmail, subject, body);
                    if (isSent)
                    {
                        emailCount++;
                    }
                }
                await _emailService.SendEmail(
                    teacher.UsersEmail,
                    $"Thông báo: Bài kiểm tra đã được cập nhật\n",
                    $"Bài kiểm tra: \"{request.Quiz.QuizTitle}\" đã được cập nhật thành công và đã được gửi đến {emailCount} sinh viên."
                );
                _context.Announcements.Add(announcement);
                await _context.SaveChangesAsync();

            }
            return Ok("Cập nhật bài kiểm tra thành công");
        }

        // Ẩn/Hiện bài Quiz
        [HttpPut("visibility/{id}")]
        public async Task<IActionResult> ToggleQuizVisibility(int id, int teacherId)
        {
            var quiz = await _context.Quizzes.FindAsync(id);
            if (quiz == null) return NotFound("Bài kiểm tả quiz không tồn tại");
            var existClassCourse = _context.ClassCourses
                    .Join(_context.TeacherClasses, cc => cc.CcId, tc => tc.TcClassCourseId, (cc, tc) => new { cc, tc.TcUsersId })
                    .Where(x => x.TcUsersId == teacherId && x.cc.CcId == quiz.QuizClassCourseId)
                    .FirstOrDefault();
            if (existClassCourse == null) return Unauthorized("Khóa học không tồn tại hoặc không thuộc quyền quản lý của giáo viên.");
            quiz.QuizStatus = !quiz.QuizStatus;
            quiz.QuizUpdateAt = DateTime.Now;
            await _context.SaveChangesAsync();

            if (quiz.QuizStatus)
            {
                int quizId = quiz.QuizId;


                var teacher = await _context.Users.Where(u => u.UsersId == teacherId).Select(u => new { u.UsersName, u.UsersEmail }).FirstOrDefaultAsync();
                var className = await _context.Classes.Where(c => c.ClassId == existClassCourse.cc.ClassId).Select(c => c.ClassTitle).FirstOrDefaultAsync();

                var announcement = new Announcement
                {
                    AnnouncementClassId = existClassCourse.cc.ClassId,
                    AnnouncementTitle = $"📝 Bài kiểm tra mới: {quiz.QuizTitle} đã được tạo vào {quiz.QuizCreateAt} bởi giáo viên {teacher.UsersName}",
                    AnnouncementDescription = $"📖 Mô tả: {quiz.QuizDescription} \n🏫 Lớp học: {className} \n⏳ Thời gian làm bài: {(quiz.QuizStartAt - quiz.QuizEndAt).TotalMinutes} phút \n📅 Ngày mở: {quiz.QuizStartAt} - Ngày đóng: {quiz.QuizEndAt}",
                    AnnouncementDate = DateTime.Now,
                    AnnouncementTeacherId = teacherId
                };

                var students = await _context.StudentClasses
                   .Where(sc => sc.ScClassId == existClassCourse.cc.ClassId && sc.ScStatus == 1)
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
                var courseName = _context.Courses.Find(existClassCourse.cc.CourseId)?.CourseTitle;
                int emailCount = 0;
                string subject = $"Giáo viên {teacher.UsersName} đã thêm bài kiểm tra mới!";
                string body = $"<h3>Bài kiểm tra mới: {quiz.QuizTitle}</h3>"
                            + $"<p>Mô tả: {quiz.QuizDescription}</p>"
                            + $"<p>Khóa học: {courseName}</p>"
                            + $"<p>Lớp: {className}</p>"
                            + $"<p>Thời gian làm bài: {(quiz.QuizEndAt - quiz.QuizStartAt).TotalMinutes} phút</p>"
                            + $"<p>Thời gian mở: {quiz.QuizStartAt} - Thời gian đóng: {quiz.QuizEndAt}</p>"
                            + "<p>Vui lòng đăng nhập để làm bài kiểm tra.</p>";

                foreach (var student in students)
                {
                    bool isSent = await _emailService.SendEmail(student.UsersEmail, subject, body);
                    if (isSent)
                    {
                        emailCount++;
                    }
                }
                await _emailService.SendEmail(
                    teacher.UsersEmail,
                    "Thông báo: Bài kiểm tra mới đã được tạo",
                    $"Bài kiểm tra: \"{quiz.QuizTitle}\" đã được tạo thành công và đã được gửi đến {emailCount} sinh viên."
                );
                _context.Announcements.Add(announcement);
                await _context.SaveChangesAsync();

            }
            return Ok($"Trạng thái bài kiểm tra quiz đã được thay đổi thành {(quiz.QuizStatus ? "Hiện" : "Ẩn")}.");
        }

        // Xóa bài Quiz
        [HttpDelete("delete-quiz/{id}")]
        public async Task<IActionResult> DeleteQuiz(int id, int teacherId)
        {
            var quiz = await _context.Quizzes.FindAsync(id);
            if (quiz == null) return NotFound("Bài kiểm tra không tồn tại.");
            var existClassCourse = _context.ClassCourses
                .Join(_context.TeacherClasses, cc => cc.CcId, tc => tc.TcClassCourseId, (cc, tc) => new { cc, tc.TcUsersId })
                .Where(x => x.TcUsersId == teacherId && x.cc.CcId == quiz.QuizClassCourseId)
                .FirstOrDefault();
            if (existClassCourse == null) return Unauthorized("Khóa học không tồn tại hoặc không thuộc quyền quản lý của giáo viên.");
            bool isLinked = await _context.QuizResults.AnyAsync(qr => qr.QrQuizId == id);
            if (isLinked)
            {
                return BadRequest("Không thể xóa bài kiểm tra này vì nó đã có sinh viên làm bài kiểm tra.\n Vui lòng xóa điểm bài kiểm tra này trước.");
            }
            var questions = await _context.QuizQuestions.Where(q => q.QqQuizId == id).ToListAsync();

            if (questions.Any())
            {
                _context.QuizQuestions.RemoveRange(questions);
                await _context.SaveChangesAsync();
            }
            _context.Quizzes.Remove(quiz);
            await _context.SaveChangesAsync();

            var className = await _context.Classes.Where(c => c.ClassId == existClassCourse.cc.ClassId).Select(c => c.ClassTitle).FirstOrDefaultAsync();

            var announcement = new Announcement
            {
                AnnouncementClassId = existClassCourse.cc.ClassId,
                AnnouncementTitle = $"❌ Bài kiểm tra {quiz.QuizTitle} đã bị xóa vào {DateTime.Now} bởi giáo viên {teacherId}",
                AnnouncementDescription = $"📖 Mô tả: {quiz.QuizDescription} \n🏫 Lớp học: {className} \n⏳ Thời gian làm bài: {(quiz.QuizStartAt - quiz.QuizEndAt).TotalMinutes} phút \n📅 Ngày mở: {quiz.QuizStartAt} - Ngày đóng: {quiz.QuizEndAt}",
                AnnouncementDate = DateTime.Now,
                AnnouncementTeacherId = teacherId
            };
            var teacher = await _context.Users.Where(u => u.UsersId == teacherId).Select(u => new { u.UsersName, u.UsersEmail }).FirstOrDefaultAsync();
            var students = await _context.StudentClasses
               .Where(sc => sc.ScClassId == existClassCourse.cc.ClassId && sc.ScStatus == 1)
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
            var courseName = _context.Courses.Find(existClassCourse.cc.CourseId)?.CourseTitle;
            int emailCount = 0;
            string subject = $"Giáo viên {teacher.UsersName} đã xóa bài kiểm tra!";
            string body = $"<h3>Bài kiểm tra đã bị xóa: {quiz.QuizTitle}</h3>"
                        + $"<p>Mô tả: {quiz.QuizDescription}</p>"
                        + $"<p>Khóa học: {courseName}</p>"
                        + $"<p>Lớp: {className}</p>"
                        + $"<p>Thời gian làm bài: {(quiz.QuizEndAt - quiz.QuizStartAt).TotalMinutes} phút</p>"
                        + $"<p>Thời gian mở: {quiz.QuizStartAt} - Thời gian đóng: {quiz.QuizEndAt}</p>"
                        + "<p>Vui lòng đăng nhập để xem thông tin chi tiết.</p>";
            foreach (var student in students)
            {
                bool isSent = await _emailService.SendEmail(student.UsersEmail, subject, body);
                if (isSent)
                {
                    emailCount++;
                }
            }
            await _emailService.SendEmail(
                teacher.UsersEmail,
                "Thông báo: Bài kiểm tra đã bị xóa",
                $"Bài kiểm tra: \"{quiz.QuizTitle}\" đã bị xóa thành công và đã được gửi đến {emailCount} sinh viên."
            );
            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();
            return Ok("Xóa bài kiểm tra thành công");
        }

        // Nhân bản bài Quiz sang lớp khác
        [HttpPost("duplicate/{quizId}/{classCourseId}")]
        public async Task<IActionResult> DuplicateQuiz(int teacherId, int quizId, int classCourseId)
        {
            var quiz = await _context.Quizzes.FindAsync(quizId);
            if (quiz == null) return NotFound("Bài kiểm tra không tồn tại.");

            var newClass = await _context.ClassCourses.FindAsync(classCourseId);
            if (newClass == null) return NotFound("Lớp học phần không tồn tại.");

            var courseId = await _context.ClassCourses
                .Where(cc => cc.CcId == quiz.QuizClassCourseId)
                .Select(cc => cc.CourseId)
                .FirstOrDefaultAsync();

            if (courseId == null)
                return BadRequest("Không tìm thấy học phần của bài kiểm tra.");

            bool exists = await _context.ClassCourses
                .AnyAsync(cc => cc.CcId == classCourseId && cc.CourseId == courseId);

            if (!exists)
                return BadRequest("Lớp học mới không thuộc cùng học phần với bài kiểm tra.");

            var teacherClass = _context.TeacherClasses.Join(_context.ClassCourses,
                                                            tc => tc.TcClassCourseId,
                                                            cc => cc.CcId,
                                                            (tc, cc) => new { tc.TcUsersId, cc.CcId })
                                                        .Where(x => x.TcUsersId == teacherId && x.CcId == classCourseId)
                                                        .FirstOrDefault();
            if (teacherClass == null) return BadRequest("Giáo viên của bài giảng không thuộc lớp mới.");
            // Tạo bản sao của bài kiểm tra
            var newQuiz = new Quiz
            {
                QuizTitle = quiz.QuizTitle,
                QuizDescription = quiz.QuizDescription,
                QuizStartAt = quiz.QuizStartAt,
                QuizEndAt = quiz.QuizEndAt,
                QuizClassCourseId = classCourseId,
                QuizStatus = quiz.QuizStatus,
                QuizCreateAt = DateTime.Now,
                QuizUpdateAt = DateTime.Now
            };
            _context.Quizzes.Add(newQuiz);
            await _context.SaveChangesAsync();
            // Nhân bản các câu hỏi
            var questions = await _context.QuizQuestions
                .Where(q => q.QqQuizId == quizId)
                .ToListAsync();
            foreach (var question in questions)
            {
                var newQuestion = new QuizQuestion
                {
                    QqQuestion = question.QqQuestion,
                    QqOption1 = question.QqOption1,
                    QqOption2 = question.QqOption2,
                    QqOption3 = question.QqOption3,
                    QqOption4 = question.QqOption4,
                    QqCorrect = question.QqCorrect,
                    QqDescription = question.QqDescription,
                    QqQuizId = newQuiz.QuizId
                };
                _context.QuizQuestions.Add(newQuestion);
            }
            await _context.SaveChangesAsync();
            if (newQuiz.QuizStatus)
            {
                var teacher = await _context.Users.Where(u => u.UsersId == teacherId).Select(u => new { u.UsersName, u.UsersEmail }).FirstOrDefaultAsync();
                var className = await _context.Classes.Where(c => c.ClassId == newClass.ClassId).Select(c => c.ClassTitle).FirstOrDefaultAsync();
                var announcement = new Announcement
                {
                    AnnouncementClassId = newClass.ClassId,
                    AnnouncementTitle = $"📝 Bài kiểm tra mới: {newQuiz.QuizTitle} đã được tạo vào {newQuiz.QuizCreateAt} bởi giáo viên {teacher.UsersName}",
                    AnnouncementDescription = $"📖 Mô tả: {newQuiz.QuizDescription} \n🏫 Lớp học: {className} \n⏳ Thời gian làm bài: {(newQuiz.QuizStartAt - newQuiz.QuizEndAt).TotalMinutes} phút \n📅 Ngày mở: {newQuiz.QuizStartAt} - Ngày đóng: {newQuiz.QuizEndAt}",
                    AnnouncementDate = DateTime.Now,
                    AnnouncementTeacherId = teacherId
                };
                var students = await _context.StudentClasses
                   .Where(sc => sc.ScClassId == newClass.ClassId && sc.ScStatus == 1)
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
                var courseName = _context.Courses.Find(newClass.CourseId)?.CourseTitle;
                int emailCount = 0;
                string subject = $"Giáo viên {teacher.UsersName} đã thêm bài kiểm tra mới!";
                string body = $"<h3>Bài kiểm tra mới: {newQuiz.QuizTitle}</h3>"
                            + $"<p>Mô tả: {newQuiz.QuizDescription}</p>"
                            + $"<p>Khóa học: {courseName}</p>"
                            + $"<p>Lớp: {className}</p>"
                            + $"<p>Thời gian làm bài: {(newQuiz.QuizEndAt - newQuiz.QuizStartAt).TotalMinutes} phút</p>"
                            + $"<p>Thời gian mở: {newQuiz.QuizStartAt} - Thời gian đóng: {newQuiz.QuizEndAt}</p>"
                            + "<p>Vui lòng đăng nhập để làm bài kiểm tra.</p>";
                foreach (var student in students)
                {
                    bool isSent = await _emailService.SendEmail(student.UsersEmail, subject, body);
                    if (isSent)
                    {
                        emailCount++;
                    }
                }
                await _emailService.SendEmail(
                        teacher.UsersEmail,
                        "Thông báo: Bài kiểm tra mới đã được tạo",
                        $"Bài kiểm tra: \"{newQuiz.QuizTitle}\" đã được tạo thành công và đã được gửi đến {emailCount} sinh viên."
                    );
                _context.Announcements.Add(announcement);
                await _context.SaveChangesAsync();
            }
            return Ok(new { Message = "Nhân bản bài kiểm tra thành công", QuizId = newQuiz });
        }

        // Tìm kiếm bài Quiz theo kĩ thuật full-search text
        [HttpGet("search")]
        public async Task<IActionResult> SearchQuizzes(
            string? keyword = "",
            int? teacherId = null,
            int? classCourseId = null,
            string? status = null,
            int page = 1,
            int pageSize = 10)
        {
            // Chuẩn hóa từ khóa và tách thành danh sách từ
            var keywords = keyword?.ToLower().Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? new string[0];

            var query = from quiz in _context.Quizzes
                        join classCourse in _context.ClassCourses on quiz.QuizClassCourseId equals classCourse.CcId
                        join teacherClass in _context.TeacherClasses on classCourse.CcId equals teacherClass.TcClassCourseId
                        join teacher in _context.Users on teacherClass.TcUsersId equals teacher.UsersId
                        select new
                        {
                            quiz.QuizId,
                            quiz.QuizClassCourseId,
                            quiz.QuizTitle,
                            quiz.QuizDescription,
                            quiz.QuizStartAt,
                            quiz.QuizEndAt,
                            quiz.QuizStatus,
                            quiz.QuizCreateAt,
                            quiz.QuizUpdateAt,
                            TeacherName = teacher.UsersName,
                            TeacherId = teacher.UsersId,
                        };

            // Tìm kiếm theo tiêu chí (Full-Text Search)
            if (keywords.Length > 0)
            {
                query = query.Where(q => keywords.Any(kw =>
                    (q.QuizTitle != null && q.QuizTitle.ToLower().Contains(kw)) ||
                    (q.QuizDescription != null && q.QuizDescription.ToLower().Contains(kw)) ||
                    (q.TeacherName != null && q.TeacherName.ToLower().Contains(kw))
                ));
            }
            // Lọc theo giáo viên
            if (teacherId.HasValue)
            {
                query = query.Where(q => q.TeacherId == teacherId);
            }
            // Lọc theo lớp học phần
            if (classCourseId.HasValue)
            {
                query = query.Where(q => q.QuizClassCourseId == classCourseId);
            }
            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(status))
            {
                switch (status.ToLower())
                {
                    case "1": //hiển thị
                        query = query.Where(q => q.QuizStatus == true);
                        break;
                    case "0": //ẩn
                        query = query.Where(q => q.QuizStatus == false);
                        break;
                    case "2": //đang diễn ra
                        query = query.Where(q => q.QuizStartAt <= DateTime.Now && q.QuizEndAt >= DateTime.Now);
                        break;
                    case "3": //đã kết thúc
                        query = query.Where(q => q.QuizEndAt < DateTime.Now);
                        break;
                }
            }
            // Tổng số kết quả tìm thấy
            int totalItems = await query.CountAsync();

            // Phân trang
            var result = await query.OrderByDescending(q => q.QuizUpdateAt)
                                    .Skip((page - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToListAsync();
            return Ok(new
            {
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                CurrentPage = page,
                PageSize = pageSize,
                Data = result
            });
        }

        // Xuất bài kiểm tra và câu hỏi ra file excel theo mã bài kiểm tra quiz
        [HttpGet("export-quiz-by-quizid/{quizId}")]
        public async Task<IActionResult> ExportQuizByQuizId(int quizId, int teacherId)
        {
            bool isTeacher = _context.ClassCourses
                .Join(_context.TeacherClasses, cc => cc.CcId, tc => tc.TcClassCourseId, (cc, tc) => new { cc, tc })
                .Join(_context.Quizzes, combined => combined.cc.CcId, q => q.QuizClassCourseId, (combined, q) => new { combined.tc, q })
                .Any(result => result.tc.TcUsersId == teacherId && result.q.QuizId == quizId);
            if (!isTeacher)
            {
                return Unauthorized("Bạn không có quyền truy cập vào bài kiểm tra này.");
            }
            var quiz = await _context.Quizzes.FindAsync(quizId);
            if (quiz == null)
                return NotFound("Bài kiểm tra không tồn tại.");

            var questions = _context.QuizQuestions
                .Where(q => q.QqQuizId == quizId)
                .ToList();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Quiz Export");

                // Định dạng tiêu đề bài kiểm tra
                worksheet.Cells["A1"].Value = "Tên bài kiểm tra";
                worksheet.Cells["B1"].Value = quiz.QuizTitle;
                worksheet.Cells["A2"].Value = "Thời gian bắt đầu";
                worksheet.Cells["B2"].Value = quiz.QuizStartAt.ToString("dd/MM/yyyy HH:mm:ss");
                worksheet.Cells["A3"].Value = "Thời gian kết thúc";
                worksheet.Cells["B3"].Value = quiz.QuizEndAt.ToString("dd/MM/yyyy HH:mm:ss");
                worksheet.Cells["A4"].Value = "Mô tả";
                worksheet.Cells["B4"].Value = quiz.QuizDescription;
                worksheet.Cells["A5"].Value = "Trạng thái";
                worksheet.Cells["B5"].Value = quiz.QuizStatus ? "Hiện" : "Ẩn";

                // Định dạng tiêu đề bảng câu hỏi
                worksheet.Cells["A6"].Value = "Câu hỏi";
                worksheet.Cells["B6"].Value = "Câu trả lời 1";
                worksheet.Cells["C6"].Value = "Câu trả lời 2";
                worksheet.Cells["D6"].Value = "Câu trả lời 3";
                worksheet.Cells["E6"].Value = "Câu trả lời 4";
                worksheet.Cells["F6"].Value = "Đáp án";
                worksheet.Cells["G6"].Value = "Mô tả";

                using (var range = worksheet.Cells["A6:G6"])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
                }

                // Đổ dữ liệu câu hỏi
                int row = 7;
                foreach (var question in questions)
                {
                    worksheet.Cells[row, 1].Value = question.QqQuestion;
                    worksheet.Cells[row, 2].Value = question.QqOption1;
                    worksheet.Cells[row, 3].Value = question.QqOption2;
                    worksheet.Cells[row, 4].Value = question.QqOption3;
                    worksheet.Cells[row, 5].Value = question.QqOption4;
                    worksheet.Cells[row, 6].Value = question.QqCorrect;
                    worksheet.Cells[row, 7].Value = question.QqDescription;
                    row++;
                }

                // Auto-fit cột
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                // Xuất file Excel
                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                string excelName = $"Quiz_{quiz.QuizTitle.Replace(" ", "_")}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
            }
        }

        // Xuất bài kiểm tra và câu hỏi ra file excel theo mã lớp học phần giáo viên dạy
        [HttpGet("export-quiz-by-classcourseid/{classCourseId}")]
        public async Task<IActionResult> ExportQuizByClassCourseId(int classCourseId, int teacherId)
        {
            bool existingClassCourse = _context.ClassCourses.Any(cc => cc.CcId == classCourseId);
            if (!existingClassCourse)
            {
                return NotFound("Lớp học phần không tồn tại");
            }
            bool isTeacher = _context.TeacherClasses.Any(result => result.TcUsersId == teacherId && result.TcClassCourseId == classCourseId);
            if (!isTeacher)
            {
                return Unauthorized("Bạn không có quyền truy cập vào bài kiểm tra này.");
            }

            var quizzes = await _context.Quizzes
                .Where(q => q.QuizClassCourseId == classCourseId)
                .AsNoTracking()
                .ToListAsync();

            if (!quizzes.Any())
                return NotFound("Không tìm thấy bài kiểm tra nào cho lớp học này.");

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                // Lặp qua mỗi bài kiểm tra và tạo một sheet mới cho từng bài
                foreach (var quiz in quizzes)
                {
                    // Thêm worksheet cho bài kiểm tra
                    int sheetIndex = 1;
                    string sheetName = quiz.QuizTitle; // Dùng tên bài kiểm tra làm tên sheet

                    // Kiểm tra nếu worksheet với tên đó đã tồn tại
                    while (package.Workbook.Worksheets.Any(ws => ws.Name == sheetName))
                    {
                        sheetName = $"{quiz.QuizTitle}_{sheetIndex}"; // Thêm chỉ số vào tên
                        sheetIndex++;
                    }

                    var worksheet = package.Workbook.Worksheets.Add(sheetName);

                    worksheet.Cells["A1"].Value = "Tên bài kiểm tra";
                    worksheet.Cells["B1"].Value = quiz.QuizTitle;
                    worksheet.Cells["A2"].Value = "Thời gian bắt đầu";
                    worksheet.Cells["B2"].Value = quiz.QuizStartAt.ToString("dd/MM/yyyy HH:mm:ss");
                    worksheet.Cells["A3"].Value = "Thời gian kết thúc";
                    worksheet.Cells["B3"].Value = quiz.QuizEndAt.ToString("dd/MM/yyyy HH:mm:ss");
                    worksheet.Cells["A4"].Value = "Mô tả";
                    worksheet.Cells["B4"].Value = quiz.QuizDescription;
                    worksheet.Cells["A5"].Value = "Trạng thái";
                    worksheet.Cells["B5"].Value = quiz.QuizStatus ? "Hiện" : "Ẩn";

                    // Lấy các câu hỏi của bài kiểm tra này
                    var questions = _context.QuizQuestions
                        .Where(q => q.QqQuizId == quiz.QuizId)
                        .AsNoTracking()
                        .ToList();

                    // Định dạng tiêu đề bảng câu hỏi
                    worksheet.Cells["A6"].Value = "Câu hỏi";
                    worksheet.Cells["B6"].Value = "Câu trả lời 1";
                    worksheet.Cells["C6"].Value = "Câu trả lời 2";
                    worksheet.Cells["D6"].Value = "Câu trả lời 3";
                    worksheet.Cells["E6"].Value = "Câu trả lời 4";
                    worksheet.Cells["F6"].Value = "Đáp án";
                    worksheet.Cells["G6"].Value = "Mô tả";

                    // Định dạng kiểu chữ cho tiêu đề bảng
                    using (var range = worksheet.Cells["A6:G6"])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
                    }

                    // Đổ dữ liệu câu hỏi vào bảng
                    int row = 7;
                    foreach (var question in questions)
                    {
                        worksheet.Cells[row, 1].Value = question.QqQuestion;
                        worksheet.Cells[row, 2].Value = question.QqOption1;
                        worksheet.Cells[row, 3].Value = question.QqOption2;
                        worksheet.Cells[row, 4].Value = question.QqOption3;
                        worksheet.Cells[row, 5].Value = question.QqOption4;
                        worksheet.Cells[row, 6].Value = question.QqCorrect;
                        worksheet.Cells[row, 7].Value = question.QqDescription;
                        row++;
                    }

                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                }

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                var classCourseInfo = from cc in _context.ClassCourses
                                      join cl in _context.Classes on cc.ClassId equals cl.ClassId
                                      join co in _context.Courses on cc.CourseId equals co.CourseId
                                      where cc.CcId == classCourseId
                                      select new { cl.ClassTitle, co.CourseTitle };
                var classCourse = classCourseInfo.FirstOrDefault();
                string excelName = $"Quizzes_[{classCourse.CourseTitle.Replace(" ", "_")}]_[{classCourse.ClassTitle.Replace(" ", "_")}].xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
            }
        }

        // Xuất bài kiểm tra và câu hỏi ra file excel theo mã giáo viên
        [HttpGet("export-quiz-by-teacherid/{teacherId}")]
        public async Task<IActionResult> ExportQuizByTeacherId(int teacherId)
        {
            var existingTeacher = _context.Users.Where(u => u.UsersId == teacherId && u.UsersRoleId == 2).Select(u => u.UsersName).FirstOrDefault();
            if (existingTeacher == null)
            {
                return NotFound("Giáo viên không tồn tại");
            }
            var quizzes = await _context.Quizzes
                .Join(_context.TeacherClasses, q => q.QuizClassCourseId, tc => tc.TcClassCourseId, (q, tc) => new { q, tc.TcUsersId })
                .Where(result => result.TcUsersId == teacherId)
                .AsNoTracking()
                .ToListAsync();

            if (!quizzes.Any())
                return NotFound("Không tìm thấy bài kiểm tra nào cho lớp học này.");

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                // Lặp qua mỗi bài kiểm tra và tạo một sheet mới cho từng bài
                foreach (var quiz in quizzes)
                {
                    // Thêm worksheet cho bài kiểm tra
                    int sheetIndex = 1;
                    string sheetName = quiz.q.QuizTitle; // Dùng tên bài kiểm tra làm tên sheet

                    // Kiểm tra nếu worksheet với tên đó đã tồn tại
                    while (package.Workbook.Worksheets.Any(ws => ws.Name == sheetName))
                    {
                        sheetName = $"{quiz.q.QuizTitle}_{sheetIndex}"; // Thêm chỉ số vào tên
                        sheetIndex++;
                    }

                    var worksheet = package.Workbook.Worksheets.Add(sheetName);

                    worksheet.Cells["A1"].Value = "Tên bài kiểm tra";
                    worksheet.Cells["B1"].Value = quiz.q.QuizTitle;
                    worksheet.Cells["A2"].Value = "Thời gian bắt đầu";
                    worksheet.Cells["B2"].Value = quiz.q.QuizStartAt.ToString("dd/MM/yyyy HH:mm:ss");
                    worksheet.Cells["A3"].Value = "Thời gian kết thúc";
                    worksheet.Cells["B3"].Value = quiz.q.QuizEndAt.ToString("dd/MM/yyyy HH:mm:ss");
                    worksheet.Cells["A4"].Value = "Mô tả";
                    worksheet.Cells["B4"].Value = quiz.q.QuizDescription;
                    worksheet.Cells["A5"].Value = "Trạng thái";
                    worksheet.Cells["B5"].Value = quiz.q.QuizStatus ? "Hiện" : "Ẩn";

                    // Lấy các câu hỏi của bài kiểm tra này
                    var questions = _context.QuizQuestions
                        .Where(q => q.QqQuizId == quiz.q.QuizId)
                        .AsNoTracking()
                        .ToList();

                    // Định dạng tiêu đề bảng câu hỏi
                    worksheet.Cells["A6"].Value = "Câu hỏi";
                    worksheet.Cells["B6"].Value = "Câu trả lời 1";
                    worksheet.Cells["C6"].Value = "Câu trả lời 2";
                    worksheet.Cells["D6"].Value = "Câu trả lời 3";
                    worksheet.Cells["E6"].Value = "Câu trả lời 4";
                    worksheet.Cells["F6"].Value = "Đáp án";
                    worksheet.Cells["G6"].Value = "Mô tả";

                    // Định dạng kiểu chữ cho tiêu đề bảng
                    using (var range = worksheet.Cells["A6:G6"])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
                    }

                    // Đổ dữ liệu câu hỏi vào bảng
                    int row = 7;
                    foreach (var question in questions)
                    {
                        worksheet.Cells[row, 1].Value = question.QqQuestion;
                        worksheet.Cells[row, 2].Value = question.QqOption1;
                        worksheet.Cells[row, 3].Value = question.QqOption2;
                        worksheet.Cells[row, 4].Value = question.QqOption3;
                        worksheet.Cells[row, 5].Value = question.QqOption4;
                        worksheet.Cells[row, 6].Value = question.QqCorrect;
                        worksheet.Cells[row, 7].Value = question.QqDescription;
                        row++;
                    }

                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                }

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                string excelName = $"Quizzes_{existingTeacher.Replace(" ", "_")}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
            }
        }

        // Xuất tất cả bài kiểm tra và câu hỏi ra file excel
        [HttpGet("export-quiz-all")]
        public async Task<IActionResult> ExportQuizAll()
        {
            var quizzes = await _context.Quizzes
                .AsNoTracking()
                .ToListAsync();

            if (!quizzes.Any())
                return NotFound("Không tìm thấy bài kiểm tra nào cho lớp học này.");

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                // Lặp qua mỗi bài kiểm tra và tạo một sheet mới cho từng bài
                foreach (var quiz in quizzes)
                {
                    // Thêm worksheet cho bài kiểm tra
                    int sheetIndex = 1;
                    string sheetName = quiz.QuizTitle; // Dùng tên bài kiểm tra làm tên sheet

                    // Kiểm tra nếu worksheet với tên đó đã tồn tại
                    while (package.Workbook.Worksheets.Any(ws => ws.Name == sheetName))
                    {
                        sheetName = $"{quiz.QuizTitle}_{sheetIndex}"; // Thêm chỉ số vào tên
                        sheetIndex++;
                    }

                    var worksheet = package.Workbook.Worksheets.Add(sheetName);

                    worksheet.Cells["A1"].Value = "Tên bài kiểm tra";
                    worksheet.Cells["B1"].Value = quiz.QuizTitle;
                    worksheet.Cells["A2"].Value = "Thời gian bắt đầu";
                    worksheet.Cells["B2"].Value = quiz.QuizStartAt.ToString("dd/MM/yyyy HH:mm:ss");
                    worksheet.Cells["A3"].Value = "Thời gian kết thúc";
                    worksheet.Cells["B3"].Value = quiz.QuizEndAt.ToString("dd/MM/yyyy HH:mm:ss");
                    worksheet.Cells["A4"].Value = "Mô tả";
                    worksheet.Cells["B4"].Value = quiz.QuizDescription;
                    worksheet.Cells["A5"].Value = "Trạng thái";
                    worksheet.Cells["B5"].Value = quiz.QuizStatus ? "Hiện" : "Ẩn";

                    // Lấy các câu hỏi của bài kiểm tra này
                    var questions = _context.QuizQuestions
                        .Where(q => q.QqQuizId == quiz.QuizId)
                        .AsNoTracking()
                        .ToList();

                    // Định dạng tiêu đề bảng câu hỏi
                    worksheet.Cells["A6"].Value = "Câu hỏi";
                    worksheet.Cells["B6"].Value = "Câu trả lời 1";
                    worksheet.Cells["C6"].Value = "Câu trả lời 2";
                    worksheet.Cells["D6"].Value = "Câu trả lời 3";
                    worksheet.Cells["E6"].Value = "Câu trả lời 4";
                    worksheet.Cells["F6"].Value = "Đáp án";
                    worksheet.Cells["G6"].Value = "Mô tả";

                    // Định dạng kiểu chữ cho tiêu đề bảng
                    using (var range = worksheet.Cells["A6:G6"])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
                    }

                    // Đổ dữ liệu câu hỏi vào bảng
                    int row = 7;
                    foreach (var question in questions)
                    {
                        worksheet.Cells[row, 1].Value = question.QqQuestion;
                        worksheet.Cells[row, 2].Value = question.QqOption1;
                        worksheet.Cells[row, 3].Value = question.QqOption2;
                        worksheet.Cells[row, 4].Value = question.QqOption3;
                        worksheet.Cells[row, 5].Value = question.QqOption4;
                        worksheet.Cells[row, 6].Value = question.QqCorrect;
                        worksheet.Cells[row, 7].Value = question.QqDescription;
                        row++;
                    }

                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                }

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                string excelName = $"Quizzes_All.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
            }
        }

        // Thống kê kết quả Quiz theo học sinh và lớp học phần
        [HttpGet("quiz-results-by-classcourse/{classCourseId}")]
        public async Task<IActionResult> QuizResultsByClassCourse(int classCourseId)
        {
            // Lấy thông tin lớp học và các bài kiểm tra
            var quizInfos = await (from q in _context.Quizzes
                                   join cc in _context.ClassCourses on q.QuizClassCourseId equals cc.CcId
                                   join c in _context.Classes on cc.ClassId equals c.ClassId
                                   join co in _context.Courses on cc.CourseId equals co.CourseId
                                   where cc.CcId == classCourseId
                                   select new
                                   {
                                       classCourseId = cc.CcId,
                                       classTitle = c.ClassTitle,
                                       courseTitle = co.CourseTitle,
                                       quizTitle = q.QuizTitle,
                                       quizId = q.QuizId,
                                       quizStartAt = q.QuizStartAt,
                                       quizEndAt = q.QuizEndAt
                                   })
                             .ToListAsync();

            if (quizInfos == null || quizInfos.Count == 0)
            {
                return NotFound("Không có bài kiểm tra nào cho lớp học phần này.");
            }

            // Lấy thống kê kết quả của học sinh cho mỗi bài kiểm tra
            var students = await (from qr in _context.QuizResults
                                  join u in _context.Users on qr.QrStudentId equals u.UsersId
                                  join s in _context.Students on u.UsersId equals s.StudentId
                                  join q in _context.Quizzes on qr.QrQuizId equals q.QuizId
                                  where q.QuizClassCourseId == classCourseId
                                  group qr by new { u.UsersId, u.UsersName, u.UsersEmail, s.StudentCode, q.QuizId } into g
                                  orderby g.Max(qr => qr.QrDate) descending
                                  select new
                                  {
                                      quizId = g.Key.QuizId,
                                      studentId = g.Key.UsersId,
                                      studentName = g.Key.UsersName,
                                      studentEmail = g.Key.UsersEmail,
                                      studentCode = g.Key.StudentCode,
                                      totalQuestions = g.Sum(qr => qr.QrTotalQuestion),
                                      totalCorrectAnswers = g.Sum(qr => qr.QrAnswer),
                                      completionTime = g.Max(qr => qr.QrDate),
                                      averageScore = g.Average(qr => qr.QrTotalQuestion > 0
                                            ? Math.Round((qr.QrAnswer * 10.0 / qr.QrTotalQuestion), 2)
                                            : 0)
                                  })
                             .ToListAsync();

            if (students == null || students.Count == 0)
            {
                return NotFound("Chưa có kết quả bài kiểm tra nào cho lớp học phần này.");
            }

            // Lắp kết quả các bài kiểm tra và học sinh vào nhau theo quizId
            var result = quizInfos.Select(quiz => new
            {
                classes = new
                {
                    classCourseId = quiz.classCourseId,
                    classTitle = quiz.classTitle,
                    courseTitle = quiz.courseTitle,
                    quizTitle = quiz.quizTitle,
                    quizStartAt = quiz.quizStartAt,
                    quizEndAt = quiz.quizEndAt
                },
                students = students.Where(s => s.quizId == quiz.quizId).ToList()
            }).ToList();

            return Ok(result);
        }

        // Thống kê kết quả Quiz theo bài kiểm tra quiz
        [HttpGet("quiz-results-by-quiz/{quizId}")]
        public async Task<IActionResult> QuizResultsByQuiz(int quizId)
        {
            // Lấy thông tin bài kiểm tra và lớp học
            var quizInfo = await (from q in _context.Quizzes
                                  join cc in _context.ClassCourses on q.QuizClassCourseId equals cc.CcId
                                  join c in _context.Classes on cc.ClassId equals c.ClassId
                                  join co in _context.Courses on cc.CourseId equals co.CourseId
                                  where q.QuizId == quizId
                                  select new
                                  {
                                      classCourseId = cc.CcId,
                                      classTitle = c.ClassTitle,
                                      courseTitle = co.CourseTitle,
                                      quizTitle = q.QuizTitle,
                                      quizId = q.QuizId,
                                      quizStartAt = q.QuizStartAt,
                                      quizEndAt = q.QuizEndAt
                                  })
                                 .FirstOrDefaultAsync();

            if (quizInfo == null)
            {
                return NotFound("Quiz not found.");
            }

            // Lấy thống kê kết quả của học sinh cho bài kiểm tra
            var students = await (from qr in _context.QuizResults
                                  join u in _context.Users on qr.QrStudentId equals u.UsersId
                                  join s in _context.Students on u.UsersId equals s.StudentId
                                  where qr.QrQuizId == quizId
                                  group qr by new { u.UsersId, u.UsersName, u.UsersEmail, s.StudentCode } into g
                                  orderby g.Max(qr => qr.QrDate) descending
                                  select new
                                  {
                                      studentId = g.Key.UsersId,
                                      studentName = g.Key.UsersName,
                                      studentEmail = g.Key.UsersEmail,
                                      studentCode = g.Key.StudentCode,
                                      totalQuestions = g.Sum(qr => qr.QrTotalQuestion), // Tổng số câu hỏi của tất cả kết quả bài kiểm tra
                                      totalCorrectAnswers = g.Sum(qr => qr.QrAnswer), // Tổng số câu trả lời đúng của học sinh'
                                      completionTime = g.Max(qr => qr.QrDate),
                                      averageScore = g.Average(qr => qr.QrTotalQuestion > 0
                                            ? Math.Round((qr.QrAnswer * 10.0 / qr.QrTotalQuestion), 2)
                                            : 0) // Tính điểm trung bình
                                  })
                     .ToListAsync();


            // Trả về kết quả
            var result = new
            {
                classes = new
                {
                    classCourseId = quizInfo.classCourseId,
                    classTitle = quizInfo.classTitle,
                    courseTitle = quizInfo.courseTitle,
                    quizTitle = quizInfo.quizTitle,
                    quizStartAt = quizInfo.quizStartAt,
                    quizEndAt = quizInfo.quizEndAt
                },
                students
            };

            return Ok(result);
        }

        // Xuất excel thống kê kết quả Quiz theo bài kiểm tra quiz
        [HttpGet("export-excel/quiz-results-by-quiz/{quizId}")]
        public async Task<IActionResult> ExportQuizResultsByQuiz(int quizId)
        {
            var quizResultResponse = await QuizResultsByQuiz(quizId);
            if (quizResultResponse is NotFoundObjectResult)
            {
                return NotFound("Quiz not found.");
            }

            var result = (quizResultResponse as OkObjectResult).Value as dynamic;
            var quizInfo = result.classes;
            var students = result.students;

            // Xuất ra Excel
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add($"{quizInfo.quizTitle}");

                worksheet.Cells["A1"].Value = "Mã lớp học phần";
                worksheet.Cells["A2"].Value = "Tên lớp";
                worksheet.Cells["A3"].Value = "Tên học phần";
                worksheet.Cells["A4"].Value = "Tên bài kiểm tra";
                worksheet.Cells["A5"].Value = "Thời gian bắt đầu";
                worksheet.Cells["A6"].Value = "Thời gian kết thúc";

                worksheet.Cells["B1"].Value = quizInfo.classCourseId;
                worksheet.Cells["B2"].Value = quizInfo.classTitle;
                worksheet.Cells["B3"].Value = quizInfo.courseTitle;
                worksheet.Cells["B4"].Value = quizInfo.quizTitle;
                worksheet.Cells["B5"].Value = quizInfo.quizStartAt.ToString("dd/MM/yyyy HH:mm:ss");
                worksheet.Cells["B6"].Value = quizInfo.quizEndAt.ToString("dd/MM/yyyy HH:mm:ss");

                worksheet.Cells["A7"].Value = "Tên sinh viên";
                worksheet.Cells["B7"].Value = "Email sinh viên";
                worksheet.Cells["C7"].Value = "Mã số sinh viên";
                worksheet.Cells["D7"].Value = "Tổng số câu hỏi";
                worksheet.Cells["E7"].Value = "Số câu trả lời đúng";
                worksheet.Cells["F7"].Value = "Thời gian nộp bài";
                worksheet.Cells["G7"].Value = "Điểm";

                using (var range = worksheet.Cells["A7:G7"])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
                }

                int row = 8;
                foreach (var student in students)
                {
                    worksheet.Cells[row, 1].Value = student.studentName;
                    worksheet.Cells[row, 2].Value = student.studentEmail;
                    worksheet.Cells[row, 3].Value = student.studentCode;
                    worksheet.Cells[row, 4].Value = student.totalQuestions;
                    worksheet.Cells[row, 5].Value = student.totalCorrectAnswers;
                    worksheet.Cells[row, 6].Value = student.completionTime.ToString("dd/MM/yyyy HH:mm:ss");
                    worksheet.Cells[row, 7].Value = student.averageScore;
                    row++;
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "QuizResults.xlsx");
            }
        }

        // Xuất excel thống kê kết quả Quiz theo học sinh và lớp học phần
        [HttpGet("export-excel/quiz-results-by-classcourse/{classCourseId}")]
        public async Task<IActionResult> ExportQuizResultsByClassCourse(int classCourseId)
        {
            var quizResults = await QuizResultsByClassCourse(classCourseId);

            if (quizResults is NotFoundObjectResult)
            {
                return NotFound("No data found for this class course.");
            }

            var result = (quizResults as OkObjectResult).Value as dynamic;

            // Tạo file Excel
            using (var package = new ExcelPackage())
            {
                // Duyệt qua các bài kiểm tra trong dữ liệu
                foreach (var quizInfo in result)
                {
                    var students = quizInfo.students;

                    // Thêm worksheet cho bài kiểm tra
                    int sheetIndex = 1;
                    string sheetName = quizInfo.classes.quizTitle; // Dùng tên bài kiểm tra làm tên sheet

                    // Kiểm tra nếu worksheet với tên đó đã tồn tại
                    while (package.Workbook.Worksheets.Any(ws => ws.Name == sheetName))
                    {
                        sheetName = $"{quizInfo.classes.quizTitle}_{sheetIndex}"; // Thêm chỉ số vào tên
                        sheetIndex++;
                    }

                    // Tạo một sheet mới cho mỗi bài kiểm tra
                    var worksheet = package.Workbook.Worksheets.Add(sheetName);

                    worksheet.Cells["A1"].Value = "Mã lớp học phần";
                    worksheet.Cells["B1"].Value = quizInfo.classes.classCourseId;
                    worksheet.Cells["A2"].Value = "Tên lớp";
                    worksheet.Cells["B2"].Value = quizInfo.classes.classTitle;
                    worksheet.Cells["A3"].Value = "Tên học phần";
                    worksheet.Cells["B3"].Value = quizInfo.classes.courseTitle;
                    worksheet.Cells["A4"].Value = "Tên bài kiểm tra";
                    worksheet.Cells["B4"].Value = quizInfo.classes.quizTitle;
                    worksheet.Cells["A5"].Value = "Thời gian bắt đầu";
                    worksheet.Cells["B5"].Value = quizInfo.classes.quizStartAt.ToString("dd/MM/yyyy HH:mm:ss");
                    worksheet.Cells["A6"].Value = "Thời gian kết thúc";
                    worksheet.Cells["B6"].Value = quizInfo.classes.quizEndAt.ToString("dd/MM/yyyy HH:mm:ss");

                    worksheet.Cells["A7"].Value = "Tên sinh viên";
                    worksheet.Cells["B7"].Value = "Email sinh viên";
                    worksheet.Cells["C7"].Value = "Mã số sinh viên";
                    worksheet.Cells["D7"].Value = "Tổng số câu hỏi";
                    worksheet.Cells["E7"].Value = "Số câu trả lời đúng";
                    worksheet.Cells["F7"].Value = "Thời gian nộp bài";
                    worksheet.Cells["G7"].Value = "Điểm";

                    using (var range = worksheet.Cells["A7:G7"])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
                    }

                    var row = 8;
                    foreach (var student in students)
                    {
                        worksheet.Cells[row, 1].Value = student.studentName;
                        worksheet.Cells[row, 2].Value = student.studentEmail;
                        worksheet.Cells[row, 3].Value = student.studentCode;
                        worksheet.Cells[row, 4].Value = student.totalQuestions;
                        worksheet.Cells[row, 5].Value = student.totalCorrectAnswers;
                        worksheet.Cells[row, 6].Value = student.completionTime.ToString("dd/MM/yyyy HH:mm:ss");
                        worksheet.Cells[row, 7].Value = student.averageScore;
                        row++;
                    }
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                }

                var fileName = "QuizResultsByClassCourse.xlsx";
                var fileBytes = package.GetAsByteArray();
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

    }
}
