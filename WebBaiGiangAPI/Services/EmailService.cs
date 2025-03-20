using MailKit.Net.Smtp;
using MimeKit;
using System.Threading.Tasks;
using WebBaiGiangAPI.Models;

public class EmailService
{
    private readonly string _smtpServer;
    private readonly int _smtpPort;
    private readonly string _smtpUser;
    private readonly string _smtpPass;
    private readonly IConfiguration _configuration;
    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
        _smtpServer = _configuration["EmailSettings:SmtpServer"];
        _smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
        _smtpUser = _configuration["EmailSettings:SmtpUser"];
        _smtpPass = _configuration["EmailSettings:SmtpPass"];
    }

    public async Task<bool> SendEmailAsync(string email, string otp)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Web bài giảng", _smtpUser));
            message.To.Add(new MailboxAddress("", email));
            message.Subject = "Mã OTP khôi phục mật khẩu";
            message.Body = new TextPart("plain")
            {
                Text = $"Mã OTP của bạn là: {otp}. Mã này có hiệu lực trong 5 phút."
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(_smtpServer, _smtpPort, false);
            await client.AuthenticateAsync(_smtpUser, _smtpPass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            return true;
        }
        catch (Exception ex)
        {   
            return false;
        }
    }
    public async Task<bool> SendClassInvitationEmail(string studentEmail, string studentName, string token)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Web bài giảng", _smtpUser));
            message.To.Add(new MailboxAddress("", studentEmail));
            message.Subject = "Xác nhận tham gia lớp học";

            var confirmationLink = $"https://localhost:7166/api/StudentClasses/confirm?token={token}";
            var body = $"Chào {studentName},\n\n"
                      + "Bạn đã được mời tham gia một lớp học.\n"
                      + $"Nhấp vào liên kết sau để xác nhận: {confirmationLink}\n\n";

            message.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync(_smtpServer, _smtpPort, false);
            await client.AuthenticateAsync(_smtpUser, _smtpPass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
    public async Task<bool> SendZoomEmail(Event dataEvent, string recipientEmail, string studentName, string classTitle, string  joinUrl, bool isTeacher, string password, string hostKey = "")
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Website bài giảng", _smtpUser));
            message.To.Add(new MailboxAddress(studentName, recipientEmail));
            message.Subject = $"Mời tham gia lớp học: {classTitle}";

            var body = $"Chào {studentName},\n\n"
                        + $"Bạn đã được mời tham gia lớp học \"{classTitle}\".\n\n"
                        + $"📄 Nội dung sự kiện: {dataEvent.EventDescription}\n"
                        + $"🔗 Link tham gia Zoom: {joinUrl}\n"
                        + $"⌚ Thời gian diễn ra sự kiện: {dataEvent.EventDateStart}\n"
                        + $"⏳ Thời gian kết thúc sự kiện {dataEvent.EventDateEnd}\n"
                        + $"🔑 Mật khẩu đăng nhập: {password}\n";
            // Nếu là giáo viên, gửi kèm Host Key
            if (isTeacher)
            {
                body += $"🔑 Mã Host Key: {hostKey}\n"
                     + "Mã này dùng để chuyển thành quyền chủ trì cuộc họp. Vui lòng không chia sẻ mã này cho bất kỳ ai!\n";
            }

            body += "Trân trọng,\nHệ thống quản lý lớp học.";

            message.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync(_smtpServer, _smtpPort, false);
            await client.AuthenticateAsync(_smtpUser, _smtpPass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
    public async Task<bool> SendUpdatedZoomEmail(Event oldEvent, Event newEvent, string recipientEmail, string studentName, string classTitle, bool isTeacher, string hostKey = "")
    {
        try
        {
            var changes = new List<string>();

            // Kiểm tra thay đổi
            if (oldEvent.EventDateStart != newEvent.EventDateStart)
                changes.Add($"📅 **Thời gian bắt đầu**: {oldEvent.EventDateStart} ➝ {newEvent.EventDateStart}");

            if (oldEvent.EventDateEnd != newEvent.EventDateEnd)
                changes.Add($"⏳ **Thời gian kết thúc**: {oldEvent.EventDateEnd} ➝ {newEvent.EventDateEnd}");

            if (oldEvent.EventDescription != newEvent.EventDescription)
                changes.Add($"📄 **Mô tả sự kiện**: {oldEvent.EventDescription} ➝ {newEvent.EventDescription}");

            if (!changes.Any())
            {
                return false; // Không có thay đổi, không gửi email
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Website bài giảng", _smtpUser));
            message.To.Add(new MailboxAddress(studentName, recipientEmail));
            message.Subject = $"Cập nhật lịch học: {classTitle}";

            var body = $"Chào {studentName},\n\n"
                     + $"Lớp học \"{classTitle}\" đã có một số thay đổi quan trọng:\n\n"
                     + string.Join("\n", changes) + "\n\n"
                     + $"🔗 Link tham gia Zoom: {oldEvent.EventZoomLink}\n"
                     + $"🔑 Mật khẩu đăng nhập: {oldEvent.EventPassword}\n";

            if (isTeacher)
            {
                body += $"🔑 Mã Host Key: {hostKey}\n"
                      + "Mã này dùng để chuyển thành quyền chủ trì cuộc họp. Vui lòng không chia sẻ mã này cho bất kỳ ai!\n";
            }

            body += "\nTrân trọng,\nHệ thống quản lý lớp học.";

            message.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync(_smtpServer, _smtpPort, false);
            await client.AuthenticateAsync(_smtpUser, _smtpPass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
    public async Task SendDeletedZoomEmail(Event dataEvent, string recipientEmail, string recipientName, string classTitle, bool isTeacher)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Website bài giảng", _smtpUser));
        message.To.Add(new MailboxAddress(recipientName, recipientEmail));
        message.Subject = $"Thông báo hủy lớp học: {classTitle}";

        var body = $@"
        <p>Chào {recipientName},</p>
        <p>Sự kiện <b>{dataEvent.EventTitle}</b> đã bị hủy.</p>
        <p>📅 Ngày diễn ra: {dataEvent.EventDateStart}</p>
        <p>Chúng tôi xin lỗi vì sự bất tiện này.</p>
        <p>Trân trọng,<br>Hệ thống quản lý lớp học.</p>";

        message.Body = new TextPart("html") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(_smtpServer, _smtpPort, false);
        await client.AuthenticateAsync(_smtpUser, _smtpPass);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
    public async Task<bool> SendEmailAddFeedback(Tuple<string, string> user, string lop, string subject, Feedback feedback)
    {
        try
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("Website bài giảng", _smtpUser));
            email.To.Add(new MailboxAddress(user.Item2, user.Item1));
            email.Subject = subject;

            var body = $"Chào {user.Item2},\n\n"
                        + $"{subject}: \"{lop}\".\n\n"
                        + $"📄 Nội dung đánh giá: {feedback.FeedbackContent}\n"
                        + $"⭐ Số sao đánh giá: {feedback.FeedbackRate}\n"
                        + $"⌚ Thời gian đánh giá: {feedback.FeedbackDate}\n";
            
            email.Body = new TextPart("plain")
            {
                Text = body
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(_smtpServer, _smtpPort, false);
            await client.AuthenticateAsync(_smtpUser, _smtpPass);
            await client.SendAsync(email);
            await client.DisconnectAsync(true);
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
    public async Task<bool> SendEmailUpdateFeedback(Tuple<string, string> user, string lop, string subject, Feedback oldFeedback ,Feedback newFeedback)
    {
        try
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("Website bài giảng", _smtpUser));
            email.To.Add(new MailboxAddress(user.Item2, user.Item1));
            email.Subject = subject;

            var body = $"Chào {user.Item2},\n\n"
                        + $"{subject}: \"{lop}\".\n\n"
                        + $"📄 Nội dung đánh giá: {oldFeedback.FeedbackContent} ➝ {newFeedback.FeedbackContent}\n"
                        + $"⭐ Số sao đánh giá: {oldFeedback.FeedbackRate} ➝ {newFeedback.FeedbackRate}\n"
                        + $"⌚ Thời gian thay đổi đánh giá: {newFeedback.FeedbackDate}\n";

            email.Body = new TextPart("plain")
            {
                Text = body
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(_smtpServer, _smtpPort, false);
            await client.AuthenticateAsync(_smtpUser, _smtpPass);
            await client.SendAsync(email);
            await client.DisconnectAsync(true);
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
    public async Task<bool> SendEmailDeleteFeedback(
        Tuple<string, string> student,
        Tuple<string, string> teacher,
        string className,
        string subject,
        Feedback feedback)
    {
        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_smtpServer, _smtpPort, false);
            await client.AuthenticateAsync(_smtpUser, _smtpPass);

            // Gửi email cho sinh viên
            if (student != null)
            {
                var studentEmail = new MimeMessage();
                studentEmail.From.Add(new MailboxAddress("Website bài giảng", _smtpUser));
                studentEmail.To.Add(new MailboxAddress(student.Item2, student.Item1));
                studentEmail.Subject = subject;

                studentEmail.Body = new TextPart("html")
                {
                    Text = $@"
                <p>Chào {student.Item2},</p>
                <p>Đánh giá của bạn về lớp học <b>{className}</b> đã bị xóa.</p>
                <p>Nội dung đánh giá: {feedback.FeedbackContent}</p>
                <p>Nếu bạn có bất kỳ thắc mắc nào, vui lòng liên hệ với giáo viên phụ trách lớp.</p>
                <p>Trân trọng,<br>Hệ thống quản lý lớp học.</p>"
                };

                await client.SendAsync(studentEmail);
            }

            // Gửi email cho giáo viên
            if (teacher != null)
            {
                var teacherEmail = new MimeMessage();
                teacherEmail.From.Add(new MailboxAddress("Website bài giảng", _smtpUser));
                teacherEmail.To.Add(new MailboxAddress(teacher.Item2, teacher.Item1));
                teacherEmail.Subject = $"Thông báo: Một đánh giá trong lớp {className} đã bị xóa";

                teacherEmail.Body = new TextPart("html")
                {
                    Text = $@"
                <p>Chào {teacher.Item2},</p>
                <p>Một đánh giá trong lớp học <b>{className}</b> đã bị xóa.</p>
                <p>Nội dung đánh giá: {feedback.FeedbackContent}</p>
                <p>Vui lòng kiểm tra lại nếu cần.</p>
                <p>Trân trọng,<br>Hệ thống quản lý lớp học.</p>"
                };

                await client.SendAsync(teacherEmail);
            }

            await client.DisconnectAsync(true);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi gửi email: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SendEmail(string toEmail, string subject, string body)
    {
        try
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("Hệ thống lớp học", _smtpUser));
            email.To.Add(new MailboxAddress("", toEmail));
            email.Subject = subject;
            email.Body = new TextPart("html") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync(_smtpServer, _smtpPort, false);
            await client.AuthenticateAsync(_smtpUser, _smtpPass);
            await client.SendAsync(email);
            await client.DisconnectAsync(true);

            return true;
        }
        catch
        {
            return false;
        }
    }
}
