using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
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
    public class TeacherController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;
        private string patternPass = @"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[!@#$%^&*()_+\-~=`{}[\]:"";'<>?,./]).{8,}$";
        private string patternEmail = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        private string patternSDT = @"^(0)[1-9][0-9]{8}$";

        public TeacherController(AppDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpGet("get-teachers")]
        public async Task<ActionResult<Users>> GetListTeachers()
        {
            /*var authorizationHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
            //if (_jwtService.GetToken(authorizationHeader) == null)
            //{
            //    return Unauthorized(new { message = "Token không tồn tại" });
            //}
            //var token = _jwtService.GetToken(authorizationHeader);
            //var tokenInfo = _jwtService.GetTokenInfoFromToken(token);
            //tokenInfo.TryGetValue(JwtRegisteredClaimNames.UniqueName, out string username);
            //tokenInfo.TryGetValue("role", out string role);
            //var user_log = _context.UserLogs
            //    .Where(u => u.UlogUsername == username)
            //    .OrderByDescending(u => u.UlogId)
            //    .FirstOrDefault();
            //if (username == null || user_log.UlogLogoutDate != null)
            //{
            //    return Unauthorized(new { message = "Token không hợp lệ hoặc đã hết hạn"});
            //}

            //var isUser = _context.Users.SingleOrDefault(u => u.UsersUsername == username);
            //if (isUser == null)
            //{
            //    return Unauthorized(new { message = "Tài khoản không tồn tại" });
            //}
            //if (role != "admin" && role != "1")
            //{
            //    return Unauthorized(new { message = "Bạn không phải là admin" });
            }*/
            var errorResult = KiemTraTokenAdmin();
            if (errorResult != null)
            {
                return errorResult;
            }
            var user = await _context.Users.Where(u => u.UsersRoleId == 2).ToListAsync();
            if (user == null)
            {
                return NotFound(new
                {
                    message = "Hiện tại không có giáo viên nào"
                });
            }
            return Ok(new
            {
                message = "Danh sách giáo viên",
                data = user
            });
        }

        [HttpGet("get-teacher")]
        public async Task<ActionResult<Users>> GetTeacher(int id)
        {
            var errorResult = KiemTraTokenAdmin();
            if (errorResult != null)
            {
                return errorResult;
            }
            var user = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.City)
                .Include(u => u.Country)
                .Include(u => u.Role)
                .Include(u => u.LoginLevel)
                .Include(u => u.State)
                .Select (u => new
                {
                    u.UsersId,
                    u.UsersName,
                    u.UsersUsername,
                    u.UsersRoleId,
                    u.Role.RoleName,
                    u.UsersEmail,
                    u.UsersMobile,
                    u.UsersDob,
                    u.UsersImage,
                    u.UsersAdd,
                    u.City.CityName,
                    u.Country.CountryName,
                    u.State.StateName,
                    u.Department.DepartmentTitle,
                    u.LoginLevel.LevelDescription,
                    u.UserGender,
                })
                .Where(u => u.UsersId == id && u.UsersRoleId == 2)
                .FirstOrDefaultAsync();
            if (user == null)
            {
                return NotFound( new
                {
                    message = "Giáo viên không tồn tại"
                });
            }
            return Ok (user);
        }

        [HttpPut("update-teacher")]
        public async Task<IActionResult> UpdateTeacher([FromForm] UsersDTO user, [FromForm] IFormFile? anhdaidien)
        {
            var errorResult = KiemTraTokenAdmin();
            if (errorResult != null)
            {
                return errorResult;
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            // Kiểm tra tài khoản tồn tại
            var saveuser = await _context.Users.SingleOrDefaultAsync(u => u.UsersId == user.UsersId);
            if (saveuser == null)
            {
                return Unauthorized(new { message = "Người dùng không tồn tại" });
            }
            // Kiểm tra email
            if (!Regex.IsMatch(user.UsersEmail, patternEmail))
            {
                return BadRequest(new
                {
                    message = "Email không hợp lệ",
                    data = user
                });
            }
            saveuser.UsersEmail = user.UsersEmail;
            saveuser.UsersMobile = user.UsersMobile;
            // Kiểm tra sdt
            if (!Regex.IsMatch(user.UsersMobile, patternSDT))
            {
                return BadRequest(new
                {
                    message = "Số điện thoại không hợp lệ",
                    data = user
                });
            }
            // Kiểm tra giới tính
            if (user.UserGender != "Nam" && user.UserGender != "Nữ")
            {
                return BadRequest(new
                {
                    message = "Giới tính chỉ chấp nhận giá trị Nam hoặc Nữ",
                    data = user
                });
            }
            // Loại bỏ khoảng trắng dư thừa
            saveuser.UsersName = Regex.Replace(user.UsersName.Trim(), @"\s+", " ");
            saveuser.UsersAdd = Regex.Replace(user.UsersAdd.Trim(), @"\s+", " ");

            // Kiểm tra tồn tại
            if (_context.Users.Any(u => u.UsersEmail == user.UsersEmail) && (saveuser.UsersEmail != user.UsersEmail))
            {
                return Conflict(new { message = "Email đã tồn tại trong hệ thống." });
            };

            if (_context.Users.Any(u => u.UsersMobile == user.UsersMobile) && (saveuser.UsersMobile != user.UsersMobile))
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

                // Kiểm tra và xóa ảnh cũ nếu tồn tại
                if (!string.IsNullOrEmpty(saveuser.UsersImage))
                {
                    string oldFilePath = Path.Combine(uploadPath, saveuser.UsersImage);
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
                        await anhdaidien.CopyToAsync(stream);
                    }
                    // Gán đường dẫn file cho thuộc tính AnhDaiDien của user
                    saveuser.UsersImage = uniqueFileName;
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "Lỗi khi lưu ảnh.", error = ex.Message });
                }
            }
            saveuser.UsersRoleId = user.UsersRoleId;
            saveuser.UsersDepartmentId = user.UsersDepartmentId;
            saveuser.UserGender = user.UserGender;
            saveuser.UsersDob = user.UsersDob;
            saveuser.UsersCity = user.UsersCity;
            saveuser.UsersCountry = user.UsersCountry;
            saveuser.UsersState = user.UsersState;
            saveuser.UserLevelId = user.UserLevelId;
            _context.Users.Update(saveuser);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (UsersExists(saveuser.UsersId))
                {
                    return Conflict(new
                    {
                        message = "Tài khoản không tồn tại"
                    });
                }
                else
                {
                    throw;
                }
            }

            return Ok( new
            {
                message = "Thay đổi thông tin giáo viên thành công",
                data = saveuser
            });
        }

        [HttpPost("add-teacher")]
        public async Task<IActionResult> AddTeacher([FromForm] UsersDTO user, [FromForm] IFormFile? anhdaidien)
        {
            var errorResult = KiemTraTokenAdmin();
            if (errorResult != null)
            {
                return errorResult;
            }
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
                message = "Tạo tài khoản giáo viên thành công",
                data = user
            });
        }

        private bool UsersExists(int id)
        {
            return _context.Users.Any(e => e.UsersId == id);
        }
        private ActionResult? KiemTraTokenAdmin()
        {
            var authorizationHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (_jwtService.GetToken(authorizationHeader) == null)
            {
                return Unauthorized(new { message = "Token không tồn tại" });
            }

            var token = _jwtService.GetToken(authorizationHeader);
            var tokenInfo = _jwtService.GetTokenInfoFromToken(token);

            if (!tokenInfo.TryGetValue(JwtRegisteredClaimNames.UniqueName, out string username) || string.IsNullOrEmpty(username))
            {
                return Unauthorized(new { message = "Token không hợp lệ hoặc đã hết hạn" });
            }

            tokenInfo.TryGetValue("role", out string role);

            var user_log = _context.UserLogs
                .Where(u => u.UlogUsername == username)
                .OrderByDescending(u => u.UlogId)
                .FirstOrDefault();

            if (user_log == null || user_log.UlogLogoutDate != null)
            {
                return Unauthorized(new { message = "Token không hợp lệ hoặc đã hết hạn" });
            }

            var isUser = _context.Users.SingleOrDefault(u => u.UsersUsername == username);
            if (isUser == null)
            {
                return Unauthorized(new { message = "Tài khoản không tồn tại" });
            }

            if (role != "admin" && role != "1")
            {
                return Unauthorized(new { message = "Bạn không phải là admin" });
            }

            return null; // Không có lỗi
        }


    }
}
