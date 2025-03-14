using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WebBaiGiangAPI.Data;
using WebBaiGiangAPI.Models;

public class ZoomService
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _dbContext;
    private readonly EmailService _emailServise;
    private readonly string hostKey = "064664";
    public ZoomService(HttpClient httpClient, AppDbContext dbContext, EmailService emailService)
    {
        _httpClient = httpClient;
        _dbContext = dbContext;
        _emailServise = emailService;
    }

    public async Task<string> GetAccessTokenAsync()
    {
        var clientId = "CXrAjTP0RY6gg66EU79S4Q";
        var clientSecret = "ranvk1xqdImS7ypbO8VjO7W65mtHKSWJ";
        var accountId = "hH6raBhVR0yHpaMBW3Ml_g";

        using var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "https://zoom.us/oauth/token");

        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "account_credentials" },
            { "account_id", accountId }
        });

        request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}")));

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Lỗi lấy Access Token: {response.StatusCode} - {error}");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(jsonResponse);
        return jsonDoc.RootElement.GetProperty("access_token").GetString();
    }

    public async Task<Event> CreateZoomEventAsync([FromBody] Event dataEvent)
    {
        var accessToken = await GetAccessTokenAsync();
        var requestUrl = "https://api.zoom.us/v2/users/me/meetings";

        var requestBody = new
        {
            topic = dataEvent.EventTitle,
            type = 2,
            start_time = dataEvent.EventDateStart,
            duration = (int)(dataEvent.EventDateEnd - dataEvent.EventDateStart).TotalMinutes,
            timezone = "Asia/Ho_Chi_Minh",
            password = dataEvent.EventPassword,
            agenda = dataEvent.EventDescription,
            screen_sharing = true, // Cho phép người tham gia share màn hình
            settings = new
            {
                host_video = true,
                participant_video = true,
                mute_upon_entry = true,
                join_before_host = true, // Cho phép tham gia trước khi host vào
                waiting_room = false, // Tắt phòng chờ nếu cần
                meeting_authentication =  true, // Đăng nhập khi vào zoom
                embed_password_in_join_link = false // Không nhúng mật khẩu vào link
            }
        };

        var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", accessToken) },
            Content = jsonContent
        };

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Lỗi tạo Zoom Event: {response.StatusCode} - {error}");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(jsonResponse);
        var joinUrl = jsonDoc.RootElement.GetProperty("join_url").GetString();

        var students = await (from sc in _dbContext.StudentClasses
                                   join u in _dbContext.Users on sc.ScStudentId equals u.UsersId
                                   join c in _dbContext.Classes on sc.ScClassId equals c.ClassId
                                   where sc.ScStatus == 1 && sc.ScClassId == dataEvent.EventClassId
                                   select new
                                   {
                                       u.UsersEmail,
                                       u.UsersName,
                                       c.ClassTitle
                                   }).ToListAsync();

        var teacher = await _dbContext.Users.FindAsync(dataEvent.EventTeacherId);

        foreach (var student in students)
        {
            await _emailServise.SendZoomEmail(dataEvent, student.UsersEmail, student.UsersName, student.ClassTitle, joinUrl, false, dataEvent.EventPassword);
        }


        await _emailServise.SendZoomEmail(dataEvent, teacher.UsersEmail, teacher.UsersName, students.First().ClassTitle , joinUrl, true, dataEvent.EventPassword, hostKey);

        var zoomEvent = new Event
        {
            EventClassId = dataEvent.EventClassId,
            EventTeacherId =dataEvent.EventTeacherId,
            EventTitle = requestBody.topic,
            EventZoomLink = jsonDoc.RootElement.GetProperty("join_url").GetString(),
            EventPassword = requestBody.password,
            EventDateStart = dataEvent.EventDateStart,
            EventDescription = requestBody.agenda,
            EventDateEnd = dataEvent.EventDateEnd,
        };

        // Lưu vào Database
        _dbContext.Events.Add(zoomEvent);
        await _dbContext.SaveChangesAsync();

        return zoomEvent;
    }

    public async Task<bool> UpdateZoomEventAsync(string meetingId,Event oldEvent, Event dataEvent)
    {
        var accessToken = await GetAccessTokenAsync();
        var requestUrl = $"https://api.zoom.us/v2/meetings/{meetingId}";

        var requestBody = new
        {
            topic = dataEvent.EventTitle,
            start_time = dataEvent.EventDateStart.ToString("yyyy-MM-ddTHH:mm:ss"),
            duration = (dataEvent.EventDateEnd != null)
                ? (int)(dataEvent.EventDateEnd - dataEvent.EventDateStart).TotalMinutes
                : 40,
            timezone = "Asia/Ho_Chi_Minh",
            password = dataEvent.EventPassword ?? "",
            agenda = dataEvent.EventDescription,
            screen_sharing = true,
            settings = new
            {
                host_video = true,
                participant_video = true,
                mute_upon_entry = true,
                join_before_host = true,
                waiting_room = false,
                meeting_authentication = true,
                embed_password_in_join_link = false
            }
        };

        var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Patch, requestUrl)
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", accessToken) },
            Content = jsonContent
        };

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Lỗi cập nhật sự kiện Zoom: {response.StatusCode} - {error}");
        }

        var students = await (from sc in _dbContext.StudentClasses
                              join u in _dbContext.Users on sc.ScStudentId equals u.UsersId
                              join c in _dbContext.Classes on sc.ScClassId equals c.ClassId
                              where sc.ScStatus == 1 && sc.ScClassId == dataEvent.EventClassId
                              select new
                              {
                                  u.UsersEmail,
                                  u.UsersName,
                                  c.ClassTitle
                              }).ToListAsync();

        var teacher = await _dbContext.Users.FindAsync(dataEvent.EventTeacherId);

        // Gửi email cho học sinh
        foreach (var student in students)
        {
            await _emailServise.SendUpdatedZoomEmail(oldEvent, dataEvent, student.UsersEmail, student.UsersName, student.ClassTitle, false);
        }

        // Gửi email cho giáo viên
        await _emailServise.SendUpdatedZoomEmail(oldEvent, dataEvent, teacher.UsersEmail, teacher.UsersName, students.First().ClassTitle, true, hostKey);

        return true;
    }

    public async Task<bool> DeleteZoomEventAsync(string meetingId, Event dataEvent)
    {
        var accessToken = await GetAccessTokenAsync();
        var requestUrl = $"https://api.zoom.us/v2/meetings/{meetingId}";

        var request = new HttpRequestMessage(HttpMethod.Delete, requestUrl)
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", accessToken) }
        };

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Lỗi xóa sự kiện trên Zoom: {response.StatusCode} - {error}");
        }

        // Lấy danh sách sinh viên trong lớp
        var students = await (from sc in _dbContext.StudentClasses
                              join u in _dbContext.Users on sc.ScStudentId equals u.UsersId
                              join c in _dbContext.Classes on sc.ScClassId equals c.ClassId
                              where sc.ScStatus == 1 && sc.ScClassId == dataEvent.EventClassId
                              select new
                              {
                                  u.UsersEmail,
                                  u.UsersName,
                                  c.ClassTitle
                              }).ToListAsync();

        var teacher = await _dbContext.Users.FindAsync(dataEvent.EventTeacherId);

        // Gửi email thông báo hủy sự kiện
        foreach (var student in students)
        {
            await _emailServise.SendDeletedZoomEmail(dataEvent, student.UsersEmail, student.UsersName, student.ClassTitle, false);
        }

        await _emailServise.SendDeletedZoomEmail(dataEvent, teacher.UsersEmail, teacher.UsersName, students.First().ClassTitle, true);

        return true;
    }

}

