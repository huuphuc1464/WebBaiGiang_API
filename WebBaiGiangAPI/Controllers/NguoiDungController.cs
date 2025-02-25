using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using WebBaiGiangAPI.Data;
using WebBaiGiangAPI.Models;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;

namespace WebBaiGiangAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NguoiDungController : ControllerBase
    {
        private readonly AppDbContext _context;
        private string patternPass = @"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[!@#$%^&*()_+\-~=`{}[\]:"";'<>?,./]).{8,}$";
        private string patternEmail = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        private string patternSDT = @"^(0)[1-9][0-9]{8}$";
        private string patternMSSV = @"^(04|03)(01|02|03|04|06|07|08|09|12|61|62|63|64|65|66|67|68|69)\d{2}(\d{1})\d{3}$";

        public NguoiDungController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/NguoiDung
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetNguoiDungs()
        {
            try
            {
                var nguoiDungs = await _context.NguoiDungs
                    .Include(nd => nd.Khoa)
                    .Include(nd => nd.BoMon)
                    .Include(nd => nd.Quyen)
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
                        nd.Khoa.TenKhoa,
                        nd.BoMon.TenBoMon,
                        nd.Quyen.TenQuyen,
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
        [HttpGet("id")]
        public async Task<ActionResult<object>> GetNguoiDung(string id)
        {
            var nguoiDung = await _context.NguoiDungs
                .Include(nd => nd.Khoa)
                .Include(nd => nd.BoMon)
                .Include(nd => nd.Quyen)
                .Where(nd => nd.MaNguoiDung.Contains(id))
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
                    nd.Khoa.TenKhoa,
                    nd.BoMon.TenBoMon,
                    nd.Quyen.TenQuyen,
                })
                .ToListAsync();

            if (nguoiDung == null)
            {
                return NotFound();
            }

            return nguoiDung;
        }

        /* PUT: api/NguoiDung/5
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
        }*/

        /* POST: api/NguoiDung
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
        }*/

        [HttpPost]
        [Route("/api/NguoiDung/DangKyNguoiDung")]
        public async Task<IActionResult> DangKyNguoiDung([FromForm] NguoiDung nguoiDung, [FromForm] IFormFile anhDaiDien1)
        {
            var passwordHasher = new PasswordHasher<NguoiDung>();
            // Lấy mã người dùng lớn nhất
            var maxMaNguoiDung = await _context.NguoiDungs
                .OrderByDescending(nd => nd.MaNguoiDung)
                .Select(nd => nd.MaNguoiDung)
                .FirstOrDefaultAsync();
            // Tạo mã người dùng
            var newMaNguoiDung = "";
            if (maxMaNguoiDung != null)
            {
                // Lấy phần số từ chuỗi
                var numberPart = int.Parse(maxMaNguoiDung.Substring(2)); // Bỏ ký tự 'ND'
                // Tăng lên 1 và tạo mã mới
                newMaNguoiDung = "ND" + (numberPart + 1).ToString("D1");
            }
            else
            {
                // Nếu bảng rỗng, đặt giá trị mặc định
                newMaNguoiDung = "ND1";
            }
            nguoiDung.MaNguoiDung = newMaNguoiDung;

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

            /* Kiểm tra password theo định dạng
                Độ dài: tối thiểu 8 kí tự 
                - bao gồm:
                   + Chữ in hoa: A, B, ...,Z
                   + chữ in thường: a, b, ...,z
                   + Chữ số và ký tự đb (!@#$%^&*()_+-~=\`{}[]:";'<>?,./)
             */
            if (!Regex.IsMatch(nguoiDung.Password, patternPass))
            {
                return BadRequest(new
                {
                    message = "Password không hợp lệ",
                    data = nguoiDung
                });
            }

            nguoiDung.Password = passwordHasher.HashPassword(nguoiDung, nguoiDung.Password);

            // Kiểm tra email
            if (!Regex.IsMatch(nguoiDung.Email, patternEmail))
            {
                return BadRequest(new
                {
                    message = "Email không hợp lệ",
                    data = nguoiDung
                });
            }

            // Kiểm tra sdt
            if (!Regex.IsMatch(nguoiDung.SDT, patternSDT))
            {
                return BadRequest(new
                {
                    message = "Số điện thoại không hợp lệ",
                    data = nguoiDung
                });
            }

            // Kiểm tra giới tính
            if (nguoiDung.GioiTinh != "Nam" && nguoiDung.GioiTinh != "Nữ")
            {
                return BadRequest(new
                {
                    message = "Giới tính chỉ chấp nhận giá trị Nam hoặc Nữ",
                    data = nguoiDung
                });
            }

            // Loại bỏ khoảng trắng dư thừa
            nguoiDung.HoTen = Regex.Replace(nguoiDung.HoTen.Trim(), @"\s+", " ");
            nguoiDung.DiaChi = Regex.Replace(nguoiDung.DiaChi.Trim(), @"\s+", " ");
            nguoiDung.Lop = Regex.Replace(nguoiDung.Lop.Trim(), @"\s+", " ");

            /* Kiểm tra MSSV
             Quy tắc đặt MSSV theo mã đào tạo trường CĐ Kỹ Thuật Cao Thắng (CKC)
            [mã bậc(mã số)].[mã ngành(mã số)].[khoá(hai số cuối của năm)].[mã loại hình đào tạo(mã số)].[số thứ tự] */
            if (!string.IsNullOrEmpty(nguoiDung.MSSV) && !Regex.IsMatch(nguoiDung.MSSV, patternMSSV))
            {
                return BadRequest(new
                {
                    message = "MSSV không đúng định dạng của trường Cao đẳng Kỹ Thuật Cao Thắng",
                    data = nguoiDung
                });
            }

            // Kiểm tra tồn tại
            if (_context.NguoiDungs.Any(u => u.Email == nguoiDung.Email))
            {
                return Conflict(new { message = "Email đã tồn tại trong hệ thống." });
            };

            if (_context.NguoiDungs.Any(u => u.SDT == nguoiDung.SDT))
            {
                return Conflict(new { message = "SDT đã tồn tại trong hệ thống." });
            };

            if (_context.NguoiDungs.Any(u => u.MSSV == nguoiDung.MSSV))
            {
                return Conflict(new { message = "MSSV đã tồn tại trong hệ thống." });
            };

            //Xử lý upload ảnh
            if (anhDaiDien1 != null && anhDaiDien1.Length > 0)
            {
                // Kiểm tra loại file (chỉ chấp nhận ảnh PNG, JPG, JPEG)
                var allowedExtensions = new[] { ".png", ".jpg", ".jpeg" };
                var fileExtension = Path.GetExtension(anhDaiDien1.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { message = "Chỉ chấp nhận file ảnh định dạng PNG, JPG, JPEG." });
                }

                // Tạo tên file duy nhất
                string uniqueFileName = $"{nguoiDung.MaNguoiDung}{fileExtension}";

                // Đường dẫn thư mục lưu file
                string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "AnhNguoiDung");

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
                        await anhDaiDien1.CopyToAsync(stream);
                    }
                    // Gán đường dẫn file cho thuộc tính AnhDaiDien của nguoiDung
                    nguoiDung.AnhDaiDien = uniqueFileName;
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "Lỗi khi lưu ảnh.", error = ex.Message });
                }
            }
            nguoiDung.TrangThai = "1";

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

        [HttpPost("DangNhap")]
        public async Task<IActionResult> DangNhap(string email, string password)
        {
            // Tìm người dùng theo email
            var user = await _context.NguoiDungs.SingleOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return Unauthorized(new { message = "Email không tồn tại" });
            }
            // Kiểm tra mật khẩu (đã băm)
            var passwordHasher = new PasswordHasher<NguoiDung>();
            var verificationResult = passwordHasher.VerifyHashedPassword(user, user.Password, password);
            if (verificationResult == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new { message = "Mật khẩu không chính xác", email = email });
            }
            if (user.TrangThai == "0")
            {
                return StatusCode(403, new { message = "Tài khoản của bạn đã bị khóa" });
            }

            // Đăng nhập thành công
            return Ok(new
            {
                message = "Đăng nhập thành công", 
                data = user,
            });
        }

        [HttpPost("DoiMatKhau")]
        public async Task<IActionResult> DoiMatKhau(string maNguoiDung, string matKhauCu, string matKhauMoi, string xacNhanMatKhau)
        {
            var user = await _context.NguoiDungs.SingleOrDefaultAsync(u => u.MaNguoiDung == maNguoiDung && u.TrangThai == "1");

            // Kiểm tra tồn tại tài khoản
            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "Người dùng không tồn tại",
                });
            }

            // Kiểm tra mật khẩu (đã băm)
            var passwordHasher = new PasswordHasher<NguoiDung>();
            var kiemTraMarKhauCu = passwordHasher.VerifyHashedPassword(user, user.Password, matKhauCu);

            // Kiểm tra trùng khớp mật khẩu cũ
            if (kiemTraMarKhauCu == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new
                {
                    message = "Mật khẩu cũ không chính xác",
                    MaNguoiDung = maNguoiDung,
                    MatKhauCu = matKhauCu,
                });
            }

            // Kiểm tra trùng khớp giữa mật khẩu cũ mà mật khẩu mới
            if (matKhauCu == matKhauMoi)
            {
                return Unauthorized(new
                {
                    message = "Mật khẩu cũ và mật khẩu mới không được phép giống nhau",
                    MaNguoiDung = maNguoiDung,
                    MatKhauCu = matKhauCu,
                });
            }

            // Kiểm tra định dạng của mật khẩu cũ mà mật khẩu mới
            if (!Regex.IsMatch(matKhauMoi, patternPass) || !Regex.IsMatch(xacNhanMatKhau, patternPass))
            {
                return Unauthorized(new
                {
                    message = "Mật khẩu mới hoặc xác nhận mật khẩu mới không đúng định dạng",
                    MaNguoiDung = maNguoiDung,
                    MatKhauCu = matKhauCu,
                });
            }

            // Kiểm tra trùng khớp mật khẩu cũ và mật khẩu mới
            if (matKhauMoi != xacNhanMatKhau)
            {
                return Unauthorized(new
                {
                    message = "Xác nhận mật khẩu mới không chính xác",
                    MaNguoiDung = maNguoiDung,
                    MatKhauCu = matKhauCu,
                });
            }

            // Lưu mật khẩu mới
            user.Password = passwordHasher.HashPassword(user, matKhauMoi);
            _context.SaveChanges();

            return Ok(new
            {
                message = "Đổi mật khẩu thành công",
                data = user,
            });
        }

        [HttpPut]
        [Route("/api/NguoiDung/ThayDoiThongTin")]
        public async Task<IActionResult> ThayDoiThongTin([FromForm] NguoiDungDTO nguoiDung, [FromForm] IFormFile? anhDaiDien1)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            // Kiểm tra tài khoản tồn tại
            var user = await _context.NguoiDungs.SingleOrDefaultAsync(u => u.MaNguoiDung == nguoiDung.MaNguoiDung && u.TrangThai == "1");
            if (user == null)
            {
                return Unauthorized(new { message = "Người dùng không tồn tại" });
            }
            // Kiểm tra email
            if (!Regex.IsMatch(nguoiDung.Email, patternEmail))
            {
                return BadRequest(new
                {
                    message = "Email không hợp lệ",
                    data = nguoiDung
                });
            }
            // Kiểm tra sdt
            if (!Regex.IsMatch(nguoiDung.SDT, patternSDT))
            {
                return BadRequest(new
                {
                    message = "Số điện thoại không hợp lệ",
                    data = nguoiDung
                });
            }
            // Kiểm tra giới tính
            if (nguoiDung.GioiTinh != "Nam" && nguoiDung.GioiTinh != "Nữ")
            {
                return BadRequest(new
                {
                    message = "Giới tính chỉ chấp nhận giá trị Nam hoặc Nữ",
                    data = nguoiDung
                });
            }
            // Loại bỏ khoảng trắng dư thừa
            nguoiDung.HoTen = Regex.Replace(nguoiDung.HoTen.Trim(), @"\s+", " ");
            nguoiDung.DiaChi = Regex.Replace(nguoiDung.DiaChi.Trim(), @"\s+", " ");
            nguoiDung.Lop = Regex.Replace(nguoiDung.Lop.Trim(), @"\s+", " ");
            /* Kiểm tra MSSV
             Quy tắc đặt MSSV theo mã đào tạo trường CĐ Kỹ Thuật Cao Thắng (CKC)
            [mã bậc(mã số)].[mã ngành(mã số)].[khoá(hai số cuối của năm)].[mã loại hình đào tạo(mã số)].[số thứ tự] */
            if (nguoiDung.MSSV != "null" )
            {
                if (!Regex.IsMatch(nguoiDung.MSSV, patternMSSV))
                {
                    return BadRequest(new
                    {
                        message = "MSSV không đúng định dạng của trường Cao đẳng Kỹ Thuật Cao Thắng",
                        data = nguoiDung,
                    });
                }
                else
                {
                    user.MSSV = nguoiDung.MSSV;
                }
            }

            // Kiểm tra tồn tại
            if (_context.NguoiDungs.Any(u => u.Email == nguoiDung.Email) && (user.Email != nguoiDung.Email))
            {
                return Conflict(new { message = "Email đã tồn tại trong hệ thống." });
            };

            if (_context.NguoiDungs.Any(u => u.SDT == nguoiDung.SDT) &&  (user.SDT != nguoiDung.SDT))
            {
                return Conflict(new { message = "SDT đã tồn tại trong hệ thống." });
            };

            if (nguoiDung.MSSV != "null" && _context.NguoiDungs.Any(u => u.MSSV == nguoiDung.MSSV) && (user.MSSV != nguoiDung.MSSV))
            {
                return Conflict(new { message = "MSSV đã tồn tại trong hệ thống." });
            };

            //Xử lý upload ảnh
            if (anhDaiDien1 != null && anhDaiDien1.Length > 0)
            {
                // Kiểm tra loại file (chỉ chấp nhận ảnh PNG, JPG, JPEG)
                var allowedExtensions = new[] { ".png", ".jpg", ".jpeg" };
                var fileExtension = Path.GetExtension(anhDaiDien1.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { message = "Chỉ chấp nhận file ảnh định dạng PNG, JPG, JPEG." });
                }

                // Tạo tên file duy nhất
                string uniqueFileName = $"{nguoiDung.MaNguoiDung}{fileExtension}";

                // Đường dẫn thư mục lưu file
                string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "AnhNguoiDung");

                // Tạo thư mục nếu chưa tồn tại
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                // Đường dẫn đầy đủ của file
                string filePath = Path.Combine(uploadPath, uniqueFileName);

                // Kiểm tra và xóa ảnh cũ nếu tồn tại
                if (!string.IsNullOrEmpty(user.AnhDaiDien))
                {
                    string oldFilePath = Path.Combine(uploadPath, user.AnhDaiDien);
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
                        await anhDaiDien1.CopyToAsync(stream);
                    }
                    // Gán đường dẫn file cho thuộc tính AnhDaiDien của nguoiDung
                    user.AnhDaiDien = uniqueFileName;
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "Lỗi khi lưu ảnh.", error = ex.Message });
                }
            }

            user.MaKhoa = nguoiDung.MaKhoa;
            user.MaBoMon = nguoiDung.MaBoMon;
            user.Email = nguoiDung.Email;
            user.HoTen = nguoiDung.HoTen;
            user.Lop = nguoiDung.Lop;
            user.DiaChi = nguoiDung.DiaChi;
            user.SDT = nguoiDung.SDT;
            user.GioiTinh = nguoiDung.GioiTinh;
            user.NgaySinh = nguoiDung.NgaySinh;

            _context.NguoiDungs.Update(user);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (NguoiDungExists(user.MaNguoiDung))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }
            return CreatedAtAction(nameof(GetNguoiDung), new { id = user.MaNguoiDung }, user);
        }

        // Chưa làm
        // Lấy ds lớp học theo học kỳ 
        [HttpGet]
        [Route("/DanhSachLopHoc/{hk}")]
        public async Task<IActionResult> DanhSachLopHoc(int hk)
        {
            return Ok();
        }
        //DELETE: api/NguoiDung/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNguoiDung(string id)
        {
            var nguoiDung = await _context.NguoiDungs.FindAsync(id);
            if (nguoiDung == null)
            {
                return NotFound();
            }
            nguoiDung.TrangThai = "0";
            //_context.NguoiDungs.Remove(nguoiDung);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool NguoiDungExists(string id)
        {
            return _context.NguoiDungs.Any(e => e.MaNguoiDung == id);
        }
    }
}
