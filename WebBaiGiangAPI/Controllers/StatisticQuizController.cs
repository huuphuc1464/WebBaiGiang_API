using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml;
using WebBaiGiangAPI.Data;
using WebBaiGiangAPI.Models;
using System.Drawing;
using OfficeOpenXml.Style;

namespace WebBaiGiangAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatisticQuizController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StatisticQuizController(AppDbContext context)
        {
            _context = context;
        }

        // Tổng số bài Quiz và số lượng câu hỏi trong hệ thống và chi tiết theo giáo viên.
        [HttpGet("total-quiz-question")]
        public async Task<IActionResult> GetTotalQuizQuestion()
        {
            var totalQuiz = await _context.Quizzes.CountAsync();
            var totalQuestions = await _context.QuizQuestions.CountAsync();

            var quizByTeacher = await (from q in _context.Quizzes
                                       join cc in _context.ClassCourses on q.QuizClassCourseId equals cc.CcId
                                       join tc in _context.TeacherClasses on cc.CcId equals tc.TcClassCourseId
                                       join u in _context.Users on tc.TcUsersId equals u.UsersId
                                       select new
                                       {
                                           q.QuizId,
                                           u.UsersId,
                                           u.UsersName
                                       })
                                      .ToListAsync();

            var grouped = quizByTeacher
                .GroupBy(x => new { x.UsersId, x.UsersName })
                .Select(g => new
                {
                    TeacherId = g.Key.UsersId,
                    TeacherName = g.Key.UsersName,
                    QuizCount = g.Count(),
                    QuestionCount = _context.QuizQuestions.Count(q => g.Select(quiz => quiz.QuizId).Contains(q.QqQuizId))
                })
                .ToList();

            return Ok(new
            {
                TotalQuiz = totalQuiz,
                TotalQuestions = totalQuestions,
                QuizByTeacher = grouped
            });
        }

        // Tổng số bài làm theo học sinh, lớp học phần, theo bài quiz, theo giáo viên.
        [HttpGet("quiz-submissions")]
        public async Task<IActionResult> GetQuizSubmissionStatistics()
        {
            var totalSubmissions = await _context.QuizResults.CountAsync();

            // Số bài làm theo học sinh
            var submissionsByStudent = await _context.QuizResults
                .GroupBy(qr => qr.QrStudentId)
                .Select(g => new
                {
                    StudentId = g.Key,
                    StudentName = _context.Users.FirstOrDefault(u => u.UsersId == g.Key).UsersName,
                    StudentCode = _context.Students.FirstOrDefault(u => u.StudentId == g.Key).StudentCode,
                    SubmissionCount = g.Count()
                })
                .ToListAsync();

            // Số bài làm theo lớp học phần
            var submissionsByClass = await (from qr in _context.QuizResults
                                            join q in _context.Quizzes on qr.QrQuizId equals q.QuizId
                                            join cc in _context.ClassCourses on q.QuizClassCourseId equals cc.CcId
                                            join c in _context.Classes on cc.ClassId equals c.ClassId
                                            group qr by new { cc.CcId, cc.CcDescription } into g
                                            select new
                                            {
                                                g.Key.CcId,
                                                g.Key.CcDescription,
                                                SubmissionCount = g.Count()
                                            }).ToListAsync();

            // Số bài làm theo bài quiz
            var submissionsByQuiz = await _context.QuizResults
                .GroupBy(qr => qr.QrQuizId)
                .Select(g => new
                {
                    QuizId = g.Key,
                    QuizName = _context.Quizzes.FirstOrDefault(q => q.QuizId == g.Key).QuizTitle,
                    SubmissionCount = g.Count()
                })
                .ToListAsync();

            // Số bài làm theo giáo viên
            var submissionsByTeacher = await (from qr in _context.QuizResults
                                              join q in _context.Quizzes on qr.QrQuizId equals q.QuizId
                                              join cc in _context.ClassCourses on q.QuizClassCourseId equals cc.CcId
                                              join tc in _context.TeacherClasses on cc.CcId equals tc.TcClassCourseId
                                              join u in _context.Users on tc.TcUsersId equals u.UsersId
                                              group qr by new { u.UsersId, u.UsersName } into g
                                              select new
                                              {
                                                  TeacherId = g.Key.UsersId,
                                                  TeacherName = g.Key.UsersName,
                                                  SubmissionCount = g.Count()
                                              }).ToListAsync();

            return Ok(new
            {
                TotalSubmissions = totalSubmissions,
                SubmissionsByStudent = submissionsByStudent,
                SubmissionsByClassCourse = submissionsByClass,
                SubmissionsByQuiz = submissionsByQuiz,
                SubmissionsByTeacher = submissionsByTeacher
            });
        }

        // Tỷ lệ hoàn thành Quiz của học sinh theo bài Quiz.
        [HttpGet("completion-rate/{quizId}")]
        public async Task<IActionResult> GetCompletionRate(int quizId)
        {
            if (!_context.Quizzes.Any(q => q.QuizId == quizId))
            {
                return NotFound("Quiz not found.");
            }
            var totalStudents = _context.Quizzes
                .Join(_context.ClassCourses, q => q.QuizClassCourseId, cc => cc.CcId, (q, cc) => new { q, cc })
                .Join(_context.StudentClasses, cc => cc.cc.ClassId, sc => sc.ScClassId, (cc, sc) => new { cc, sc })
                .Where(x => x.cc.q.QuizId == quizId)
                .Count();

            var totalSubmissions = await _context.QuizResults.Where(qr => qr.QrQuizId == quizId).CountAsync();
            var completionRate = (double)totalSubmissions / totalStudents * 100;
            return Ok(new
            {
                QuizName = _context.Quizzes.FirstOrDefault(q => q.QuizId == quizId)?.QuizTitle,
                TotalStudents = totalStudents,
                TotalSubmissions = totalSubmissions,
                CompletionRate = completionRate
            });
        }

        // Phát hiện xu hướng điểm số tăng/giảm theo thời gian: Xác định xu hướng học tập của học sinh trong lớp học phần qua nhiều bài Quiz.
        [HttpGet("score-trend/{classCourseId}")]
        public async Task<IActionResult> GetScoreTrend(int classCourseId)
        {
            // Lấy danh sách bài kiểm tra trong lớp học phần
            var quizzes = await _context.Quizzes
                .Where(q => q.QuizClassCourseId == classCourseId)
                .Select(q => new { q.QuizId, q.QuizTitle, q.QuizCreateAt })
                .ToListAsync();

            // Lấy danh sách sinh viên trong lớp học phần
            var students = await (from sc in _context.StudentClasses
                                  join cc in _context.ClassCourses on sc.ScClassId equals cc.ClassId
                                  join u in _context.Users on sc.ScStudentId equals u.UsersId
                                  join s in _context.Students on sc.ScStudentId equals s.StudentId
                                  where cc.CcId == classCourseId
                                  select new { sc.ScStudentId, u.UsersName, u.UsersEmail, s.StudentCode })
                                 .Distinct()
                                 .ToListAsync();

            // Lấy kết quả làm bài
            var quizResults = await _context.QuizResults
                .Where(qr => quizzes.Select(q => q.QuizId).Contains(qr.QrQuizId))
                .ToListAsync();

            // Kết hợp: mỗi sinh viên + mỗi quiz
            var scoreTrend = students.Select(s => new
            {
                StudentId = s.ScStudentId,
                StudentName = s.UsersName,
                StudentEmail = s.UsersEmail,
                StudentCode = s.StudentCode,
                ScoreTrend = quizzes.OrderBy(q => q.QuizCreateAt)
                    .Select(q =>
                    {
                        var result = quizResults.FirstOrDefault(r =>
                            r.QrQuizId == q.QuizId && r.QrStudentId == s.ScStudentId);

                        double? score = result != null && result.QrTotalQuestion > 0
                            ? Math.Round((double)result.QrAnswer * 10 / result.QrTotalQuestion, 2)
                            : (double?)0;

                        return new
                        {
                            q.QuizTitle,
                            q.QuizCreateAt,
                            Score = score
                        };
                    }).ToList()
            }).ToList();

            return Ok(scoreTrend);
        }

        [HttpGet("export-score-trend/{classCourseId}")]
        public async Task<IActionResult> ExportScoreTrend(int classCourseId)
        {
            var result = await GetScoreTrend(classCourseId);

            // Lấy ObjectResult và ép kiểu lại
            if (result is OkObjectResult okResult && okResult.Value is IEnumerable<dynamic> scoreTrendData)
            {
                if (!scoreTrendData.Any())
                    return NotFound("Không có dữ liệu để xuất.");

                using var package = new ExcelPackage();
                var ws = package.Workbook.Worksheets.Add("Score Trend");

                ws.Cells[1, 1].Value = "Student Name";
                ws.Cells[1, 2].Value = "Student Code";

                var first = scoreTrendData.First();
                var quizTitles = ((IEnumerable<dynamic>)first.ScoreTrend)
                    .Select((x, i) => new { Title = x.QuizTitle, Index = i }).ToList();

                for (int i = 0; i < quizTitles.Count; i++)
                {
                    ws.Cells[1, i + 3].Value = quizTitles[i].Title;
                }

                int row = 2;
                foreach (dynamic student in scoreTrendData)
                {
                    ws.Cells[row, 1].Value = student.StudentName;
                    ws.Cells[row, 2].Value = student.StudentCode;

                    var trends = (IEnumerable<dynamic>)student.ScoreTrend;
                    int col = 3;
                    foreach (var q in trends)
                    {
                        ws.Cells[row, col++].Value = q.Score ?? 0;
                    }

                    row++;
                }

                var chart = ws.Drawings.AddChart("ScoreTrendChart", eChartType.Line) as ExcelLineChart;
                chart.Title.Text = "Score Trend";
                chart.SetPosition(scoreTrendData.Count() + 3, 0, 0, 0);
                chart.SetSize(1000, 400);
                chart.Legend.Position = eLegendPosition.Bottom;

                row = 2;
                foreach (dynamic student in scoreTrendData)
                {
                    var scoreRange = ws.Cells[row, 3, row, 2 + quizTitles.Count];
                    var titleRange = ws.Cells[1, 3, 1, 2 + quizTitles.Count];
                    chart.Series.Add(scoreRange, titleRange).Header = student.StudentName;
                    row++;
                }

                chart.YAxis.Border.Width = 0.25;
                chart.YAxis.Border.Fill.Color = Color.Transparent;
                chart.YAxis.MajorGridlines.Fill.Color = Color.LightGray;
                chart.YAxis.MajorGridlines.Width = 0;
                chart.XAxis.Border.Width = 0.25;
                chart.XAxis.Border.Fill.Color = Color.LightGray;
                chart.XAxis.MajorGridlines.Width = 0;

                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                var fileName = $"ScoreTrend_{classCourseId}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }

            return BadRequest("Không thể xử lý dữ liệu.");
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

        // Danh sách học sinh không hoàn thành bài kiểm tra:
        [HttpGet("questions/most-incorrect/{quizId}")]
        public async Task<IActionResult> GetMostIncorrectQuestions(int quizId)
        {
            var result = await _context.QuizResultDetails
                .Include(qrd => qrd.QuizQuestion)
                .GroupBy(qrd => new {
                    qrd.QrdQuestionId,
                    qrd.QuizQuestion.QqQuestion,
                    qrd.QuizQuestion.QqCorrect,
                    qrd.QuizQuestion.QqQuizId
                })
                .Select(group => new {
                    QuizId = group.Key.QqQuizId,
                    QuestionId = group.Key.QrdQuestionId,
                    QuestionText = group.Key.QqQuestion,
                    TotalAnswers = group.Count(),
                    WrongAnswers = group.Count(x => x.QrdStudentAnswer != group.Key.QqCorrect),
                    WrongRate = (double)group.Count(x => x.QrdStudentAnswer != group.Key.QqCorrect) / group.Count() * 100
                })
                .Where(x => x.TotalAnswers > 0 && x.QuizId == quizId && x.WrongRate > 0)
                .OrderByDescending(x => x.WrongRate)
                .Take(10)
                .ToListAsync();

            return Ok(result);
        }

        // Danh sách câu hỏi có tỷ lệ trả lời sai cao nhất
        [HttpGet("students/not-submitted/{quizId}")]
        public async Task<IActionResult> GetStudentsNotSubmitted(int quizId)
        {
            var allStudents = (from q in _context.Quizzes
                             join cc in _context.ClassCourses on q.QuizClassCourseId equals cc.CcId
                             join sc in _context.StudentClasses on cc.ClassId equals sc.ScClassId
                             join s in _context.Students on sc.ScStudentId equals s.StudentId
                             where q.QuizId == quizId
                             select new
                             {
                                 s.StudentId,
                                 s.StudentCode,
                                 FullName = s.Users.UsersName
                             }).ToList();

            var submittedStudentIds = await _context.QuizResults
                .Where(qr => qr.QrQuizId == quizId)
                .Select(qr => qr.QrStudentId)
                .Distinct()
                .ToListAsync();

            var notSubmitted = allStudents
                .Where(s => !submittedStudentIds.Contains(s.StudentId))
                .Select(s => new {
                    s.StudentId,
                    s.StudentCode,
                    FullName = s.FullName
                })
                .ToList();

            return Ok(notSubmitted);
        }

    }
}
