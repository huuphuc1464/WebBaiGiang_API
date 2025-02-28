using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public class HocPhanController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HocPhanController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/HocPhan
        [HttpGet]
        public async Task<ActionResult<IEnumerable<HocPhan>>> GetHocPhans()
        {
            return await _context.HocPhans.ToListAsync();
        }

        // GET: api/HocPhan/5
        [HttpGet("{id}")]
        public async Task<ActionResult<HocPhan>> GetHocPhan(string id)
        {
            var hocPhan = await _context.HocPhans.FindAsync(id);

            if (hocPhan == null)
            {
                return NotFound();
            }

            return hocPhan;
        }

        // PUT: api/HocPhan/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutHocPhan(string id, HocPhan hocPhan)
        {
            if (id != hocPhan.MaHocPhan)
            {
                return BadRequest();
            }

            _context.Entry(hocPhan).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!HocPhanExists(id))
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

        // POST: api/HocPhan
        [HttpPost]
        [Route("/api/HocPhan/ThemHocPhan")]
        public async Task<IActionResult> ThemHocPhan([FromForm]HocPhanDTO hocPhan, [FromForm]IFormFile anhDaiDien)
        {
            var maxMaHocPhan = await _context.HocPhans
                .OrderByDescending(nd => nd.MaHocPhan)
                .Select(nd => nd.MaHocPhan)
                .FirstOrDefaultAsync();
            var newMaHocPhan = "";
            if (maxMaHocPhan != null)
            {
                // Lấy phần số từ chuỗi
                var numberPart = int.Parse(maxMaHocPhan.Substring(2)); 
                // Tăng lên 1 và tạo mã mới 
                newMaHocPhan = "HP" + (numberPart + 1).ToString("D1");
            }
            else
            {
                // Nếu bảng rỗng, đặt giá trị mặc định
                newMaHocPhan = "HP1";
            }
            
            // Kiểm tra dữ liệu theo validate
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return BadRequest(new
                {
                    message = "Dữ liệu nhập vào không hợp lệ",
                    title = "One or more validation errors occurred.",
                    status = 400,
                    errors,
                    traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
                });
            }

            var existUser = _context.NguoiDungs.Any(u => u.MaNguoiDung ==  hocPhan.MaGiangVien);
            if (!existUser)
            {
                return Unauthorized(new {  
                    message = "Người dùng không tồn tại",
                    data = hocPhan,
                });
            }
            var existBoMon = _context.BoMons.Any(u => u.MaBoMon == hocPhan.MaBoMon);
            if (!existBoMon)
            {
                return Unauthorized(new {
                    message = "Bộ môn không tồn tại",
                    data = hocPhan
                });
            }
            
            HocPhan hp = new HocPhan();
            hp.MaHocPhan = newMaHocPhan.ToUpper();
            hp.MaBoMon = hocPhan.MaBoMon.ToUpper();
            hp.MaGiangVien = hocPhan.MaGiangVien.ToUpper();
            hp.TenHocPhan = Regex.Replace(hocPhan.TenHocPhan.Trim(), @"\s+", " ");
            hp.MoTaNgan = Regex.Replace(hocPhan.MoTaNgan.Trim(), @"\s+", " ");
            hp.MoTaChiTiet = Regex.Replace(hocPhan.MoTaChiTiet.Trim(), @"\s+", " ");
            hp.DiemDanhGia = hocPhan.DiemDanhGia;
            hp.SoLuongSinhVien = hocPhan.SoLuongSinhVien;
            hp.LanCapNhatCuoi = DateTime.Now;
            hp.NgayBatDau = hocPhan.NgayBatDau;
            hp.HinhThucHoc = Regex.Replace(hocPhan.HinhThucHoc.Trim(), @"\s+", " ");
            hp.NoiDung = Regex.Replace(hocPhan.NoiDung.Trim(), @"\s+", " ");
            hp.SoTiet = hocPhan.SoTiet;
            hp.SoTinChi = hocPhan.SoTinChi;
            hp.LoaiHocPhan = Regex.Replace(hocPhan.LoaiHocPhan.Trim(), @"\s+", " ");
            hp.TrangThai = hocPhan.TrangThai;
            //Xử lý upload ảnh
            if (anhDaiDien != null && anhDaiDien.Length > 0)
            {
                // Kiểm tra loại file (chỉ chấp nhận ảnh PNG, JPG, JPEG)
                var allowedExtensions = new[] { ".png", ".jpg", ".jpeg" };
                var fileExtension = Path.GetExtension(anhDaiDien.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { message = "Chỉ chấp nhận file ảnh định dạng PNG, JPG, JPEG.", data = hocPhan });
                }

                // Tạo tên file duy nhất
                string uniqueFileName = $"{hp.MaHocPhan}{fileExtension}";

                // Đường dẫn thư mục lưu file
                string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "HocPhan");

                // Tạo thư mục nếu chưa tồn tại
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                // Đường dẫn đầy đủ của file
                string filePath = Path.Combine(uploadPath, uniqueFileName);

                // Lưu file vào local
                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await anhDaiDien.CopyToAsync(stream);
                    }
                    // Gán đường dẫn file cho thuộc tính AnhDaiDien của nguoiDung
                    hp.AnhDaiDien = uniqueFileName;
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "Lỗi khi lưu ảnh.", error = ex.Message });
                }
            }
            _context.HocPhans.Add(hp);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (HocPhanExists(hocPhan.MaHocPhan))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetHocPhan", new { id = hp.MaHocPhan }, hp);
        }

        // DELETE: api/HocPhan/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHocPhan(string id)
        {
            var hocPhan = await _context.HocPhans.FindAsync(id);
            if (hocPhan == null)
            {
                return NotFound();
            }

            _context.HocPhans.Remove(hocPhan);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool HocPhanExists(string id)
        {
            return _context.HocPhans.Any(e => e.MaHocPhan == id);
        }
    }
}
