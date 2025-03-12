using MailKit.Net.Smtp;
using MimeKit;
using System.Threading.Tasks;

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

}
