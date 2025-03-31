using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.EntityFrameworkCore;
using WebBaiGiangAPI.Data;
using WebBaiGiangAPI.Models;

namespace WebBaiGiangAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public FilesController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // Lấy danh sách file
        [HttpGet("get-files")]
        public async Task<IActionResult> GetFiles()
        {
            var files = await _context.Files.ToListAsync();
            if (files == null)
            {
                return NotFound("Không có file nào.");
            }
            return Ok(files);
        }

        // Tải lên file
        [DisableRequestSizeLimit]
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(
            [FromForm] IFormFile file, 
            [FromForm] string title, 
            [FromForm] int classId, 
            [FromForm] int teacherId, 
            [FromForm] string? description)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Vui lòng chọn một file.");
            }

            if (string.IsNullOrWhiteSpace(title) || classId <= 0 || teacherId <= 0)
            {
                return BadRequest("Vui lòng nhập đầy đủ thông tin.");
            }
            if (_context.Classes.Find(classId) == null)
            {
                return BadRequest("Lớp học không tồn tại.");
            }
            if (_context.Users.Where(t => t.UsersId == teacherId && t.UsersRoleId == 2).FirstOrDefault() == null)
            {
                return BadRequest("Giáo viên không tồn tại.");
            }
            if (_context.TeacherClasses.Where(tc => tc.TcUsersId == teacherId && tc.ClassCourses.ClassId == classId).FirstOrDefault() == null)
            {
                return BadRequest("Giáo viên không dạy lớp này.");
            }
            var videoExtensions = new HashSet<string> { ".mp4", ".avi", ".mov", ".mkv", ".flv", ".wmv", ".webm" };

            // Lấy phần mở rộng file
            string fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            // Kiểm tra giới hạn dung lượng
            long maxSize = videoExtensions.Contains(fileExtension) ? 100 * 1024 * 1024 : 10 * 1024 * 1024; // 100MB cho video, 10MB cho file khác
            if (file.Length > maxSize)
            {
                return BadRequest($"File quá lớn! {(videoExtensions.Contains(fileExtension) ? "Video" : $"File {fileExtension}")} không được vượt quá {(maxSize / (1024 * 1024))}MB.");
            }
            string uploadPath = Path.Combine(_environment.WebRootPath, "files");

            // Kiểm tra nếu thư mục không tồn tại thì tạo mới
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            string uniqueFileName = file.FileName;
            string filePath = Path.Combine(uploadPath, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            var className = _context.Classes.Where(c => c.ClassId == classId).Select(c => c.ClassTitle).FirstOrDefault();
            var teacherName = _context.Users.Where(u => u.UsersId == teacherId).Select(u => u.UsersName).FirstOrDefault();
            title = Regex.Replace(title.Trim(), @"\s+", " ");
            description = Regex.Replace(description.Trim(), @"\s+", " ");
            var fileModel = new Files
            {
                FilesTitle = title,
                FilesClassId = classId,
                FilesTeacherId = teacherId,
                FilesFilename = uniqueFileName,
                FilesDescription = description
            };
            var announcement = new Announcement
            {
                AnnouncementClassId = classId,
                AnnouncementTeacherId = teacherId,
                AnnouncementTitle = $"Giáo viên {teacherName} đã thêm file mới trong lớp {className}",
                AnnouncementDescription = $"Giáo viên {teacherName} đã thêm file {uniqueFileName} vào lớp {className} với chủ đề {title}",
                AnnouncementDate = DateTime.Now,
            };
            _context.Files.Add(fileModel);
            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            return Ok(new { message = "File đã tải lên thành công.", fileId = fileModel });
        }

        // Tải lên nhiều file
        [DisableRequestSizeLimit]
        [HttpPost("upload/multiple")]
        public async Task<IActionResult> UploadMultipleFiles(
            [FromForm] List<IFormFile> files,
            [FromForm] string title,
            [FromForm] int classId,
            [FromForm] int teacherId,
            [FromForm] string? description)
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest("Vui lòng chọn ít nhất một file.");
            }

            List<object> uploadedFiles = new List<object>();
            List<object> errors = new List<object>();

            foreach (var file in files)
            {
                var result = await UploadFile(file, title, classId, teacherId, description) as ObjectResult;

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


        // Xem thông tin file
        [HttpGet("get-file/{id}")]
        public async Task<IActionResult> GetFile(int id)
        {
            var file = _context.Files
            .Join(_context.Classes,
                f => f.FilesClassId,
                c => c.ClassId,
                (f, c) => new { f, c })
            .Join(_context.Users,
                fc => fc.f.FilesTeacherId,
                t => t.UsersId,
                (fc, t) => new
                {
                    fc.f.FilesId,
                    fc.f.FilesTitle,
                    fc.f.FilesClassId,
                    fc.f.FilesTeacherId,
                    fc.f.FilesFilename,
                    fc.f.FilesDescription,
                    ClassName = fc.c.ClassTitle,
                    TeacherName = t.UsersName
                })
            .FirstOrDefault(f => f.FilesId == id);
            if (file == null) return NotFound("Không tìm thấy file.");
            return Ok(file);
        }

        // Tải xuống file
        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadFile(int id)
        {
            var file = await _context.Files.FindAsync(id);
            if (file == null) return NotFound("Không tìm thấy file.");

            var filePath = Path.Combine(_environment.WebRootPath, "files", file.FilesFilename);
            if (!System.IO.File.Exists(filePath)) return NotFound("File không tồn tại.");

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return File(memory, "application/octet-stream", file.FilesFilename);
        }

        // Xóa file
        [HttpDelete("delete-file/{id}")]
        public async Task<IActionResult> DeleteFile(int id)
        {
            var file = await _context.Files.FindAsync(id);
            if (file == null) return NotFound("Không tìm thấy file.");

            var filePath = Path.Combine(_environment.WebRootPath, "files", file.FilesFilename);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            _context.Files.Remove(file);
            await _context.SaveChangesAsync();

            return Ok("File đã được xóa.");
        }

        // Update file
        [DisableRequestSizeLimit]
        [HttpPost("update/{id}")]
        public async Task<IActionResult> UpdateFile(
            int id,
            [FromForm] IFormFile? file,
            [FromForm] string? title,
            [FromForm] string? description)
        {
            var existingFile = await _context.Files.FindAsync(id);
            if (existingFile == null)
            {
                return NotFound("File không tồn tại.");
            }

            string uploadPath = Path.Combine(_environment.WebRootPath, "files");
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            string newFileName = existingFile.FilesFilename; // Giữ nguyên file cũ nếu không tải file mới

            if (file != null && file.Length > 0)
            {
                var videoExtensions = new HashSet<string> { ".mp4", ".avi", ".mov", ".mkv", ".flv", ".wmv", ".webm" };
                string fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

                long maxSize = videoExtensions.Contains(fileExtension) ? 100 * 1024 * 1024 : 5 * 1024 * 1024; // 100MB cho video, 5MB cho file khác
                if (file.Length > maxSize)
                {
                    return BadRequest($"File quá lớn! {(videoExtensions.Contains(fileExtension) ? "Video" : $"File {fileExtension}")} không được vượt quá {(maxSize / (1024 * 1024))}MB.");
                }

                // Xóa file cũ nếu có file mới
                string oldFilePath = Path.Combine(uploadPath, existingFile.FilesFilename);
                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }

                // Lưu file mới
                newFileName = file.FileName;
                string newFilePath = Path.Combine(uploadPath, newFileName);
                using (var stream = new FileStream(newFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
            }

            var className = _context.Classes.Where(c => c.ClassId == existingFile.FilesClassId).Select(c => c.ClassTitle).FirstOrDefault();
            var teacherName = _context.Users.Where(u => u.UsersId == existingFile.FilesTeacherId).Select(u => u.UsersName).FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(title))
            {
                existingFile.FilesTitle = Regex.Replace(title.Trim(), @"\s+", " ");
            }
            if (!string.IsNullOrWhiteSpace(description))
            {
                existingFile.FilesDescription = Regex.Replace(description.Trim(), @"\s+", " ");
            }

            existingFile.FilesFilename = newFileName;
            var announcement = new Announcement
            {
                AnnouncementClassId = existingFile.FilesClassId,
                AnnouncementTeacherId = existingFile.FilesTeacherId,
                AnnouncementTitle = $"Giáo viên {teacherName} đã cập nhật file trong lớp {className}",
                AnnouncementDescription = $"File {newFileName} trong lớp {className} đã được cập nhật.",
                AnnouncementDate = DateTime.Now,
            };

            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            return Ok(new { message = "File đã cập nhật thành công.", fileId = existingFile.FilesId });
        }
    }
}
