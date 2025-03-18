using System;
using System.Collections.Generic;
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
    public class FeedbacksController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;

        public FeedbacksController(AppDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet("get-feedbacks")]
        public async Task<ActionResult<IEnumerable<Feedback>>> GetFeedbacks()
        {
            var feedback = from f in _context.Feedbacks
                           join u in _context.Users on f.FeedbackUsersId equals u.UsersId
                           join c in _context.Classes on f.FeedbackClassId equals c.ClassId
                           select new
                           {
                               f.FeedbackId,
                               f.FeedbackUsersId,
                               u.UsersName,
                               f.FeedbackClassId,
                               c.ClassTitle,
                               f.FeedbackContent,
                               f.FeedbackRate,
                               f.FeedbackDate,
                               f.FeedbackStatus,
                           };
            if (feedback == null || !feedback.Any())
            {
                return NotFound("Không có đánh giá nào tồn tại");
            }
            return Ok(feedback);
        }

        [HttpGet("get-feedback-by-id")]
        public async Task<ActionResult<Feedback>> GetFeedbackById(int id)
        {
            var feedback = from f in _context.Feedbacks
                           join u in _context.Users on f.FeedbackUsersId equals u.UsersId
                           join c in _context.Classes on f.FeedbackClassId equals c.ClassId
                           where f.FeedbackId == id
                           select new
                           {
                               f.FeedbackId,
                               f.FeedbackUsersId,
                               u.UsersName,
                               f.FeedbackClassId,
                               c.ClassTitle,
                               f.FeedbackContent,
                               f.FeedbackRate,
                               f.FeedbackDate,
                               f.FeedbackStatus,
                           };
            if (feedback == null || !feedback.Any())
            {
                return NotFound("Đánh giá không tồn tại");
            }
            return Ok(feedback);
        }

        [HttpGet("get-feedback-by-class-id")]
        public async Task<ActionResult<Feedback>> GetFeedbackByClassId(int id)
        {
            var exist = _context.Classes.Any(c => c.ClassId == id);
            if (exist == null)
            {
                return NotFound("Lớp học không tồn tại");
            }
            var feedback = from f in _context.Feedbacks
                           join u in _context.Users on f.FeedbackUsersId equals u.UsersId
                           join c in _context.Classes on f.FeedbackClassId equals c.ClassId
                           where f.FeedbackClassId == id
                           select new
                           {
                               f.FeedbackId,
                               f.FeedbackUsersId,
                               u.UsersName,
                               f.FeedbackClassId,
                               c.ClassTitle,
                               f.FeedbackContent,
                               f.FeedbackRate,
                               f.FeedbackDate,
                               f.FeedbackStatus,
                           };
            if (feedback == null || !feedback.Any())
            {
                return NotFound("Đánh giá không tồn tại");
            }
            return Ok(feedback);
        }

        [HttpGet("get-feedback-by-student-id")]
        public async Task<ActionResult<Feedback>> GetFeedbackByStudentId(int id)
        {
            var exist = _context.Users.Any(s => s.UsersId == id);
            if (exist == null)
            {
                return NotFound("Sinh viên không tồn tại");
            }
            var feedback = from f in _context.Feedbacks
                           join u in _context.Users on f.FeedbackUsersId equals u.UsersId
                           join c in _context.Classes on f.FeedbackClassId equals c.ClassId
                           where f.FeedbackUsersId == id
                           select new
                           {
                               f.FeedbackId,
                               f.FeedbackUsersId,
                               u.UsersName,
                               f.FeedbackClassId,
                               c.ClassTitle,
                               f.FeedbackContent,
                               f.FeedbackRate,
                               f.FeedbackDate,
                               f.FeedbackStatus,
                           };
            if (feedback == null || !feedback.Any())
            {
                return NotFound("Đánh giá không tồn tại");
            }
            return Ok(feedback);
        }

        [HttpPut("update-feedback")]
        public async Task<IActionResult> UpdateFeedback([FromBody]Feedback feedback)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            if (feedback == null || string.IsNullOrEmpty(feedback.FeedbackContent))
            {
                return BadRequest("Đánh giá không hợp lệ!");
            }
            var existingFeedback = _context.Feedbacks.FirstOrDefault(f => f.FeedbackId == feedback.FeedbackId && f.FeedbackUsersId == feedback.FeedbackUsersId);
            if (existingFeedback == null)
            {
                return NotFound("Đánh giá không tồn tại");
            }
            if (_context.Feedbacks.Count(f => f.FeedbackClassId == feedback.FeedbackClassId && f.FeedbackUsersId == feedback.FeedbackUsersId && f.FeedbackId != feedback.FeedbackId) >= 1)
            {
                return BadRequest("Bạn đã đánh giá lớp học này rồi!");
            }
            if (feedback.FeedbackRate < 0 || feedback.FeedbackRate > 5)
            {
                return BadRequest("Số sao đánh giá chỉ nhận trong khoảng từ 0 đến 5");
            }
            Feedback oldFeedback = new Feedback
            {
                FeedbackId = existingFeedback.FeedbackId,
                FeedbackRate = existingFeedback.FeedbackRate,
                FeedbackContent = existingFeedback.FeedbackContent,
                FeedbackStatus = existingFeedback.FeedbackStatus,
                FeedbackDate = existingFeedback.FeedbackDate,
                FeedbackClassId = existingFeedback.FeedbackClassId,
                FeedbackUsersId = existingFeedback.FeedbackUsersId
            };
            existingFeedback.FeedbackRate = feedback.FeedbackRate;
            existingFeedback.FeedbackContent = Regex.Replace(feedback.FeedbackContent.Trim(), @"\s+", " ");
            existingFeedback.FeedbackDate = DateTime.Now;
            _context.Feedbacks.Update(existingFeedback);
            
            var student = _context.Users.Where(u => u.UsersId == feedback.FeedbackUsersId).Select(c => new Tuple<string, string>(c.UsersEmail, c.UsersName)).FirstOrDefault();
            var lop = _context.ClassCourses.Where(c => c.ClassId == existingFeedback.FeedbackClassId).Select(c => c.Course.CourseTitle).FirstOrDefault();
            var teacher = _context.TeacherClasses
                .Where(tc => tc.TcClassId == existingFeedback.FeedbackClassId)
                .Join(_context.Users, tc => tc.TcUsersId, u => u.UsersId, (tc, u) => new Tuple<string, string>(u.UsersEmail, u.UsersName))
                .FirstOrDefault();
            try
            {
                await _emailService.SendEmailUpdateFeedback(student, lop, "Bạn đã thay đổi đánh giá lớp học", oldFeedback, existingFeedback);
                await _emailService.SendEmailUpdateFeedback(teacher, lop, "Bạn có một thay đổi đánh giá về lớp học ", oldFeedback, existingFeedback);
                await _context.SaveChangesAsync();
            }catch(Exception)
            {
                return BadRequest("Có lỗi xảy ra, vui lòng thử lại sau");
            }
            return Ok( new
            {
                message = "Thay đổi thông tin đánh giá thành công",
                data = existingFeedback
            });
        }

        [HttpPost("add-feedback")]
        public async Task<ActionResult<Feedback>> AddFeedback([FromBody]Feedback feedback)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            if (feedback == null || string.IsNullOrEmpty(feedback.FeedbackContent))
            {
                return BadRequest("Đánh giá không hợp lệ!");
            }
            var wasLearn = _context.StudentClasses.Any(sc => sc.ScClassId == feedback.FeedbackClassId && sc.ScStudentId == feedback.FeedbackUsersId && sc.ScStatus == 1);
            if (!wasLearn)
            {
                return BadRequest("Bạn chưa tham gia lớp học này, không thể đánh giá");
            }
            if (_context.Feedbacks.Count(f => f.FeedbackClassId == feedback.FeedbackClassId && f.FeedbackUsersId == feedback.FeedbackUsersId) >= 1)
            {
                return BadRequest("Bạn đã đánh giá lớp học này rồi!");
            }
            if (!_context.Classes.Any(c => c.ClassId == feedback.FeedbackClassId))
            {
                return BadRequest("Lớp học không tồn tại");
            }
            if (!_context.Users.Any(u => u.UsersId == feedback.FeedbackUsersId))
            {
                return BadRequest("Sinh viên không tồn tại");
            }
            if(feedback.FeedbackRate < 0 || feedback.FeedbackRate > 5)
            {
                return BadRequest("Số sao đánh giá chỉ nhận trong khoảng từ 0 đến 5");
            }
            feedback.FeedbackContent = Regex.Replace(feedback.FeedbackContent.Trim(), @"\s+", " ");
            feedback.FeedbackDate = DateTime.Now;
            feedback.FeedbackStatus = 1;
            _context.Feedbacks.Add(feedback);


            var student = _context.Users.Where(u => u.UsersId == feedback.FeedbackUsersId).Select(c => new Tuple<string, string>(c.UsersEmail, c.UsersName)).FirstOrDefault();
            var lop = _context.ClassCourses.Where(c => c.ClassId == feedback.FeedbackClassId).Select(c => c.Course.CourseTitle).FirstOrDefault();
            var teacher = _context.TeacherClasses
                .Where(tc => tc.TcClassId == feedback.FeedbackClassId)
                .Join(_context.Users, tc => tc.TcUsersId, u => u.UsersId, (tc, u) => new Tuple<string, string>(u.UsersEmail, u.UsersName))
                .FirstOrDefault();

            await _emailService.SendEmailAddFeedback(student, lop, "Cảm ơn bạn đã đánh giá lớp học", feedback);
            await _emailService.SendEmailAddFeedback(teacher, lop, "Bạn nhận được một đánh giá lớp học mới", feedback);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Thêm đánh giá thành công",
                data = feedback
            });
        }

        [HttpDelete("delete-feedback")]
        public async Task<IActionResult> DeleteFeedback(int id)
        {
            var existingFeedback = await _context.Feedbacks.FindAsync(id);
            if (existingFeedback == null)
            {
                return NotFound("Đánh giá không tồn tại");
            }

            // Lấy thông tin sinh viên
            var student = await _context.Users
                .Where(u => u.UsersId == existingFeedback.FeedbackUsersId)
                .Select(u => new Tuple<string, string>(u.UsersEmail, u.UsersName))
                .FirstOrDefaultAsync();

            // Lấy tên lớp học
            var className = await _context.ClassCourses.Where(c => c.ClassId == existingFeedback.FeedbackClassId).Select(c => c.Course.CourseTitle).FirstOrDefaultAsync();

            // Lấy thông tin giáo viên dạy lớp đó
            var teacher = await _context.TeacherClasses
                .Where(tc => tc.TcClassId == existingFeedback.FeedbackClassId)
                .Join(_context.Users, tc => tc.TcUsersId, u => u.UsersId, (tc, u) => new Tuple<string, string>(u.UsersEmail, u.UsersName))
                .FirstOrDefaultAsync();

            // Kiểm tra xem đã có đầy đủ thông tin người nhận email chưa
            if (student == null || teacher == null || className == null)
            {
                return BadRequest("Thiếu thông tin sinh viên, giáo viên hoặc lớp học.");
            }

            // Xóa đánh giá
            _context.Feedbacks.Remove(existingFeedback);

            // Gửi email thông báo sau khi xóa thành công
            string subject = "Thông báo: Đánh giá đã bị xóa";
            var emailSent = await _emailService.SendEmailDeleteFeedback(student, teacher, className, subject, existingFeedback);

            if (!emailSent)
            {
                return StatusCode(500, "Gửi email không thành công. Vui lòng thử lại.");
            }

            await _context.SaveChangesAsync();

            return Ok("Đánh giá đã được xóa thành công và email đã được gửi.");
        }


    }
}
