using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.DependencyResolver;
using OfficeOpenXml;
using OfficeOpenXml.Drawing;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;
using WebBaiGiangAPI.Data;
using WebBaiGiangAPI.Models;

namespace WebBaiGiangAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuizResultsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;

        public QuizResultsController(AppDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // Lưu kết quả bài làm
        [HttpPost("submit/{quizId}/{studentId}")]
        public async Task<IActionResult> SubmitQuiz(int quizId, int studentId, [FromBody] List<QuizResultDetail> result)
        {
            if (result == null || result == null || result.Count == 0)
            {
                return BadRequest(new { message = "Không có kết quả bài làm." });
            }
            // Kiểm tra xem tất cả các câu hỏi trong kết quả bài làm có tồn tại trong quiz không
            var quiz = await _context.Quizzes.FindAsync(quizId);
            if (quiz == null)
            {
                return NotFound(new { message = "Quiz không tồn tại." });
            }
            if (quiz.QuizStartAt > DateTime.Now || quiz.QuizEndAt < DateTime.Now)
            {
                return BadRequest(new { message = "Thời gian làm bài đã hết hoặc chưa bắt đầu." });
            }
            var student = from s in _context.Students
                          join u in _context.Users on s.StudentId equals u.UsersId
                          join sc in _context.StudentClasses on s.StudentId equals sc.ScStudentId
                          join cc in _context.ClassCourses on sc.ScClassId equals cc.ClassId
                          join q in _context.Quizzes on cc.CcId equals q.QuizClassCourseId
                          where u.UsersId == studentId && u.UsersRoleId == 3 && q.QuizId == quizId && sc.ScStatus == 1
                          select new
                          {
                              s.StudentId,
                              s.StudentCode,
                              u.UsersId,
                              u.UsersName,
                              u.UsersEmail
                          };
            if (!student.Any())
            {
                return NotFound(new { message = "Sinh viên không tồn tại hoặc không thuộc lớp học phần của bài kiểm tra quiz này." });
            }
            // Kiểm tra xem sinh viên đã làm bài quiz này chưa
            var existingResult = await _context.QuizResults
                .FirstOrDefaultAsync(qr => qr.QrQuizId == quizId && qr.QrStudentId == studentId);
            if (existingResult != null)
            {
                return BadRequest(new { message = "Sinh viên đã làm bài quiz này." });
            }

            // Kiểm tra xem tất cả các câu hỏi trong kết quả bài làm có tồn tại trong quiz không
            var questionIds = result.Select(r => r.QrdQuestionId).ToList();
            var quizQuestions = await _context.QuizQuestions
                .Where(q => q.QqQuizId == quizId && questionIds.Contains(q.QqId))
                .ToListAsync();
            if (quizQuestions.Count != questionIds.Count)
            {
                return BadRequest(new { message = "Một hoặc nhiều câu hỏi không tồn tại trong quiz." });
            }

            // Kiểm tra xem sinh viên có làm hết tất cả các câu hỏi không
            var countQuestion = await _context.QuizQuestions
                .Where(q => q.QqQuizId == quizId)
                .CountAsync();
            if (result.Count != countQuestion)
            {
                return BadRequest(new { message = "Sinh viên chưa làm hết tất cả các câu hỏi." });
            }

            // Kiểm tra xem tất cả các câu trả lời của sinh viên có hợp lệ không
            foreach (var detail in result)
            {
                var question = quizQuestions.FirstOrDefault(q => q.QqId == detail.QrdQuestionId);
                if (question == null)
                {
                    return BadRequest(new { message = $"Câu hỏi với ID {detail.QrdQuestionId} không tồn tại trong quiz." });
                }
                // Kiểm tra xem câu trả lời của sinh viên có hợp lệ không
                if (string.IsNullOrWhiteSpace(detail.QrdStudentAnswer))
                {
                    return BadRequest(new { message = $"Câu trả lời cho câu hỏi với ID {detail.QrdQuestionId} không hợp lệ." });
                }
            }

            int correct = 0;
            int total = countQuestion;

            // Kiểm tra câu trả lời của sinh viên với câu trả lời đúng
            foreach (var detail in result)
            {
                var question = await _context.QuizQuestions.FindAsync(detail.QrdQuestionId);
                if (question != null)
                {
                    bool isCorrect = question.QqCorrect.Trim().Equals(detail.QrdStudentAnswer.Trim(), StringComparison.OrdinalIgnoreCase);
                    detail.QrdIsCorrect = isCorrect;
                    correct += isCorrect ? 1 : 0;
                }
            }

            var quizResult = new QuizResult
            {
                QrQuizId = quizId,
                QrStudentId = studentId,
                QrTotalQuestion = total,
                QrAnswer = correct,
                QrDate = DateTime.Now
            };

            _context.QuizResults.Add(quizResult);
            await _context.SaveChangesAsync();

            foreach (var detail in result)
            {
                detail.QrdResultId = quizResult.QrId;
                _context.QuizResultDetails.Add(detail);
            }

            await _context.SaveChangesAsync();

            var teacherInfo = from t in _context.Quizzes
                              join tc in _context.TeacherClasses on t.QuizClassCourseId equals tc.TcClassCourseId
                              join u in _context.Users on tc.TcUsersId equals u.UsersId
                              where t.QuizId == quizId
                              select new
                              {
                                  u.UsersName,
                                  u.UsersEmail
                              };

            var studentInfo = from s in _context.Students
                              join u in _context.Users on s.StudentId equals u.UsersId
                              where s.StudentId == studentId
                              select new
                              {
                                  u.UsersName,
                                  u.UsersEmail
                              };
            var className = from s in _context.Quizzes
                            join cc in _context.ClassCourses on s.QuizClassCourseId equals cc.CcId
                            join c in _context.Classes on cc.ClassId equals c.ClassId
                            where s.QuizId == quizId
                            select new
                            {
                                c.ClassTitle
                            };
            string subject = $"Sinh viên {studentInfo.FirstOrDefault()?.UsersName} đã nộp bài kiểm tra!";
            string body = $"<h3>Bài kiểm tra: {quiz.QuizTitle}</h3>"
                        + $"<p>Lớp: {className.FirstOrDefault().ClassTitle}</p>"
                        + $"<p>Sinh viên {studentInfo.FirstOrDefault()?.UsersName} đã hoàn thành và nộp bài kiểm tra.</p>"
                        + "<p>Vui lòng kiểm tra kết quả bài làm của sinh viên.</p>";
            await _emailService.SendEmail(teacherInfo.FirstOrDefault()?.UsersEmail, subject, body);

            await _emailService.SendEmail(
                 studentInfo.FirstOrDefault()?.UsersEmail,
                 "Cảm ơn bạn đã nộp bài kiểm tra",
                 $"Chúng tôi xin cảm ơn bạn, {studentInfo.FirstOrDefault()?.UsersName}, đã nộp bài kiểm tra: \"{quiz.QuizTitle}\" thành công. "
                 + "Chúng tôi sẽ sớm kiểm tra kết quả và thông báo cho bạn."
             );

            return Ok(new { message = "Đã lưu kết quả bài làm", correct, total });
        }

        // Lấy danh sách kết quả bài làm theo mã quiz
        [HttpGet("get-quiz-results-by-quizid/{quizId}")]
        public async Task<IActionResult> GetQuizResultsByQuizId(int quizId)
        {
            var quizResults = await _context.QuizResults
                .Include(qr => qr.Student)
                .Include(qr => qr.QuizResultDetails)
                .Where(qr => qr.QrQuizId == quizId)
                .OrderByDescending(qr => qr.QrDate)
                .ToListAsync();

            if (!quizResults.Any())
            {
                return NotFound(new { message = "Không có kết quả bài làm cho bài kiểm tra này." });
            }

            var result = quizResults.Select(qr => new
            {
                qr.QrId,
                qr.QrQuizId,
                qr.QrStudentId,
                qr.QrTotalQuestion,
                qr.QrAnswer,
                qr.QrDate,
                QuizResultDetails = qr.QuizResultDetails.Select(qrd => new
                {
                    qrd.QrdId,
                    qrd.QrdQuestionId,
                    qrd.QrdStudentAnswer,
                    qrd.QrdIsCorrect,
                    Question = qrd.QuizQuestion?.QqQuestion
                })
            }).ToList();

            return Ok(result);
        }

        // Xem thông tin chi tiết của một bài làm, bao gồm điểm số, số câu đúng/sai, thời gian làm bài.
        [HttpGet("get-quiz-result-detail/{resultId}")]
        public async Task<IActionResult> GetQuizResultDetail(int resultId)
        {
            var quizResult = await _context.QuizResults
                .Include(qr => qr.Student)
                .ThenInclude(u => u.Users)
                .Include(qr => qr.QuizResultDetails)
                .ThenInclude(qrd => qrd.QuizQuestion)
                .FirstOrDefaultAsync(qr => qr.QrId == resultId);
            if (quizResult == null)
            {
                return NotFound(new { message = "Không tìm thấy kết quả bài làm." });
            }
            var result = new
            {
                quizResult.QrId,
                quizResult.QrQuizId,
                quizResult.QrStudentId,
                quizResult.QrTotalQuestion,
                quizResult.QrAnswer,
                quizResult.QrDate,
                Core = Math.Round((quizResult.QrAnswer * 10.0 / quizResult.QrTotalQuestion), 2),
                StudentName = quizResult.Student.Users.UsersName,
                QuizResultDetails = quizResult.QuizResultDetails.Select(qrd => new
                {
                    qrd.QrdId,
                    qrd.QrdQuestionId,
                    qrd.QrdStudentAnswer,
                    qrd.QrdIsCorrect,
                    Question = qrd.QuizQuestion?.QqQuestion
                })
            };
            return Ok(result);
        }

        // Xóa kết quả bài làm
        [HttpDelete("delete-quiz-result/{resultId}")]
        public async Task<IActionResult> DeleteQuizResult(int resultId)
        {
            var quizResult = await _context.QuizResults
                .Include(qr => qr.QuizResultDetails)
                .FirstOrDefaultAsync(qr => qr.QrId == resultId);
            if (quizResult == null)
            {
                return NotFound(new { message = "Không tìm thấy kết quả bài làm." });
            }
            _context.QuizResultDetails.RemoveRange(quizResult.QuizResultDetails);
            _context.QuizResults.Remove(quizResult);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa kết quả bài làm." });
        }

        // Xuất báo cáo kết quả ra excel
        [HttpGet("export-quiz-result/{quizId}")]
        public async Task<IActionResult> ExportQuizResult(int quizId)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.ClassCourse)
                    .ThenInclude(cc => cc.Classes)
                .Include(q => q.ClassCourse)
                    .ThenInclude(cc => cc.Course)
                .Include(q => q.QuizQuestions)
                .FirstOrDefaultAsync(q => q.QuizId == quizId);

            if (quiz == null)
                return NotFound(new { message = "Không tìm thấy bài kiểm tra." });

            var quizResults = await _context.QuizResults
                .Include(qr => qr.Student).ThenInclude(s => s.Users)
                .Include(qr => qr.QuizResultDetails).ThenInclude(qrd => qrd.QuizQuestion)
                .Where(qr => qr.QrQuizId == quizId)
                .ToListAsync();

            if (!quizResults.Any())
                return NotFound(new { message = "Không có kết quả bài làm cho bài kiểm tra này." });

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var package = new ExcelPackage();

            // === Sheet 1: Tổng quan ===
            var overview = package.Workbook.Worksheets.Add("Tổng quan");
            overview.Cells["A1"].Value = "BÁO CÁO KẾT QUẢ BÀI KIỂM TRA";
            overview.Cells["A1"].Style.Font.Size = 18;
            overview.Cells["A1"].Style.Font.Bold = true;
            overview.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            overview.Cells["A1:D1"].Merge = true;

            overview.Cells["A3"].Value = "Tên bài kiểm tra:";
            overview.Cells["B3"].Value = quiz.QuizTitle;

            overview.Cells["A4"].Value = "Lớp học:";
            overview.Cells["B4"].Value = quiz.ClassCourse?.Classes?.ClassTitle ?? "(Không xác định)";

            overview.Cells["A5"].Value = "Khóa học:";
            overview.Cells["B5"].Value = quiz.ClassCourse?.Course?.CourseTitle ?? "(Không xác định)";

            overview.Cells["A6"].Value = "Thời gian tạo bài kiểm tra:";
            overview.Cells["B6"].Value = quiz.QuizCreateAt.ToString("dd/MM/yyyy HH:mm:ss");

            overview.Cells["A7"].Value = "Thời gian cập nhật cuối cùng:";
            overview.Cells["B7"].Value = quiz.QuizUpdateAt.ToString("dd/MM/yyyy HH:mm:ss");

            overview.Cells["A8"].Value = "Thời gian bắt đầu làm bài:";
            overview.Cells["B8"].Value = quiz.QuizStartAt.ToString("dd/MM/yyyy HH:mm:ss");

            overview.Cells["A9"].Value = "Thời gian kết thúc làm bài:";
            overview.Cells["B9"].Value = quiz.QuizEndAt.ToString("dd/MM/yyyy HH:mm:ss");

            overview.Cells["A10"].Value = "Số lượng câu hỏi của bài:";
            overview.Cells["B10"].Value = quiz.QuizQuestions.Count;

            overview.Cells["A11"].Value = "Số sinh viên đã nộp:";
            overview.Cells["B11"].Value = quizResults.Count;

            overview.Cells["A12"].Value = "Ngày xuất báo cáo:";
            overview.Cells["B12"].Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            overview.Cells["A3:A12"].Style.Font.Bold = true;
            overview.Cells[overview.Dimension.Address].AutoFitColumns();

            // === Sheet 2: Chi tiết kết quả ===
            var sheet = package.Workbook.Worksheets.Add("Chi tiết kết quả");

            string[] headers = { "STT", "Mã số sinh viên", "Tên sinh viên", "Email", "Thời gian nộp", "Tổng câu", "Số câu đúng", "Điểm", "Câu hỏi", "Đáp án", "Kết quả" };
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cells[1, i + 1].Value = headers[i];
                sheet.Cells[1, i + 1].Style.Font.Bold = true;
                sheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(Color.SteelBlue);
                sheet.Cells[1, i + 1].Style.Font.Color.SetColor(Color.White);
                sheet.Cells[1, i + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                sheet.Cells[1, i + 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                sheet.Cells[1, i + 1].Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.Black);
            }

            int row = 2, index = 1;
            var mergeRegions = new List<(int startRow, int endRow)>();

            foreach (var qr in quizResults)
            {
                var details = qr.QuizResultDetails;
                int startRow = row;
                int endRow = row + details.Count - 1;

                double correctRatio = qr.QrTotalQuestion > 0
                    ? (double)qr.QrAnswer / qr.QrTotalQuestion * 10 : 0;

                foreach (var detail in details)
                {
                    sheet.Cells[row, 9].Value = detail.QuizQuestion?.QqQuestion;
                    sheet.Cells[row, 10].Value = detail.QrdStudentAnswer;
                    sheet.Cells[row, 11].Value = detail.QrdIsCorrect ? "Đúng" : "Sai";

                    sheet.Cells[row, 11].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    sheet.Cells[row, 11].Style.Fill.BackgroundColor.SetColor(
                        detail.QrdIsCorrect ? Color.LightGreen : Color.LightSalmon);

                    row++;
                }

                sheet.Cells[startRow, 1].Value = index;
                sheet.Cells[startRow, 2].Value = qr.Student.StudentCode;
                sheet.Cells[startRow, 3].Value = qr.Student.Users.UsersName;
                sheet.Cells[startRow, 4].Value = qr.Student.Users.UsersEmail;
                sheet.Cells[startRow, 5].Value = qr.QrDate.ToString("dd/MM/yyyy HH:mm:ss");
                sheet.Cells[startRow, 6].Value = qr.QrTotalQuestion;
                sheet.Cells[startRow, 7].Value = qr.QrAnswer;
                sheet.Cells[startRow, 8].Value = correctRatio.ToString("0.##");

                mergeRegions.Add((startRow, endRow));
                index++;
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();

            foreach (var (startRow, endRow) in mergeRegions)
            {
                for (int col = 1; col <= 8; col++)
                {
                    var mergedCell = sheet.Cells[startRow, col, endRow, col];
                    mergedCell.Merge = true;
                    mergedCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    mergedCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    mergedCell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    mergedCell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    mergedCell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    mergedCell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                }
                for (int col = 9; col <= 11; col++)
                {
                    var mergedCell = sheet.Cells[startRow, col, endRow, col];
                    mergedCell.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.Black);
                }
            }

            // === Sheet 3: Biểu đồ ===
            var chartSheet = package.Workbook.Worksheets.Add("Biểu đồ");

            chartSheet.Cells[1, 1].Value = "Tên sinh viên";
            chartSheet.Cells[1, 2].Value = "Điểm";
            chartSheet.Cells[$"A1:B1"].Style.Font.Bold = true;
            chartSheet.Cells[$"A1:B1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            chartSheet.Cells[$"A1:B1"].Style.Fill.BackgroundColor.SetColor(Color.SteelBlue);
            chartSheet.Cells[$"A1:B1"].Style.Font.Color.SetColor(Color.White);
            chartSheet.Cells[$"A1:B1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            chartSheet.Cells[$"A1:B1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            chartSheet.Cells[$"A1:A1"].Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.Black);
            chartSheet.Cells[$"B1:B1"].Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.Black);
            chartSheet.Cells[$"A1:B1"].AutoFitColumns();

            int chartRow = 2;
            foreach (var qr in quizResults)
            {
                double core = qr.QrTotalQuestion > 0
                       ? (double)qr.QrAnswer / qr.QrTotalQuestion * 10 : 0;
                chartSheet.Cells[chartRow, 1].Value = qr.Student.Users.UsersName;
                chartSheet.Cells[chartRow, 2].Value = core.ToString("0.##");
                chartSheet.Cells[$"A{chartRow}:A{chartRow}"].Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.Black);
                chartSheet.Cells[$"B{chartRow}:B{chartRow}"].Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.Black);
                chartSheet.Cells[$"B{chartRow}:B{chartRow}"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                chartSheet.Cells[$"A{chartRow}:B{chartRow}"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                chartSheet.Cells[$"A{chartRow}:B{chartRow}"].AutoFitColumns();
                chartRow++;
            }

            var scores = quizResults.Where(qr => qr.QrTotalQuestion > 0)
                .Select(qr => (double)qr.QrAnswer / qr.QrTotalQuestion * 10).ToList();

            double avgScore = scores.Any() ? scores.Average() : 0;
            double maxScore = scores.Any() ? scores.Max() : 0;
            double minScore = scores.Any() ? scores.Min() : 0;

            int xuatSac = scores.Count(s => s >= 9 && s <= 10);
            int gioi = scores.Count(s => s >= 8 && s < 9);
            int kha = scores.Count(s => s >= 7 && s < 8);
            int trungBinhKha = scores.Count(s => s >= 6 && s < 7);
            int trungBinh = scores.Count(s => s >= 5 && s < 6);
            int yeu = scores.Count(s => s >= 3.5 && s < 5);
            int kem = scores.Count(s => s < 3.5);

            int statStartRow = chartRow + 1;

            var gopThongKe1 = chartSheet.Cells[$"A{statStartRow}:B{statStartRow}"];
            gopThongKe1.Merge = true;
            gopThongKe1.Value = "Thống kê điểm (%)";
            gopThongKe1.Style.Fill.PatternType = ExcelFillStyle.Solid;
            gopThongKe1.Style.Fill.BackgroundColor.SetColor(Color.SteelBlue);
            gopThongKe1.Style.Font.Color.SetColor(Color.White);
            gopThongKe1.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            gopThongKe1.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.Black);
            gopThongKe1.AutoFitColumns();
            gopThongKe1.Style.Font.Bold = true;

            chartSheet.Cells[$"A{statStartRow + 1}"].Value = "Điểm trung bình:";
            chartSheet.Cells[$"B{statStartRow + 1}"].Value = avgScore.ToString("0.##");

            chartSheet.Cells[$"A{statStartRow + 2}"].Value = "Điểm cao nhất:";
            chartSheet.Cells[$"B{statStartRow + 2}"].Value = maxScore.ToString("0.##");

            chartSheet.Cells[$"A{statStartRow + 3}"].Value = "Điểm thấp nhất:";
            chartSheet.Cells[$"B{statStartRow + 3}"].Value = minScore.ToString("0.##");
            chartSheet.Cells[$"A{statStartRow + 1}:B{statStartRow + 3}"].Style.Border.Top.Style = ExcelBorderStyle.Thin;
            chartSheet.Cells[$"A{statStartRow + 1}:B{statStartRow + 3}"].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            chartSheet.Cells[$"A{statStartRow + 1}:B{statStartRow + 3}"].Style.Border.Left.Style = ExcelBorderStyle.Thin;
            chartSheet.Cells[$"A{statStartRow + 1}:B{statStartRow + 3}"].Style.Border.Right.Style = ExcelBorderStyle.Thin;
            chartSheet.Cells[$"B{statStartRow + 1}:B{statStartRow + 8}"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

            int classifyRow = statStartRow + 5;
            chartSheet.Cells[$"A{classifyRow}"].Value = "Phân loại";
            chartSheet.Cells[$"B{classifyRow}"].Value = "Số lượng";
            chartSheet.Cells[$"C{classifyRow}"].Value = "Mức điểm";
            chartSheet.Cells[$"A{classifyRow}:C{classifyRow}"].Style.Font.Bold = true;
            chartSheet.Cells[$"A{classifyRow}:C{classifyRow}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            chartSheet.Cells[$"A{classifyRow}:C{classifyRow}"].Style.Fill.BackgroundColor.SetColor(Color.SteelBlue);
            chartSheet.Cells[$"A{classifyRow}:C{classifyRow}"].Style.Font.Color.SetColor(Color.White);
            chartSheet.Cells[$"A{classifyRow}:C{classifyRow}"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            chartSheet.Cells[$"A{classifyRow}:C{classifyRow}"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            chartSheet.Cells[$"A{classifyRow}:C{classifyRow}"].AutoFitColumns();

            chartSheet.Cells[$"A{classifyRow + 1}"].Value = "Tổng";
            chartSheet.Cells[$"B{classifyRow + 1}"].Value = quizResults.Count;

            chartSheet.Cells[$"A{classifyRow + 2}"].Value = "Xuất sắc";
            chartSheet.Cells[$"B{classifyRow + 2}"].Value = xuatSac;
            chartSheet.Cells[$"C{classifyRow + 2}"].Value = "9 - 10";

            chartSheet.Cells[$"A{classifyRow + 3}"].Value = "Giỏi";
            chartSheet.Cells[$"B{classifyRow + 3}"].Value = gioi;
            chartSheet.Cells[$"C{classifyRow + 3}"].Value = "8 - 8.9";

            chartSheet.Cells[$"A{classifyRow + 4}"].Value = "Khá";
            chartSheet.Cells[$"B{classifyRow + 4}"].Value = kha;
            chartSheet.Cells[$"C{classifyRow + 4}"].Value = "7 - 7.9";

            chartSheet.Cells[$"A{classifyRow + 5}"].Value = "Trung bình khá";
            chartSheet.Cells[$"B{classifyRow + 5}"].Value = trungBinhKha;
            chartSheet.Cells[$"C{classifyRow + 5}"].Value = "6 - 6.9";

            chartSheet.Cells[$"A{classifyRow + 6}"].Value = "Trung bình";
            chartSheet.Cells[$"B{classifyRow + 6}"].Value = trungBinh;
            chartSheet.Cells[$"C{classifyRow + 6}"].Value = "5 - 5.9";

            chartSheet.Cells[$"A{classifyRow + 7}"].Value = "Yếu";
            chartSheet.Cells[$"B{classifyRow + 7}"].Value = yeu;
            chartSheet.Cells[$"C{classifyRow + 7}"].Value = "3.5 - 4.9";

            chartSheet.Cells[$"A{classifyRow + 8}"].Value = "Kém";
            chartSheet.Cells[$"B{classifyRow + 8}"].Value = kem;
            chartSheet.Cells[$"C{classifyRow + 8}"].Value = "< 3.5";

            chartSheet.Cells[$"A{classifyRow + 1}:A{classifyRow + 8}"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            chartSheet.Cells[$"B{classifyRow + 1}:B{classifyRow + 8}"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            chartSheet.Cells[$"C{classifyRow + 1}:C{classifyRow + 8}"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            chartSheet.Cells[$"A{classifyRow}:C{classifyRow + 8}"].Style.Border.Top.Style = ExcelBorderStyle.Thin;
            chartSheet.Cells[$"A{classifyRow}:C{classifyRow + 8}"].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            chartSheet.Cells[$"A{classifyRow}:C{classifyRow + 8}"].Style.Border.Left.Style = ExcelBorderStyle.Thin;
            chartSheet.Cells[$"A{classifyRow}:C{classifyRow + 8}"].Style.Border.Right.Style = ExcelBorderStyle.Thin;
            chartSheet.Cells[$"A{statStartRow}:C{classifyRow + 8}"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            chartSheet.Cells[chartSheet.Dimension.Address].AutoFitColumns();

            // Thêm biểu đồ cột vào sheet "Biểu đồ"
            var chart = chartSheet.Drawings.AddChart("barChart", eChartType.ColumnClustered);
            chart.Title.Text = "Phân loại kết quả theo tỉ lệ điểm";
            chart.SetPosition(1, 0, 4, 0);
            chart.SetSize(600, 300);

            // Thêm series dữ liệu cho biểu đồ
            var chartSeries = chart.Series.Add(
                 chartSheet.Cells[$"B{classifyRow + 2}:B{classifyRow + 8}"],
                chartSheet.Cells[$"A{classifyRow + 2}:A{classifyRow + 8}"]
            );
            chartSeries.Header = "Số sinh viên";

            var pieChart = chartSheet.Drawings.AddChart("PieChart", eChartType.PieExploded3D) as ExcelPieChart;
            pieChart.Title.Text = "Phân loại kết quả (%)";
            pieChart.Series.Add(
                chartSheet.Cells[$"B{classifyRow + 2}:B{classifyRow + 8}"],
                chartSheet.Cells[$"A{classifyRow + 2}:A{classifyRow + 8}"]
            );

            pieChart.DataLabel.ShowPercent = true;
            pieChart.Legend.Position = eLegendPosition.Right;
            pieChart.SetPosition(1, 0, 14, 0);
            pieChart.SetSize(600, 300);

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"QuizReport_{quizId}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            return File(stream, contentType, fileName);
        }

        // So sánh kết quả giữa các lớp học chung giáo viên
        [HttpGet("compare-quiz-results/{teacherId}")]
        public async Task<IActionResult> CompareQuizResults(int teacherId)
        {
            var quizResults = await _context.QuizResults
                .Include(qr => qr.Student)
                .ThenInclude(s => s.Users)
                .Include(qr => qr.Quiz)
                .ThenInclude(q => q.ClassCourse)
                .ThenInclude(cc => cc.TeacherClasses)
                .ThenInclude(tc => tc.User)
                .Include(qr => qr.Quiz.ClassCourse)
                .ThenInclude(cc => cc.Classes)
                .Where(qr => qr.Quiz.ClassCourse.TeacherClasses.First().TcUsersId == teacherId)
                .ToListAsync();
            if (!quizResults.Any())
            {
                return NotFound(new { message = "Không có kết quả bài làm cho giáo viên này." });
            }
            var result = quizResults.GroupBy(qr => new
            {
                qr.Quiz.ClassCourse.Classes.ClassTitle
            })
            .Select(g => new
            {
                ClassTitle = g.Key.ClassTitle,
                TeacherName = g.FirstOrDefault().Quiz.ClassCourse.TeacherClasses.First().User.UsersName,
                MaxCore = g.Max(qr => (double)qr.QrAnswer / qr.QrTotalQuestion * 10),
                MinCore = g.Min(qr => (double)qr.QrAnswer / qr.QrTotalQuestion * 10),
                AverageScore = g.Average(qr => (double)qr.QrAnswer / qr.QrTotalQuestion * 10),
                TotalStudents = g.Count()
            })
            .ToList();
            return Ok(result);
        }

        // Cập nhật kết quả của sinh viên (QrAnswer, QrdIsCorrect) khi có đáp án đúng thay đổi (QqCorrect)
        [HttpPut("update-quiz-result/{quizId}")]
        public async Task<IActionResult> UpdateQuizResult(int quizId, List<QuizQuestion> updatedQuestions)
        {
            if (updatedQuestions == null || !updatedQuestions.Any())
                return BadRequest("Danh sách câu hỏi không hợp lệ.");
            if (_context.Quizzes.Any(q => q.QuizId == quizId) == false)
                return NotFound("Không tìm thấy bài kiểm tra.");
            foreach (var updated in updatedQuestions)
            {
                var original = await _context.QuizQuestions.FindAsync(updated.QqId);
                if (original != null)
                {
                    original.QqCorrect = updated.QqCorrect?.Trim();
                }
            }

            await _context.SaveChangesAsync(); 

            var questions = await _context.QuizQuestions
                .Where(q => q.QqQuizId == quizId)
                .ToListAsync();

            var quizResults = await _context.QuizResults
                .Where(qr => qr.QrQuizId == quizId)
                .ToListAsync();

            foreach (var quizResult in quizResults)
            {
                var details = await _context.QuizResultDetails
                    .Where(d => d.QrdResultId == quizResult.QrId)
                    .ToListAsync();

                int correctCount = 0;

                foreach (var detail in details)
                {
                    var question = questions.FirstOrDefault(q => q.QqId == detail.QrdQuestionId);
                    if (question == null) continue;

                    bool isCorrect = string.Equals(
                        detail.QrdStudentAnswer?.Trim(),
                        question.QqCorrect?.Trim(),
                        StringComparison.OrdinalIgnoreCase
                    );

                    detail.QrdIsCorrect = isCorrect;
                    if (isCorrect) correctCount++;
                }

                quizResult.QrAnswer = correctCount;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã cập nhật đáp án và tính lại kết quả bài làm của sinh viên." });
        }
    }
}
