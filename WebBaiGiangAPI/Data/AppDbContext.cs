using Microsoft.EntityFrameworkCore;
using WebBaiGiangAPI.Models;

namespace WebBaiGiangAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        { }
        public DbSet<Country> Countries { get; set; }
        public DbSet<State> States { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<LoginLevel> LoginLevels { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Month> Months { get; set; }
        public DbSet<ExamType> ExamTypes { get; set; }
        public DbSet<SchoolYear> SchoolYears { get; set; }
        public DbSet<Semester> Semesters { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Users> Users { get; set; }
        public DbSet<UsersLog> UserLogs { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<LessonFile> LessonFiles { get; set; }
        public DbSet<TeacherClass> TeacherClasses { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<QuizQuestion> QuizQuestions { get; set; }
        public DbSet<QuizResult> QuizResults { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<Marks> Marks { get; set; }
        public DbSet<AttendanceMarks> AttendanceMarks { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<Submit> Submits { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Files> Files { get; set; }
        public DbSet<StudentClass> StudentClasses { get; set; }
        public DbSet<ClassCourse> ClassCourses { get; set; }
        public DbSet<StatusLearn> StatusLearns { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // --- User Entity ---
            modelBuilder.Entity<Users>()
                .HasOne(u => u.Department)
                .WithMany(d => d.Users)
                .HasForeignKey(u => u.UsersDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Users>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.UsersRoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Users>()
                .HasOne(u => u.LoginLevel)
                .WithMany(ll => ll.Users)
                .HasForeignKey(u => u.UserLevelId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Users>()
                .HasOne(u => u.City)
                .WithMany(c => c.Users)
                .HasForeignKey(u => u.UsersCity)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Users>()
                .HasOne(u => u.State)
                .WithMany(s => s.Users)
                .HasForeignKey(u => u.UsersState)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Users>()
                .HasOne(u => u.Country)
                .WithMany(cn => cn.Users)
                .HasForeignKey(u => u.UsersCountry)
                .OnDelete(DeleteBehavior.Restrict);

            // --- UserLog ---
            modelBuilder.Entity<UsersLog>()
                .HasOne(ul => ul.Users)
                .WithMany(u => u.UsersLog)
                .HasForeignKey(ul => ul.UlogUsersId)
                .OnDelete(DeleteBehavior.Cascade);

            // --- Class ---
            modelBuilder.Entity<Class>()
                .HasOne(c => c.Semester)
                .WithMany(s => s.Classes)
                .HasForeignKey(c => c.ClassSemesterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Class>()
                .HasOne(c => c.SchoolYear)
                .WithMany(sy => sy.Classes)
                .HasForeignKey(c => c.ClassSyearId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- Feedback ---
            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.User)
                .WithMany(u => u.Feedbacks)
                .HasForeignKey(f => f.FeedbackUsersId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.Classes)
                .WithMany(c => c.Feedbacks)
                .HasForeignKey(f => f.FeedbackClassId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- Lesson ---
            //modelBuilder.Entity<Lesson>()
            //    .HasOne(l => l.Classes)
            //    .WithMany(c => c.Lessons)
            //    .HasForeignKey(l => l.LessonClassId)
            //    .OnDelete(DeleteBehavior.Restrict);

            //modelBuilder.Entity<Lesson>()
            //    .HasOne(l => l.Course)
            //    .WithMany(c => c.Lessons)
            //    .HasForeignKey(l => l.LessonCourseId)
            //    .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Lesson>()
                .HasOne(l => l.ClassCourse)
                .WithMany(c => c.Lessons)
                .HasForeignKey(l => l.LessonClassCourseId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- LessonFile ---
            modelBuilder.Entity<LessonFile>()
                .HasOne(lf => lf.Lesson)
                .WithMany(l => l.LessonFiles)
                .HasForeignKey(lf => lf.LfLessonId)
                .OnDelete(DeleteBehavior.Cascade);

            // --- TeacherClass ---
            modelBuilder.Entity<TeacherClass>()
                .HasOne(tc => tc.User)
                .WithMany(u => u.TeacherClasses)
                .HasForeignKey(tc => tc.TcUsersId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TeacherClass>()
                .HasOne(tc => tc.ClassCourses)
                .WithMany(c => c.TeacherClasses)
                .HasForeignKey(tc => tc.TcClassCourseId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- Student (StudentId là FK đến User) ---
            modelBuilder.Entity<Student>()
                .HasOne(s => s.Users)
                .WithOne()
                .HasForeignKey<Student>(s => s.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // --- Subject ---
            modelBuilder.Entity<Subject>()
                .HasOne(sub => sub.Classes)
                .WithMany(c => c.Subjects)
                .HasForeignKey(sub => sub.SubjectClassId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- Quiz ---
            //modelBuilder.Entity<Quiz>()
            //    .HasOne(q => q.Classes)
            //    .WithMany(c => c.Quizzes)
            //    .HasForeignKey(q => q.QuizClassId)
            //    .OnDelete(DeleteBehavior.Restrict);

            //modelBuilder.Entity<Quiz>()
            //    .HasOne(q => q.Teacher)
            //    .WithMany(u => u.Quizzes)
            //    .HasForeignKey(q => q.QuizTeacherId)
            //    .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Quiz>()
                .HasOne(q => q.ClassCourse)
                .WithMany(u => u.Quizzes)
                .HasForeignKey(q => q.QuizClassCourseId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- QuizQuestion ---
            modelBuilder.Entity<QuizQuestion>()
                .HasOne(qq => qq.Quiz)
                .WithMany(q => q.QuizQuestions)
                .HasForeignKey(qq => qq.QqQuizId)
                .OnDelete(DeleteBehavior.Cascade);

            // --- QuizResult ---
            modelBuilder.Entity<QuizResult>()
                .HasOne(qr => qr.Quiz)
                .WithMany(q => q.QuizResults)
                .HasForeignKey(qr => qr.QrQuizId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizResult>()
                .HasOne(qr => qr.Student)
                .WithMany(s => s.QuizResults)
                .HasForeignKey(qr => qr.QrStudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // --- Exam ---
            modelBuilder.Entity<Exam>()
                .HasOne(e => e.ExamType)
                .WithMany(et => et.Exams)
                .HasForeignKey(e => e.ExamEtypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Exam>()
                .HasOne(e => e.Month)
                .WithMany(m => m.Exams)
                .HasForeignKey(e => e.ExamMonth)
                .OnDelete(DeleteBehavior.Restrict);

            // --- Marks ---
            modelBuilder.Entity<Marks>()
                .HasOne(m => m.Exam)
                .WithMany(e => e.Marks)
                .HasForeignKey(m => m.MarksExamId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Marks>()
                .HasOne(m => m.Student)
                .WithMany(s => s.Marks)
                .HasForeignKey(m => m.MarksStudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Marks>()
                .HasOne(m => m.Subject)
                .WithMany(sub => sub.Marks)
                .HasForeignKey(m => m.MarksSubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Marks>()
                .HasOne(m => m.Semester)
                .WithMany(se => se.Marks)
                .HasForeignKey(m => m.MarksSemesterId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- AttendanceMark ---
            modelBuilder.Entity<AttendanceMarks>()
                .HasOne(am => am.Student)
                .WithMany(s => s.AttendanceMarks)
                .HasForeignKey(am => am.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AttendanceMarks>()
                .HasOne(am => am.Classes)
                .WithMany(c => c.AttendanceMarks)
                .HasForeignKey(am => am.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- Assignment ---
            modelBuilder.Entity<Assignment>()
                .HasOne(a => a.Users)
                .WithMany(tc => tc.Assignments)
                .HasForeignKey(a => a.AssignmentTeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Assignment>()
                .HasOne(a => a.Classes)
                .WithMany(c => c.Assignments)
                .HasForeignKey(a => a.AssignmentClassId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- Submit ---
            modelBuilder.Entity<Submit>()
                .HasOne(su => su.Assignment)
                .WithMany(a => a.Submits)
                .HasForeignKey(su => su.SubmitAssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Submit>()
                .HasOne(su => su.Student)
                .WithMany(s => s.Submits)
                .HasForeignKey(su => su.SubmitStudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // --- Announcement ---
            modelBuilder.Entity<Announcement>()
                .HasOne(a => a.Classes)
                .WithMany(c => c.Announcements)
                .HasForeignKey(a => a.AnnouncementClassId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Announcement>()
                .HasOne(a => a.Teacher)
                .WithMany(u => u.Announcements)
                .HasForeignKey(a => a.AnnouncementTeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- Event ---
            modelBuilder.Entity<Event>()
                .HasOne(e => e.Classes)
                .WithMany(c => c.Events)
                .HasForeignKey(e => e.EventClassId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Event>()
                .HasOne(e => e.Teacher)
                .WithMany(u => u.Events)
                .HasForeignKey(e => e.EventTeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- Message ---
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.MessageSenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(m => m.MessageReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- Files ---
            modelBuilder.Entity<Files>()
                .HasOne(f => f.Classes)
                .WithMany(c => c.Files)
                .HasForeignKey(f => f.FilesClassId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Files>()
                .HasOne(f => f.Teacher)
                .WithMany(u => u.Files)
                .HasForeignKey(f => f.FilesTeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- StudentClass ---
            modelBuilder.Entity<StudentClass>()
                .HasOne(c => c.Student)
                .WithMany(s => s.StudentClasses)
                .HasForeignKey(c => c.ScStudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentClass>()
                .HasOne(sc => sc.Classes)
                .WithMany(c => c.StudentClasses)
                .HasForeignKey(c => c.ScClassId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- ClassCourse ---
            modelBuilder.Entity<ClassCourse>()
                .HasOne(cc => cc.Classes)
                .WithMany(c => c.ClassCourses)
                .HasForeignKey(cc => cc.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClassCourse>()
                .HasOne(cc => cc.Course)
                .WithMany(c => c.ClassCourses)
                .HasForeignKey(cc => cc.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            base.OnModelCreating(modelBuilder);
        }
    }
}

