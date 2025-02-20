using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBaiGiangAPI.Data;
using WebBaiGiangAPI.Models;

namespace WebBaiGiangAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ThongTinWebController : ControllerBase
    {
        private readonly AppDbContext _context;
        private string patternEmail = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        private string patternSDT = @"^(0)[1-9][0-9]{8}$";
        private string patternWebsite = @"^https?:\/\/(www\.)?[a-zA-Z0-9\-]+(\.[a-zA-Z]{2,})+\/?([a-zA-Z0-9#?&%=_\-\/.]*)?$";
        private string patternGmail = @"^[a-zA-Z0-9._%+-]+@gmail\.com$";
        private string patternFb = @"^https?:\/\/(www\.)?facebook\.com\/[a-zA-Z0-9.]+\/?$";
        private string patternFax = @"^\+?[0-9]{1,4}(\s|-)?(\(?[0-9]{1,4}\)?(\s|-)?|)?[0-9]{3,10}(\s?x[0-9]{1,5})?$";
        public ThongTinWebController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/ThongTinWeb
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ThongTinWeb>>> GetThongTinWebs()
        {
            return await _context.ThongTinWebs.ToListAsync();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> DoiThongTinWeb(int id, string maND, [FromForm]ThongTinWebDTO thongTinWeb, [FromForm]IFormFile? logoDoi)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (!Regex.IsMatch(thongTinWeb.Email, patternEmail)) return BadRequest(new
            {
                message = "Email không hợp lệ",
                data = thongTinWeb
            });
            if (!Regex.IsMatch(thongTinWeb.SDT, patternSDT)) return BadRequest(new
            {
                message = "Số điện thoại không hợp lệ",
                data = thongTinWeb
            });
            if(!Regex.IsMatch(thongTinWeb.Website, patternWebsite)) return BadRequest(new
            {
                message = "Đường dẫn website không hợp lệ",
                data = thongTinWeb
            });
            if (!Regex.IsMatch(thongTinWeb.Gmail, patternGmail)) return BadRequest(new
            {
                message = "Đường dẫn gmail không hợp lệ",
                data = thongTinWeb
            });
            if (!Regex.IsMatch(thongTinWeb.Facebook, patternFb)) return BadRequest(new
            {
                message = "Đường dẫn Facebook không hợp lệ",
                data = thongTinWeb
            });
            if (!Regex.IsMatch(thongTinWeb.Fax, patternFax)) return BadRequest(new
            {
                message = "Số fax không hợp lệ",
                data = thongTinWeb
            });
            thongTinWeb.TenWeb = Regex.Replace(thongTinWeb.TenWeb.Trim(), @"\s+", " ");
            thongTinWeb.DiaChi = Regex.Replace(thongTinWeb.DiaChi.Trim(), @"\s+", " ");
            thongTinWeb.Facebook = Regex.Replace(thongTinWeb.Facebook.Trim(), @"\s+", " ");
            thongTinWeb.Website = Regex.Replace(thongTinWeb.Website.Trim(), @"\s+", " ");
            var tt = await _context.ThongTinWebs.SingleOrDefaultAsync(u => u.Id == id);
            if (tt == null)
            {
                return Unauthorized(new
                {
                    message = "Thông tin web không tồn tại",
                });
            }
            //Xử lý upload ảnh
            if (logoDoi != null && logoDoi.Length > 0)
            {
                // Kiểm tra loại file (chỉ chấp nhận ảnh PNG, JPG, JPEG)
                var allowedExtensions = new[] { ".png", ".jpg", ".jpeg" };
                var fileExtension = Path.GetExtension(logoDoi.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { message = "Chỉ chấp nhận file ảnh định dạng PNG, JPG, JPEG." });
                }

                // Tạo tên file duy nhất
                string uniqueFileName = $"logo{fileExtension}";

                // Đường dẫn thư mục lưu file
                string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "LogoWeb");

                // Tạo thư mục nếu chưa tồn tại
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                // Đường dẫn đầy đủ của file
                string filePath = Path.Combine(uploadPath, uniqueFileName);

                // Kiểm tra và xóa ảnh cũ nếu tồn tại
                if (!string.IsNullOrEmpty(tt.Logo))
                {
                    string oldFilePath = Path.Combine(uploadPath, tt.Logo);
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        try
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                        catch (Exception ex)
                        {
                            return StatusCode(500, new { message = "Lỗi khi xóa ảnh cũ.", error = ex.Message });
                        }
                    }
                }
                // Lưu file vào local
                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await logoDoi.CopyToAsync(stream);
                    }
                    // Gán đường dẫn file cho thuộc tính AnhDaiDien của nguoiDung
                    tt.Logo = uniqueFileName;
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "Lỗi khi lưu ảnh.", error = ex.Message });
                }
            }

            tt.TenWeb = thongTinWeb.TenWeb;
            tt.Website = thongTinWeb.Website;
            tt.DiaChi = thongTinWeb.DiaChi;
            tt.SDT = thongTinWeb.SDT; 
            tt.Email = thongTinWeb.Email;
            tt.Facebook = thongTinWeb.Facebook;
            tt.Gmail = thongTinWeb.Gmail;
            tt.Fax = thongTinWeb.Fax;
            tt.MaNguoiThayDoiCuoi = maND;
            tt.ThoiGianThayDoiCuoi = DateTime.Now;
            //_context.Entry(thongTinWeb).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ThongTinWebExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        private bool ThongTinWebExists(int id)
        {
            return _context.ThongTinWebs.Any(e => e.Id == id);
        }
    }
}
