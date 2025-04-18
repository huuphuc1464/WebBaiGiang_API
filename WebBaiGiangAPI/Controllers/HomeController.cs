using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using WebBaiGiangAPI.Data;
using WebBaiGiangAPI.Models;

namespace WebBaiGiangAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;
        public HomeController(AppDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }
        [HttpGet("get-menu")]
        public async Task<ActionResult> GetMenu()
        {
            var result = (from de in _context.Departments
                          join te in _context.Users on de.DepartmentId equals te.UsersDepartmentId into userGroup
                          from te in userGroup.Where(u => u.UsersRoleId == 2).DefaultIfEmpty()
                          join co in _context.Courses on de.DepartmentId equals co.CourseDepartmentId into courseGroup
                          from co in courseGroup.DefaultIfEmpty()
                          join cc in _context.ClassCourses on co.CourseId equals cc.CourseId into classCourseGroup
                          from cc in classCourseGroup.DefaultIfEmpty()
                          join cl in _context.Classes on cc.ClassId equals cl.ClassId into classGroup
                          from cl in classGroup.DefaultIfEmpty()
                          select new
                          {
                              de.DepartmentId,
                              de.DepartmentTitle,
                              UsersId = te != null ? te.UsersId : (int?)null,  // Kiểm tra null
                              UsersName = te != null ? te.UsersName : null,    // Kiểm tra null
                              CourseId = co != null ? co.CourseId : (int?)null,  // Kiểm tra null
                              CourseTitle = co != null ? co.CourseTitle : null,  // Kiểm tra null
                              ClassId = cc != null ? cc.ClassId : (int?)null,   // Kiểm tra null
                              ClassTitle = cl != null ? cl.ClassTitle : null   // Kiểm tra null
                          })
              .Distinct()
              .OrderBy(x => x.DepartmentId)
              .ThenBy(x => x.CourseId)
              .ThenBy(x => x.ClassId)
              .ToList();
            return Ok(result);
        }

        /*
         BÁO CÁO CỦA HỆ THỐNG QUẢN LÝ E-LEARNING: Báo cáo về Nhật ký hoạt động, Giáo viên, Lớp học, Bài tập, Khoa, Sự kiện, File, Sinh viên và Môn học.
         */
        [HttpGet("report/overview")]
        public async Task<IActionResult> GetSystemOverviewReport([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            var from = fromDate ?? DateTime.MinValue;
            var to = toDate ?? DateTime.MaxValue;

            var totalActivities = await _context.UserLogs
                .Where(x => x.UlogLoginDate >= from && x.UlogLoginDate <= to)
                .CountAsync();

            var totalTeachers = await _context.Users
                .Where(x => x.UsersRoleId == 2)
                .CountAsync();

            var totalClasses = await _context.Classes.CountAsync();

            var totalClassCourses = await _context.ClassCourses.CountAsync();

            var totalCourses = await _context.Courses.CountAsync();

            var totalAssignments = await _context.Assignments.CountAsync();

            var totalEvents = await _context.Events
                .Where(x => x.EventDateStart >= from && x.EventDateEnd <= to)
                .CountAsync();

            var totalFiles = await _context.Files.CountAsync();

            var totalStudents = await _context.Users
                .Where(x => x.UsersRoleId == 3)
                .CountAsync();

            var totalSubjects = await _context.Subjects.CountAsync();

            var topActiveUsers = await _context.UserLogs
                .Include(x => x.Users)
                .Where(x => x.UlogLoginDate >= from && x.UlogLoginDate <= to)
                .GroupBy(x => x.UlogUsersId)
                .Select(g => new
                {
                    Username = g.FirstOrDefault().Users.UsersUsername, // Lấy username từ Users
                    ActivityCount = g.Count()
                })
                .OrderByDescending(x => x.ActivityCount)
                .Take(5)
                .ToListAsync();

            var monthlyActivityRaw = await _context.UserLogs
             .Where(x => x.UlogLoginDate >= from && x.UlogLoginDate <= to)
             .GroupBy(x => new { x.UlogLoginDate.Year, x.UlogLoginDate.Month })
             .Select(g => new
             {
                 Year = g.Key.Year,
                 Month = g.Key.Month,
                 Count = g.Count()
             })
             .OrderBy(x => x.Year)
             .ThenBy(x => x.Month)
             .ToListAsync();

            var monthlyActivity = monthlyActivityRaw
                .Select(x => new
                {
                    Label = $"{x.Month:D2}/{x.Year}",
                    x.Count
                })
                .ToList();


            return Ok(new
            {
                TotalActivities = totalActivities,
                TotalTeachers = totalTeachers,
                TotalClasses = totalClasses,
                TotalAssignments = totalAssignments,
                TotalEvents = totalEvents,
                TotalFiles = totalFiles,
                TotalStudents = totalStudents,
                TotalSubjects = totalSubjects,
                TopActiveUsers = topActiveUsers,
                MonthlyActivityChart = monthlyActivity
            });
        }
    }
}
