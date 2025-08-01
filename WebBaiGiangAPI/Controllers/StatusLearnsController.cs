﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit.Tnef;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using WebBaiGiangAPI.Data;
using WebBaiGiangAPI.Models;

namespace WebBaiGiangAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatusLearnsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StatusLearnsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("get-status-learn-by-lesson/{lessonId}")]
        public async Task<IActionResult> GetStatusLearnByLesson(int lessonId)
        {
            var result = await _context.StatusLearns
                .Join(_context.Users, sl => sl.SlStudentId, u => u.UsersId, (sl, u) => new { sl, u })
                .Join(_context.Students, sl => sl.sl.SlStudentId, s => s.StudentId, (sl, s) => new { sl, s })
                .Join(_context.Lessons, sl => sl.sl.sl.SlLessonId, l => l.LessonId, (sl, l) => new { sl, l })
                .Where(sl => sl.sl.sl.sl.SlLessonId == lessonId)
                .Select(a => new
                {
                    a.sl.sl.sl.SlId,
                    a.sl.sl.sl.SlStudentId,
                    a.sl.sl.u.UsersName,
                    a.sl.s.StudentCode,
                    a.sl.sl.u.UsersEmail,
                    a.sl.sl.sl.SlLessonId,
                    a.l.LessonName,
                    a.sl.sl.sl.SlStatus,
                    a.sl.sl.sl.SlLearnedDate
                })
                .ToListAsync();
            if (result.Count == 0) return NotFound("Vẫn chưa có sinh viên nào hoàn thành");
            return Ok(result);
        }

        [HttpGet("get-detail-status-learn/{lessonId}/{studentId}")]
        public async Task<IActionResult> GetDetailStatusLearn(int lessonId, int studentId)
        {
            var result = await _context.StatusLearns
                            .Join(_context.Users, sl => sl.SlStudentId, u => u.UsersId, (sl, u) => new { sl, u })
                            .Join(_context.Students, sl => sl.sl.SlStudentId, s => s.StudentId, (sl, s) => new { sl, s })
                            .Join(_context.Lessons, sl => sl.sl.sl.SlLessonId, l => l.LessonId, (sl, l) => new { sl, l })
                            .Where(sl => sl.sl.sl.sl.SlLessonId == lessonId && sl.sl.sl.sl.SlStudentId == studentId)
                            .Select(a => new
                            {
                                a.sl.sl.sl.SlId,
                                a.sl.sl.sl.SlStudentId,
                                a.sl.sl.u.UsersName,
                                a.sl.s.StudentCode,
                                a.sl.sl.u.UsersEmail,
                                a.sl.sl.sl.SlLessonId,
                                a.l.LessonName,
                                a.sl.sl.sl.SlStatus,
                                a.sl.sl.sl.SlLearnedDate
                            })
                            .ToListAsync();
            if (!result.Any()) return NotFound("Không tìm thấy thông tin");
            return Ok(result);
        }

        [HttpPost("add-status-learn")]
        public async Task<IActionResult> AddStatusLearn(StatusLearn statusLearn)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (_context.StatusLearns.Any(sl => sl.SlLessonId == statusLearn.SlLessonId && sl.SlStudentId == statusLearn.SlStudentId))
                return BadRequest("Sinh viên đã hoàn thành bài giảng này");
            if (!_context.Lessons.Any(l => l.LessonId == statusLearn.SlLessonId))
                return BadRequest("Không tìm thấy bài giảng");
            if (!_context.Students.Any(s => s.StudentId == statusLearn.SlStudentId))
                return BadRequest("Không tìm thấy sinh viên");
            //var classId = _context.Lessons.FirstOrDefault(l => l.LessonId == statusLearn.SlLessonId).ClassCourse.ClassId;
            var classId = _context.Lessons
                .Join(_context.ClassCourses, l => l.LessonClassCourseId, cc => cc.CcId, (l, cc) => new { l, cc })
                .Where(classId => classId.l.LessonId == statusLearn.SlLessonId)
                .Select(lc => lc.cc.ClassId)
                .FirstOrDefault();

            if (!_context.StudentClasses.Any(sc => sc.ScClassId == classId && sc.ScStudentId == statusLearn.SlStudentId))
                return BadRequest("Sinh viên không thuộc lớp học này");
            statusLearn.SlLearnedDate = DateTime.Now;
            statusLearn.SlStatus = true;
            _context.StatusLearns.Add(statusLearn);
            await _context.SaveChangesAsync();

            return Ok(statusLearn);
        }

        // Thống kê Tổng số sinh viên hoàn thành từng bài giảng theo lớp học phần.
        [HttpGet("statistics-student-learned-by-class/{classCourseId}")]
        public async Task<IActionResult> StatisticsStudentLearnedByClass(int classCourseId)
        {
            var result = await _context.Lessons
                .Where(l => l.LessonClassCourseId == classCourseId)
                .Select(l => new
                {
                    LessonId = l.LessonId,
                    LessonName = l.LessonName,
                    TotalStudentLearned = _context.StatusLearns
                        .Count(sl => sl.SlLessonId == l.LessonId && sl.SlStatus == true),
                    StudentList = _context.StatusLearns
                        .Where(sl => sl.SlLessonId == l.LessonId && sl.SlStatus == true)
                        .Select(sl => new
                        {
                            StudentCode = sl.Students.StudentCode,
                            UsersName = sl.Students.Users.UsersName
                        })
                        .Distinct()
                        .ToList()
                })
                .OrderByDescending(a => a.TotalStudentLearned)
                .ToListAsync();

            if (!result.Any())
                return NotFound("Chưa có sinh viên nào hoàn thành bài giảng trong lớp học phần này.");

            return Ok(result);
        }

        // Thống kê Tổng số sinh viên hoàn thành từng bài giảng tất cả các lớp học phần.
        [HttpGet("statistics-student-learned")]
        public async Task<IActionResult> StatisticsStudentLearned()
        {
            var result = await _context.ClassCourses
        .Select(cc => new
        {
            ClassCourseId = cc.CcId,  
            CourseName = cc.Course.CourseTitle,
            ClassName = cc.Classes.ClassTitle,

            Statistics = _context.Lessons
                .Where(l => l.ClassCourse.ClassId == cc.ClassId && l.ClassCourse.CourseId == cc.CourseId)
                .GroupJoin(
                    _context.StatusLearns.Where(sl => sl.SlStatus == true),
                    l => l.LessonId, 
                    sl => sl.SlLessonId,
                    (l, slGroup) => new
                    {
                        LessonId = l.LessonId,
                        LessonName = l.LessonName,
                        LessonDescription = l.LessonDescription,
                        TotalStudentLearned = slGroup.Count() 
                    }
                ).ToList()
            })
            .ToListAsync();

            if (!result.Any()) return NotFound("Chưa có sinh viên nào hoàn thành.");

            return Ok(result);
        }

        // Thống kê tỉ lệ hoàn thành bài học theo học viên ở lớp học phần (lọc theo thời gian nếu có).
        [HttpGet("statistics-student-progress/{classCourseId}")]
        public async Task<IActionResult> StatisticsStudentProgress(
            int classCourseId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            if (fromDate != null && fromDate > DateTime.Now) return BadRequest("Ngày bắt đầu không hợp lệ.");
            if (toDate != null && toDate > DateTime.Now) return BadRequest("Ngày kết thúc không hợp lệ.");
            if (fromDate != null && toDate != null && fromDate > toDate) return BadRequest("Ngày bắt đầu không thể lớn hơn ngày kết thúc.");

            // Lấy tổng số bài giảng trong lớp học phần (lọc theo thời gian nếu có)
            var lessonQuery = _context.Lessons
                .Where(l => _context.ClassCourses
                .Any(cc => cc.CcId == classCourseId && l.LessonClassCourseId == cc.CcId));
            if (fromDate.HasValue)
            {
                DateTime fromDateOnly = fromDate.Value.Date;
                lessonQuery = lessonQuery.Where(l => l.LessonCreateAt >= fromDateOnly);
            }

            if (toDate.HasValue)
            {
                DateTime toDateOnly = toDate.Value.Date.AddDays(1).AddTicks(-1);
                lessonQuery = lessonQuery.Where(l => l.LessonCreateAt <= toDateOnly);
            }

            var totalLessons = await lessonQuery.CountAsync();

            int classId = _context.ClassCourses
                .Where(cc => cc.CcId == classCourseId)
                .Select(cc => cc.ClassId)
                .FirstOrDefault();

            // Lấy danh sách tất cả sinh viên trong lớp học phần
            var studentsInClass = await _context.Students
                .Where(s => s.StudentClasses.Any(sc => sc.ScClassId == classId))
                .Select(s => new
                {
                    s.StudentId,
                    s.StudentCode,
                    s.Users.UsersName,
                    ClassId = classId,
                    CompletedLessons = 0, // Mặc định 0 nếu chưa học bài nào
                    TotalLessons = totalLessons,
                    CompletionRate = 0.0 // Tỉ lệ hoàn thành = 0%
                })
                .ToListAsync();

            // Lấy danh sách sinh viên đã học (lọc theo thời gian nếu có)
            var studentsWithProgressQuery = _context.Students
                .Join(_context.StatusLearns.Where(sl => sl.SlStatus == true),
                    s => s.StudentId,
                    sl => sl.SlStudentId,
                    (s, sl) => new { s, sl })
                .Join(_context.Lessons,
                    ssl => ssl.sl.SlLessonId,
                    l => l.LessonId,
                    (ssl, l) => new { ssl.s, l, ssl.sl.SlLearnedDate })
                .Where(x => _context.ClassCourses
                .Any(cc => cc.CcId == classCourseId && x.l.LessonClassCourseId == cc.CcId));

            if (fromDate.HasValue)
            {
                DateTime fromDateOnly = fromDate.Value.Date;
                studentsWithProgressQuery = studentsWithProgressQuery.Where(x => x.SlLearnedDate >= fromDateOnly && x.l.LessonCreateAt >= fromDateOnly);
            }

            if (toDate.HasValue)
            {
                DateTime toDateOnly = toDate.Value.Date.AddDays(1).AddTicks(-1);
                studentsWithProgressQuery = studentsWithProgressQuery.Where(x => x.SlLearnedDate <= toDateOnly && x.l.LessonCreateAt <= toDateOnly);
            }


            var studentsWithProgress = await studentsWithProgressQuery
                .GroupBy(x => new { x.s.StudentId, x.s.StudentCode, x.s.Users.UsersName })
                .Select(g => new
                {
                    g.Key.StudentId,
                    g.Key.StudentCode,
                    g.Key.UsersName,
                    ClassId = classId,
                    CompletedLessons = g.Count(),
                    TotalLessons = totalLessons,
                    CompletionRate = totalLessons > 0 ? Math.Round((g.Count() * 100.0) / totalLessons, 2) : 0.0 // Tính % hoàn thành
                })
                .ToListAsync();

            // Gộp danh sách sinh viên chưa học vào danh sách sinh viên đã học
            var result = (fromDate.HasValue || toDate.HasValue)
                ? studentsWithProgress
                : studentsInClass
                    .Select(s => studentsWithProgress.FirstOrDefault(sp => sp.StudentId == s.StudentId) ?? s)
                    .OrderByDescending(x => x.CompletedLessons)
                    .ToList();

            if (!result.Any()) return NotFound("Không có dữ liệu thống kê cho lớp học phần này.");

            return Ok(result);
        }

        // Top 10 học viên tích cực (hoàn thành nhiều bài giảng nhất) theo bài giảng và lớp học phần
        [HttpGet("top-students/{classCourseId}")]
        public async Task<IActionResult> GetTopStudents(
            int classCourseId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            // Kiểm tra đầu vào
            if (fromDate != null && fromDate > DateTime.Now) return BadRequest("Ngày bắt đầu không hợp lệ.");
            if (toDate != null && toDate > DateTime.Now) return BadRequest("Ngày kết thúc không hợp lệ.");
            if (fromDate != null && toDate != null && fromDate > toDate) return BadRequest("Ngày bắt đầu không thể lớn hơn ngày kết thúc.");

            // Lấy danh sách bài giảng của lớp học phần
            var lessonQuery = _context.Lessons
                .Where(l => _context.ClassCourses
                    .Any(cc => cc.CcId == classCourseId &&
                               l.LessonClassCourseId == cc.CcId));

            // Lọc bài giảng theo ngày nếu có fromDate hoặc toDate
            if (fromDate.HasValue)
            {
                DateTime fromDateOnly = fromDate.Value.Date;
                lessonQuery = lessonQuery.Where(l => l.LessonCreateAt >= fromDateOnly);
            }
            if (toDate.HasValue)
            {
                DateTime toDateOnly = toDate.Value.Date.AddDays(1).AddTicks(-1);
                lessonQuery = lessonQuery.Where(l => l.LessonCreateAt <= toDateOnly);
            }

            // Tổng số bài giảng trong lớp học phần
            var totalLessons = await lessonQuery.CountAsync();

            // Lấy danh sách học viên đã học bài giảng trong lớp học phần
            var topStudentsQuery = _context.Students
                .Join(_context.StatusLearns.Where(sl => sl.SlStatus == true),
                    s => s.StudentId,
                    sl => sl.SlStudentId,
                    (s, sl) => new { s, sl })
                .Join(lessonQuery,  
                    ssl => ssl.sl.SlLessonId,
                    l => l.LessonId,
                    (ssl, l) => new { ssl.s, l, ssl.sl.SlLearnedDate });

            // Lọc ngày học nếu có fromDate hoặc toDate
            if (fromDate.HasValue)
            {
                DateTime fromDateOnly = fromDate.Value.Date;
                topStudentsQuery = topStudentsQuery.Where(x => x.SlLearnedDate >= fromDateOnly);
            }
            if (toDate.HasValue)
            {
                DateTime toDateOnly = toDate.Value.Date.AddDays(1).AddTicks(-1);
                topStudentsQuery = topStudentsQuery.Where(x => x.SlLearnedDate <= toDateOnly);
            }

            // Nhóm theo học viên và lấy tất cả ngày đã học
            var topStudents = await topStudentsQuery
                .GroupBy(x => new { x.s.StudentId, x.s.StudentCode, x.s.Users.UsersName })
                .Select(g => new
                {
                    g.Key.StudentId,
                    g.Key.StudentCode,
                    g.Key.UsersName,
                    LearnedDates = g.Select(x => x.SlLearnedDate).Distinct().OrderBy(d => d).ToList(), 
                    CompletedLessons = g.Count(), 
                    TotalLessons = totalLessons, 
                    CompletionRate = totalLessons > 0 ? Math.Round((g.Count() * 100.0) / totalLessons, 2) : 0.0,
                    EarliestLearnedDate = g.Min(x => x.SlLearnedDate) 
                })
                .OrderByDescending(x => x.CompletedLessons) 
                .ThenBy(x => x.EarliestLearnedDate) 
                .Take(10) 
                .ToListAsync();

            if (!topStudents.Any()) return NotFound("Không có học viên nào hoàn thành bài giảng.");

            return Ok(topStudents);
        }

        // Thống kê học sinh đã/ chưa xem bài giảng theo lớp học phần
        [HttpGet("students-lesson-view-status/{classCourseId}")]
        public async Task<IActionResult> GetStudentsLessonViewStatus(int classCourseId)
        {
            // Lấy danh sách bài giảng theo lớp và khóa học
            var lessons = await _context.Lessons
                .Where(l => _context.ClassCourses.Any(cc =>
                    cc.CcId == classCourseId &&
                    l.LessonClassCourseId == cc.CcId))
                .Select(l => new
                {
                    l.LessonId,
                    l.LessonName,
                    l.LessonCreateAt,
                })
                .ToListAsync();

            if (!lessons.Any()) return NotFound("Không tìm thấy bài giảng nào.");
            
            int classId = _context.ClassCourses.Where(cc => cc.CcId == classCourseId)
                .Select(cc => cc.ClassId)
                .FirstOrDefault();

            // Lấy danh sách sinh viên của lớp học
            var students = await _context.StudentClasses
                .Where(sc => sc.ScClassId == classId)
                .Select(sc => sc.ScStudentId)
                .ToListAsync();

            var totalStudents = students.Count;
            if (totalStudents == 0) return NotFound("Không có sinh viên trong lớp học này.");

            // Danh sách bài giảng và số lượng SV đã xem/chưa xem
            var lessonViewStats = new List<object>();

            foreach (var lesson in lessons)
            {
                // Số sinh viên đã xem bài giảng
                var studentsViewed = await _context.StatusLearns
                    .Where(sl => sl.SlStatus == true && sl.SlLessonId == lesson.LessonId && students.Contains(sl.SlStudentId))
                    .Select(sl => sl.SlStudentId)
                    .Distinct()
                    .CountAsync();

                // Số sinh viên chưa xem bài giảng
                var studentsNotViewed = totalStudents - studentsViewed;

                lessonViewStats.Add(new
                {
                    lesson.LessonId,
                    lesson.LessonName,
                    lesson.LessonCreateAt,
                    StudentsViewed = studentsViewed,
                    StudentsNotViewed = studentsNotViewed
                });
            }

            return Ok(new
            {
                TotalStudents = totalStudents,
                Lessons = lessonViewStats
            });
        }

        // Thống kê học sinh đã/ chưa xem bài giảng theo lớp học phần chi tiết
        [HttpGet("students-lesson-view-status-detail/{classCourseId}")]
        public async Task<IActionResult> GetStudentsLessonViewStatusDetail(int classCourseId)
        {
            // Lấy danh sách bài giảng theo lớp & khóa học
            var lessons = await _context.Lessons
                .Where(l => _context.ClassCourses.Any(cc =>
                    cc.CcId == classCourseId &&
                    l.LessonClassCourseId == cc.CcId))
                .Select(l => new
                {
                    l.LessonId,
                    l.LessonName,
                    l.LessonCreateAt
                })
                .ToListAsync();

            if (!lessons.Any()) return NotFound("Không tìm thấy bài giảng nào.");

            int classId = _context.ClassCourses
                .Where(cc => cc.CcId == classCourseId)
                .Select(cc => cc.ClassId)
                .FirstOrDefault();

            // Lấy danh sách sinh viên của lớp học
            var students = await _context.StudentClasses
                .Where(sc => sc.ScClassId == classId)
                .Select(sc => new
                {
                    sc.ScStudentId,
                    sc.Student.Users.UsersName,
                    sc.Student.StudentCode
                })
                .ToListAsync();

            int totalStudents = students.Count;
            if (totalStudents == 0) return NotFound("Không có sinh viên trong lớp học này.");

            // Lấy danh sách sinh viên đã xem bài giảng
            var lessonViewData = await _context.StatusLearns
                .Where(sl => sl.SlStatus == true && lessons.Select(l => l.LessonId).Contains(sl.SlLessonId))
                .Select(sl => new { sl.SlLessonId, sl.SlStudentId })
                .ToListAsync();

            // Xử lý dữ liệu trả về
            var lessonViewStats = lessons.Select(lesson =>
            {
                // Danh sách SV đã xem
                var viewedStudentIds = new HashSet<int>(
                    lessonViewData.Where(sl => sl.SlLessonId == lesson.LessonId)
                    .Select(sl => sl.SlStudentId)
                );

                var studentsViewed = students
                    .Where(s => viewedStudentIds.Contains(s.ScStudentId))
                    .Select(s => new { s.ScStudentId, s.UsersName, s.StudentCode })
                    .ToList();

                var studentsNotViewed = students
                    .Where(s => !viewedStudentIds.Contains(s.ScStudentId))
                    .Select(s => new { s.ScStudentId, s.UsersName, s.StudentCode })
                    .ToList();

                return new
                {
                    lesson.LessonId,
                    lesson.LessonName,
                    lesson.LessonCreateAt,
                    StudentsViewed = studentsViewed.Count,
                    StudentsNotViewed = studentsNotViewed.Count,
                    ViewedList = studentsViewed,
                    NotViewedList = studentsNotViewed
                };
            }).ToList();

            return Ok(new
            {
                TotalStudents = totalStudents,
                Lessons = lessonViewStats
            });
        }

        // So sánh mức độ hoàn thành bài giảng giữa các lớp chung gv
        [HttpGet("compare-lesson-completion/{teacherId}/{courseId}")]
        public async Task<IActionResult> CompareLessonCompletion(int teacherId, int courseId, string? timeUnit = null)
        {
            // Chuẩn hóa timeUnit
            timeUnit = string.IsNullOrEmpty(timeUnit) ? null : timeUnit.ToLower();
            if (timeUnit != null && timeUnit != "week" && timeUnit != "month")
                return BadRequest("Đơn vị thời gian không hợp lệ. Chỉ hỗ trợ 'week', 'month' hoặc không truyền để lấy tất cả dữ liệu.");

            DateTime? startDate = timeUnit switch
            {
                "week" => DateTime.UtcNow.AddDays(-7),
                "month" => DateTime.UtcNow.AddMonths(-1),
                _ => null
            };

            // Lấy danh sách lớp mà giáo viên dạy thuộc courseId
            var teacherClasses = await _context.ClassCourses
                .Where(cc => cc.CourseId == courseId)
                .Join(_context.TeacherClasses,
                      cc => cc.ClassId,
                      tc => tc.ClassCourses.ClassId,
                      (cc, tc) => new { cc.ClassId, cc.CcId, tc.ClassCourses.Classes.ClassTitle, tc.TcUsersId })
                .Where(tc => tc.TcUsersId == teacherId)
                .ToListAsync();

            if (!teacherClasses.Any())
            {
                return NotFound("Không tìm thấy lớp nào của giáo viên trong khóa học này.");
            }

            var classIds = teacherClasses.Select(tc => tc.ClassId).ToList();
            var courseClassIds = teacherClasses.Select(tc => tc.CcId).ToList();

            // Lấy danh sách bài giảng theo các lớp học của giáo viên
            var lessons = await _context.Lessons
                .Where(l => courseClassIds.Contains(l.LessonClassCourseId))
                .Select(l => new { l.LessonId, l.LessonName, l.LessonClassCourseId, l.LessonCreateAt })
                .ToListAsync();

            var studentsPerClass = await _context.StudentClasses
                .Where(sc => classIds.Contains(sc.ScClassId))
                .GroupBy(sc => sc.ScClassId)
                .Select(g => new { ClassId = g.Key, TotalStudents = g.Count() })
                .ToListAsync();

            var lessonIds = lessons.Select(l => l.LessonId).ToList();

            var lessonViewDataQuery = _context.StatusLearns
                .Where(sl => sl.SlStatus == true && lessonIds.Contains(sl.SlLessonId));

            if (startDate.HasValue)
            {
                lessonViewDataQuery = lessonViewDataQuery.Where(sl => sl.SlLearnedDate >= startDate.Value);
            }

            var lessonViewData = await lessonViewDataQuery
                .GroupBy(sl => new { sl.SlLessonId, sl.SlLearnedDate.Date })
                .Select(g => new
                {
                    LessonId = g.Key.SlLessonId,
                    ViewedCount = g.Count(),
                    LearnedDate = g.Key.Date
                })
                .ToListAsync();

            var courseData = teacherClasses
                .GroupBy(tc => courseId)
                .Select(courseGroup => new
                {
                    CourseId = courseGroup.Key,
                    Classes = courseGroup
                        .GroupBy(tc => tc.ClassId) // Nhóm theo ClassId để tránh bị trùng lặp
                        .Select(classGroup => new
                        {
                            ClassId = classGroup.Key,
                            ClassName = classGroup.First().ClassTitle, // Lấy tên lớp từ phần tử đầu tiên
                            TotalStudents = studentsPerClass.FirstOrDefault(s => s.ClassId == classGroup.Key)?.TotalStudents ?? 0,
                            Lessons = lessons.Where(l => classGroup.Select(cg => cg.CcId).Contains(l.LessonClassCourseId))
                                             .Select(lesson =>
                                             {
                                                 var viewedCount = lessonViewData.FirstOrDefault(v => v.LessonId == lesson.LessonId)?.ViewedCount ?? 0;
                                                 var totalStudents = studentsPerClass.FirstOrDefault(s => s.ClassId == classGroup.Key)?.TotalStudents ?? 0;

                                                 var viewedLastWeek = lessonViewData
                                                     .Where(v => v.LessonId == lesson.LessonId && v.LearnedDate >= DateTime.UtcNow.AddDays(-7))
                                                     .Sum(v => v.ViewedCount);

                                                 var viewedLastMonth = lessonViewData
                                                     .Where(v => v.LessonId == lesson.LessonId && v.LearnedDate >= DateTime.UtcNow.AddMonths(-1))
                                                     .Sum(v => v.ViewedCount);

                                                 return new
                                                 {
                                                     lesson.LessonId,
                                                     lesson.LessonName,
                                                     lesson.LessonCreateAt,
                                                     CompletionOverTime = (timeUnit == "week") ? new[]
                                                     {
                                             new
                                             {
                                                 TimePeriod = "Last Week",
                                                 StudentsViewed = viewedLastWeek,
                                                 StudentsNotViewed = totalStudents - viewedLastWeek,
                                                 CompletionRate = totalStudents > 0 ? Math.Round((viewedLastWeek * 100.0) / totalStudents, 2) : 0
                                             }
                                                     } : (timeUnit == "month") ? new[]
                                                     {
                                             new
                                             {
                                                 TimePeriod = "Last Month",
                                                 StudentsViewed = viewedLastMonth,
                                                 StudentsNotViewed = totalStudents - viewedLastMonth,
                                                 CompletionRate = totalStudents > 0 ? Math.Round((viewedLastMonth * 100.0) / totalStudents, 2) : 0
                                             }
                                                     } : new[]
                                                     {
                                             new
                                             {
                                                 TimePeriod = "All Time",
                                                 StudentsViewed = viewedCount,
                                                 StudentsNotViewed = totalStudents - viewedCount,
                                                 CompletionRate = totalStudents > 0 ? Math.Round((viewedCount * 100.0) / totalStudents, 2) : 0
                                             }
                                                     }
                                                 };
                                             }).ToList()
                        }).ToList()
                }).ToList();

            return Ok(new { Courses = courseData });
        }

        // So sánh mức độ hoàn thành bài giảng giữa các lớp chung học phần(admin)
        [HttpGet("compare-lesson-completion/{courseId}")]
        public async Task<IActionResult> CompareLessonCompletion(int courseId, string? timeUnit = null)

        {
            // Chuẩn hóa timeUnit
            timeUnit = string.IsNullOrEmpty(timeUnit) ? null : timeUnit.ToLower();
            if (timeUnit != null && timeUnit != "week" && timeUnit != "month")
                return BadRequest("Đơn vị thời gian không hợp lệ. Chỉ hỗ trợ 'week', 'month' hoặc không truyền để lấy tất cả dữ liệu.");

            DateTime? startDate = timeUnit switch
            {
                "week" => DateTime.UtcNow.AddDays(-7),
                "month" => DateTime.UtcNow.AddMonths(-1),
                _ => null
            };

            var courseClasses = await _context.ClassCourses
                .Where(cc => cc.CourseId == courseId) // Lọc theo courseId
                .Select(cc => new { cc.CourseId, cc.ClassId, cc.Classes.ClassTitle, cc.CcId, cc.CcDescription })
                .Distinct()
                .ToListAsync();


            var courseClassIds = courseClasses.Select(c => c.CcId).ToList();

            var lessons = await _context.Lessons
                .Where(l => courseClassIds.Contains(l.LessonClassCourseId))
                .Select(l => new { l.LessonId, l.LessonName, l.LessonClassCourseId, l.LessonCreateAt })
                .ToListAsync();

            var studentsPerClass = await _context.StudentClasses
                .Where(sc => courseClasses.Select(c => c.ClassId).Contains(sc.ScClassId))
                .GroupBy(sc => sc.ScClassId)
                .Select(g => new { ClassId = g.Key, TotalStudents = g.Count() })
                .ToListAsync();

            var lessonIds = lessons.Select(l => l.LessonId).ToList();

            var lessonViewDataQuery = _context.StatusLearns
                .Where(sl => sl.SlStatus == true && lessonIds.Contains(sl.SlLessonId));

            if (startDate.HasValue)
            {
                lessonViewDataQuery = lessonViewDataQuery.Where(sl => sl.SlLearnedDate >= startDate.Value);
            }

            var lessonViewData = await lessonViewDataQuery
                .GroupBy(sl => new { sl.SlLessonId, sl.SlLearnedDate.Date })
                .Select(g => new
                {
                    LessonId = g.Key.SlLessonId,
                    ViewedCount = g.Count(),
                    LearnedDate = g.Key.Date
                })
                .ToListAsync();

            var courseData = courseClasses
                .Where(cc => cc.CourseId == courseId) // Lọc chỉ lấy courseId cần tìm
                .GroupBy(cc => cc.CourseId)
                .Select(courseGroup => new
                {
                    CourseId = courseGroup.Key,
                    Classes = courseGroup.Select(tc => new
                    {
                        ClassId = tc.ClassId,
                        ClassName = tc.ClassTitle,
                        ClassDescription = tc.CcDescription,
                        TotalStudents = studentsPerClass.FirstOrDefault(s => s.ClassId == tc.ClassId)?.TotalStudents ?? 0,
                        Lessons = lessons.Where(l => l.LessonClassCourseId == tc.CcId).Select(lesson =>
                        {
                            var viewedCount = lessonViewData.FirstOrDefault(v => v.LessonId == lesson.LessonId)?.ViewedCount ?? 0;
                            var totalStudents = studentsPerClass.FirstOrDefault(s => s.ClassId == tc.ClassId)?.TotalStudents ?? 0;

                            var viewedLastWeek = lessonViewData
                                .Where(v => v.LessonId == lesson.LessonId && v.LearnedDate >= DateTime.UtcNow.AddDays(-7))
                                .Sum(v => v.ViewedCount);

                            var viewedLastMonth = lessonViewData
                                .Where(v => v.LessonId == lesson.LessonId && v.LearnedDate >= DateTime.UtcNow.AddMonths(-1))
                                .Sum(v => v.ViewedCount);

                            return new
                            {
                                lesson.LessonId,
                                lesson.LessonName,
                                lesson.LessonCreateAt,
                                CompletionOverTime = (timeUnit == "week") ? new[]
                                {
                                    new
                                    {
                                        TimePeriod = "Last Week",
                                        StudentsViewed = viewedLastWeek,
                                        StudentsNotViewed = totalStudents - viewedLastWeek,
                                        CompletionRate = totalStudents > 0 ? Math.Round((viewedLastWeek * 100.0) / totalStudents, 2) : 0
                                    }
                                            } : (timeUnit == "month") ? new[]
                                            {
                                    new
                                    {
                                        TimePeriod = "Last Month",
                                        StudentsViewed = viewedLastMonth,
                                        StudentsNotViewed = totalStudents - viewedLastMonth,
                                        CompletionRate = totalStudents > 0 ? Math.Round((viewedLastMonth * 100.0) / totalStudents, 2) : 0
                                    }
                                            } : new[]
                                            {
                                    new
                                    {
                                        TimePeriod = "All Time",
                                        StudentsViewed = viewedCount,
                                        StudentsNotViewed = totalStudents - viewedCount,
                                        CompletionRate = totalStudents > 0 ? Math.Round((viewedCount * 100.0) / totalStudents, 2) : 0
                                    }
                                }
                            };
                        }).ToList()
                    }).ToList()
                }).ToList();

            return Ok(new { Courses = courseData });

        }

        // So sánh mức độ hoàn thành bài giảng giữa tất cả các lớp (admin)
        [HttpGet("compare-lesson-completion")]
        public async Task<IActionResult> CompareLessonCompletion(string? timeUnit = null)
        {
            // Chuẩn hóa timeUnit
            timeUnit = string.IsNullOrEmpty(timeUnit) ? null : timeUnit.ToLower();
            if (timeUnit != null && timeUnit != "week" && timeUnit != "month")
                return BadRequest("Đơn vị thời gian không hợp lệ. Chỉ hỗ trợ 'week', 'month' hoặc không truyền để lấy tất cả dữ liệu.");

            DateTime? startDate = timeUnit switch
            {
                "week" => DateTime.UtcNow.AddDays(-7),
                "month" => DateTime.UtcNow.AddMonths(-1),
                _ => null
            };

            var courseClasses = await _context.ClassCourses
                .Select(cc => new { cc.CourseId, cc.ClassId, cc.Classes.ClassTitle, cc.CcId, cc.CcDescription})
                .Distinct()
                .ToListAsync();

            var courseClassIds = courseClasses.Select(c => c.CcId).ToList();

            var lessons = await _context.Lessons
                .Where(l => courseClassIds.Contains(l.LessonClassCourseId))
                .Select(l => new { l.LessonId, l.LessonName, l.LessonClassCourseId, l.LessonCreateAt })
                .ToListAsync();

            var studentsPerClass = await _context.StudentClasses
                .Where(sc => courseClasses.Select(c => c.ClassId).Contains(sc.ScClassId))
                .GroupBy(sc => sc.ScClassId)
                .Select(g => new { ClassId = g.Key, TotalStudents = g.Count() })
                .ToListAsync();

            var lessonIds = lessons.Select(l => l.LessonId).ToList(); 

            var lessonViewDataQuery = _context.StatusLearns
                .Where(sl => sl.SlStatus == true && lessonIds.Contains(sl.SlLessonId));

            if (startDate.HasValue)
            {
                lessonViewDataQuery = lessonViewDataQuery.Where(sl => sl.SlLearnedDate >= startDate.Value);
            }

            var lessonViewData = await _context.StatusLearns
                .Where(sl => sl.SlStatus == true && lessonIds.Contains(sl.SlLessonId))
                .GroupBy(sl => new { sl.SlLessonId, sl.SlLearnedDate.Date }) 
                .Select(g => new
                {
                    LessonId = g.Key.SlLessonId,
                    ViewedCount = g.Count(),
                    LearnedDate = g.Key.Date 
                })
                .ToListAsync();

            var courseData = courseClasses.GroupBy(cc => cc.CourseId)
                .Select(courseGroup => new
                {
                    CourseId = courseGroup.Key,
                    Classes = courseGroup.Select(tc => new
                    {
                        ClassId = tc.ClassId,
                        ClassName = tc.ClassTitle,
                        ClassDescription = tc.CcDescription,
                        TotalStudents = studentsPerClass.FirstOrDefault(s => s.ClassId == tc.ClassId)?.TotalStudents ?? 0,
                        Lessons = lessons.Where(l => l.LessonClassCourseId == tc.CcId).Select(lesson =>
                        {
                            var viewedCount = lessonViewData.FirstOrDefault(v => v.LessonId == lesson.LessonId)?.ViewedCount ?? 0;
                            var totalStudents = studentsPerClass.FirstOrDefault(s => s.ClassId == tc.ClassId)?.TotalStudents ?? 0;

                             var viewedLastWeek = lessonViewData
                                .Where(v => v.LessonId == lesson.LessonId && v.LearnedDate >= DateTime.UtcNow.AddDays(-7))
                                .Sum(v => v.ViewedCount);

                            var viewedLastMonth = lessonViewData
                                .Where(v => v.LessonId == lesson.LessonId && v.LearnedDate >= DateTime.UtcNow.AddMonths(-1))
                                .Sum(v => v.ViewedCount);

                            return new
                            {
                                lesson.LessonId,
                                lesson.LessonName,
                                lesson.LessonCreateAt,
                                CompletionOverTime = (timeUnit == "week") ? new[]
                                {
                                    new
                                    {
                                        TimePeriod = "Last Week",
                                        StudentsViewed = viewedLastWeek,
                                        StudentsNotViewed = totalStudents - viewedLastWeek,
                                        CompletionRate = totalStudents > 0 ? Math.Round((viewedLastWeek * 100.0) / totalStudents, 2) : 0
                                    }
                                } : (timeUnit == "month") ? new[]
                                                            {
                                    new
                                    {
                                        TimePeriod = "Last Month",
                                        StudentsViewed = viewedLastMonth,
                                        StudentsNotViewed = totalStudents - viewedLastMonth,
                                        CompletionRate = totalStudents > 0 ? Math.Round((viewedLastMonth * 100.0) / totalStudents, 2) : 0
                                    }
                                } : new[] // Nếu không có timeUnit hoặc không hợp lệ, trả về tất cả
                                                            {
                                    new
                                    {
                                        TimePeriod = "All Time",
                                        StudentsViewed = viewedCount,
                                        StudentsNotViewed = totalStudents - viewedCount,
                                        CompletionRate = totalStudents > 0 ? Math.Round((viewedCount * 100.0) / totalStudents, 2) : 0
                                    }
                                }
                            };
                        }).ToList()
                    }).ToList()
                }).ToList();

            return Ok(new { Courses = courseData });

        }

        // Xuất excel thống kê Tổng số sinh viên hoàn thành từng bài giảng theo lớp học phần.
        [HttpGet("export-excel/statistics-student-learned-by-class/{classCourseId}")]
        public async Task<IActionResult> ExcelStudentLearnedByClass(int classCourseId)
        {
            var result = await StatisticsStudentLearnedByClass(classCourseId) as OkObjectResult;

            if (result == null || result.Value == null)
                return NotFound("Không có dữ liệu để xuất.");

            var statisticsData = result.Value as IEnumerable<dynamic>;
            if (statisticsData == null || !statisticsData.Any())
                return NotFound("Dữ liệu rỗng.");

            //string className = _context.Classes.Where(e => e.ClassId == classId).Select(c => c.ClassTitle).FirstOrDefault();
            string className = _context.ClassCourses.Join(_context.Classes,
                cc => cc.ClassId,
                c => c.ClassId,
                (cc, c) => new { cc.CcId, c.ClassTitle })
                .Where(e => e.CcId == classCourseId)
                .Select(c => c.ClassTitle)
                .FirstOrDefault();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add($"{className}");

                // Tiêu đề cột
                string[] headers = { "Mã bài giảng", "Tên bài giảng", "Số sinh viên đã học", "Mã số sinh viên", "Họ và tên sinh viên" };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    worksheet.Cells[1, i + 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    worksheet.Cells[1, i + 1].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                }

                int row = 2;

                foreach (var lesson in statisticsData)
                {
                    int startRow = row;
                    int studentCount = lesson.StudentList.Count;

                    if (studentCount == 0)
                    {
                        worksheet.Cells[row, 1].Value = lesson.LessonId;
                        worksheet.Cells[row, 2].Value = lesson.LessonName;
                        worksheet.Cells[row, 3].Value = lesson.TotalStudentLearned;
                        worksheet.Cells[row, 1, row, 3].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        worksheet.Cells[row, 1, row, 3].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                        row++;
                    }
                    else
                    {
                        foreach (var student in lesson.StudentList)
                        {
                            worksheet.Cells[row, 4].Value = student.StudentCode;
                            worksheet.Cells[row, 5].Value = student.UsersName;
                            worksheet.Cells[row, 4, row, 5].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
                            worksheet.Cells[row, 4, row, 5].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                            row++;
                        }

                        if (studentCount > 1)
                        {
                            worksheet.Cells[startRow, 1, row - 1, 1].Merge = true;
                            worksheet.Cells[startRow, 2, row - 1, 2].Merge = true;
                            worksheet.Cells[startRow, 3, row - 1, 3].Merge = true;
                        }

                        worksheet.Cells[startRow, 1].Value = lesson.LessonId;
                        worksheet.Cells[startRow, 2].Value = lesson.LessonName;
                        worksheet.Cells[startRow, 3].Value = lesson.TotalStudentLearned;

                        worksheet.Cells[startRow, 1, row - 1, 3].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        worksheet.Cells[startRow, 1, row - 1, 3].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                    }
                }

                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Thống Kê Trạng Thái Học {className}.xlsx");
            }
        }

        // Xuất excel thống kê Tổng số sinh viên hoàn thành từng bài giảng tất cả các lớp học phần.
        [HttpGet("export-excel/statistics-student-learned/")]
        public async Task<IActionResult> ExcelStudentLearned()
        {
            var result = await StatisticsStudentLearned() as OkObjectResult;

            if (result == null || result.Value == null)
                return NotFound("Không có dữ liệu để xuất.");

            var statisticsData = result.Value as IEnumerable<dynamic>;
            if (statisticsData == null || !statisticsData.Any())
                return NotFound("Dữ liệu rỗng.");

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Thống kê");

                // Tiêu đề cột
                string[] headers = { "Mã khóa học", "Tên khóa học", "Tên lớp", "Mã bài giảng", "Tên bài giảng", "Mô tả bài giảng", "Số sinh viên đã học" };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    worksheet.Cells[1, i + 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    worksheet.Cells[1, i + 1].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                }

                int row = 2;

                foreach (var classCourse in statisticsData)
                {
                    int startRowClass = row;
                    int lessonCount = classCourse.Statistics.Count;

                    foreach (var lesson in classCourse.Statistics)
                    {
                        worksheet.Cells[row, 4].Value = lesson.LessonId;
                        worksheet.Cells[row, 5].Value = lesson.LessonName;
                        worksheet.Cells[row, 6].Value = lesson.LessonDescription;
                        worksheet.Cells[row, 7].Value = lesson.TotalStudentLearned;

                        worksheet.Cells[row, 4, row, 7].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
                        worksheet.Cells[row, 4, row, 7].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                        row++;
                    }

                    // Merge các ô khóa học và lớp học nếu có nhiều bài giảng
                    if (lessonCount > 1)
                    {
                        worksheet.Cells[startRowClass, 1, row - 1, 1].Merge = true;
                        worksheet.Cells[startRowClass, 2, row - 1, 2].Merge = true;
                        worksheet.Cells[startRowClass, 3, row - 1, 3].Merge = true;
                    }

                    worksheet.Cells[startRowClass, 1].Value = classCourse.ClassCourseId;
                    worksheet.Cells[startRowClass, 2].Value = classCourse.CourseName;
                    worksheet.Cells[startRowClass, 3].Value = classCourse.ClassName;

                    worksheet.Cells[startRowClass, 1, row - 1, 3].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    worksheet.Cells[startRowClass, 1, row - 1, 3].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                }

                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Thống kê trạng thái học.xlsx");
            }
        }
              
        // Xuất excel top 10 học viên tích cực (hoàn thành nhiều bài giảng nhất) theo bài giảng và lớp học phần
        [HttpGet("export-excel/top-students/{classCourseId}")]
        public async Task<IActionResult> ExportTopStudents(
            int classCourseId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            var result = await GetTopStudents(classCourseId, fromDate, toDate) as OkObjectResult;

            if (result == null || result.Value == null)
                return NotFound("Không có dữ liệu để xuất.");

            var topStudents = result.Value as IEnumerable<dynamic>;
            if (topStudents == null || !topStudents.Any())
                return NotFound("Dữ liệu rỗng.");

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Top 10 Students");

                // Tiêu đề cột
                string[] headers = { "MSSV", "Họ và tên sinh viên", "Ngày học", "Số bài đã hoàn thành", "Tổng số bài", "Tỷ lệ hoàn thành (%)" };
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    worksheet.Cells[1, i + 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                }

                int row = 2;
                foreach (var student in topStudents)
                {
                    worksheet.Cells[row, 1].Value = student.StudentCode;
                    worksheet.Cells[row, 2].Value = student.UsersName;

                    // Xuất tất cả ngày học, cách nhau bằng dấu phẩy
                    worksheet.Cells[row, 3].Value = string.Join(", ", student.LearnedDates);

                    worksheet.Cells[row, 4].Value = student.CompletedLessons;
                    worksheet.Cells[row, 5].Value = student.TotalLessons;
                    worksheet.Cells[row, 6].Value = student.CompletionRate;

                    worksheet.Cells[row, 1, row, 6].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
                    row++;
                }

                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                string thoiGian = null;
                if (fromDate.HasValue && toDate.HasValue)
                    thoiGian = $"_{fromDate.Value:yyyyMMdd}_{toDate.Value:yyyyMMdd}";
                else if (fromDate.HasValue)
                    thoiGian = $"_from_{fromDate.Value:yyyyMMdd}";
                else if (toDate.HasValue)
                    thoiGian = $"_to_{toDate.Value:yyyyMMdd}";

                string fileName = $"Top 10 Sinh Viên Tích Cực{thoiGian}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        // Xuất excel thống kê tỉ lệ hoàn thành bài học theo học viên ở lớp học phần (lọc theo thời gian nếu có).
        [HttpGet("export-excel/statistics-student-progress/{classCourseId}")]
        public async Task<IActionResult> ExportStudentProgress(
            int classCourseId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            var result = await StatisticsStudentProgress(classCourseId, fromDate, toDate) as OkObjectResult;

            if (result == null || result.Value == null)
                return NotFound("Không có dữ liệu để xuất.");

            var statisticsData = result.Value as IEnumerable<dynamic>;
            if (statisticsData == null || !statisticsData.Any())
                return NotFound("Dữ liệu rỗng.");

            string thoiGian = null;
            if (fromDate != null && toDate != null)
                thoiGian = $"_{fromDate.Value:yyyyMMdd}_{toDate.Value:yyyyMMdd}";
            else if (fromDate != null)
                thoiGian = $"_from_{fromDate.Value:yyyyMMdd}";
            else if (toDate != null)
                thoiGian = $"_to_{toDate.Value:yyyyMMdd}";

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Thống kê tiến độ");

                // Tiêu đề cột
                string[] headers = { "ID Sinh viên", "Mã Sinh viên", "Tên Sinh viên", "Số bài đã học", "Tổng số bài", "Tỷ lệ hoàn thành (%)" };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    worksheet.Cells[1, i + 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    worksheet.Cells[1, i + 1].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                }

                int row = 2;

                foreach (var student in statisticsData)
                {
                    worksheet.Cells[row, 1].Value = student.StudentId;
                    worksheet.Cells[row, 2].Value = student.StudentCode;
                    worksheet.Cells[row, 3].Value = student.UsersName;
                    worksheet.Cells[row, 4].Value = student.CompletedLessons;
                    worksheet.Cells[row, 5].Value = student.TotalLessons;
                    worksheet.Cells[row, 6].Value = student.CompletionRate;

                    worksheet.Cells[row, 1, row, 6].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    worksheet.Cells[row, 1, row, 6].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                    row++;
                }

                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"ThongKeTienDo{thoiGian}.xlsx");
            }
        }

        // Xuất excel thống kê học sinh đã/ chưa xem bài giảng theo lớp học phần
        [HttpGet("export-excel/lesson-view-status/{classCourseId}")]
        public async Task<IActionResult> ExportLessonViewStatus(int classCourseId)
        {
            var result = await GetStudentsLessonViewStatus(classCourseId) as OkObjectResult;

            if (result == null || result.Value == null)
                return NotFound("Không có dữ liệu để xuất.");

            var data = result.Value as dynamic;
            if (data == null || data.Lessons == null)
                return NotFound("Dữ liệu rỗng.");

            var totalStudents = data.TotalStudents;
            var lessonStats = data.Lessons as IEnumerable<dynamic>;

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Lesson View Status");

                // Tiêu đề cột
                string[] headers = { "Mã Bài Giảng", "Tên Bài Giảng", "Ngày Tạo", "Số SV Đã Xem", "Số SV Chưa Xem", "Tổng Số SV" };
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    worksheet.Cells[1, i + 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                }

                int row = 2;
                foreach (var lesson in lessonStats)
                {
                    worksheet.Cells[row, 1].Value = lesson.LessonId;
                    worksheet.Cells[row, 2].Value = lesson.LessonName;
                    worksheet.Cells[row, 3].Value = ((DateTime)lesson.LessonCreateAt).ToString("yyyy-MM-dd hh:mm:ss");
                    worksheet.Cells[row, 4].Value = lesson.StudentsViewed;
                    worksheet.Cells[row, 5].Value = lesson.StudentsNotViewed;
                    worksheet.Cells[row, 6].Value = totalStudents;

                    worksheet.Cells[row, 1, row, 6].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
                    row++;
                }

                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                var classCourseInfo = _context.ClassCourses.Where(cc => cc.CcId == classCourseId).Select(cc => new { cc.ClassId, cc.CourseId }).FirstOrDefault();
                var className = _context.Classes.Where(c => c.ClassId == classCourseInfo.ClassId).Select(c => c.ClassTitle).FirstOrDefault();
                var courseName = _context.Courses.Where(c => c.CourseId == classCourseInfo.CourseId).Select(c => c.CourseTitle).FirstOrDefault();
                className = className.Replace(" ", "");
                courseName = courseName.Replace(" ", "");
                string fileName = $"LessonViewStatus_{className}_{courseName}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        // Xuất excel thống kê học sinh đã/ chưa xem bài giảng theo lớp học phần chi tiết
        [HttpGet("export-excel/lesson-view-status-detail/{classCourseId}")]
        public async Task<IActionResult> ExportLessonViewStatusDetail(int classCourseId)
        {
            var result = await GetStudentsLessonViewStatusDetail(classCourseId) as OkObjectResult;

            if (result == null || result.Value == null)
                return NotFound("Không có dữ liệu để xuất.");

            var data = result.Value as dynamic;
            if (data == null || data.Lessons == null)
                return NotFound("Dữ liệu rỗng.");

            var totalStudents = data.TotalStudents;
            var lessons = data.Lessons as IEnumerable<dynamic>;

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Lesson View Status");

                // Tiêu đề cột chính
                string[] headers = { "Mã Bài Giảng", "Tên Bài Giảng", "Ngày Tạo", "Số SV Đã Xem", "Số SV Chưa Xem", "Tổng Số SV" };
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    worksheet.Cells[1, i + 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                }

                int row = 2;
                foreach (var lesson in lessons)
                {
                    worksheet.Cells[row, 1].Value = lesson.LessonId;
                    worksheet.Cells[row, 2].Value = lesson.LessonName;
                    worksheet.Cells[row, 3].Value = ((DateTime)lesson.LessonCreateAt).ToString("yyyy-MM-dd hh:mm:ss");
                    worksheet.Cells[row, 4].Value = lesson.StudentsViewed;
                    worksheet.Cells[row, 5].Value = lesson.StudentsNotViewed;
                    worksheet.Cells[row, 6].Value = totalStudents;

                    worksheet.Cells[row, 1, row, 6].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
                    row++;
                }

                // Thêm bảng chi tiết SV
                var detailSheet = package.Workbook.Worksheets.Add("Student Details");

                string[] detailHeaders = { "Mã Bài Giảng", "Tên Bài Giảng", "Trạng thái", "Mã SV", "Tên SV" };
                for (int i = 0; i < detailHeaders.Length; i++)
                {
                    detailSheet.Cells[1, i + 1].Value = detailHeaders[i];
                    detailSheet.Cells[1, i + 1].Style.Font.Bold = true;
                    detailSheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    detailSheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    detailSheet.Cells[1, i + 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                }

                int detailRow = 2;
                foreach (var lesson in lessons)
                {
                    foreach (var student in lesson.ViewedList)
                    {
                        detailSheet.Cells[detailRow, 1].Value = lesson.LessonId;
                        detailSheet.Cells[detailRow, 2].Value = lesson.LessonName;
                        detailSheet.Cells[detailRow, 3].Value = "Đã Học";
                        detailSheet.Cells[detailRow, 4].Value = student.StudentCode;
                        detailSheet.Cells[detailRow, 5].Value = student.UsersName;
                        detailRow++;
                    }

                    foreach (var student in lesson.NotViewedList)
                    {
                        detailSheet.Cells[detailRow, 1].Value = lesson.LessonId;
                        detailSheet.Cells[detailRow, 2].Value = lesson.LessonName;
                        detailSheet.Cells[detailRow, 3].Value = "Chưa Học";
                        detailSheet.Cells[detailRow, 4].Value = student.StudentCode;
                        detailSheet.Cells[detailRow, 5].Value = student.UsersName;
                        detailRow++;
                    }
                }

                worksheet.Cells.AutoFitColumns();
                detailSheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                var classCourseInfo = _context.ClassCourses.Where(cc => cc.CcId == classCourseId).Select(cc => new { cc.ClassId, cc.CourseId }).FirstOrDefault();
                var className = _context.Classes.Where(c => c.ClassId == classCourseInfo.ClassId).Select(c => c.ClassTitle).FirstOrDefault();
                var courseName = _context.Courses.Where(c => c.CourseId == classCourseInfo.CourseId).Select(c => c.CourseTitle).FirstOrDefault();
                className = className.Replace(" ", "");
                courseName = courseName.Replace(" ", "");

                string fileName = $"LessonViewStatusDetail_{className}_{courseName}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        // Xuất excel so sánh mức độ hoàn thành bài giảng giữa các lớp chung gv
        [HttpGet("export-excel/export-compare-lesson-completion/{teacherId}/{courseId}")]
        public async Task<IActionResult> ExportLessonCompletion(int teacherId, int courseId, string? timeUnit = null)
        {
            var result = await CompareLessonCompletion(teacherId, courseId, timeUnit) as OkObjectResult;

            if (result == null || result.Value == null)
                return NotFound("Không có dữ liệu để xuất.");

            var data = result.Value as dynamic;
            if (data == null || data.Courses == null || data.Courses.Count == 0)
                return NotFound("Dữ liệu rỗng.");

            var course = data.Courses[0];
            var classData = course.Classes as IEnumerable<dynamic>;

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Lesson Completion");

                // Tiêu đề cột chính
                string[] headers = { "Mã Lớp", "Tên Lớp", "Mã Bài Giảng", "Tên Bài Giảng", "Ngày Tạo", "Thời Gian", "SV Đã Xem", "SV Chưa Xem", "Tỷ Lệ Hoàn Thành (%)" };
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    worksheet.Cells[1, i + 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                }

                int row = 2;
                foreach (var classItem in classData)
                {
                    foreach (var lesson in classItem.Lessons)
                    {
                        foreach (var stat in lesson.CompletionOverTime)
                        {
                            worksheet.Cells[row, 1].Value = classItem.ClassId;
                            worksheet.Cells[row, 2].Value = classItem.ClassName;
                            worksheet.Cells[row, 3].Value = lesson.LessonId;
                            worksheet.Cells[row, 4].Value = lesson.LessonName;
                            worksheet.Cells[row, 5].Value = ((DateTime)lesson.LessonCreateAt).ToString("yyyy-MM-dd HH:mm:ss");
                            worksheet.Cells[row, 6].Value = stat.TimePeriod;
                            worksheet.Cells[row, 7].Value = stat.StudentsViewed;
                            worksheet.Cells[row, 8].Value = stat.StudentsNotViewed;
                            worksheet.Cells[row, 9].Value = stat.CompletionRate;

                            worksheet.Cells[row, 1, row, 9].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
                            row++;
                        }
                    }
                }

                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                string fileName = $"LessonCompletion_{teacherId}_{courseId}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        // Xuất excel so sánh mức độ hoàn thành bài giảng giữa các lớp chung học phần(admin)
        [HttpGet("export-compare-lesson-completion/{courseId}")]
        public async Task<IActionResult> ExportCompareLessonCompletion(int courseId, string? timeUnit = null)
        {
            var result = await CompareLessonCompletion(courseId, timeUnit) as OkObjectResult;

            if (result == null || result.Value == null)
                return NotFound("Không có dữ liệu để xuất.");

            var data = result.Value as dynamic;
            if (data == null || data.Courses == null)
                return NotFound("Dữ liệu rỗng.");

            var courses = data.Courses as IEnumerable<dynamic>;
            var course = courses?.FirstOrDefault();

            if (course == null || course.Classes == null)
                return NotFound("Không có lớp học trong khóa học này.");

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Lesson Completion");
                string[] headers = { "Mã Lớp", "Tên Lớp", "Mã Bài Giảng", "Tên Bài Giảng", "Ngày Tạo", "Thời Gian", "SV Đã Xem", "SV Chưa Xem", "Tỷ Lệ Hoàn Thành (%)" };
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    worksheet.Cells[1, i + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                int row = 2;
                foreach (var classItem in course.Classes)
                {
                    foreach (var lesson in classItem.Lessons)
                    {
                        foreach (var stat in lesson.CompletionOverTime)
                        {
                            worksheet.Cells[row, 1].Value = classItem.ClassId;
                            worksheet.Cells[row, 2].Value = classItem.ClassName;
                            worksheet.Cells[row, 3].Value = lesson.LessonId;
                            worksheet.Cells[row, 4].Value = lesson.LessonName;
                            worksheet.Cells[row, 5].Value = ((DateTime)lesson.LessonCreateAt).ToString("yyyy-MM-dd HH:mm:ss");
                            worksheet.Cells[row, 6].Value = stat.TimePeriod;
                            worksheet.Cells[row, 7].Value = stat.StudentsViewed;
                            worksheet.Cells[row, 8].Value = stat.StudentsNotViewed;
                            worksheet.Cells[row, 9].Value = stat.CompletionRate;
                            row++;
                        }
                    }
                }

                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                string fileName = $"LessonCompletion_{courseId}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        // Xuất excel so sánh mức độ hoàn thành bài giảng giữa tất cả các lớp (admin)
        [HttpGet("export-compare-lesson-completion")]
        public async Task<IActionResult> ExportCompareLessonCompletion(string? timeUnit = null)
        {
            // Gọi trực tiếp hàm CompareLessonCompletion
            var result = await CompareLessonCompletion(timeUnit) as OkObjectResult;
            if (result == null || result.Value == null)
                return BadRequest("Không có dữ liệu để xuất Excel.");

            var jsonData = JObject.FromObject(result.Value);
            if (jsonData["Courses"] == null)
                return NotFound("Không có dữ liệu để xuất Excel.");

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Lesson Completion");

            // Header
            worksheet.Cells["A1"].Value = "STT";
            worksheet.Cells["B1"].Value = "Tên lớp";
            worksheet.Cells["C1"].Value = "Tên lớp học phần";
            worksheet.Cells["D1"].Value = "Tên bài giảng";
            worksheet.Cells["E1"].Value = "Ngày tạo";
            worksheet.Cells["F1"].Value = "Khoảng thời gian";
            worksheet.Cells["G1"].Value = "Số học sinh đã xem";
            worksheet.Cells["H1"].Value = "Số học sinh chưa xem";
            worksheet.Cells["I1"].Value = "Tỷ lệ hoàn thành (%)";

            int row = 2;
            int index = 1;

            foreach (var course in jsonData["Courses"])
            {
                foreach (var classData in course["Classes"])
                {
                    string className = classData["ClassName"].ToString();
                    string classDescription = classData["ClassDescription"].ToString();
                    foreach (var lesson in classData["Lessons"])
                    {
                        string lessonName = lesson["LessonName"].ToString();
                        string lessonCreatedAt = lesson["LessonCreateAt"].ToString();
                        foreach (var stat in lesson["CompletionOverTime"])
                        {
                            worksheet.Cells[row, 1].Value = index++;
                            worksheet.Cells[row, 2].Value = className;
                            worksheet.Cells[row, 3].Value = classDescription;
                            worksheet.Cells[row, 4].Value = lessonName;
                            worksheet.Cells[row, 5].Value = lessonCreatedAt;
                            worksheet.Cells[row, 6].Value = stat["TimePeriod"].ToString();
                            worksheet.Cells[row, 7].Value = stat["StudentsViewed"].ToString();
                            worksheet.Cells[row, 8].Value = stat["StudentsNotViewed"].ToString();
                            worksheet.Cells[row, 9].Value = stat["CompletionRate"].ToString();

                            row++;
                        }
                    }
                }
            }

            // Định dạng cột
            worksheet.Cells["A:I"].AutoFitColumns();
            worksheet.Cells["A1:I1"].Style.Font.Bold = true;
            worksheet.Cells["A1:I1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells["A1:I1"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            worksheet.Cells["I2:I" + row].Style.Numberformat.Format = "0.00";

            // Xuất file Excel
            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "LessonCompletion.xlsx");
        }
    }
}
