using Microsoft.AspNetCore.Mvc;
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
        
    }
}
