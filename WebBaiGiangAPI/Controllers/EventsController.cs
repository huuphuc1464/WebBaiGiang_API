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
    public class EventsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ZoomService _zoomService;
        private readonly IJwtService _jwtService;

        public EventsController(AppDbContext context, ZoomService zoomService, IJwtService jwtService)
        {
            _context = context;
            _zoomService = zoomService;
            _jwtService = jwtService;
        }

        [HttpGet("get-events")]
        public async Task<ActionResult<IEnumerable<Event>>> GetEvents()
        {
            var events = await _context.Events.ToListAsync();
            if (events == null || !events.Any())
            {
                return BadRequest("Không tồn tại cuộc họp nào");
            }
            return await _context.Events.ToListAsync();
        }

        [HttpGet("get-event-by-id")]
        public async Task<ActionResult<Event>> GetEventById(int id)
        {
            var @event = from e in _context.Events
                         join c in _context.Classes on e.EventClassId equals c.ClassId
                         join t in _context.Users on e.EventTeacherId equals t.UsersId
                         where e.EventId == id
                         select new
                         {
                             e.EventId,
                             e.EventClassId,
                             c.ClassTitle,
                             e.EventTeacherId,
                             t.UsersName,
                             e.EventTitle,
                             e.EventDescription,
                             e.EventDateStart,
                             e.EventDateEnd,
                             e.EventZoomLink,
                             e.EventPassword
                         };
            if (@event == null ||!@event.Any())
            {
                return NotFound("Cuộc họp không tồn tại");
            }

            return Ok(@event);
        }

        [HttpGet("get-event-by-class-id")]
        public async Task<ActionResult<Event>> GetEventByClassId(int id)
        {
            var @event = from e in _context.Events
                         join c in _context.Classes on e.EventClassId equals c.ClassId
                         join t in _context.Users on e.EventTeacherId equals t.UsersId
                         where e.EventClassId == id
                         select new
                         {
                             e.EventId,
                             e.EventClassId,
                             c.ClassTitle,
                             e.EventTeacherId,
                             t.UsersName,
                             e.EventTitle,
                             e.EventDescription,
                             e.EventDateStart,
                             e.EventDateEnd,
                             e.EventZoomLink,
                             e.EventPassword
                         };
            if (@event == null || !@event.Any())
            {
                return BadRequest("Không tồn tại cuộc họp nào");
            }

            return Ok(@event);
        }

        [HttpGet("get-event-by-teacher-id")]
        public async Task<ActionResult<Event>> GetEventByTeacherId(int id)
        {
            var @event = from e in _context.Events
                         join c in _context.Classes on e.EventClassId equals c.ClassId
                         join t in _context.Users on e.EventTeacherId equals t.UsersId
                         where e.EventTeacherId == id
                         select new
                         {
                             e.EventId,
                             e.EventClassId,
                             c.ClassTitle,
                             e.EventTeacherId,
                             t.UsersName,
                             e.EventTitle,
                             e.EventDescription,
                             e.EventDateStart,
                             e.EventDateEnd,
                             e.EventZoomLink,
                             e.EventPassword
                         };
            if (@event == null || !@event.Any())
            {
                return BadRequest("Không tồn tại cuộc họp nào");
            }

            return Ok(@event);
        }
        
        [HttpPost("create-event")]
        public async Task<IActionResult> CreateZoomEvent([FromBody] Event dataEvent)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (!_context.Users.Any(t => t.UsersId == dataEvent.EventTeacherId && t.UsersRoleId == 2))
            {
                return BadRequest("Giáo viên không tồn tại");
            }
            if (!_context.Classes.Any(c => c.ClassId == dataEvent.EventClassId))
            {
                return BadRequest("Lớp học không tồn tại");
            }
            if (dataEvent.EventDateStart < DateTime.Now)
            {
                return Conflict("Vui lòng nhập thời gian bắt đầu lớn hơn thời gian hiện tại");
            }
            if (dataEvent.EventDateStart > dataEvent.EventDateEnd)
            {
                return Conflict("Vui lòng nhập thời gian kết thúc lớn hơn thời gian bắt đầu");
            }
            dataEvent.EventDescription = Regex.Replace(dataEvent.EventDescription.Trim(), @"\s+", " ");
            dataEvent.EventTitle = Regex.Replace(dataEvent.EventTitle.Trim(), @"\s+", " ");
            var zoomEvent = await _zoomService.CreateZoomEventAsync(dataEvent);
            Announcement announcement = new Announcement();
            announcement.AnnouncementClassId = dataEvent.EventClassId;
            announcement.AnnouncementTeacherId = dataEvent.EventTeacherId;
            announcement.AnnouncementTitle = dataEvent.EventTitle;
            announcement.AnnouncementDescription = dataEvent.EventDescription;
            announcement.AnnouncementDate = DateTime.Now;
            _context.Announcements.Add(announcement);
            _context.SaveChanges();
            return Ok(new { message = "Zoom Event created successfully", join_url = zoomEvent.EventZoomLink });
        }

        [HttpPut("update-event")]
        public async Task<IActionResult> UpdateEvent(Event @event)
        {
            var existEvent = await _context.Events.FirstOrDefaultAsync(e => e.EventId == @event.EventId);
            if (existEvent == null)
            {
                return NotFound("Sự kiện không tồn tại");
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (@event.EventDateStart < DateTime.Now)
            {
                return Conflict("Vui lòng nhập thời gian bắt đầu lớn hơn thời gian hiện tại");
            }
            if (@event.EventDateStart > @event.EventDateEnd)
            {
                return Conflict("Vui lòng nhập thời gian kết thúc lớn hơn thời gian bắt đầu");
            }
            Announcement announcement = new Announcement();
            announcement.AnnouncementClassId = @event.EventClassId;
            announcement.AnnouncementTeacherId = @event.EventTeacherId;
            if (existEvent.EventTitle != @event.EventTitle)
            {
                announcement.AnnouncementTitle = $"Giáo viên đã thay đổi thông tin sự kiện zoom {existEvent.EventTitle} thành {@event.EventTitle}";
            }
            else
            {
                announcement.AnnouncementTitle = $"Giáo viên đã thay đổi thông tin sự kiện zoom {existEvent.EventTitle}";
            }
            string description = "";
            if (existEvent.EventDateStart != @event.EventDateStart)
            {
                description += $"Thời gian bắt đầu: {existEvent.EventDateStart} -> {@event.EventDateStart}\n";
            }
            if (existEvent.EventDateEnd != @event.EventDateEnd)
            {
                description += $"Thời gian kết thúc: {existEvent.EventDateEnd} -> {@event.EventDateEnd}\n";
            }
            if (existEvent.EventDescription != @event.EventDescription)
            {
                description += $"Mô tả: {existEvent.EventDescription} -> {@event.EventDescription}\n";
            }
            announcement.AnnouncementDescription = description.TrimEnd('\n');
            announcement.AnnouncementDate = DateTime.Now;

            Event oldEvent = new Event
            {
                EventClassId = existEvent.EventClassId,
                EventTeacherId = existEvent.EventTeacherId,
                EventTitle = existEvent.EventTitle,
                EventZoomLink = existEvent.EventZoomLink,
                EventPassword = existEvent.EventPassword,
                EventDateStart = existEvent.EventDateStart,
                EventDateEnd = existEvent.EventDateEnd,
                EventDescription = existEvent.EventDescription
            };
            // Cập nhật thông tin sự kiện
            existEvent.EventDateEnd = @event.EventDateEnd;
            existEvent.EventDateStart = @event.EventDateStart;
            existEvent.EventDescription = Regex.Replace(@event.EventDescription.Trim(), @"\s+", " ");
            existEvent.EventTitle = Regex.Replace(@event.EventTitle.Trim(), @"\s+", " ");

            // Nếu sự kiện có liên kết Zoom -> Cập nhật trên Zoom API
            if (!string.IsNullOrEmpty(existEvent.EventZoomLink))
            {
                try
                {
                    string meetingId = ExtractMeetingId(existEvent.EventZoomLink);
                    bool updated = await _zoomService.UpdateZoomEventAsync(meetingId, oldEvent, existEvent);
                    if (!updated)
                    {
                        return StatusCode(500, "Lỗi khi cập nhật sự kiện Zoom");
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Lỗi Zoom API: {ex.Message}");
                }
            }
            _context.Events.Update(existEvent);
            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        private string ExtractMeetingId(string zoomLink)
        {
            var match = Regex.Match(zoomLink, @"\/j\/(\d+)");
            return match.Success ? match.Groups[1].Value : throw new Exception("Không tìm thấy Meeting ID");
        }

        [HttpDelete("delete-event")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var existEvent = await _context.Events.FirstOrDefaultAsync(e => e.EventId == id);
            if (existEvent == null)
            {
                return NotFound("Sự kiện không tồn tại");
            }

            if (existEvent.EventDateStart < DateTime.Now)
            {
                _context.Events.Remove(existEvent);
                await _context.SaveChangesAsync();
                return Ok("Sự kiện đã diễn ra, chỉ xóa trong hệ thống.");
            }

            // Tạo thông báo hủy sự kiện
            Announcement announcement = new Announcement
            {
                AnnouncementClassId = existEvent.EventClassId,
                AnnouncementTeacherId = existEvent.EventTeacherId,
                AnnouncementTitle = $"Sự kiện Zoom {existEvent.EventTitle} đã bị hủy",
                AnnouncementDescription = $"Sự kiện {existEvent.EventTitle} vào ngày {existEvent.EventDateStart} đã bị hủy.",
                AnnouncementDate = DateTime.Now
            };

            if (!string.IsNullOrEmpty(existEvent.EventZoomLink))
            {
                try
                {
                    string meetingId = ExtractMeetingId(existEvent.EventZoomLink);
                    bool deleted = await _zoomService.DeleteZoomEventAsync(meetingId, existEvent);
                    if (!deleted)
                    {
                        return StatusCode(500, "Lỗi khi xóa sự kiện trên Zoom");
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Lỗi Zoom API: {ex.Message}");
                }
            }

            // Xóa sự kiện trong hệ thống
            _context.Events.Remove(existEvent);
            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            return Ok("Xóa sự kiện thành công.");
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
