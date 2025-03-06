using System;
using System.Collections.Generic;
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

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUsers(int id)
        {
            var users = await _context.Users.FindAsync(id);
            if (users == null)
            {
                return NotFound();
            }

            _context.Users.Remove(users);
            await _context.SaveChangesAsync();

            return NoContent();
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
