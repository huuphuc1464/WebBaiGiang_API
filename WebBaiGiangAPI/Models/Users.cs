using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace WebBaiGiangAPI.Models
{
    [Index(nameof(UsersUsername), IsUnique = true)]
    [Index(nameof(UsersEmail), IsUnique = true)]
    [Index(nameof(UsersMobile), IsUnique = true)]
    public class Users
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UsersId { get; set; }

        [Required]
        public int UsersRoleId { get; set; }

        [Required]
        [MaxLength(100)]
        public string UsersName { get; set; }

        [Required]
        [MaxLength(255)]
        public string UsersUsername { get; set; }

        [MaxLength(255)]
        public string? UsersPassword { get; set; }

        [Required]
        [MaxLength(255)]
        public string UsersEmail { get; set; }

        [MaxLength(10)]
        public string? UsersMobile { get; set; }

        public DateOnly? UsersDob { get; set; }

        [MaxLength(255)]
        public string? UsersImage { get; set; }

        [MaxLength(255)]
        public string? UsersAdd { get; set; }

        public int? UsersCity { get; set; }
        public int? UsersState { get; set; }
        public int? UsersCountry { get; set; }

        public int UsersDepartmentId { get; set; }

        [Required]
        public int UserLevelId { get; set; }

        [MaxLength(3)]
        public string? UserGender { get; set; }

        // Navigation properties
        [JsonIgnore]
        [ForeignKey("UsersDepartmentId")]
        public Department? Department { get; set; }

        [JsonIgnore]
        [ForeignKey("UsersRoleId")]
        public Role? Role { get; set; }

        [JsonIgnore]
        [ForeignKey("UserLevelId")]
        public LoginLevel? LoginLevel { get; set; }

        [JsonIgnore]
        [ForeignKey("UsersCity")]
        public City? City { get; set; }

        [JsonIgnore]
        [ForeignKey("UsersState")]
        public State? State { get; set; }

        [JsonIgnore]
        [ForeignKey("UsersCountry")]
        public Country? Country { get; set; }
        [JsonIgnore] 
        public ICollection<UsersLog> UsersLog { get; set; } = new List<UsersLog>();
        [JsonIgnore]
        public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
        [JsonIgnore]
        public ICollection<TeacherClass> TeacherClasses { get; set; } = new List<TeacherClass>();
        //[JsonIgnore]
        //public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
        [JsonIgnore]
        public ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();
        [JsonIgnore]
        public ICollection<Event> Events { get; set; } = new List<Event>();
        [JsonIgnore]
        public ICollection<Message> SentMessages { get; set; } = new List<Message>();
        [JsonIgnore]
        public ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
        [JsonIgnore]
        public ICollection<Files> Files { get; set; } = new List<Files>();
        //[JsonIgnore]
        //public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();

        [JsonIgnore]
        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    }
}
