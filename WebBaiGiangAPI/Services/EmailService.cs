using MailKit.Net.Smtp;
using MimeKit;
using System.Threading.Tasks;

public class EmailService
{
    private readonly string _smtpServer = "smtp.gmail.com";
    private readonly int _smtpPort = 587;
    private readonly string _smtpUser = "nttd171717@gmail.com"; // Thay bằng email của bạn
    private readonly string _smtpPass = "ywxb zcay jnik cxcw";

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

}
