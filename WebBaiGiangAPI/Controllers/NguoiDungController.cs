using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBaiGiangAPI.Data;
using WebBaiGiangAPI.Models;

namespace WebBaiGiangAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NguoiDungController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NguoiDungController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/NguoiDung
        //[HttpGet]
        //public async Task<ActionResult<IEnumerable<NguoiDung>>> GetNguoiDung()
        //{
        //    try
        //    {
        //        var nguoiDungs = await _context.NguoiDungs.ToListAsync();

        //        if (!nguoiDungs.Any())
        //        {
        //            return NoContent();
        //        }

        //        return Ok(nguoiDungs);
        //    }
        //    catch (Exception ex)
        //    {
        //        // Ghi log lỗi tại đây nếu cần
        //        return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi khi lấy danh sách người dùng: " + ex.Message);
        //    }
        //}

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetNguoiDungs()
        {
            try
            {
                var nguoiDungs = await _context.NguoiDungs
                    .Include(nd => nd.Khoa) // Join với bảng Khoa
                    .Include(nd => nd.BoMon) // Join với bảng BoMon
                    .Include(nd => nd.Quyen) // Join với bảng Quyen
                    .Select(nd => new
                    {
                        nd.MaNguoiDung,
                        nd.MaQuyen,
                        nd.MaKhoa,
                        nd.MaBoMon,
                        nd.Email,
                        nd.Password,
                        nd.HoTen,
                        nd.Lop,
                        nd.DiaChi,
                        nd.AnhDaiDien,
                        nd.MSSV,
                        nd.SDT,
                        nd.GioiTinh,
                        nd.NgaySinh,
                        nd.TrangThai,
                        Khoa = new
                        {
                            nd.Khoa.MaKhoa,
                            nd.Khoa.TenKhoa
                        },
                        BoMon = new
                        {
                            nd.BoMon.MaBoMon,
                            nd.BoMon.TenBoMon
                        },
                        Quyen = new
                        {
                            nd.Quyen.MaQuyen,
                            nd.Quyen.TenQuyen
                        }
                    })
                    .ToListAsync();

                if (!nguoiDungs.Any())
                {
                    return NoContent();
                }

                return Ok(nguoiDungs);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi khi lấy danh sách người dùng: " + ex.Message);
            }
        }


       // GET: api/NguoiDung/5
        [HttpGet("{id}")]
        public async Task<ActionResult<NguoiDung>> GetNguoiDung(string id)
        {
            var nguoiDung = await _context.NguoiDungs.FindAsync(id);

            if (nguoiDung == null)
            {
                return NotFound();
            }

            return nguoiDung;
        }

        // PUT: api/NguoiDung/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutNguoiDung(string id, NguoiDung nguoiDung)
        {
            if (id != nguoiDung.MaNguoiDung)
            {
                return BadRequest();
            }

            _context.Entry(nguoiDung).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!NguoiDungExists(id))
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

        // POST: api/NguoiDung
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<NguoiDung>> PostNguoiDung(NguoiDung nguoiDung)
        {
            _context.NguoiDungs.Add(nguoiDung);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (NguoiDungExists(nguoiDung.MaNguoiDung))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction(nameof(GetNguoiDung), new { id = nguoiDung.MaNguoiDung }, nguoiDung);
        }
        [HttpPost]
        [Route("/api/NguoiDung/DangKyNguoiDung")]
        public async Task<ActionResult<NguoiDung>> DangKyNguoiDung(string maQuyen, string maKhoa, string maBoMon, string email, string password, string hoTen, string Lop, string diaChi, string anhDaiDien, string mssv, string sdt, string gioiTinh, DateOnly ngaySinh)
        {
            var passwordHasher = new PasswordHasher<NguoiDung>();
            var maxMaNguoiDung = await _context.NguoiDungs
                .OrderByDescending(nd => nd.MaNguoiDung)
                .Select(nd => nd.MaNguoiDung)
                .FirstOrDefaultAsync();
            var newMaNguoiDung = "";
            if (maxMaNguoiDung != null)
            {
                // Lấy phần số từ chuỗi
                var numberPart = int.Parse(maxMaNguoiDung.Substring(2)); // Bỏ ký tự 'ND'

                // Tăng lên 1 và tạo mã mới
                 newMaNguoiDung = "ND" + (numberPart + 1).ToString("D1"); // Format D3 để giữ 3 chữ số
            }
            else
            {
                // Nếu bảng rỗng, đặt giá trị mặc định
                 newMaNguoiDung = "ND1";
            }

            NguoiDung nguoiDung = new NguoiDung()
            {
                MaNguoiDung = newMaNguoiDung,
                MaQuyen = maQuyen,
                MaBoMon = maBoMon,
                MaKhoa = maKhoa,
                Email = email,
                Password = passwordHasher.HashPassword(null, password),
                HoTen = hoTen,
                Lop = Lop,
                DiaChi = diaChi,
                AnhDaiDien = anhDaiDien,
                MSSV = mssv,
                SDT = sdt,
                GioiTinh = gioiTinh,
                NgaySinh = ngaySinh,
                TrangThai = "1",
            };
            _context.NguoiDungs.Add(nguoiDung);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (NguoiDungExists(nguoiDung.MaNguoiDung))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction(nameof(GetNguoiDung), new { id = nguoiDung.MaNguoiDung }, nguoiDung);
        }

        // DELETE: api/NguoiDung/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNguoiDung(string id)
        {
            var nguoiDung = await _context.NguoiDungs.FindAsync(id);
            if (nguoiDung == null)
            {
                return NotFound();
            }

            _context.NguoiDungs.Remove(nguoiDung);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool NguoiDungExists(string id)
        {
            return _context.NguoiDungs.Any(e => e.MaNguoiDung == id);
        }
    }
}
