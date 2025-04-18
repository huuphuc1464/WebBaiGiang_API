using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Imap;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using WebBaiGiangAPI.Data;
using WebBaiGiangAPI.Models;

namespace WebBaiGiangAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MessagesController(AppDbContext context)
        {
            _context = context;
        }

        // Gửi tin nhắn
        // MessageSenderType: student, teacher
        // MessageType: text, file
        // POST: api/messages/send-message
        /*
            MessageSenderId x
            MessageReceiverId x
            MessageType
            MessageSenderType
            MessageDate
            MessageSubject x
            MessageContent x
         */
        [HttpPost("send-message")]
        public async Task<IActionResult> SendMessage([FromForm] int senderId,
            [FromForm] int receiverId,
            [FromForm] string? subject,
            [FromForm] string? content,
            [FromForm] IFormFile? file)
        {
            if (_context.Users.Find(senderId) == null)
            {
                return NotFound(new { message = "Người gửi không tồn tại" });
            }
            if (_context.Users.Find(receiverId) == null)
            {
                return NotFound(new { message = "Người nhận không tồn tại" });
            }
            if (_context.Users.Find(senderId).UsersRoleId == _context.Users.Find(receiverId).UsersRoleId)
            {
                return BadRequest(new { message = "Người gửi và người nhận không thể cùng một loại" });
            }
            if (senderId == receiverId)
            {
                return BadRequest(new { message = "Người gửi và người nhận không thể giống nhau" });
            }
            Message request = new Message();
            request.MessageSenderId = senderId;
            request.MessageReceiverId = receiverId;
            request.MessageSubject = subject?.Trim();
            request.MessageDate = DateTime.Now;
            if (_context.Users.Find(senderId).UsersRoleId == 3)
            {
                request.MessageSenderType = "student";
            }
            else if (_context.Users.Find(senderId).UsersRoleId == 2)
            {
                request.MessageSenderType = "teacher";
            }

            // Kiểm tra có file không
            if (file != null)
            {
                // Kiểm tra file có đúng định dạng không
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".docx", ".doc", ".rar", ".zip", ".pptx", ".ppt" };
                var fileExtension = Path.GetExtension(file.FileName);
                if (!allowedExtensions.Contains(fileExtension.ToLower()))
                {
                    return BadRequest(new { message = "File không hợp lệ" });
                }

                // Kiểm tra kích thước file
                if (file.Length > 10 * 1024 * 1024) // 10MB
                {
                    return BadRequest(new { message = "File quá lớn" });
                }

                // Tao tên file mới để tránh trùng lặp
                var fileName = Path.GetFileNameWithoutExtension(file.FileName);
                var fileExtensionWithoutDot = senderId + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + fileExtension;
                // Đổi tên file
                var newFileName = fileExtensionWithoutDot;
                // Lưu file vào thư mục
                var filePath = Path.Combine("wwwroot", "Message", newFileName);
                // Kiểm tra thư mục Message có tồn tại không
                if (!Directory.Exists(Path.Combine("wwwroot", "Message")))
                {
                    Directory.CreateDirectory(Path.Combine("wwwroot", "Message"));
                }
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                if (!string.IsNullOrEmpty(content))
                {
                    content = content.Trim();
                    request.MessageContent = $"{newFileName} ; {content}";
                    request.MessageType = "file_text";
                }
                else
                {
                    request.MessageContent = $"{newFileName}";
                    request.MessageType = "file";
                }
            }
            else
            {
                // Nếu không có file thì kiểm tra nội dung tin nhắn
                if (string.IsNullOrEmpty(content))
                {
                    return BadRequest(new { message = "Nội dung tin nhắn không được để trống" });
                }
                request.MessageContent = content.Trim();
                request.MessageType = "text";
            }

            _context.Messages.Add(request);
            await _context.SaveChangesAsync();

            return Ok(new { request });
        }

        // Lấy danh sách tin nhắn giữa 2 người dùng theo chủ đề
        [HttpGet("get-messages")]
        public async Task<IActionResult> GetMessages([FromQuery] int senderId, [FromQuery] int receiverId, [FromQuery] string subject)
        {
            if (_context.Users.Find(senderId) == null)
            {
                return NotFound(new { message = "Người gửi không tồn tại" });
            }
            if (_context.Users.Find(receiverId) == null)
            {
                return NotFound(new { message = "Người nhận không tồn tại" });
            }
            if (_context.Users.Find(senderId).UsersRoleId == _context.Users.Find(receiverId).UsersRoleId)
            {
                return BadRequest(new { message = "Người gửi và người nhận không thể cùng một loại" });
            }
            if (senderId == receiverId)
            {
                return BadRequest(new { message = "Người gửi và người nhận không thể giống nhau" });
            }
            if (string.IsNullOrEmpty(subject))
            {
                return BadRequest(new { message = "Chủ đề không được để trống" });
            }
            if (!_context.Messages.Any(m => m.MessageSubject == subject))
            {
                return BadRequest(new { message = "Chủ đề không tồn tại" });
            }
            var messages = await _context.Messages
                .Where(m => ((m.MessageSenderId == senderId && m.MessageReceiverId == receiverId) ||
                            (m.MessageSenderId == receiverId && m.MessageReceiverId == senderId)) &&
                            m.MessageSubject == subject)
                .OrderBy(m => m.MessageDate)
                .ToListAsync();
            if (messages == null || messages.Count == 0)
            {
                return NotFound(new { message = "Không có tin nhắn nào" });
            }
            return Ok(messages);
        }

        // Lấy danh sách tất cả tin nhắn
        [HttpGet("get-all-messages")]
        public async Task<IActionResult> GetAllMessages()
        {
            var messages = await _context.Messages.OrderByDescending(m => m.MessageDate).ToListAsync();
            if (messages == null || messages.Count == 0)
            {
                return NotFound(new { message = "Không có tin nhắn nào" });
            }
            return Ok(messages);
        }

        // Xóa tin nhắn theo id
        [HttpDelete("delete-message-by-id/{id}")]
        public async Task<IActionResult> DeleteMessageById(int id)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message == null)
            {
                return NotFound(new { message = "Tin nhắn không tồn tại" });
            }
            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Xóa tin nhắn thành công" });
        }

        // Xóa tin nhắn giữa 2 người theo chủ đề
        [HttpDelete("delete-messages-by-subject")]
        public async Task<IActionResult> DeleteMessagesBySubject([FromQuery] int senderId, [FromQuery] int receiverId, [FromQuery] string subject)
        {
            if (_context.Users.Find(senderId) == null)
            {
                return NotFound(new { message = "Người gửi không tồn tại" });
            }
            if (_context.Users.Find(receiverId) == null)
            {
                return NotFound(new { message = "Người nhận không tồn tại" });
            }
            if (_context.Users.Find(senderId).UsersRoleId == _context.Users.Find(receiverId).UsersRoleId)
            {
                return BadRequest(new { message = "Người gửi và người nhận không thể cùng một loại" });
            }
            if (senderId == receiverId)
            {
                return BadRequest(new { message = "Người gửi và người nhận không thể giống nhau" });
            }
            if (string.IsNullOrEmpty(subject))
            {
                return BadRequest(new { message = "Chủ đề không được để trống" });
            }
            var messages = await _context.Messages
                .Where(m => ((m.MessageSenderId == senderId && m.MessageReceiverId == receiverId) ||
                            (m.MessageSenderId == receiverId && m.MessageReceiverId == senderId)) &&
                            m.MessageSubject == subject)
                .ToListAsync();
            if (messages == null || messages.Count == 0)
            {
                return NotFound(new { message = "Không có tin nhắn nào" });
            }
            _context.Messages.RemoveRange(messages);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Xóa tin nhắn thành công" });
        }

        // Tìm kiếm tin nhắn của 1 người theo kĩ thuật full-text search
        [HttpGet("search-messages")]
        public async Task<IActionResult> SearchMessages([FromQuery] int userId, [FromQuery] string keyword)
        {
            if (_context.Users.Find(userId) == null)
            {
                return NotFound(new { message = "Người dùng không tồn tại" });
            }
            if (string.IsNullOrEmpty(keyword))
            {
                return BadRequest(new { message = "Từ khóa không được để trống" });
            }

            var keywords = keyword?.ToLower().Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? new string[0];

            var messages = await _context.Messages
                .Where(m => (m.MessageSenderId == userId || m.MessageReceiverId == userId) &&
                            (keywords.Length == 0 || keywords.Any(kw =>
                                (m.MessageSubject != null && m.MessageSubject.ToLower().Contains(kw)) ||
                                (m.MessageContent != null && m.MessageContent.ToLower().Contains(kw))
                            )))
                .OrderByDescending(m => m.MessageDate)
                .ToListAsync();
            if (messages == null || messages.Count == 0)
            {
                return NotFound(new { message = "Không có tin nhắn nào" });
            }
            return Ok(messages);
        }

        // Thống kê tin nhắn group by người gửi, người nhận, số lượng và danh sách tin nhắn
        [HttpGet("statistic-messages")]
        public async Task<IActionResult> StatisticMessages([FromQuery] int userId)
        {
            if (_context.Users.Find(userId) == null)
            {
                return NotFound(new { message = "Người dùng không tồn tại" });
            }

            var groupedMessages = await _context.Messages
                .Where(m => m.MessageSenderId == userId || m.MessageReceiverId == userId)
                .AsNoTracking()
                .ToListAsync();

            var groupedResult = groupedMessages
                .GroupBy(m =>
                {
                    var userA = Math.Min((int)m.MessageSenderId!, (int)m.MessageReceiverId!);
                    var userB = Math.Max((int)m.MessageSenderId!, (int)m.MessageReceiverId!);
                    return new { UserA = userA, UserB = userB, m.MessageSubject };
                })
                .Select(g => new
                {
                    UserAId = g.Key.UserA,
                    UserBId = g.Key.UserB,
                    MessageSubject = g.Key.MessageSubject,
                    Count = g.Count(),
                    Messages = g.ToList()
                })
                .ToList();

            // Lấy danh sách tất cả ID để truy vấn tên người gửi và người nhận
            var userIds = groupedMessages.Select(m => m.MessageSenderId)
                .Union(groupedMessages.Select(m => m.MessageReceiverId))
                .Distinct()
                .ToList();

            // Lấy tên người gửi và người nhận từ bảng Users
            var userDict = await _context.Users
                .Where(u => userIds.Contains(u.UsersId))
                .ToDictionaryAsync(u => u.UsersId, u => u.UsersName);

            // Tạo kết quả theo định dạng yêu cầu
            var finalResult = groupedResult.Select(g => new
            {
                MessageSubject = g.MessageSubject,
                Count = g.Count,
                Messages = g.Messages.Select(m => new
                {
                    MessageId = m.MessageId,
                    MessageSenderName = userDict[m.MessageSenderId], // Người gửi (thay bằng tên)
                    MessageReceiverName = userDict[m.MessageReceiverId], // Người nhận (thay bằng tên)
                    MessageType = m.MessageType,
                    MessageSenderType = m.MessageSenderType,
                    MessageDate = m.MessageDate,
                    MessageContent = m.MessageContent
                }).ToList()
            }).ToList();

            if (!finalResult.Any())
            {
                return NotFound(new { message = "Không có tin nhắn nào" });
            }

            return Ok(finalResult);
        }

        // Tạo báo cáo tin nhắn (xuất file excel từ api json hàm StatisticMessages)
        [HttpGet("export-message")]
        public async Task<IActionResult> ExportMessage([FromQuery] int userId)
        {
            var result = await StatisticMessages(userId) as OkObjectResult;
            if (result?.Value is not IEnumerable<dynamic> statisticsData || !statisticsData.Any())
                return NotFound("Không có dữ liệu để xuất.");

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add($"User_{userId}");

            var headers = new[] { "Người gửi", "Người nhận", "Chủ đề", "Số lượng", "Nội dung tin nhắn", "File", "Thời gian gửi" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                worksheet.Cells[1, i + 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[1, i + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            int row = 2;
            foreach (var item in statisticsData)
            {
                string subject = item.MessageSubject;
                int count = item.Count;
                var messages = item.Messages;

                foreach (var message in messages)
                {
                    worksheet.Cells[row, 1].Value = message.MessageSenderName;
                    worksheet.Cells[row, 2].Value = message.MessageReceiverName;
                    worksheet.Cells[row, 5].Value = message.MessageContent?.Contains(";") == true
                        ? message.MessageContent.Split(';')[1].Trim()
                        : message.MessageContent;
                    worksheet.Cells[row, 6].Value = message.MessageContent?.Contains(";") == true
                        ? message.MessageContent.Split(';')[0]
                        : null;
                    worksheet.Cells[row, 7].Value = Convert.ToDateTime(message.MessageDate).ToString("dd/MM/yyyy HH:mm:ss");

                    if (messages.IndexOf(message) == 0)
                    {
                        worksheet.Cells[row, 3].Value = subject;
                        worksheet.Cells[row, 4].Value = count;
    
                        if (messages.Count > 1)
                        {
                            worksheet.Cells[row, 3, row + messages.Count - 1, 3].Merge = true;
                            worksheet.Cells[row, 4, row + messages.Count - 1, 4].Merge = true;
                            worksheet.Cells[row, 3, row + messages.Count - 1, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            worksheet.Cells[row, 3, row + messages.Count - 1, 4].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        }
                    }

                    // Sau khi ghi dữ liệu cho tất cả các cột, gọi AutoFit một lần nữa cho tất cả cột
                    for (int col = 1; col <= headers.Length; col++)
                    {
                        worksheet.Cells[row, col].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        if (col == 3 || col == 4)
                        {
                            worksheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        }
                        worksheet.Cells[row, col].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        // Chỉ gọi AutoFit sau khi đã ghi dữ liệu và merge
                        worksheet.Column(col).AutoFit();
                    }

                    row++;

                }
            }

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"Messages_{userId}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            return File(stream, contentType, fileName);
        }
    }
}
