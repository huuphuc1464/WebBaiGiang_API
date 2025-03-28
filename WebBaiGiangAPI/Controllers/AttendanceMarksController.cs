using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using WebBaiGiangAPI.Data;
using WebBaiGiangAPI.Models;

namespace WebBaiGiangAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttendanceMarksController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;
        public AttendanceMarksController(AppDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // Lấy danh sách điểm danh theo lớp
        [HttpGet("class/{classId}")]
        public async Task<IActionResult> GetAttendanceByClass(int classId)
        {
            if (_context.Classes.Find(classId) == null)
            {
                return NotFound("Không tìm thấy lớp học.");
            }
            var attendanceList = await _context.AttendanceMarks
                .Where(a => a.ClassId == classId)
                .Select(a => new
                {
                    a.AttendanceMarksId,
                    a.StudentId,
                    StudentName = a.Student.Users.UsersName,
                    a.AttendanceDate,
                    a.AttendanceStatus
                })
                .ToListAsync();
            if (attendanceList.Count == 0)
            {
                return NotFound("Không có dữ liệu điểm danh.");
            }
            return Ok(attendanceList);
        }

        // Kiểm tra điều kiện hợp lệ trước khi điểm danh
        private async Task<List<string>> ValidateAttendanceAsync(AttendanceMarks attendance)
        {
            var errors = new List<string>();

            attendance.AttendanceDate = DateTime.Now;

            var student = await _context.Students
                .Include(s => s.Users)
                .FirstOrDefaultAsync(s => s.StudentId == attendance.StudentId);
            var classInfo = await _context.Classes.FirstOrDefaultAsync(c => c.ClassId == attendance.ClassId);

            if (classInfo == null)
            {
                errors.Add($"Không tìm thấy lớp học với ID {attendance.ClassId}.");
            }
            if (student == null)
            {
                errors.Add($"Không tìm thấy sinh viên với ID {attendance.StudentId}.");
            }

            if (student != null && classInfo != null)
            {
                var thuocLop = await _context.StudentClasses
                    .FirstOrDefaultAsync(sc => sc.ScClassId == attendance.ClassId && sc.ScStudentId == attendance.StudentId);

                if (thuocLop == null)
                {
                    errors.Add($"Sinh viên {student.Users.UsersName} không thuộc lớp {classInfo.ClassTitle}.");
                }
                else if (thuocLop.ScStatus == 0)
                {
                    errors.Add($"Sinh viên {student.Users.UsersName} đã bị khóa hoặc chưa xác nhận lớp học.");
                }
            }

            var validStatuses = new List<string> { "Yes", "No", "Late" };
            if (!validStatuses.Contains(attendance.AttendanceStatus))
            {
                errors.Add($"Trạng thái điểm danh '{attendance.AttendanceStatus}' không hợp lệ.");
            }

            // Kiểm tra thời gian điểm danh gần nhất
            var lastAttendance = await _context.AttendanceMarks
                .Where(a => a.StudentId == attendance.StudentId && a.ClassId == attendance.ClassId)
                .OrderByDescending(a => a.AttendanceDate)
                .FirstOrDefaultAsync();

            if (lastAttendance != null)
            {
                var timeDiff = (attendance.AttendanceDate - lastAttendance.AttendanceDate).TotalMinutes;
                if (timeDiff < 45)
                {
                    errors.Add($"Sinh viên {student.Users.UsersName} đã điểm danh trước đó {Math.Round(timeDiff)} phút. Vui lòng chờ đủ 45 phút.");
                }
            }

            // Kiểm tra số lần điểm danh trong ngày
            var today = DateTime.Today;
            int countToday = await _context.AttendanceMarks
                .CountAsync(a => a.StudentId == attendance.StudentId &&
                                 a.ClassId == attendance.ClassId &&
                                 a.AttendanceDate.Date == today);

            if (countToday >= 4)
            {
                errors.Add($"Sinh viên {student.Users.UsersName} đã điểm danh 4 lần hôm nay.");
            }

            return errors;
        }

        // Điểm danh 1 sinh viên
        [HttpPost("add-attendance")]
        public async Task<IActionResult> AddAttendance([FromBody] AttendanceMarks attendance)
        {
            var errors = await ValidateAttendanceAsync(attendance);
            if (errors.Count > 0)
            {
                return BadRequest(new { message = "Có lỗi xảy ra.", errors });
            }
            await _context.SaveChangesAsync();

            return Ok(new { message = "Điểm danh thành công!", attendance });
        }

        // Điểm danh danh sách sinh viên
        [HttpPost("save-attendance-marks")]
        public async Task<IActionResult> SaveAttendance([FromBody] List<AttendanceMarks> attendanceMarks)
        {
            if (attendanceMarks == null || attendanceMarks.Count == 0)
            {
                return BadRequest("Danh sách điểm danh không hợp lệ.");
            }

            var errors = new List<string>();
            var validAttendances = new List<AttendanceMarks>();

            foreach (var attendance in attendanceMarks)
            {
                var validationErrors = await ValidateAttendanceAsync(attendance);
                if (validationErrors.Count > 0)
                {
                    errors.AddRange(validationErrors);
                }
                else
                {
                    validAttendances.Add(attendance);
                }
            }

            if (errors.Count > 0)
            {
                return BadRequest(new { message = "Có lỗi xảy ra.", errors });
            }

            if (validAttendances.Count > 0)
            {
                await _context.AttendanceMarks.AddRangeAsync(validAttendances);
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Điểm danh thành công!", totalSaved = validAttendances.Count });
        }

        // Cập nhật trạng thái điểm danh
        [HttpPut("update-attendance")]
        public async Task<IActionResult> UpdateAttendance([FromBody] AttendanceMarks updatedAttendance)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Dữ liệu không hợp lệ.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }

            var attendance = await _context.AttendanceMarks.FindAsync(updatedAttendance.AttendanceMarksId);
            if (attendance == null)
            {
                return NotFound("Không tìm thấy điểm danh.");
            }

            var validStatuses = new List<string> { "Yes", "No", "Late" };
            if (!validStatuses.Contains(updatedAttendance.AttendanceStatus))
            {
                return BadRequest($"Trạng thái điểm danh '{updatedAttendance.AttendanceStatus}' không hợp lệ.");
            }

            attendance.AttendanceStatus = updatedAttendance.AttendanceStatus;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật điểm danh thành công.", attendance });
        }

        // Thống kê số ngày vắng mặt, đi trễ và đi học của từng sinh viên trong lớp
        [HttpGet("statistics/status-attendance/class/{classId}")]
        public async Task<IActionResult> GetStatusAttendanceStatistics(int classId)
        {
            var totalSessions = await _context.AttendanceMarks
            .Where(a => a.ClassId == classId)
            .GroupBy(a => new
            {
                Date = a.AttendanceDate.Date,
                Hour = a.AttendanceDate.Hour, 
                Minute = a.AttendanceDate.Minute / 45 
            })
            .CountAsync(); // Tổng số buổi học, mỗi buổi cách nhau 45 phút


            var statistics = await _context.AttendanceMarks
                .Where(a => a.ClassId == classId)
                .GroupBy(a => new { a.StudentId, a.Student.Users.UsersName })
                .Select(g => new
                {
                    g.Key.StudentId,
                    StudentName = g.Key.UsersName,
                    AbsencesCount = g.Count(a => a.AttendanceStatus == "No"),
                    LateCount = g.Count(a => a.AttendanceStatus == "Late"),
                    PresentCount = g.Count(g => g.AttendanceStatus == "Yes")
                })
                .ToListAsync();

            return Ok(new
            {
                soBuoiHoc = totalSessions,
                thongKeTrangThaiDiemDanh = statistics
            });
        }

        // Tự động gửi email cảnh báo trước khi sinh viên vắng mặt quá số buổi quy định (3 buổi)
        [HttpGet("send-absence-warnings/class/{classId}")]
        public async Task<IActionResult> SendAbsenceWarnings(int classId)
        {
            int maxAllowedAbsences = 3; // Số buổi vắng tối đa trước khi cảnh báo

            var studentsWithAbsences = await _context.AttendanceMarks
                .Where(a => a.ClassId == classId && a.AttendanceStatus == "No")
                .GroupBy(a => new { a.StudentId, a.Student.Users.UsersName, a.Student.Users.UsersEmail })
                .Select(g => new
                {
                    g.Key.StudentId,
                    UsersName = g.Key.UsersName,
                    UsersEmail = g.Key.UsersEmail,
                    AbsenceCount = g.Count(),
                    AbsenceDates = g.Select(a => a.AttendanceDate).OrderBy(d => d).ToList() // Lấy danh sách ngày vắng mặt
                })
                .Where(s => s.AbsenceCount >= maxAllowedAbsences && s.UsersEmail != null)
                .ToListAsync();

            var classInfo = await _context.Classes
                .Where(c => c.ClassId == classId)
                .Select(c => new { c.ClassTitle })
                .FirstOrDefaultAsync();

            if (classInfo == null)
            {
                return NotFound("Không tìm thấy lớp học.");
            }

            int emailCount = 0;
            foreach (var student in studentsWithAbsences)
            {
                string formattedDates = string.Join(", ", student.AbsenceDates.Select(d => d.ToString("dd/MM/yyyy HH:mm:ss")));

                string subject = $"[Cảnh báo vắng mặt] {classInfo.ClassTitle}";
                string body = $@"
                <p>Chào <strong>{student.UsersName}</strong>,</p>
                <p>Bạn đã vắng mặt <strong>{student.AbsenceCount}</strong> buổi trong lớp <strong>{classInfo.ClassTitle}</strong>.</p>
                <p><strong>Ngày vắng mặt:</strong> {formattedDates}</p>
                <p>Vui lòng đảm bảo tham gia đầy đủ các buổi học để tránh ảnh hưởng đến kết quả học tập.</p>
                <p>Trân trọng,<br><strong>Hệ thống quản lý lớp học</strong></p>";

                bool isSent = await _emailService.SendEmail(student.UsersEmail, subject, body);
                if (isSent)
                {
                    emailCount++;
                }
            }

            return Ok($"Đã gửi cảnh báo đến {emailCount} sinh viên vắng quá số buổi quy định.");
        }

        // Xem lịch sử điểm danh
        [HttpGet("history/{studentId}/{classId}")]
        public async Task<IActionResult> GetAttendanceHistory(int studentId, int classId)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
            {
                return NotFound("Không tìm thấy sinh viên.");
            }
            var cls = await _context.Classes.FindAsync(classId);
            if (cls == null)
            {
                return NotFound("Không tìm thấy lớp học.");
            }
            var thuocLop = await _context.StudentClasses
                .FirstOrDefaultAsync(sc => sc.ScClassId == classId && sc.ScStudentId == studentId && sc.ScStatus == 1);
            if (thuocLop == null)
            {
                return BadRequest("Sinh viên không thuộc lớp học này.");
            }    
            var history = await _context.AttendanceMarks
            .Where(a => a.StudentId == studentId && a.ClassId == classId)
            .Select(a => new
            {
                a.AttendanceDate,
                a.AttendanceStatus
            })
            .OrderBy(a => a.AttendanceDate)
            .ToListAsync();
            if (history == null)
            {
                return NotFound("Không có dữ liệu điểm danh.");
            }
            return Ok(history);
        }
        
        // API lấy danh sách sinh viên trong lớp
        [HttpGet("students/{classId}")]
        public async Task<IActionResult> GetStudentsByClass(int classId)
        {
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
                          sc.Student.StudentCode
                      })
                .OrderBy(s => s.StudentCode)
                .ToListAsync();

            if (students == null || students.Count == 0)
            {
                return NotFound("Không có sinh viên nào trong lớp này.");
            }

            return Ok(students);
        }

        // Thống kê tỷ lệ điểm danh theo lớp: Tính toán tỷ lệ sinh viên có mặt trong lớp theo phần trăm.
        [HttpGet("statistics/class/present/{classId}")]
        public async Task<IActionResult> GetAttendanceStatistics(int classId)
        {
            var totalSessions = await _context.AttendanceMarks
            .Where(a => a.ClassId == classId)
            .GroupBy(a => new
            {
                Date = a.AttendanceDate.Date,
                Hour = a.AttendanceDate.Hour,
                Minute = a.AttendanceDate.Minute / 45
            })
            .CountAsync(); // Tổng số buổi học, mỗi buổi cách nhau 45 phút

            var statistics = await _context.AttendanceMarks
                .Where(a => a.ClassId == classId)
                .GroupBy(a => new { a.StudentId, a.Student.Users.UsersName })
                .Select(g => new
                {
                    g.Key.StudentId,
                    g.Key.UsersName,
                    TotalPresent = g.Count(a => a.AttendanceStatus == "Yes"),
                    TotalLate = g.Count(a => a.AttendanceStatus == "Late"),
                    TotalAbsent = totalSessions - g.Count(a => a.AttendanceStatus == "Yes") - g.Count(a => a.AttendanceStatus == "Late"),
                    AttendanceRate = (double)g.Count(a => a.AttendanceStatus == "Yes") / totalSessions * 100,
                    Mark = GetAttendanceMark(totalSessions, totalSessions - g.Count(a => a.AttendanceStatus == "Yes"), g.Count(a => a.AttendanceStatus == "Late"))
                })
                .ToListAsync();


            return Ok(statistics);
        }

        // Hàm tính điểm chuyên cần
        private static int GetAttendanceMark(int totalSessions, int totalAbsent, int totalLate)
        {
            double absentRate = (double)totalAbsent / totalSessions * 100;

            if (absentRate > 20 && totalAbsent >= 5) return 0;

            // Chấm điểm theo tiêu chí
            if (totalAbsent == 0 && totalLate == 0)
                return 10;
            if (totalAbsent == 0 && totalLate <= 2)
                return 9;
            if (totalAbsent <= (totalSessions <= 15 ? 3 : 4) && totalLate <= 2) // giả sử 1 buổi điểm danh 1 lần
                return 8;
            if (totalAbsent <= (totalSessions <= 15 ? 3 : 4))
                return 7;
            if (totalAbsent <= (totalSessions <= 15 ? 3 : 4))
                return 6;
            if (totalAbsent <= (totalSessions <= 15 ? 6 : 8))
                return 5;
            else
                return new Random().Next(1, 5); // Dao động từ 1-4 nếu vắng + trễ quá nhiều
        }

        /*
         Tự động gửi báo cáo điểm danh hàng tuần:
            - Hệ thống gửi email báo cáo điểm danh hàng tuần đến giáo viên và sinh viên.
            - Báo cáo có tổng số buổi vắng mặt và tỷ lệ điểm danh.
        */
        [HttpGet("send-weekly-report")]
        public async Task<IActionResult> SendWeeklyAttendanceReport(int studentId, int classId)
        {
            var classes = await _context.Classes.ToListAsync();

            foreach (var cls in classes)
            {
                var students = await _context.AttendanceMarks
                    .Where(a => a.ClassId == cls.ClassId)
                    .GroupBy(a => new { a.StudentId, a.Student.Users.UsersName, a.Student.Users.UsersEmail })
                    .Select(g => new
                    {
                        g.Key.UsersName,
                        g.Key.UsersEmail,
                        TotalAbsent = g.Count(a => a.AttendanceStatus == "No"),
                        TotalLate = g.Count(a => a.AttendanceStatus == "Late"),
                        AttendanceRate = 100 - (double)g.Count(a => a.AttendanceStatus == "No") / g.Count() * 100
                    })
                    .ToListAsync();

                string reportContent = $@"
                <h3>Báo cáo điểm danh - Lớp: {cls.ClassTitle}</h3>
                <table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse; width: 100%;'>
                    <thead>
                        <tr style='background-color: #f2f2f2;'>
                            <th>Tên sinh viên</th>
                            <th>Số buổi vắng</th>
                            <th>Số buổi đi trễ</th>
                            <th>Tỷ lệ tham gia (%)</th>
                        </tr>
                    </thead>
                    <tbody>";

                foreach (var student in students)
                {
                    reportContent += $@"
                    <tr>
                        <td>{student.UsersName}</td>
                        <td style='text-align: center;'>{student.TotalAbsent}</td>
                        <td style='text-align: center;'>{student.TotalLate}</td>
                        <td style='text-align: center;'>{student.AttendanceRate:F2}%</td>
                    </tr>";
                }

                reportContent += "</tbody></table>";

                string subject = $"[Báo cáo điểm danh] - Lớp: {cls.ClassTitle}";

                var teacher = await _context.TeacherClasses
                    .Where(tc => tc.TcClassCourseId == cls.ClassId)
                    .Join(_context.Users, tc => tc.TcUsersId, u => u.UsersId, (tc, u) => u)
                    .FirstOrDefaultAsync();

                if (teacher != null)
                {
                    await _emailService.SendEmail(teacher.UsersEmail, subject, reportContent);
                }
            }

            return Ok("Đã gửi báo cáo điểm danh hàng tuần.");
        }

        // Xuất excel
        [HttpGet("export/excel/{classId}")]
        public async Task<IActionResult> ExportAttendanceToExcel(int classId)
        {
            var attendances = await _context.AttendanceMarks
                .Where(a => a.ClassId == classId)
                .Include(a => a.Student.Users)
                .ToListAsync();

            var className = await _context.Classes
                .Where(c => c.ClassId == classId)
                .Select(c => c.ClassTitle)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(className))
            {
                return NotFound("Không tìm thấy lớp học.");
            }

            className = Regex.Replace(className.Trim(), @"\s+", "");

            if (!attendances.Any())
            {
                return NotFound("Không có dữ liệu điểm danh cho lớp này.");
            }

            // Lấy danh sách sinh viên, thêm mssv và email
            var students = attendances
                .Select(a => new { a.StudentId, a.Student.StudentCode, a.Student.Users.UsersName, a.Student.Users.UsersEmail })
                .Distinct()
                .ToList();

            // Lấy danh sách ngày điểm danh, chỉ lấy đến phút
            var dates = attendances
                .Select(a => a.AttendanceDate.ToString("yyyy-MM-dd HH:mm")) // Chỉ lấy đến phút
                .Distinct()
                .OrderBy(d => d)
                .Select(d => DateTime.ParseExact(d, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture))
                .ToList();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Attendance Report");

            // Ghi tiêu đề cột
            worksheet.Cells[1, 1].Value = "Họ và tên";
            worksheet.Cells[1, 2].Value = "MSSV";
            worksheet.Cells[1, 3].Value = "Email";

            for (int i = 0; i < dates.Count; i++)
            {
                worksheet.Cells[1, i + 4].Value = dates[i].ToString("dd/MM/yyyy HH:mm");
                worksheet.Cells[1, i + 4].Style.Numberformat.Format = "dd/MM/yyyy HH:mm"; // Định dạng Excel không có giây
            }
            worksheet.Cells[1, dates.Count + 4].Value = "Điểm chuyên cần";
            // Ghi dữ liệu điểm danh
            for (int row = 0; row < students.Count; row++)
            {
                worksheet.Cells[row + 2, 1].Value = students[row].UsersName;
                worksheet.Cells[row + 2, 2].Value = students[row].StudentCode; 
                worksheet.Cells[row + 2, 3].Value = students[row].UsersEmail; 

                int totalSessions = dates.Count;
                int totalAbsent = 0;
                int totalLate = 0;

                for (int col = 0; col < dates.Count; col++)
                {
                    var attendance = attendances.FirstOrDefault(a =>
                        a.StudentId == students[row].StudentId &&
                        a.AttendanceDate.ToString("yyyy-MM-dd HH:mm") == dates[col].ToString("yyyy-MM-dd HH:mm"));

                    // Chuyển đổi trạng thái điểm danh
                    string status = attendance?.AttendanceStatus switch
                    {
                        "Yes" => "Có",
                        "No" => "Vắng",
                        "Late" => "Trễ",
                        _ => "N/A"
                    };

                    // Tính số buổi vắng & đi trễ
                    if (status == "Vắng") totalAbsent++;
                    if (status == "Trễ") totalLate++;

                    worksheet.Cells[row + 2, col + 4].Value = status;
                }

                // Tính điểm chuyên cần
                int attendanceMark = GetAttendanceMark(totalSessions, totalAbsent, totalLate);
                worksheet.Cells[row + 2, dates.Count + 4].Value = attendanceMark;
            }

            worksheet.Cells.AutoFitColumns();


            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Attendance_Report_{className}.xlsx");
        }

        [HttpGet("export/mail/{classId}")]
        public async Task<IActionResult> SendAttendanceReportEmail(int classId)
        {
            var attendances = await _context.AttendanceMarks
                .Where(a => a.ClassId == classId)
                .Include(a => a.Student.Users)
                .ToListAsync();

            if (!attendances.Any())
            {
                return NotFound("Không có dữ liệu điểm danh cho lớp này.");
            }

            var classInfo = await _context.Classes
                .Where(c => c.ClassId == classId)
                .Select(c => new { c.ClassTitle })
                .FirstOrDefaultAsync();

            if (classInfo == null)
            {
                return NotFound("Không tìm thấy lớp học.");
            }

            var teacher = await _context.TeacherClasses
                .Where(tc => tc.ClassCourses.ClassId == classId)
                .Join(_context.Users, tc => tc.TcUsersId, u => u.UsersId, (tc, u) => u)
                .FirstOrDefaultAsync();

            var students = attendances
                .Select(a => new { a.StudentId, a.Student.StudentCode, a.Student.Users.UsersName, a.Student.Users.UsersEmail })
                .Distinct()
                .ToList();

            var dates = attendances
                .Select(a => a.AttendanceDate.ToString("yyyy-MM-dd HH:mm")) 
                .Distinct()
                .OrderBy(d => d)
                .Select(d => DateTime.ParseExact(d, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)) 
                .ToList();

            // Bảng điểm danh cho giáo viên
            StringBuilder teacherReport = new StringBuilder();
            teacherReport.Append($"<h2>Báo cáo điểm danh lớp {classInfo.ClassTitle}</h2>");
            teacherReport.Append("<table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse; width: 100%;'>");

            teacherReport.Append("<tr><th>Họ tên</th><th>Mã SV</th>");

            foreach (var date in dates)
            {
                teacherReport.Append($"<th>{date:dd/MM/yyyy HH:mm}</th>");
            }

            teacherReport.Append("<th>Điểm chuyên cần</th></tr>");

            //  Duyệt danh sách sinh viên và điền dữ liệu
            foreach (var student in students)
            {
                int totalSessions = dates.Count;
                int totalAbsent = 0;
                int totalLate = 0;
                StringBuilder studentReport = new StringBuilder();

                studentReport.Append($"<h2>Báo cáo điểm danh cho {student.UsersName} ({student.StudentCode})</h2>");
                studentReport.Append("<table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse; width: 100%;'>");
                studentReport.Append("<tr><th>Ngày</th><th>Trạng thái</th></tr>");

                teacherReport.Append($"<tr><td>{student.UsersName}</td><td>{student.StudentCode}</td>");

                foreach (var date in dates)
                {
                    var attendance = attendances.FirstOrDefault(a =>
                        a.StudentId == student.StudentId &&
                        a.AttendanceDate.ToString("yyyy-MM-dd HH:mm") == date.ToString("yyyy-MM-dd HH:mm"));

                    string status = attendance?.AttendanceStatus switch
                    {
                        "Yes" => "Có mặt",
                        "No" => "Vắng",
                        "Late" => "Trễ",
                        _ => "N/A"
                    };

                    if (status == "Vắng") totalAbsent++;
                    if (status == "Trễ") totalLate++;

                    studentReport.Append($"<tr><td>{date:dd/MM/yyyy HH:mm}</td><td>{status}</td></tr>");
                    teacherReport.Append($"<td>{status}</td>");
                }

                studentReport.Append("</table>");

                int attendanceMark = GetAttendanceMark(totalSessions, totalAbsent, totalLate);
                studentReport.Append($"<p><strong>Điểm chuyên cần: {attendanceMark}</strong></p>");

                // Gửi email cho sinh viên
                await _emailService.SendEmail(student.UsersEmail, "Báo cáo điểm danh", studentReport.ToString());

                teacherReport.Append($"<td>{attendanceMark}</td></tr>");
            }

            teacherReport.Append("</table>");

            // Gửi email cho giáo viên
            await _emailService.SendEmail(teacher.UsersEmail, $"Báo cáo điểm danh lớp {classInfo.ClassTitle}", teacherReport.ToString());

            return Ok("Đã gửi email báo cáo điểm danh.");
        }

    }
}
