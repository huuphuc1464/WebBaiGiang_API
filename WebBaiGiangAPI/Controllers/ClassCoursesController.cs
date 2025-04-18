using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
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
    public class ClassCoursesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;
        public ClassCoursesController(AppDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }
        [HttpGet("get-class-courses")]
        public async Task<ActionResult<IEnumerable<ClassCourse>>> GetClassCourses()
        {
            var errorResult = KiemTraTokenAdmin();
            if (errorResult != null)
            {
                return errorResult;
            }
            var result = from cc in _context.ClassCourses
                         join cl in _context.Classes on cc.ClassId equals cl.ClassId
                         join co in _context.Courses on cc.CourseId equals co.CourseId
                         select new {
                            cc.CcId,
                            cc.CcDescription,
                            cl.ClassTitle,
                            cl.ClassDescription,
                            cl.ClassSemesterId,
                            cl.ClassSyearId,
                            cl.ClassUpdateAt,
                            co.CourseDepartmentId,
                            co.CourseTitle,
                            co.CourseTotalSemester,
                            co.CourseImage,
                            co.CourseShortdescription,
                            co.CourseDescription,
                            co.CourseUpdateAt,
                         };
            var classCourse = result.ToList();
            if (classCourse == null)
            {
                return NotFound(new
                {
                    message = "Hiện tại không có lớp học phần nào"
                });
            }
            return Ok(new
            {
                message = "Danh sách lớp học phần",
                data = classCourse
            });
        }
        [HttpGet("get-class-course")]
        public async Task<ActionResult<ClassCourse>> GetClassCourse(int id)
        {
            var errorResult = KiemTraTokenAdmin();
            if (errorResult != null)
            {
                return errorResult;
            }
            var result = from cc in _context.ClassCourses
                         join cl in _context.Classes on cc.ClassId equals cl.ClassId
                         join co in _context.Courses on cc.CourseId equals co.CourseId
                         join de in _context.Departments on co.CourseDepartmentId equals de.DepartmentId
                         join sy in _context.SchoolYears on cl.ClassSyearId equals sy.SyearId
                         join se in _context.Semesters on cl.ClassSemesterId equals se.SemesterId
                         where cc.CcId == id
                         select new
                         {
                             cc.CcId,
                             cc.CcDescription,
                             cl.ClassTitle,
                             cl.ClassDescription,
                             se.SemesterTitle,
                             sy.SyearTitle,
                             cl.ClassUpdateAt,
                             de.DepartmentTitle,
                             co.CourseTitle,
                             co.CourseTotalSemester,
                             co.CourseImage,
                             co.CourseShortdescription,
                             co.CourseDescription,
                             co.CourseUpdateAt,
                         };
            var classCourse = await result.FirstOrDefaultAsync();
            if (classCourse == null)
            {
                return NotFound("Lớp học phần không tồn tại");
            }

            return Ok(classCourse);
        }
        [HttpPut("update-class-course")]
        public async Task<IActionResult> UpdateClassCourse(ClassCourse classCourse)
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
            if (!_context.ClassCourses.Any(cc => cc.CcId == classCourse.CcId)) 
            {
                return Unauthorized(new { message = "Lớp học phần không tồn tại" });
            }
            if (!_context.Classes.Any(s => s.ClassId == classCourse.ClassId))
            {
                return BadRequest(new
                {
                    message = "Lớp học không tồn tại",
                    data = classCourse
                });
            }
            if (!_context.Courses.Any(s => s.CourseId == classCourse.CourseId))
            {
                return BadRequest(new
                {
                    message = "Học phần không tồn tại",
                    data = classCourse
                });
            }
            if (classCourse.CcDescription != null)
            {
                classCourse.CcDescription = Regex.Replace(classCourse.CcDescription.Trim(), @"\s+", " ");
            }
            _context.ClassCourses.Update(classCourse);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {

                throw;
                
            }

            return Ok("Cập nhật lớp học phần thành công");
        }
        [HttpPost("add-class-course")]
        public async Task<ActionResult<ClassCourse>> AddClassCourse(ClassCourse classCourse)
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
            if (!_context.Classes.Any(s => s.ClassId == classCourse.ClassId))
            {
                return BadRequest(new
                {
                    message = "Lớp học không tồn tại",
                    data = classCourse
                });
            }
            if (!_context.Courses.Any(s => s.CourseId == classCourse.CourseId ))
            {
                return BadRequest(new
                {
                    message = "Học phần không tồn tại",
                    data = classCourse
                });
            }
            if (classCourse.CcDescription != null)
            {
                classCourse.CcDescription = Regex.Replace(classCourse.CcDescription.Trim(), @"\s+", " ");
            }
            _context.ClassCourses.Add(classCourse);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Thêm lớp học phần thành công",
                data = classCourse
            });
        }
        [HttpDelete("delete-course")]
        public async Task<IActionResult> DeleteClassCourse(int id)
        {
            var errorResult = KiemTraTokenAdmin();
            if (errorResult != null)
            {
                return errorResult;
            }
            try
            {
                var classCourse = await _context.ClassCourses.FindAsync(id);
                if (classCourse == null)
                {
                    return NotFound("Lớp học phần không tồn tại");
                }

                _context.ClassCourses.Remove(classCourse);
                await _context.SaveChangesAsync();

                return Ok("Xóa thành công lớp học phần");
            }
            catch (Exception)
            {
                return BadRequest("Không thể xóa lớp học phần hiện tại");
            }
        }
        [HttpGet("search")]
        public async Task<IActionResult> Search(
        string? keyword = "",
        int? minRating = null, // Lọc theo số sao đánh giá (có thể null)
        int page = 1,
        int pageSize = 10)
        {
            // Chuẩn hóa từ khóa và tách thành danh sách từ
            var keywords = keyword?.ToLower().Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? new string[0];

            var query = from co in _context.Courses
                        join de in _context.Departments on co.CourseDepartmentId equals de.DepartmentId into dept
                        from de in dept.DefaultIfEmpty()
                        join cc in _context.ClassCourses on co.CourseId equals cc.CourseId into classCourse
                        from cc in classCourse.DefaultIfEmpty()
                        join cl in _context.Classes on cc.ClassId equals cl.ClassId into classes
                        from cl in classes.DefaultIfEmpty()
                        join tc in _context.TeacherClasses on cl.ClassId equals tc.ClassCourses.ClassId into teacherClass
                        from tc in teacherClass.DefaultIfEmpty()
                        join u in _context.Users on tc.TcUsersId equals u.UsersId into users
                        from u in users.DefaultIfEmpty()
                        join f in _context.Feedbacks on cl.ClassId equals f.FeedbackClassId into feedbacks
                        from f in feedbacks.DefaultIfEmpty()
                        where keywords.Length == 0 || // Nếu không có từ khóa thì lấy tất cả
                            keywords.Any(kw =>
                                (co.CourseTitle != null && co.CourseTitle.ToLower().Contains(kw)) ||
                                (co.CourseShortdescription != null && co.CourseShortdescription.ToLower().Contains(kw)) ||
                                (co.CourseDescription != null && co.CourseDescription.ToLower().Contains(kw)) ||
                                (de != null && de.DepartmentTitle != null && de.DepartmentTitle.ToLower().Contains(kw)) ||
                                (cl != null && cl.ClassTitle != null && cl.ClassTitle.ToLower().Contains(kw)) ||
                                (tc != null && u != null && u.UsersName.ToLower().Contains(kw))
                            )
                        group new { co, de, u, f, cl } by new
                        {
                            co.CourseImage,
                            co.CourseTitle,
                            co.CourseShortdescription,
                            co.CourseDescription,
                            co.CourseUpdateAt,
                            de.DepartmentTitle,
                            cl.ClassTitle,
                            u.UsersName
                        } into g
                        select new
                        {
                            g.Key.CourseImage,
                            g.Key.CourseTitle,
                            g.Key.CourseShortdescription,
                            g.Key.CourseDescription,
                            g.Key.CourseUpdateAt,
                            g.Key.DepartmentTitle,
                            g.Key.ClassTitle,
                            TeacherNames = g.Select(x => x.u.UsersName)
                                            .Where(name => !string.IsNullOrEmpty(name))
                                            .Distinct()
                                            .ToList(),
                            AvgRating = g.Any(x => x.f != null) ? g.Average(x => (double?)x.f.FeedbackRate) : (double?)0.0
                        };


            // Lọc theo số sao đánh giá nếu có
            if (minRating.HasValue)
            {
                query = query.Where(x => x.AvgRating >= minRating.Value);
            }

            // Tổng số kết quả tìm thấy
            int totalItems = await query.CountAsync();

            var result = await query.OrderByDescending(x => x.CourseUpdateAt)
                                    .Skip((page - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToListAsync();

            // Trả về JSON
            return Ok(new
            {
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                CurrentPage = page,
                PageSize = pageSize,
                Data = result
            });
        }

        // XEM CHI TIẾT LỚP HỌC PHẦN
        /*
         Nội dung đầy đủ của học phần, lớp học phần
            • Ảnh đại diện. x
            • Tên khoá học. x
            • Mô tả ngắn gọn nội dung học phần, lớp học phần. x
            • Mô tả chi tiết nội dung học phần. x
            • Điểm đánh giá lớp học phần & số lượng sinh viên đánh giá x
            • Lần cập nhật cuối học phần, lớp học phần. x
            • Đề cương học phần, cho phép xem nội dung bài giảng theo từng buổi (15 tuần).
            • Danh sách feedback của học viên về khoá học x
         */
        [HttpGet("classcourse/detail/{classCourseId}")]
        public async Task<IActionResult> GetClassCourseDetail(int classCourseId)
        {
            var classCourse = await _context.ClassCourses
                .Include(cc => cc.Classes)
                .Include(cc => cc.Course)
                .Where(cc => cc.CcId == classCourseId)
                .Select(cc => new
                {
                    CcId = cc.CcId,
                    CcDescription = cc.CcDescription,
                    ClassTitle = cc.Classes.ClassTitle,
                    ClassDescription = cc.Classes.ClassDescription,
                    CourseTitle = cc.Course.CourseTitle,
                    CourseDescription = cc.Course.CourseDescription,
                    CourseImage = cc.Course.CourseImage,
                    CourseShortdescription = cc.Course.CourseShortdescription,
                    CourseUpdateAt = cc.Course.CourseUpdateAt,
                })
                .FirstOrDefaultAsync();

            if (classCourse == null)
            {
                return NotFound("Lớp học phần không tồn tại");
            }

            // Điểm đánh giá lớp học phần & số lượng sinh viên đánh giá
            var feedbacks = await _context.Feedbacks
                .Where(f => f.FeedbackClassId == classCourseId)
                .ToListAsync();
            var totalFeedbacks = feedbacks.Count;
            var avgRating = feedbacks.Any() ? feedbacks.Average(f => f.FeedbackRate) : 5;
            var feedbackDetails = feedbacks.Select(f => new
            {
                f.FeedbackId,
                f.FeedbackRate,
                f.FeedbackContent,
                f.FeedbackDate,
                f.FeedbackUsersId,
                f.FeedbackClassId
            }).ToList();

            // Đề cương học phần, cho phép xem nội dung bài giảng theo từng buổi (15 tuần).
            var lessons = await _context.Lessons
                .Include(l => l.LessonFiles)
                .Where(l => l.LessonClassCourseId == classCourseId)
                .Select(l => new
                {
                    l.LessonId,
                    l.LessonChapter,
                    l.LessonDescription,
                    l.LessonWeek,
                    l.LessonClassCourseId,
                    l.LessonUpdateAt,
                    l.LessonName,
                    l.LessonCreateAt,
                    l.LessonStatus,
                    LessonFiles = l.LessonFiles.Select(f => new
                    {
                        f.LfType,
                        f.LfPath
                    }).ToList()
                })
                .ToListAsync();

            return Ok(new
            {
                TotalFeedbacks = totalFeedbacks,
                AvgRating = avgRating,
                FeedbackDetails = feedbackDetails,
                Lessons = lessons,
                ClassCourse = classCourse
            });
        }

        /*
         XEM DANH SÁCH CÁC HỌC PHẦN, LỚP HỌC PHẦN
            − Theo Khoa, Giảng viên. x
            − Có phân trang danh sách các lớp học phần. x
            Lưu ý: Học phần hiển thị trên trang chủ & trang danh sách gồm các thông tin sau
            • Tên học phần. x
            • Khoa, ngành. x
            • Giảng viên. x
            • Danh sách sinh viên lớp học phần. x
            • Ảnh đại diện khoá học x
            • Điểm danh, bảng điểm. x
         */
        [HttpGet("classcourse/list")]
        public async Task<IActionResult> GetClassCourseList(
            int? departmentId = null,
            int? teacherId = null,
            int page = 1,
            int pageSize = 10)
        {
            var query = _context.ClassCourses
                .Include(cc => cc.Classes)
                .Include(cc => cc.Course)
                    .ThenInclude(c => c.Department)
               .Include(tc => tc.TeacherClasses)
                  .ThenInclude(tc => tc.User)
               .AsQueryable();

            if (departmentId.HasValue)
            {
                query = query.Where(cc => cc.Course.Department.DepartmentId == departmentId);
            }

            if (teacherId.HasValue)
            {
                query = query.Where(cc => cc.TeacherClasses.Any(tc => tc.TcUsersId == teacherId));
            }

            var totalItems = await query.CountAsync();

            if (totalItems == 0)
            {
                return NotFound("Không có lớp học phần nào");
            }

            var classCourses = await query
                .OrderByDescending(cc => cc.Course.CourseUpdateAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Danh sách sinh viên lớp học phần.
            var students = await _context.StudentClasses
                .Include(cs => cs.Student)
                .ThenInclude(s => s.Users)
                .Where(cs => cs.ScClassId == classCourses.FirstOrDefault().ClassId)
                .Select(cs => new
                {
                    cs.Student.Users.UsersName,
                    cs.Student.Users.UsersEmail,
                    cs.Student.StudentCode,
                })
                .ToListAsync();

            // Điểm danh, bảng điểm.
            var attendance = await _context.AttendanceMarks
                .Include(sc => sc.Student)
                    .ThenInclude(s => s.Users)
                .Include(sc => sc.Classes)
                .Where(a => a.ClassId == classCourses.FirstOrDefault().ClassId)
                .Select(a => new
                {
                    a.AttendanceMarksId,
                    a.AttendanceDate,
                    a.AttendanceStatus,
                    StudentName = a.Student.Users.UsersName,
                    StudentEmail = a.Student.Users.UsersEmail,
                    StudentCode = a.Student.StudentCode
                })
                .ToListAsync();

            var result = classCourses.Select(cc => new
            {
                CcId = cc.CcId,
                CcDescription = cc.CcDescription,
                ClassTitle = cc.Classes.ClassTitle,
                ClassDescription = cc.Classes.ClassDescription,
                Department = cc.Course.Department.DepartmentTitle,
                TeacherName = cc.TeacherClasses.Select(tc => tc.User.UsersName).FirstOrDefault(),
                CourseTitle = cc.Course.CourseTitle,
                CourseDescription = cc.Course.CourseDescription,
                CourseShortDescription = cc.Course.CourseShortdescription,
                CourseImage = cc.Course.CourseImage,
                CourseUpdateAt = cc.Course.CourseUpdateAt,
                Students = students,
                Attendance = attendance
            });

            return Ok(new
            {
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                CurrentPage = page,
                PageSize = pageSize,
                Data = result
            });
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
