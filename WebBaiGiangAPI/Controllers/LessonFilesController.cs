using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailKit.Net.Imap;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using OfficeOpenXml;
using WebBaiGiangAPI.Data;
using WebBaiGiangAPI.Models;

namespace WebBaiGiangAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LessonFilesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;

        public LessonFilesController(AppDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // Upload file cho bài giảng
        [HttpPost("{lessonId}/upload")]
        public async Task<IActionResult> UploadFile(int lessonId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File không hợp lệ.");

            var lesson = await _context.Lessons.FindAsync(lessonId);
            if (lesson == null)
                return NotFound("Bài giảng không tồn tại.");

            string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "LessonFiles");

            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            // Xác định loại file & kích thước tối đa
            string fileExtension = Path.GetExtension(file.FileName).ToLower();
            string fileType = "File";
            long maxSize = 10 * 1024 * 1024; // 10MB mặc định

            var imageFormats = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp", ".svg" };
            var videoFormats = new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".mkv", ".webm", ".m4v" };
            var audioFormats = new[] { ".mp3", ".wav", ".aac", ".ogg", ".flac", ".m4a", ".opus" };
            var documentFormats = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".odt", ".ods", ".odp" };
            var archiveFormats = new[] { ".zip", ".rar", ".7z", ".tar", ".gz", ".xz", ".bz2", ".iso" };
            var codeFormats = new[] { ".html", ".css", ".js", ".json", ".xml", ".sql", ".md" };

            if (imageFormats.Contains(fileExtension))
                fileType = "Hình ảnh";
            else if (videoFormats.Contains(fileExtension))
            {
                fileType = "Video";
                maxSize = 100 * 1024 * 1024; // 100MB cho video
            }
            else if (audioFormats.Contains(fileExtension))
                fileType = "Âm thanh";
            else if (documentFormats.Contains(fileExtension))
                fileType = "Tài liệu";
            else if (archiveFormats.Contains(fileExtension))
                fileType = "File nén";
            else if (codeFormats.Contains(fileExtension))
                fileType = "Mã nguồn";
            else
                return BadRequest("Định dạng file không được hỗ trợ.");

            // Kiểm tra kích thước file
            if (file.Length > maxSize)
                return BadRequest($"Dung lượng file vượt quá giới hạn ({maxSize / (1024 * 1024)}MB).");
            string teacherName = await _context.Users.Where(u => u.UsersId == lesson.LessonTeacherId).Select(u => u.UsersName).FirstOrDefaultAsync();

            var classInfo = await _context.Classes
                .Join(_context.ClassCourses,
                      c => c.ClassId,
                      cc => cc.ClassId,
                      (c, cc) => new { c.ClassTitle, cc.CcId, c.ClassId })
                .Where(x => x.CcId == lesson.LessonClassCourseId)
                .Select(x => new { x.ClassTitle, x.ClassId })
                .FirstOrDefaultAsync();

            if (classInfo == null)
            {
                return NotFound("Lớp học không tồn tại");
            }
           
            string className = classInfo.ClassTitle;
            int classId = classInfo.ClassId;

            // Tạo tên file mới: thời gian + tên gốc
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string newFileName = $"{timestamp}_{Path.GetFileNameWithoutExtension(file.FileName)}{fileExtension}";

            string filePath = Path.Combine(uploadFolder, newFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Lưu vào database
            var lessonFile = new LessonFile
            {
                LfLessonId = lessonId,
                LfPath = newFileName,
                LfType = fileType
            };

            var announcement = new Announcement
            {
                AnnouncementClassId = classId,
                AnnouncementTeacherId = lesson.LessonTeacherId,
                AnnouncementTitle = $"Giáo viên: \"{teacherName}\" đã thêm file mới trong lớp: \"{className}\"",
                AnnouncementDescription = $"Giáo viên: \"{teacherName}\" đã thêm file: \"{newFileName}\" vào bài giảng: \"{lesson.LessonName}\" chương: \"{lesson.LessonChapter}\" ",
                AnnouncementDate = DateTime.Now,
            };

            lesson.LessonUpdateAt = DateTime.Now;
            _context.Lessons.Update(lesson);
            _context.Announcements.Add(announcement);
            _context.LessonFiles.Add(lessonFile);
            await _context.SaveChangesAsync();

            return Ok(lessonFile);
        }

        // Upload nhiều file cho bài giảng
        [HttpPost("{lessonId}/uploadMultiple")]
        public async Task<IActionResult> UploadMultipleFiles(int lessonId, [FromForm] List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest("Vui lòng chọn ít nhất một file.");
            }

            List<object> uploadedFiles = new List<object>();
            List<object> errors = new List<object>();

            foreach (var file in files)
            {
                var result = await UploadFile(lessonId, file) as ObjectResult;

                if (result.StatusCode == 200)
                {
                    uploadedFiles.Add(result.Value);
                }
                else
                {
                    errors.Add(new { fileName = file.FileName, error = result.Value });
                }
            }

            // Nếu không có file nào được tải lên thành công -> Thông báo thất bại
            if (uploadedFiles.Count == 0)
            {
                return BadRequest(new
                {
                    message = "Tất cả các file đều bị lỗi. Tải lên thất bại!",
                    errors = errors
                });
            }

            // Nếu có ít nhất một file thành công nhưng cũng có lỗi -> Thông báo thành công nhưng kèm lỗi
            return Ok(new
            {
                message = "Quá trình tải lên hoàn tất.",
                uploaded = uploadedFiles,
                errors = errors.Count > 0 ? errors : null 
            });
        }

        // Download file cho bài giảng
        [HttpGet("download/{fileId}")]
        public async Task<IActionResult> DownloadFile(int fileId)
        {
            var fileRecord = await _context.LessonFiles.FindAsync(fileId);
            if (fileRecord == null) return NotFound("File không tồn tại.");

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "LessonFiles", fileRecord.LfPath);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("File không tồn tại trên máy chủ.");
            }

            // Xóa timestamp khỏi tên file
            string fileName = Path.GetFileNameWithoutExtension(fileRecord.LfPath);
            string fileExtension = Path.GetExtension(fileRecord.LfPath);
            string pattern = @"^\d{14}_"; // YYYYMMDDHHMMSS_
            fileName = System.Text.RegularExpressions.Regex.Replace(fileName, pattern, "");

            byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, "application/octet-stream", fileName + fileExtension);
        }

        // Xóa file cho bài giảng
        [HttpDelete("delete-file/{id}")]
        public async Task<IActionResult> DeleteFile(int id)
        {
            var file = await _context.LessonFiles.FindAsync(id);
            if (file == null) return NotFound("Không tìm thấy file.");
            var lesson = await _context.Lessons.FindAsync(file.LfLessonId);
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "LessonFiles", file.LfPath);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
            lesson.LessonUpdateAt = DateTime.Now;
            _context.Lessons.Update(lesson);
            _context.LessonFiles.Remove(file);
            await _context.SaveChangesAsync();

            return Ok("File đã được xóa.");
        }

        // Lay danh sach file cua bai giang
        [HttpGet("get-file/{lessonId}")]
        public async Task<IActionResult> GetFile(int lessonId)
        {
            var lesson = await _context.Lessons.FindAsync(lessonId);
            if (lesson == null) return NotFound("Không tìm thấy bài giảng.");

            var lessonFiles = await _context.LessonFiles.Where(lf => lf.LfLessonId == lessonId).ToListAsync();
            
            if (lessonFiles == null || lessonFiles.Count == 0)
            {
                return NotFound("Không có file nào cho bài giảng này.");
            }
            return Ok(lessonFiles);
        }

        // Thống kê số lượng file tất cả bài giảng theo định dạng
        [HttpGet("statistic-file")]
        public async Task<IActionResult> StatisticsFile()
        {
            var imageFormats = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp", ".svg" };
            var videoFormats = new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".mkv", ".webm", ".m4v" };
            var audioFormats = new[] { ".mp3", ".wav", ".aac", ".ogg", ".flac", ".m4a", ".opus" };
            var documentFormats = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".odt", ".ods", ".odp" };
            var archiveFormats = new[] { ".zip", ".rar", ".7z", ".tar", ".gz", ".xz", ".bz2", ".iso" };
            var codeFormats = new[] { ".html", ".css", ".js", ".json", ".xml", ".sql", ".md" };

            var files = _context.LessonFiles.AsEnumerable() 
                .Select(f => new
                {
                    FileExtension = Path.GetExtension(f.LfPath).ToLower(),
                    FileId = f.LfId
                })
                .GroupBy(f =>
                    imageFormats.Contains(f.FileExtension) ? "Hình ảnh" :
                    videoFormats.Contains(f.FileExtension) ? "Video" :
                    audioFormats.Contains(f.FileExtension) ? "Âm thanh" :
                    documentFormats.Contains(f.FileExtension) ? "Tài liệu" :
                    archiveFormats.Contains(f.FileExtension) ? "File nén" :
                    codeFormats.Contains(f.FileExtension) ? "Mã nguồn" : "Khác")
                .Select(group => new
                {
                    FileTypeGroup = group.Key,
                    FileCount = group.Count()
                })
                .ToList();

            return Ok(files);
        }

        // Thống kê số lượng file bài giảng theo định dạng theo lớp
        [HttpGet("statistic-file-by-class/{classId}")]
        public async Task<IActionResult> StatisticsFileByClass(int classId)
        {
            var imageFormats = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp", ".svg" };
            var videoFormats = new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".mkv", ".webm", ".m4v" };
            var audioFormats = new[] { ".mp3", ".wav", ".aac", ".ogg", ".flac", ".m4a", ".opus" };
            var documentFormats = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".odt", ".ods", ".odp" };
            var archiveFormats = new[] { ".zip", ".rar", ".7z", ".tar", ".gz", ".xz", ".bz2", ".iso" };
            var codeFormats = new[] { ".html", ".css", ".js", ".json", ".xml", ".sql", ".md" };

            var files = _context.LessonFiles
                .Where(lf => lf.Lesson.ClassCourse.ClassId == classId)
                .AsEnumerable()
                .Select(f => new
                {
                    FileExtension = Path.GetExtension(f.LfPath).ToLower(),
                    FileId = f.LfId
                })
                .GroupBy(f =>
                    imageFormats.Contains(f.FileExtension) ? "Hình ảnh" :
                    videoFormats.Contains(f.FileExtension) ? "Video" :
                    audioFormats.Contains(f.FileExtension) ? "Âm thanh" :
                    documentFormats.Contains(f.FileExtension) ? "Tài liệu" :
                    archiveFormats.Contains(f.FileExtension) ? "File nén" :
                    codeFormats.Contains(f.FileExtension) ? "Mã nguồn" : "Khác")
                .Select(group => new
                {
                    FileTypeGroup = group.Key,
                    FileCount = group.Count()
                })
                .ToList();
            if (!files.Any())
            {
                return NotFound("Không có file nào cho lớp này.");
            }
            return Ok(files);
        }

    }
}
