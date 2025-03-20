using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebBaiGiangAPI.Data;

public class AbsenceLateWarningService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AbsenceLateWarningService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromDays(1); // Chạy mỗi ngày

    public AbsenceLateWarningService(IServiceScopeFactory scopeFactory, ILogger<AbsenceLateWarningService> logger)
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
                await SendAbsenceWarnings();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi gửi email cảnh báo: {ex.Message}");
            }

            // Đợi đến lần chạy tiếp theo (1 ngày)
            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task SendAbsenceWarnings()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

        int maxAllowedAbsences = 3;

        var classes = await context.Classes.ToListAsync();

        foreach (var cls in classes)
        {
            var studentsWithAbsences = await context.AttendanceMarks
                .Where(a => a.ClassId == cls.ClassId && a.AttendanceStatus == "No")
                .GroupBy(a => new { a.StudentId, a.Student.Users.UsersName, a.Student.Users.UsersEmail })
                .Select(g => new
                {
                    g.Key.StudentId,
                    UsersName = g.Key.UsersName,
                    UsersEmail = g.Key.UsersEmail,
                    AbsenceCount = g.Count(),
                    AbsenceDates = g.Select(a => a.AttendanceDate).OrderBy(d => d).ToList()
                })
                .Where(s => s.AbsenceCount >= maxAllowedAbsences && s.UsersEmail != null)
                .ToListAsync();

            foreach (var student in studentsWithAbsences)
            {
                string formattedDates = string.Join(", ", student.AbsenceDates.Select(d => d.ToString("dd/MM/yyyy HH:mm:ss")));
                string subject = $"[Cảnh báo vắng mặt] {cls.ClassTitle}";
                string body = $@"
                    <p>Chào <strong>{student.UsersName}</strong>,</p>
                    <p>Bạn đã vắng mặt <strong>{student.AbsenceCount}</strong> buổi trong lớp <strong>{cls.ClassTitle}</strong>.</p>
                    <p><strong>Ngày vắng mặt:</strong> {formattedDates}</p>
                    <p>Vui lòng tham gia đầy đủ các buổi học để tránh bị ảnh hưởng đến kết quả học tập.</p>
                    <p>Trân trọng,<br><strong>Hệ thống quản lý lớp học</strong></p>";

                bool isSent = await emailService.SendEmail(student.UsersEmail, subject, body);
                if (isSent)
                {
                    _logger.LogInformation($"Đã gửi email cảnh báo đến {student.UsersEmail}");
                }
            }
        }
    }
}
