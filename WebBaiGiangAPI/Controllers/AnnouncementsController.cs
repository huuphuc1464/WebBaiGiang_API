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
    public class AnnouncementsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AnnouncementsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("get-announcements")]
        public async Task<ActionResult<IEnumerable<Announcement>>> GetAnnouncements()
        {
            var announcements = from a in _context.Announcements
                                join c in _context.Classes on a.AnnouncementClassId equals c.ClassId
                                join u in _context.Users on a.AnnouncementTeacherId equals u.UsersId
                                orderby a.AnnouncementDate descending
                                select new
                                {
                                    AnnouncementId = a.AnnouncementId,
                                    AnnouncementClassId = a.AnnouncementClassId,
                                    AnnouncementTeacherId = a.AnnouncementTeacherId,
                                    AnnouncementTitle = a.AnnouncementTitle,
                                    AnnouncementDescription = a.AnnouncementDescription,
                                    AnnouncementDate = a.AnnouncementDate,
                                    ClassTitle = c.ClassTitle,
                                    TeacherName = u.UsersUsername
                                };
            if (!announcements.Any() || announcements == null)
            {
                return NotFound("Hiện tại không có thông báo nào");
            }
            return Ok(announcements);
        }

        [HttpGet("get-announcement-by-id")]
        public async Task<ActionResult<Announcement>> GetAnnouncementById(int id)
        {
            var announcement = from a in _context.Announcements
                               join c in _context.Classes on a.AnnouncementClassId equals c.ClassId
                               join u in _context.Users on a.AnnouncementTeacherId equals u.UsersId
                               where a.AnnouncementId == id
                               orderby a.AnnouncementDate descending
                               select new
                               {
                                   AnnouncementId = a.AnnouncementId,
                                   AnnouncementClassId = a.AnnouncementClassId,
                                   AnnouncementTeacherId = a.AnnouncementTeacherId,
                                   AnnouncementTitle = a.AnnouncementTitle,
                                   AnnouncementDescription = a.AnnouncementDescription,
                                   AnnouncementDate = a.AnnouncementDate,
                                   ClassTitle = c.ClassTitle,
                                   TeacherName = u.UsersUsername
                               };
            if (!announcement.Any() || announcement == null)
            {
                return NotFound("Không tìm thấy thông báo nào");
            }
            return Ok(announcement);
        }

        [HttpGet("get-announcement-by-teacher-id")]
        public async Task<ActionResult<Announcement>> GetAnnouncementByTeacherId(int id)
        {
            var announcement = from a in _context.Announcements
                               join c in _context.Classes on a.AnnouncementClassId equals c.ClassId
                               join u in _context.Users on a.AnnouncementTeacherId equals u.UsersId
                               where a.AnnouncementTeacherId == id
                               orderby a.AnnouncementDate descending
                               select new
                               {
                                   AnnouncementId = a.AnnouncementId,
                                   AnnouncementClassId = a.AnnouncementClassId,
                                   AnnouncementTeacherId = a.AnnouncementTeacherId,
                                   AnnouncementTitle = a.AnnouncementTitle,
                                   AnnouncementDescription = a.AnnouncementDescription,
                                   AnnouncementDate = a.AnnouncementDate,
                                   ClassTitle = c.ClassTitle,
                                   TeacherName = u.UsersUsername
                               };
            if (!announcement.Any() || announcement == null)
            {
                return NotFound("Không tìm thấy thông báo nào");
            }
                return Ok(announcement);
        }

        [HttpGet("get-announcement-by-class-id")]
        public async Task<ActionResult<Announcement>> GetAnnouncementByClassId(int id)
        {
            var announcement = from a in _context.Announcements
                               join c in _context.Classes on a.AnnouncementClassId equals c.ClassId
                               join u in _context.Users on a.AnnouncementTeacherId equals u.UsersId
                               where a.AnnouncementClassId == id
                               orderby a.AnnouncementDate descending
                               select new
                               {
                                   AnnouncementId = a.AnnouncementId,
                                   AnnouncementClassId = a.AnnouncementClassId,
                                   AnnouncementTeacherId = a.AnnouncementTeacherId,
                                   AnnouncementTitle = a.AnnouncementTitle,
                                   AnnouncementDescription = a.AnnouncementDescription,
                                   AnnouncementDate = a.AnnouncementDate,
                                   ClassTitle = c.ClassTitle,
                                   TeacherName = u.UsersUsername
                               };
            if (!announcement.Any() || announcement == null)
            {
                return NotFound("Không tìm thấy thông báo nào");
            }
            return Ok(announcement);
        }

        [HttpPut("update-announcement")]
        public async Task<IActionResult> UpdateAnnouncement(Announcement announcement)
        {
            var existingAnnouncement = await _context.Announcements.FindAsync(announcement.AnnouncementId);
            if (existingAnnouncement == null)
            {
                return NotFound("Không tìm thấy thông báo");
            }
            if (!_context.Classes.Any(c => c.ClassId == announcement.AnnouncementClassId))
            {
                return BadRequest("ClassId is not valid");
            }
            if (!_context.Users.Any(u => u.UsersId == announcement.AnnouncementTeacherId && u.UsersRoleId == 2))
            {
                return BadRequest("TeacherId is not valid");
            }
            existingAnnouncement.AnnouncementClassId = announcement.AnnouncementClassId;
            existingAnnouncement.AnnouncementTeacherId = announcement.AnnouncementTeacherId;
            existingAnnouncement.AnnouncementTitle = Regex.Replace(announcement.AnnouncementTitle.Trim(), @"\s+", " ");
            existingAnnouncement.AnnouncementDescription = Regex.Replace(announcement.AnnouncementDescription.Trim(), @"\s+", " ");
            existingAnnouncement.AnnouncementDate = DateTime.Now;
            _context.Announcements.Update(existingAnnouncement);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }

            return Ok(new
            {
                message = "Thông báo đã được cập nhật",
                data = existingAnnouncement
            });
        }

        [HttpPost("add-announcement")]
        public async Task<ActionResult<Announcement>> AddAnnouncement(Announcement announcement)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!_context.Classes.Any(c => c.ClassId == announcement.AnnouncementClassId))
            {
                return BadRequest("ClassId is not valid");
            }

            if (!_context.Users.Any(u => u.UsersId == announcement.AnnouncementTeacherId && u.UsersRoleId == 2))
            {
                return BadRequest("TeacherId is not valid");
            }

            announcement.AnnouncementTitle = Regex.Replace(announcement.AnnouncementTitle.Trim(), @"\s+", " ");
            announcement.AnnouncementDescription = Regex.Replace(announcement.AnnouncementDescription.Trim(), @"\s+", " ");
            announcement.AnnouncementDate = DateTime.Now;

            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Announcement added successfully",
                Data = announcement
            });
        }

        [HttpDelete("delete-announcement")]
        public async Task<IActionResult> DeleteAnnouncement(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null)
            {
                return NotFound("Không tìm thấy thông báo");
            }

            _context.Announcements.Remove(announcement);
            await _context.SaveChangesAsync();

            return Conflict("Xóa thông báo thành công");
        }
    }
}
