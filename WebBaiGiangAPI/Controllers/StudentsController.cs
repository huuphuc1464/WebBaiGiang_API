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
using OfficeOpenXml;
using Org.BouncyCastle.Asn1.Pkcs;
using WebBaiGiangAPI.Data;
using WebBaiGiangAPI.Models;

namespace WebBaiGiangAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;
        private string patternEmail = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        private string patternSDT = @"^(0)[1-9][0-9]{8}$";
        private string patternMSSV = @"^(04|03)(01|02|03|04|06|07|08|09|12|61|62|63|64|65|66|67|68|69)\d{2}(\d{1})\d{3}$";

        public StudentsController(AppDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpGet("get-students")]
        public async Task<ActionResult> GetStudents()
        {
            var errorResult = KiemTraTokenTeacher();
            if (errorResult != null)
            {
                return errorResult;
            }

            var students = from u in _context.Users
                           join s in _context.Students on u.UsersId equals s.StudentId
                           join c in _context.Cities on u.UsersCity equals c.CityId into cityGroup
                           from c in cityGroup.DefaultIfEmpty()
                           join st in _context.States on u.UsersState equals st.StateId into stateGroup
                           from st in stateGroup.DefaultIfEmpty()
                           join co in _context.Countries on u.UsersCountry equals co.CountryId into countryGroup
                           from co in countryGroup.DefaultIfEmpty()
                           join d in _context.Departments on u.UsersDepartmentId equals d.DepartmentId into deptGroup
                           from d in deptGroup.DefaultIfEmpty()
                           where u.UsersRoleId == 3
                           select new
                           {
                               u.UsersId,
                               u.UsersName,
                               u.UsersUsername,
                               u.UsersEmail,
                               u.UsersMobile,
                               u.UsersDob,
                               u.UsersImage,
                               u.UsersAdd,
                               u.UserGender,
                               s.StudentCode,
                               s.StudentRollno,
                               s.StudentFatherName,
                               s.StudentDetails,
                               CityName = c != null ? c.CityName : null,
                               StateName = st != null ? st.StateName : null,
                               CountryName = co != null ? co.CountryName : null,
                               DepartmentTitle = d != null ? d.DepartmentTitle : null
                           };

            var result = students.ToList();


            if (result == null)
            {
                return NotFound(new
                {
                    message = "Hiện tại không có sinh viên nào"
                });
            }
            return Ok(new
            {
                message = "Danh sách sinh viên",
                data = result
            });
        }

        [HttpGet("get-student")]
        public async Task<ActionResult<Student>> GetStudent(int id)
        {
            var errorResult = KiemTraTokenTeacher();
            if (errorResult != null)
            {
                return errorResult;
            }

            var students = from u in _context.Users
                           join s in _context.Students on u.UsersId equals s.StudentId
                           join c in _context.Cities on u.UsersCity equals c.CityId into cityGroup
                           from c in cityGroup.DefaultIfEmpty()
                           join st in _context.States on u.UsersState equals st.StateId into stateGroup
                           from st in stateGroup.DefaultIfEmpty()
                           join co in _context.Countries on u.UsersCountry equals co.CountryId into countryGroup
                           from co in countryGroup.DefaultIfEmpty()
                           join d in _context.Departments on u.UsersDepartmentId equals d.DepartmentId into deptGroup
                           from d in deptGroup.DefaultIfEmpty()
                           where u.UsersRoleId == 3 && u.UsersId == id
                           select new
                           {
                               u.UsersId,
                               u.UsersName,
                               u.UsersUsername,
                               u.UsersEmail,
                               u.UsersMobile,
                               u.UsersDob,
                               u.UsersImage,
                               u.UsersAdd,
                               u.UserGender,
                               s.StudentCode,
                               s.StudentRollno,
                               s.StudentFatherName,
                               s.StudentDetails,
                               CityName = c != null ? c.CityName : null,
                               StateName = st != null ? st.StateName : null,
                               CountryName = co != null ? co.CountryName : null,
                               DepartmentTitle = d != null ? d.DepartmentTitle : null
                           };

            var result = students.ToList();

            if (result == null || !result.Any())
            {
                return NotFound(new
                {
                    message = "Sinh viên không tồn tại"
                });
            }
            return Ok(result);
        }

        [HttpPut("update-student")]
        public async Task<IActionResult> UpdateStudent(StudentDTO studentDTO)
        {
            //var errorResult = KiemTraTokenTeacher();
            //if (errorResult != null)
            //{
            //    return errorResult;
            //}

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existUserId = await _context.Users.SingleOrDefaultAsync(u => u.UsersId == studentDTO.UsersId && u.UsersRoleId == 3);
            var existStudent = await _context.Students.SingleOrDefaultAsync( s => s.StudentId == studentDTO.UsersId);
            if (existUserId ==  null || existStudent == null)
            {
                return BadRequest("Sinh viên không tồn tại");
            }

            // Kiểm tra email đã tồn tại chưa
            var existingEmail = await _context.Users.FirstOrDefaultAsync(u => u.UsersEmail == studentDTO.UsersEmail && u.UsersId != studentDTO.UsersId);
            if (existingEmail != null)
            {
                return BadRequest("Email đã tồn tại.");
            }

            // Kiểm tra email
            if (!Regex.IsMatch(studentDTO.UsersEmail, patternEmail))
            {
                return BadRequest(new
                {
                    message = "Email không hợp lệ",
                    data = studentDTO
                });
            }

            // Kiểm tra mobile đã tồn tại chưa
            var existingMobile = await _context.Users.FirstOrDefaultAsync(u => u.UsersMobile == studentDTO.UsersMobile && u.UsersId != studentDTO.UsersId);
            if (existingMobile != null)
            {
                return BadRequest("Số điện thoại đã tồn tại.");
            }

            // Kiểm tra sdt
            if (!Regex.IsMatch(studentDTO.UsersMobile, patternSDT))
            {
                return BadRequest(new
                {
                    message = "Số điện thoại không hợp lệ",
                    data = studentDTO
                });
            }

            // Kiểm tra MSSV đã tồn tại chưa
            var existingStudentCode = await _context.Students.FirstOrDefaultAsync(u => u.StudentCode == studentDTO.StudentCode && u.StudentId != studentDTO.UsersId);
            if (existingStudentCode != null)
            {
                return BadRequest("Mã số sinh viên đã tồn tại.");
            }
            /* Kiểm tra MSSV
             Quy tắc đặt MSSV theo mã đào tạo trường CĐ Kỹ Thuật Cao Thắng (CKC)
            [mã bậc(mã số)].[mã ngành(mã số)].[khoá(hai số cuối của năm)].[mã loại hình đào tạo(mã số)].[số thứ tự] */
            if (!Regex.IsMatch(studentDTO.StudentCode, patternMSSV))
            {
                return BadRequest(new
                {
                    message = "MSSV không đúng định dạng của trường Cao đẳng Kỹ Thuật Cao Thắng",
                    data = studentDTO
                });
            }

            var passwordHasher = new PasswordHasher<Users>();

            studentDTO.UsersName = Regex.Replace(studentDTO.UsersName.Trim(), @"\s+", " ");
            studentDTO.UsersAdd = Regex.Replace(studentDTO.UsersAdd.Trim(), @"\s+", " ");
            studentDTO.StudentFatherName = Regex.Replace(studentDTO.StudentFatherName.Trim(), @"\s+", " ");
            studentDTO.StudentDetails = Regex.Replace(studentDTO.StudentDetails.Trim(), @"\s+", " ");

            // Kiểm tra giới tính
            if (studentDTO.UserGender != "Nam" && studentDTO.UserGender != "Nữ")
            {
                return BadRequest(new
                {
                    message = "Giới tính chỉ chấp nhận giá trị Nam hoặc Nữ",
                    data = studentDTO
                });
            }

            // Kiểm tra ngày sinh
            if (studentDTO.UsersDob >= DateOnly.FromDateTime(DateTime.Today))
            {
                return BadRequest(new
                {
                    message = "Ngày sinh không thể lớn hơn ngày hiện tại",
                    data = studentDTO
                });
            }
            else
            {
                DateOnly ngayHienTai = DateOnly.FromDateTime(DateTime.Today);
                int tuoi = ngayHienTai.Year - studentDTO.UsersDob.Value.Year;

                if (ngayHienTai < studentDTO.UsersDob.Value.AddYears(tuoi))
                {
                    tuoi--;
                }

                if (tuoi < 18)
                {
                    return BadRequest(new
                    {
                        message = "Bạn chưa đủ 18 tuổi.",
                        data = studentDTO
                    });
                }
            }

            // Kiểm tra tồn tại phòng ban
            var existingDepartment = await _context.Departments.FirstOrDefaultAsync(u => u.DepartmentId == studentDTO.UsersDepartmentId);
            if (existingDepartment == null)
            {
                return BadRequest("Phòng ban không tồn tại.");
            }

            // Kiểm tra tồn tại thành phố
            var existingCity = await _context.Cities.FirstOrDefaultAsync(u => u.CityId == studentDTO.UsersCity);
            if (existingCity == null)
            {
                return BadRequest("Thành phố không tồn tại.");
            }

            // Kiểm tra tồn tại trạng thái
            var existingState = await _context.States.FirstOrDefaultAsync(u => u.StateId == studentDTO.UsersState);
            if (existingState == null)
            {
                return BadRequest("Trạng thái không tồn tại.");
            }

            // Kiểm tra tồn tại quốc gia
            var existingCountry = await _context.Countries.FirstOrDefaultAsync(u => u.CountryId == studentDTO.UsersCountry);
            if (existingCountry == null)
            {
                return BadRequest("Quốc gia không tồn tại.");
            }

            var saveUser = await _context.Users.FirstOrDefaultAsync(u => u.UsersId == studentDTO.UsersId);
            saveUser.UsersName = studentDTO.UsersName;
            saveUser.UsersEmail = studentDTO.UsersEmail;
            saveUser.UsersMobile = studentDTO.UsersMobile;
            saveUser.UsersDepartmentId = studentDTO.UsersDepartmentId;
            saveUser.UsersDob = studentDTO.UsersDob;
            saveUser.UsersAdd = studentDTO.UsersAdd;
            saveUser.UsersCity = studentDTO.UsersCity;
            saveUser.UsersCountry = studentDTO.UsersCountry;
            saveUser.UsersState = studentDTO.UsersState;
            saveUser.UserGender = studentDTO.UserGender;
            _context.Users.Update(saveUser);

            var saveStudent = await _context.Students.FirstOrDefaultAsync(u => u.StudentId == studentDTO.UsersId);
            saveStudent.StudentCode = studentDTO.StudentCode;
            saveStudent.StudentRollno = studentDTO.StudentRollno;
            saveStudent.StudentFatherName = studentDTO.StudentFatherName ?? "Chưa có";
            saveStudent.StudentDetails = studentDTO.StudentDetails ?? "Thông tin mặc định";

            _context.Students.Update(saveStudent);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {

                throw;
            }

            return NoContent();
        }

        [HttpPost("add-student")]
        public async Task<ActionResult<Student>> AddStudent(StudentDTO studentDTO)
        {
            var errorResult = KiemTraTokenTeacher();
            if (errorResult != null)
            {
                return errorResult;
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Kiểm tra email đã tồn tại chưa
            var existingEmail = await _context.Users.FirstOrDefaultAsync(u => u.UsersEmail == studentDTO.UsersEmail);
            if (existingEmail != null)
            {
                return BadRequest("Email đã tồn tại.");
            }

            // Kiểm tra email
            if (!Regex.IsMatch(studentDTO.UsersEmail, patternEmail))
            {
                return BadRequest(new
                {
                    message = "Email không hợp lệ",
                    data = studentDTO
                });
            }

            // Kiểm tra mobile đã tồn tại chưa
            var existingMobile = await _context.Users.FirstOrDefaultAsync(u => u.UsersMobile == studentDTO.UsersMobile);
            if (existingMobile != null)
            {
                return BadRequest("Số điện thoại đã tồn tại.");
            }

            // Kiểm tra sdt
            if (!Regex.IsMatch(studentDTO.UsersMobile, patternSDT))
            {
                return BadRequest(new
                {
                    message = "Số điện thoại không hợp lệ",
                    data = studentDTO
                });
            }

            // Kiểm tra MSSV đã tồn tại chưa
            var existingStudentCode = await _context.Students.FirstOrDefaultAsync(u => u.StudentCode == studentDTO.StudentCode);
            if (existingStudentCode != null)
            {
                return BadRequest("Mã số sinh viên đã tồn tại.");
            }
            /* Kiểm tra MSSV
             Quy tắc đặt MSSV theo mã đào tạo trường CĐ Kỹ Thuật Cao Thắng (CKC)
            [mã bậc(mã số)].[mã ngành(mã số)].[khoá(hai số cuối của năm)].[mã loại hình đào tạo(mã số)].[số thứ tự] */
            if (!Regex.IsMatch(studentDTO.StudentCode, patternMSSV))
            {
                return BadRequest(new
                {
                    message = "MSSV không đúng định dạng của trường Cao đẳng Kỹ Thuật Cao Thắng",
                    data = studentDTO
                });
            }

            var passwordHasher = new PasswordHasher<Users>();

            studentDTO.UsersName = Regex.Replace(studentDTO.UsersName.Trim(), @"\s+", " ");
            studentDTO.UsersAdd = Regex.Replace(studentDTO.UsersAdd.Trim(), @"\s+", " ");
            studentDTO.StudentFatherName = Regex.Replace(studentDTO.StudentFatherName.Trim(), @"\s+", " ");
            studentDTO.StudentDetails = Regex.Replace(studentDTO.StudentDetails.Trim(), @"\s+", " ");

            // Kiểm tra giới tính
            if (studentDTO.UserGender != "Nam" && studentDTO.UserGender != "Nữ")
            {
                return BadRequest(new
                {
                    message = "Giới tính chỉ chấp nhận giá trị Nam hoặc Nữ",
                    data = studentDTO
                });
            }

            // Kiểm tra ngày sinh
            if (studentDTO.UsersDob >= DateOnly.FromDateTime(DateTime.Today))
            {
                return BadRequest(new
                {
                    message = "Ngày sinh không thể lớn hơn ngày hiện tại",
                    data = studentDTO
                });
            }
            else
            {
                DateOnly ngayHienTai = DateOnly.FromDateTime(DateTime.Today);
                int tuoi = ngayHienTai.Year - studentDTO.UsersDob.Value.Year;

                if (ngayHienTai < studentDTO.UsersDob.Value.AddYears(tuoi))
                {
                    tuoi--;
                }

                if (tuoi < 18)
                {
                    return BadRequest(new
                    {
                        message = "Bạn chưa đủ 18 tuổi.",
                        data = studentDTO
                    });
                }
            }

            // Kiểm tra tồn tại phòng ban
            var existingDepartment = await _context.Departments.FirstOrDefaultAsync(u => u.DepartmentId == studentDTO.UsersDepartmentId);
            if (existingDepartment == null)
            {
                return BadRequest("Phòng ban không tồn tại.");
            }

            // Kiểm tra tồn tại thành phố
            var existingCity = await _context.Cities.FirstOrDefaultAsync(u => u.CityId == studentDTO.UsersCity);
            if (existingCity == null)
            {
                return BadRequest("Thành phố không tồn tại.");
            }

            // Kiểm tra tồn tại trạng thái
            var existingState = await _context.States.FirstOrDefaultAsync(u => u.StateId == studentDTO.UsersState);
            if (existingState == null)
            {
                return BadRequest("Trạng thái không tồn tại.");
            }

            // Kiểm tra tồn tại quốc gia
            var existingCountry = await _context.Countries.FirstOrDefaultAsync(u => u.CountryId == studentDTO.UsersCountry);
            if (existingCountry == null)
            {
                return BadRequest("Quốc gia không tồn tại.");
            }

            // Tạo user
            var user = new Users
            {
                UsersRoleId = 3,
                UsersName = studentDTO.UsersName,
                UsersUsername = studentDTO.UsersEmail,
                UsersPassword = passwordHasher.HashPassword(null, studentDTO.StudentCode),
                UsersEmail = studentDTO.UsersEmail,
                UsersMobile = studentDTO.UsersMobile,
                UserLevelId = 1,
                UsersDepartmentId = studentDTO.UsersDepartmentId,
                UsersDob = studentDTO.UsersDob,
                UsersAdd = studentDTO.UsersAdd,
                UsersCity = studentDTO.UsersCity,
                UsersCountry = studentDTO.UsersCountry,
                UsersState = studentDTO.UsersState,
                UserGender = studentDTO.UserGender,
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync(); // Lưu để lấy ID

            // Tạo Student
            var student = new Student
            {
                StudentId = user.UsersId,
                StudentCode = studentDTO.StudentCode,
                StudentRollno = studentDTO.StudentRollno,
                StudentFatherName = studentDTO.StudentFatherName ?? "Chưa có",
                StudentDetails = studentDTO.StudentDetails ?? "Thông tin mặc định"
            };

            await _context.Students.AddAsync(student);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(AddStudent), new { id = student.StudentId }, student);
        }
        [HttpPost("import-student")]
        public async Task<IActionResult> ImportStudents([FromForm] IFormFile file)
        {
            var errorResult = KiemTraTokenTeacher();
            if (errorResult != null)
            {
                return errorResult;
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest("File không hợp lệ.");
            }

            var allowedExtensions = new[] { ".xlsx" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest(new { message = "Chỉ chấp nhận file excel định dạng .xlsx" });
            }

            int successCount = 0;
            List<string> errorList = new List<string>();

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                    int rowCount = worksheet.Dimension.Rows;

                    for (int row = 2; row <= rowCount; row++)
                    {
                        try
                        {
                            var dobValue = worksheet.Cells[row, 4].Value;
                            DateOnly? parsedDob = null;

                            if (dobValue != null)
                            {
                                if (dobValue is double excelDate)
                                {
                                    DateTime dateTime = DateTime.FromOADate(excelDate);
                                    parsedDob = DateOnly.FromDateTime(dateTime);
                                }
                                else if (dobValue is DateTime dt)
                                {
                                    parsedDob = DateOnly.FromDateTime(dt);
                                }
                                else if (DateOnly.TryParse(dobValue.ToString(), out DateOnly dob))
                                {
                                    parsedDob = dob;
                                }
                                else
                                {
                                    errorList.Add($"Hàng {row}: Ngày sinh không hợp lệ.");
                                    continue;
                                }
                            }

                            string cityString = worksheet.Cells[row, 6].GetValue<string>();
                            int? cityId = ParseIdFromString(cityString);

                            string stateIdString = worksheet.Cells[row, 7].GetValue<string>();
                            int? stateId = ParseIdFromString(stateIdString);

                            string countryIdString = worksheet.Cells[row, 8].GetValue<string>();
                            int? countryId = ParseIdFromString(countryIdString);

                            string departmentIdString = worksheet.Cells[row, 9].GetValue<string>();
                            int departmentId = ParseIdFromString(departmentIdString) ?? 1;

                            var studentDTO = new StudentDTO
                            {
                                UsersName = worksheet.Cells[row, 1].GetValue<string>()?.Trim(),
                                UsersEmail = worksheet.Cells[row, 2].GetValue<string>()?.Trim(),
                                UsersMobile = worksheet.Cells[row, 3].GetValue<string>()?.Trim(),
                                UsersDob = parsedDob,
                                UsersAdd = worksheet.Cells[row, 5].GetValue<string>()?.Trim(),
                                UsersCity = cityId,
                                UsersState = stateId,
                                UsersCountry = countryId,
                                UsersDepartmentId = departmentId,
                                UserGender = worksheet.Cells[row, 10].GetValue<string>()?.Trim(),
                                StudentCode = worksheet.Cells[row, 11].GetValue<string>()?.Trim(),
                                StudentRollno = worksheet.Cells[row, 12].GetValue<string>()?.Trim(),
                                StudentFatherName = worksheet.Cells[row, 13].GetValue<string>()?.Trim(),
                                StudentDetails = worksheet.Cells[row, 14].GetValue<string>()?.Trim(),
                            };

                            var result = await AddStudent(studentDTO);
                            if (result.Result is CreatedAtActionResult)
                            {
                                successCount++;
                            }
                            else if (result.Result is BadRequestObjectResult badRequest)
                            {
                                errorList.Add($"Hàng {row}: {badRequest.Value}");
                            }
                        }
                        catch (Exception ex)
                        {
                            errorList.Add($"Hàng {row}: Lỗi không xác định - {ex.Message}");
                        }
                    }
                }
            }

            if (errorList.Any())
            {
                return BadRequest(new { message = $"Có lỗi khi import. Thêm thành công {successCount} sinh viên.", errors = errorList });
            }

            return Ok(new { message = $"Thêm thành công {successCount} sinh viên." });
        }

        private int? ParseIdFromString(string input)
        {
            if (!string.IsNullOrEmpty(input))
            {
                string[] parts = input.Split('-');
                if (int.TryParse(parts[0].Trim(), out int id))
                {
                    return id;
                }
            }
            return null;
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.StudentId == id);
        }
        private ActionResult? KiemTraTokenTeacher()
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

            if (role != "teacher" && role != "2")
            {
                return Unauthorized(new { message = "Bạn không phải là giáo viên" });
            }

            return null; // Không có lỗi
        }

    }
}
