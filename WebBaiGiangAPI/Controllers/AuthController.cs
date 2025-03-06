using Jose;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using WebBaiGiangAPI.Data;
using WebBaiGiangAPI.Models;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Newtonsoft.Json;

namespace WebBaiGiangAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly EmailService _emailService;
        private readonly OtpService _otpService;
        private string patternEmail = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        private string patternPass = @"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[!@#$%^&*()_+\-~=`{}[\]:"";'<>?,./]).{8,}$";

        public AuthController(IConfiguration configuration, AppDbContext context, 
            IJwtService jwtService, EmailService emailService, OtpService otpService)
        {
            _configuration = configuration;
            _context = context;
            _jwtService = jwtService;
            _emailService = emailService;
            _otpService = otpService;
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
            var userLog = new UsersLog();
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

        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    return BadRequest(new { message = "Vui lòng nhập email." });
                }
                if (!Regex.IsMatch(email, patternEmail))
                {
                    return BadRequest(new { message = "Email không hợp lệ." });
                }
                var otp = new Random().Next(100000, 999999).ToString();
                var emailSent = await _emailService.SendEmailAsync(email, otp);
                if (!emailSent)
                {
                    return StatusCode(500, "Gửi OTP thất bại. Vui lòng thử lại.");
                }
                _otpService.StoreOtp(email, otp);
                return Ok( new { message = "OTP đã được gửi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
            }
        }

        [HttpPost("verify-otp")]
        public IActionResult VerifyOtp(string email, string otp)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(new { message = "Vui lòng nhập email." });
            }
            if (!Regex.IsMatch(email, patternEmail))
            {
                return BadRequest(new { message = "Email không hợp lệ." });
            }
            if (_otpService.ValidateOtp(email, otp))
            {
                _otpService.RemoveOtp(email);
                return Ok( new { message = "Xác minh thành công. Bạn có thể đặt lại mật khẩu." });
            }
            return BadRequest(new { message = "Mã OTP không hợp lệ hoặc đã hết hạn." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(string email, string password)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    return BadRequest(new { message = "Vui lòng nhập email." });
                }
                if (!Regex.IsMatch(email, patternEmail))
                {
                    return BadRequest(new { message = "Email không hợp lệ." });
                }
                if (!Regex.IsMatch(password, patternPass))
                {
                    return BadRequest(new { message = "Password không đúng định dạng." });
                }
                var user = await _context.Users.SingleOrDefaultAsync(u => u.UsersEmail == email);

                // Kiểm tra tồn tại tài khoản
                if (user == null)
                {
                    return Unauthorized(new { message = "Người dùng không tồn tại" });
                }
                var passwordHasher = new PasswordHasher<Users>();
                user.UsersPassword = passwordHasher.HashPassword(user, user.UsersPassword);
                _context.SaveChanges();
                return Ok("Mật khẩu đã được đặt lại thành công.");
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        [HttpGet("login-google")]
        public IActionResult LoginWithGoogle()
        {
            var redirectUrl = Url.Action("GoogleResponse", "Auth", null, Request.Scheme);
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("google-response")]
        public async Task<IActionResult> GoogleResponse()
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!authenticateResult.Succeeded)
            {
                return BadRequest("Google authentication failed.");
            }
            var properties = authenticateResult.Properties;
            var tokens = properties?.Items;

            var accessToken = properties?.GetTokenValue("access_token");
            if (string.IsNullOrEmpty(accessToken))
                return BadRequest("Không lấy được Access Token.");

            var idToken = properties?.GetTokenValue("id_token");

            if (accessToken == null && idToken == null)
            {
                return BadRequest("Không lấy được token. Kiểm tra cấu hình Google API.");
            }

            var claims = authenticateResult.Principal?.Identities.FirstOrDefault()?.Claims;
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync("https://www.googleapis.com/oauth2/v3/userinfo");
            if (!response.IsSuccessStatusCode)
                return BadRequest("Không thể lấy thông tin người dùng.");

            // Đọc dữ liệu trả về từ Google
            var userInfoJson = await response.Content.ReadAsStringAsync();

            // Chuyển từ JSON string thành Dictionary
            var userInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(userInfoJson);
            string googleEmail = userInfo.ContainsKey("email") ? userInfo["email"]?.ToString() : null;

            // Kiểm tra và lưu thông tin user mới
            var existUser = await _context.Users.SingleOrDefaultAsync(u => u.UsersEmail == googleEmail);
            if (existUser == null)
            {            
                Users users = new Users();
                users.UsersRoleId = 2;
                users.UserLevelId = 2;
                users.UsersName = userInfo.ContainsKey("name") ? userInfo["name"]?.ToString() : null;
                users.UsersEmail = googleEmail;
                users.UsersImage = userInfo.ContainsKey("picture") ? userInfo["picture"]?.ToString() : null;
                users.UsersUsername = googleEmail;
                users.UsersDepartmentId = 1;
                users.UsersMobile = "N" + userInfo["sub"].ToString().Substring(0, 9);
                _context.Users.Add(users);
                _context.SaveChanges();
            }

            // Lấy thông tin user và lưu vào user_log
            var userIF = await _context.Users.SingleOrDefaultAsync(u => u.UsersEmail == googleEmail);
            var userLog = new UsersLog();
            userLog.UlogUsersId = userIF.UsersId;
            userLog.UlogUsername = userIF.UsersUsername;
            userLog.UlogLoginDate = DateTime.Now;
            _context.UserLogs.Add(userLog);
            _context.SaveChanges();

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
                    new Claim(ClaimTypes.Name, googleEmail),
                    new Claim(ClaimTypes.Role, "teacher"),
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
            return Ok(new
            {
                message = "Xác thực thành công!",
                accessToken = accessToken,
                idToken = idToken,
                userinfo = userInfo,
                jwt = jwt,
            });
        }

        [HttpGet("logout-google")]
        public async Task<IActionResult> LogoutGoogle()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            var username = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (username == null)
            {
                return Unauthorized(new { message = "Bạn đã đăng xuất rồi, vui lòng thử lại" });
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

            // Kiểm tra user và lưu vào user_log
            user_log.UlogLogoutDate = DateTime.Now;
            _context.Update(user_log);
            _context.SaveChanges();
            return Ok( new { message = "Logout Successful" });
        }

        [HttpGet("login-github")]
        public IActionResult LoginWithGitHub()
        {
            var redirectUrl = Url.Action("GitHubResponse", "Auth", null, Request.Scheme);
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, "GitHub");
        }

        [HttpGet("github-response")]
        public async Task<IActionResult> GitHubResponse()
        {
            var authenticateResult = await HttpContext.AuthenticateAsync();
            if (!authenticateResult.Succeeded)
                return BadRequest(new { message = "GitHub authentication failed." });

            var properties = authenticateResult.Properties;
            var tokens = properties?.Items;

            // Log tất cả token để kiểm tra
            if (tokens != null)
            {
                foreach (var token in tokens)
                {
                    Console.WriteLine($"{token.Key}: {token.Value}");
                }
            }

            var accessToken = properties?.GetTokenValue("access_token");

            if (string.IsNullOrEmpty(accessToken))
            {
                return BadRequest(new { message = "Không lấy được access token." });
            }

            // Gọi API GitHub để lấy tất cả thông tin
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("request");

                var response = await httpClient.GetAsync("https://api.github.com/user");
                if (!response.IsSuccessStatusCode)
                {
                    return BadRequest( new { message = "Không thể lấy thông tin từ GitHub API."});
                }

                var userJson = await response.Content.ReadAsStringAsync();
                var userData = System.Text.Json.JsonDocument.Parse(userJson).RootElement;
                var githubEmail = userData.GetProperty("email").ToString();
                var existUser = await _context.Users.SingleOrDefaultAsync(u => u.UsersEmail == githubEmail);
                var userName = userData.TryGetProperty("login", out var login) ? login.GetString() : null;
                if (existUser == null)
                {
                    Users users = new Users();
                    users.UsersRoleId = 2;
                    users.UserLevelId = 3;
                    users.UsersName = userData.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null;
                    users.UsersEmail = githubEmail;
                    users.UsersImage = userData.TryGetProperty("avatar_url", out var avatar) ? avatar.GetString() : null;
                    users.UsersUsername = userName;
                    users.UsersAdd = userData.TryGetProperty("location", out var location) ? location.GetString() : null;
                    users.UsersDepartmentId = 1;
                    users.UsersMobile = "N" + userData.GetProperty("id").ToString().Substring(0, 9);
                    _context.Users.Add(users);
                    _context.SaveChanges();
                }
                
                // Lấy thông tin user và lưu vào user_log
                var userIF = await _context.Users.SingleOrDefaultAsync(u => u.UsersUsername == userName);
                var userLog = new UsersLog();
                userLog.UlogUsersId = userIF.UsersId;
                userLog.UlogUsername = userIF.UsersUsername;
                userLog.UlogLoginDate = DateTime.Now;
                _context.UserLogs.Add(userLog);
                _context.SaveChanges();
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, userName),
                    new Claim(ClaimTypes.Email, githubEmail),
                    new Claim("AccessToken", accessToken) // Lưu access token vào Claims
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                // Lưu Access Token vào AuthenticationProperties
                var authProperties = new AuthenticationProperties();
                authProperties.StoreTokens(new List<AuthenticationToken>
                {
                    new AuthenticationToken { Name = "access_token", Value = accessToken }
                });

                // Lưu Claims và Access Token vào HttpContext
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal, authProperties);

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
                        new Claim(ClaimTypes.Name, userName),
                        new Claim(ClaimTypes.Role, "teacher"),
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
                return Ok(new
                {
                    Message = "Xác thực GitHub thành công!",
                    AccessToken = accessToken,
                    User = userData,
                    Jwt = jwt,
                });
            }
        }

        [HttpPost("logout-github")]
        public async Task<IActionResult> LogoutGithub()
        {
            // Lấy username từ Claims
            var username = HttpContext.User.Identity?.Name;

            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized(new { message = "Bạn đã đăng xuất rồi, vui lòng thử lại" });
            }

            // Lấy log đăng nhập gần nhất
            var userLog = await _context.UserLogs
                .Where(u => u.UlogUsername == username)
                .OrderByDescending(u => u.UlogId)
                .FirstOrDefaultAsync();

            if (userLog == null)
            {
                return Unauthorized(new { message = "Bạn chưa đăng nhập, không thể đăng xuất!" });
            }

            if (userLog.UlogLogoutDate != null)
            {
                return Unauthorized(new { message = "Bạn đã đăng xuất rồi, vui lòng thử lại!" });
            }

            // Cập nhật log đăng xuất
            userLog.UlogLogoutDate = DateTime.Now;
            _context.Update(userLog);
            await _context.SaveChangesAsync();

            // Thực hiện đăng xuất
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return Ok(new { message = "Đăng xuất thành công!" });
        }


    }
}

