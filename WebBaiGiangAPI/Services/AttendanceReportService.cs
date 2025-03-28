using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using WebBaiGiangAPI.Data;

public class AttendanceReportService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AttendanceReportService> _logger;

    public AttendanceReportService(IServiceScopeFactory scopeFactory, ILogger<AttendanceReportService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendWeeklyReport();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi gửi báo cáo: {ex.Message}");
            }

            // Chờ đến thứ 7 hàng tuần lúc 23:59
            var nextRun = GetNextSaturdayMidnight();
            var delay = nextRun - DateTime.Now;
            await Task.Delay(delay, stoppingToken);
        }
    }

    private async Task SendWeeklyReport()
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

            var classes = await context.Classes.ToListAsync();

            foreach (var cls in classes)
            {
                var students = await context.AttendanceMarks
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

                var teacher = await context.TeacherClasses
                    .Where(tc => tc.ClassCourses.ClassId == cls.ClassId)
                    .Join(context.Users, tc => tc.TcUsersId, u => u.UsersId, (tc, u) => u)
                    .FirstOrDefaultAsync();

                if (teacher != null)
                {
                    await emailService.SendEmail(teacher.UsersEmail, subject, reportContent);
                }
            }
        }
    }

    private DateTime GetNextSaturdayMidnight()
    {
        var today = DateTime.Now;
        int daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)today.DayOfWeek + 7) % 7;
        if (daysUntilSaturday == 0) daysUntilSaturday = 7; // Nếu hôm nay là thứ 7, thì chờ đến tuần sau
        return today.Date.AddDays(daysUntilSaturday).AddHours(23).AddMinutes(59);
    }
}
