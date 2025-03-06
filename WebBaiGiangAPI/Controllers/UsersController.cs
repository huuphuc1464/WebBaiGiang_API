using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Common;
using WebBaiGiangAPI.Data;
using WebBaiGiangAPI.Models;

namespace WebBaiGiangAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;
        private string patternPass = @"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[!@#$%^&*()_+\-~=`{}[\]:"";'<>?,./]).{8,}$";
        private string patternEmail = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        private string patternSDT = @"^(0)[1-9][0-9]{8}$";
        private string patternMSSV = @"^(04|03)(01|02|03|04|06|07|08|09|12|61|62|63|64|65|66|67|68|69)\d{2}(\d{1})\d{3}$";

        public UsersController(AppDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Users>>> GetUsers()
        {
            try
            {
                var users = await _context.Users.ToListAsync();

                if (!users.Any())
                {
                    return NoContent();
                }

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi khi lấy danh sách người dùng: " + ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Users>> GetUsers(int id)
        {
            var users = await _context.Users.FindAsync(id);

            if (users == null)
            {
                return NotFound();
            }

            return users;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutUsers(int id, Users users)
        {
            if (id != users.UsersId)
            {
                return BadRequest();
            }

            _context.Entry(users).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UsersExists(id))
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

        [HttpPost]
        public async Task<ActionResult<Users>> PostUsers(Users users)
        {
            _context.Users.Add(users);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUsers", new { id = users.UsersId }, users);
        }

        [HttpPost("sign-in")]
        public async Task<IActionResult> SignIn([FromForm] UsersDTO user, [FromForm] IFormFile? anhdaidien)
        {
            var passwordHasher = new PasswordHasher<Users>();
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
            var saveUser = new Users();
            saveUser.UsersRoleId = 2;

            /* Kiểm tra password theo định dạng
                Độ dài: tối thiểu 8 kí tự 
                - bao gồm:
                   + Chữ in hoa: A, B, ...,Z
                   + chữ in thường: a, b, ...,z
                   + Chữ số và ký tự đb (!@#$%^&*()_+-~=\`{}[]:";'<>?,./)
             */
            if (!Regex.IsMatch(user.UsersPassword, patternPass))
            {
                return BadRequest(new
                {
                    message = "Password không hợp lệ",
                    data = user
                });
            }

            saveUser.UsersPassword = passwordHasher.HashPassword(saveUser, user.UsersPassword);

            // Kiểm tra email
            if (!Regex.IsMatch(user.UsersEmail, patternEmail))
            {
                return BadRequest(new
                {
                    message = "Email không hợp lệ",
                    data = user
                });
            }
            saveUser.UsersEmail = user.UsersEmail;
            // Kiểm tra sdt
            if (!Regex.IsMatch(user.UsersMobile, patternSDT))
            {
                return BadRequest(new
                {
                    message = "Số điện thoại không hợp lệ",
                    data = user
                });
            }
            saveUser.UsersMobile = user.UsersMobile;
            // Kiểm tra giới tính
            if (user.UserGender != "Nam" && user.UserGender != "Nữ")
            {
                return BadRequest(new
                {
                    message = "Giới tính chỉ chấp nhận giá trị Nam hoặc Nữ",
                    data = user
                });
            }
            // Kiểm tra ngày sinh
            if (user.UsersDob >= DateOnly.FromDateTime(DateTime.Today))
            {
                return BadRequest(new
                {
                    message = "Ngày sinh không thể lớn hơn ngày hiện tại",
                    data = user
                });
            }
            else
            {
                DateOnly ngayHienTai = DateOnly.FromDateTime(DateTime.Today);
                int tuoi = ngayHienTai.Year - user.UsersDob.Value.Year;

                if (ngayHienTai < user.UsersDob.Value.AddYears(tuoi))
                {
                    tuoi--;
                }

                if (tuoi < 18)
                {
                    return BadRequest(new
                    {
                        message = "Bạn chưa đủ 18 tuổi.",
                        data = user
                    });
                }
            }

            saveUser.UserGender = user.UserGender;
            // Loại bỏ khoảng trắng dư thừa
            saveUser.UsersName = Regex.Replace(user.UsersName.Trim(), @"\s+", " ");
            saveUser.UsersAdd = Regex.Replace(user.UsersAdd.Trim(), @"\s+", " ");
            saveUser.UsersUsername = user.UsersUsername;
            saveUser.UsersDob = user.UsersDob;
            saveUser.UsersCity = user.UsersCity;
            saveUser.UsersCountry = user.UsersCountry;
            saveUser.UsersDepartmentId = user.UsersDepartmentId;
            saveUser.UsersState = user.UsersState;
            saveUser.UserLevelId = user.UserLevelId;
            // Kiểm tra tồn tại
            if (_context.Users.Any(u => u.UsersEmail == user.UsersEmail))
            {
                return Conflict(new { message = "Email đã tồn tại trong hệ thống." });
            };

            if (_context.Users.Any(u => u.UsersMobile == user.UsersMobile))
            {
                return Conflict(new { message = "SDT đã tồn tại trong hệ thống." });
            };

            //Xử lý upload ảnh
            if (anhdaidien != null && anhdaidien.Length > 0)
            {
                // Kiểm tra loại file (chỉ chấp nhận ảnh PNG, JPG, JPEG)
                var allowedExtensions = new[] { ".png", ".jpg", ".jpeg" };
                var fileExtension = Path.GetExtension(anhdaidien.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { message = "Chỉ chấp nhận file ảnh định dạng PNG, JPG, JPEG." });
                }

                // Tạo tên file duy nhất
                string uniqueFileName = $"{user.UsersUsername}{fileExtension}";

                // Đường dẫn thư mục lưu file
                string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Users");

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
                        await anhdaidien.CopyToAsync(stream);
                    }
                    // Gán đường dẫn file cho thuộc tính AnhDaiDien của user
                    saveUser.UsersImage = uniqueFileName;
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "Lỗi khi lưu ảnh.", error = ex.Message });
                }
            }

            _context.Users.Add(saveUser);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (UsersExists(user.UsersId))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return Ok(new
            {
                message = "Đăng ký tài khoản giáo viên thành công",
                data = saveUser
            });
        }


        private bool UsersExists(int id)
        {
            return _context.Users.Any(e => e.UsersId == id);
        }

        [HttpPost("ChangePassword")]
        public async Task<IActionResult> ChangePassword(string oldPass, string newPass, string rePass)
        {
            var authorizationHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (_jwtService.GetToken(authorizationHeader) == null)
            {
                return Unauthorized(new { message = "Token không tồn tại" });
            }
            var token = _jwtService.GetToken(authorizationHeader);
            var tokenInfo = _jwtService.GetTokenInfoFromToken(token);
            tokenInfo.TryGetValue(JwtRegisteredClaimNames.UniqueName, out string username);
            if (username == null)
            {
                return Unauthorized(new { message = "Token không hợp lệ hoặc đã hết hạn" });
            }
            var user = await _context.Users.SingleOrDefaultAsync(u => u.UsersUsername == username);

            // Kiểm tra tồn tại tài khoản
            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "Người dùng không tồn tại",
                });
            }
            var user_log = _context.UserLogs
                .Where(u => u.UlogUsername == username)
                .OrderByDescending(u => u.UlogId)
                .FirstOrDefault();
            if (user_log == null)
            {
                return Unauthorized(new { message = "Bạn chưa đăng nhập, vui lòng thử lại" });
            }
            // Kiểm tra mật khẩu (đã băm)
            var passwordHasher = new PasswordHasher<Users>();
            var kiemTraMarKhauCu = passwordHasher.VerifyHashedPassword(user, user.UsersPassword, oldPass);

            // Kiểm tra trùng khớp mật khẩu cũ
            if (kiemTraMarKhauCu == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new
                {
                    message = "Mật khẩu cũ không chính xác",
                    MaNguoiDung = username,
                    MatKhauCu = oldPass,
                });
            }

            // Kiểm tra trùng khớp giữa mật khẩu cũ mà mật khẩu mới
            if (oldPass == newPass )
            {
                return Unauthorized(new
                {
                    message = "Mật khẩu cũ và mật khẩu mới không được phép giống nhau",
                    MaNguoiDung = username,
                    MatKhauCu = oldPass,
                });
            }

            // Kiểm tra định dạng của mật khẩu cũ mà mật khẩu mới
            if (!Regex.IsMatch(newPass, patternPass) || !Regex.IsMatch(rePass, patternPass))
            {
                return Unauthorized(new
                {
                    message = "Mật khẩu mới hoặc xác nhận mật khẩu mới không đúng định dạng",
                    MaNguoiDung = username,
                    MatKhauCu = oldPass,
                });
            }

            // Kiểm tra trùng khớp mật khẩu cũ và mật khẩu mới
            if (newPass != rePass)
            {
                return Unauthorized(new
                {
                    message = "Xác nhận mật khẩu mới không chính xác",
                    MaNguoiDung = username,
                    MatKhauCu = oldPass,
                });
            }

            // Lưu mật khẩu mới
            user.UsersPassword = passwordHasher.HashPassword(user, newPass);
            _context.SaveChanges();

            return Ok(new
            {
                message = "Đổi mật khẩu thành công",
                data = user,
            });
        }
    }
}
