using Jose;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebBaiGiangAPI.Data;
using WebBaiGiangAPI.Models;

namespace WebBaiGiangAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;

        public AuthController(IConfiguration configuration, AppDbContext context, IJwtService jwtService)
        {
            _configuration = configuration;
            _context = context;
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.UsersUsername == username);
            if (user == null)
            {
                return Unauthorized(new { message = "Tài khoản không tồn tại" });
            }
            var passwordHasher = new PasswordHasher<Users>();
            var verificationResult = passwordHasher.VerifyHashedPassword(user, user.UsersPassword, password);
            if (verificationResult == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new { message = "Mật khẩu không chính xác", username = username });
            }
            if (user.UsersState == 0)
            {
                return StatusCode(403, new { message = "Tài khoản của bạn đã bị khóa" });
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var keyString = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(keyString))
            {
                throw new Exception("Jwt:Key không được tìm thấy trong appsettings.json!");
            }
            var key = Encoding.UTF8.GetBytes(keyString);
            Console.WriteLine("Key tạo token (Base64): " + Convert.ToBase64String(key));

            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            if (string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
            {
                throw new Exception("Issuer hoặc Audience bị thiếu trong appsettings.json!");
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.Role, user.UsersRoleId.ToString()),
                }),
                Expires = DateTime.UtcNow.AddMinutes(30),
                IssuedAt = DateTime.UtcNow,
                NotBefore = DateTime.UtcNow,
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwt = tokenHandler.WriteToken(token);

            var user_log = new UsersLog();
            user_log.UlogUsersId = user.UsersId;
            user_log.UlogUsername = user.UsersUsername;
            user_log.UlogLoginDate = DateTime.Now;
            _context.UserLogs.Add(user_log);
            await _context.SaveChangesAsync();
            return Ok(new { jwttoken = jwt, data = user });
        }

        [HttpGet("logout")]
        public IActionResult Logout()
        {
            var authorizationHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (_jwtService.GetToken(authorizationHeader) == null)
            {
                return Unauthorized(new { message = "Token không tồn tại"});
            }
            var token = _jwtService.GetToken(authorizationHeader);
            var tokenInfo = _jwtService.GetTokenInfoFromToken(token);
            tokenInfo.TryGetValue(JwtRegisteredClaimNames.UniqueName, out string username);
            if (username == null)
            {
                return Unauthorized(new { message = "Token không hợp lệ hoặc đã hết hạn", token = token });
            }

            var user_log = _context.UserLogs
                .Where(u => u.UlogUsername == username)
                .OrderByDescending(u => u.UlogId)
                .FirstOrDefault();

            var user = _context.Users.SingleOrDefault(u => u.UsersUsername == username);
            if (user == null)
            {
                return Unauthorized(new { message = "Tài khoản không tồn tại" });
            }
            if (user_log == null)
            {
                return Unauthorized(new { message = "Bạn chưa đăng nhập, không thể đăng xuất" });
            }
            if (user_log.UlogLogoutDate != null)
            {
                return Unauthorized(new { message = "Bạn đã đăng xuất rồi, vui lòng thử lại" });
            }

            user_log.UlogLogoutDate = DateTime.Now;
            _context.Update(user_log);
            _context.SaveChanges();

            return Ok(new { message = "Logged out successfully!" });
        }

    }
}
