using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebBaiGiangAPI.Models;

namespace WebBaiGiangAPI.Data
{
    public class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new AppDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<AppDbContext>>()))
            {
                // ------------------ Seed Countries ------------------
                if (!context.Countries.Any())
                {
                    var countries = new Country[]
                    {
                        new Country { CountryName = "Việt Nam" },
                        new Country { CountryName = "USA" }
                    };
                    foreach (var c in countries)
                    {
                        context.Countries.Add(c);
                    }
                    context.SaveChanges();
                }

                // ------------------ Seed States ------------------
                if (!context.States.Any())
                {
                    var states = new State[]
                    {
                        new State { StateName = "Đang học" },
                        new State { StateName = "Thôi học" },
                        new State {StateName = "Ngừng học" }
                    };
                    foreach (var s in states)
                    {
                        context.States.Add(s);
                    }
                    context.SaveChanges();
                }

                // ------------------ Seed Cities ------------------
                if (!context.Cities.Any())
                {
                    var cities = new City[]
                    {
                        new City { CityName = "Hà Nội" },
                        new City { CityName = "Hồ Chí Minh" }
                    };
                    foreach (var city in cities)
                    {
                        context.Cities.Add(city);
                    }
                    context.SaveChanges();
                }

                // ------------------ Seed LoginLevels ------------------
                if (!context.LoginLevels.Any())
                {
                    var loginLevels = new LoginLevel[]
                    {
                    new LoginLevel { LevelTitle = "Username", LevelDescription = "Đăng nhập bằng Username" },
                    new LoginLevel { LevelTitle = "Google", LevelDescription = "Đăng nhập bằng Google" },
                    new LoginLevel { LevelTitle = "Github", LevelDescription = "Đăng nhập bằng Github" }
                    };
                    foreach (var ll in loginLevels)
                    {
                        context.LoginLevels.Add(ll);
                    }
                    context.SaveChanges();
                }

                // ------------------ Seed Roles ------------------
                if (!context.Roles.Any())
                {
                    var roles = new Role[]
                    {
                    new Role { RoleName = "Admin" },
                    new Role { RoleName = "Teacher" },
                    new Role { RoleName = "Student" }
                    };
                    foreach (var role in roles)
                    {
                        context.Roles.Add(role);
                    }
                    context.SaveChanges();
                }

                // ------------------ Seed Departments ------------------
                if (!context.Departments.Any())
                {
                    var departments = new Department[]
                    {
                    new Department { DepartmentTitle = "Rỗng", DepartmentCode = "NULL", DepartmentDescription = "Rỗng" },
                    new Department { DepartmentTitle = "Khoa CNTT", DepartmentCode = "IT", DepartmentDescription = "Khoa Công nghệ thông tin" },
                    new Department { DepartmentTitle = "Khoa Kinh tế", DepartmentCode = "EC", DepartmentDescription = "Khoa Kinh tế" }
                    };
                    foreach (var dept in departments)
                    {
                        context.Departments.Add(dept);
                    }
                    context.SaveChanges();
                }

                // ------------------ Seed Months ------------------
                if (!context.Months.Any())
                {
                    var months = new Month[]
                    {
                        new Month { MonthTitle = "Tháng 1" },
                        new Month { MonthTitle = "Tháng 2" },
                        new Month { MonthTitle = "Tháng 3" },
                        new Month { MonthTitle = "Tháng 4" },
                        new Month { MonthTitle = "Tháng 5" },
                        new Month { MonthTitle = "Tháng 6" },
                        new Month { MonthTitle = "Tháng 7" },
                        new Month { MonthTitle = "Tháng 8" },
                        new Month { MonthTitle = "Tháng 9" },
                        new Month { MonthTitle = "Tháng 10" },
                        new Month { MonthTitle = "Tháng 11" },
                        new Month { MonthTitle = "Tháng 12" },

                    };
                    foreach (var m in months)
                    {
                        context.Months.Add(m);
                    }
                    context.SaveChanges();
                }

                // ------------------ Seed ExamTypes ------------------
                if (!context.ExamTypes.Any())
                {
                    var examTypes = new ExamType[]
                    {
                        new ExamType { EtypeTitle = "Thi giữa kỳ", EtypeDescription = "Thi giữa học kỳ" },
                        new ExamType { EtypeTitle = "Thi cuối kỳ", EtypeDescription = "Thi cuối học kỳ" }
                    };
                    foreach (var et in examTypes)
                    {
                        context.ExamTypes.Add(et);
                    }
                    context.SaveChanges();
                }

                // ------------------ Seed SchoolYears ------------------
                if (!context.SchoolYears.Any())
                {
                    var schoolYears = new SchoolYear[]
                    {
                    new SchoolYear { SyearTitle = "2023 - 2024", SyearDescription = "Niên khóa 2023-2024" },
                    new SchoolYear { SyearTitle = "2024 - 2025", SyearDescription = "Niên khóa 2024-2025" }
                    };
                    foreach (var sy in schoolYears)
                    {
                        context.SchoolYears.Add(sy);
                    }
                    context.SaveChanges();
                }

                // ------------------ Seed Semesters ------------------
                if (!context.Semesters.Any())
                {
                    var semesters = new Semester[]
                    {
                    new Semester { SemesterTitle = "Học kỳ 1", SemesterDescription = "Học kỳ 1" },
                    new Semester { SemesterTitle = "Học kỳ 2", SemesterDescription = "Học kỳ 2" }
                    };
                    foreach (var sem in semesters)
                    {
                        context.Semesters.Add(sem);
                    }
                    context.SaveChanges();
                }

                // ------------------ Seed Courses ------------------
                if (!context.Courses.Any())
                {
                    var courses = new Course[]
                    {
                    new Course { CourseTitle = "Lập trình C#", CourseTotalSemester = 1 },
                    new Course { CourseTitle = "Lập trình Java", CourseTotalSemester = 1 }
                    };
                    foreach (var course in courses)
                    {
                        context.Courses.Add(course);
                    }
                    context.SaveChanges();
                }

                // ------------------ Seed Users ------------------
                if (!context.Users.Any())
                {
                    var passwordHasher = new PasswordHasher<Users>();

                    var users = new Users[]
                    {
                    new Users
                    {
                        UsersRoleId = 1,
                        UsersDepartmentId = 1,
                        UserLevelId = 1,
                        UsersName = "Admin User",
                        UsersUsername = "admin",
                        UsersPassword = passwordHasher.HashPassword(null,"123"),
                        UsersEmail = "admin@example.com",
                        UsersMobile = "0123456789"
                    },
                    new Users
                    {
                        UsersRoleId = 2,
                        UsersDepartmentId = 1,
                        UserLevelId = 2,
                        UsersName = "Teacher User",
                        UsersUsername = "teacher",
                        UsersPassword = passwordHasher.HashPassword(null,"123"),
                        UsersEmail = "teacher@example.com",
                        UsersMobile = "0987654321"
                    }
                    };
                    foreach (var u in users)
                    {
                        context.Users.Add(u);
                    }
                    context.SaveChanges();
                }

                // ------------------ Seed UserLogs ------------------
                if (!context.UserLogs.Any())
                {
                    var userLogs = new UsersLog[]
                    {
                        new UsersLog
                        {
                            UlogUsersId = 1,
                            UlogUsername = "admin",
                            UlogLoginDate = DateTime.Now
                        }
                    };
                    foreach (var ul in userLogs)
                    {
                        context.UserLogs.Add(ul);
                    }
                    context.SaveChanges();
                }

                // ------------------ Seed Class ------------------
                if (!context.Classes.Any())
                {
                    var classes = new Class[]
                    {
                        new Class
                        {
                            ClassTitle = "Lớp 10A1",
                            ClassSemesterId = 1,
                            ClassSyearId = 1,
                            ClassUpdateAt = DateTime.Now
                        },
                        new Class
                        {
                            ClassTitle = "Lớp 11A1",
                            ClassSemesterId = 2,
                            ClassSyearId = 1,
                            ClassUpdateAt = DateTime.Now
                        }
                    };
                    foreach (var sc in classes)
                    {
                        context.Classes.Add(sc);
                    }
                    context.SaveChanges();
                }

                // ------------------ Seed Feedbacks ------------------
                if (!context.Feedbacks.Any())
                {
                    var feedbacks = new Feedback[]
                    {
                        new Feedback
                        {
                            FeedbackUsersId = 2,
                            FeedbackClassId = 1,
                            FeedbackContent = "Tuyệt vời",
                            FeedbackRate = 5,
                            FeedbackDate = DateTime.Now,
                            FeedbackStatus = 1
                        }
                    };
                    foreach (var f in feedbacks)
                    {
                        context.Feedbacks.Add(f);
                    }
                    context.SaveChanges();
                }

                // ------------------ Seed Lessons ------------------
                if (!context.Lessons.Any())
                {
                    var lessons = new Lesson[]
                    {
                        new Lesson
                        {
                            LessonClassId = 1,
                            LessonCourseId = 1,
                            LessonName = "Bài học Cơ bản",
                            LessonDescription = "Mô tả bài học",
                            LessonStatus = 1
                        }
                    };
                    foreach (var l in lessons)
                    {
                        context.Lessons.Add(l);
                    }
                    context.SaveChanges();
                }

                // ------------------ Seed LessonFiles ------------------
                if (!context.LessonFiles.Any())
                {
                    var lessonFiles = new LessonFile[]
                    {
                        new LessonFile
                        {
                            LfLessonId = 1,
                            LfFilename = "file1.png",
                            LfType = "Hình ảnh"
                        }
                    };
                    foreach (var lf in lessonFiles)
                    {
                        context.LessonFiles.Add(lf);
                    }
                    context.SaveChanges();
                }

                // ------------------ Seed TeacherClasses ------------------
                if (!context.TeacherClasses.Any())
                {
                    var teacherClasses = new TeacherClass[]
                    {
                        new TeacherClass
                        {
                            TcUsersId = 2,
                            TcClassId = 1,
                            TcDescription = "Giảng dạy Toán"
                        }
                    };
                    foreach (var tc in teacherClasses)
                    {
                        context.TeacherClasses.Add(tc);
                    }
                    context.SaveChanges();
                }

                // ------------------ Seed Students ------------------
                if (!context.Students.Any())
                {
                    var students = new Student[]
                    {
                    new Student
                    {
                        StudentId = 2,
                        StudentCode = "0306222222",
                        StudentRollno = "001",
                        StudentFatherName = "Bố A",
                        StudentDetails = "Chi tiết sinh viên"
                    }
                    };
                    foreach (var s in students)
                    {
                        context.Students.Add(s);
                    }
                    context.SaveChanges();
                }

                // ------------------ Seed Subjects ------------------
                if (!context.Subjects.Any())
                {
                    var subjects = new Subject[]
                    {
                        new Subject
                        {
                            SubjectClassId = 1,
                            SubjectTitle = "Toán",
                            SubjectDescription = "Môn Toán"
                        }
                    };
                    foreach (var sub in subjects)
                    {
                        context.Subjects.Add(sub);
                    }
                    context.SaveChanges();
                }

                // ------------------ Seed Quizzes ------------------
                if (!context.Quizzes.Any())
                {
                    var quizzes = new Quiz[]
                    {
                    new Quiz
                    {
                        QuizClassId = 1,
                        QuizTeacherId = 2,
                        QuizTitle = "Kiểm tra Toán",
                        QuizDescription = "Mô tả bài kiểm tra"
                    }
                    };
                    foreach (var q in quizzes)
                    {
                        context.Quizzes.Add(q);
                    }
                    context.SaveChanges();
                }

                // ------------------ Seed QuizQuestions ------------------
                if (!context.QuizQuestions.Any())
                {
                    var quizQuestions = new QuizQuestion[]
                    {
                        new QuizQuestion
                        {
                            QqQuizId = 1,
                            QqQuestion = "2+2=?",
                            QqOption1 = "3",
                            QqOption2 = "4",
                            QqOption3 = "5",
                            QqOption4 = "6",
                            QqCorrect = "4"
                        }
                    };
                    foreach (var qq in quizQuestions)
                    {
                        context.QuizQuestions.Add(qq);
                    }
                    context.SaveChanges();
                }

                // ------------------ Seed QuizResults ------------------
                if (!context.QuizResults.Any())
                {
                    var quizResults = new QuizResult[]
                    {
                        new QuizResult
                        {
                            QrQuizId = 1,
                            QrStudentId = 2,
                            QrTotalQuestion = 1,
                            QrAnswer = 1,
                            QrDate = DateTime.Now
                        }
                    };
                    foreach (var qr in quizResults)
                    {
                        context.QuizResults.Add(qr);
                    }
                    context.SaveChanges();
                }

                // ------------------ Seed Exams ------------------
                if (!context.Exams.Any())
                {
                    var exams = new Exam[]
                    {
                        new Exam
                        {
                            ExamTitle = "Thi giữa kỳ",
                            ExamEtypeId = 1,
                            ExamMonth = 1,
                            ExamDescription = "Thi giữa kỳ"
                        }
                    };
                    foreach (var exam in exams)
                    {
                        context.Exams.Add(exam);
                    }
                    context.SaveChanges();
                }

                // ------------------ Seed Marks ------------------
                if (!context.Marks.Any())
                {
                    var marks = new Marks[]
                    {
                    new Marks
                    {
                        MarksExamId = 1,
                        MarksStudentId = 2,
                        MarksSubjectId = 1,
                        MarksWritten = 8.5m,
                        MarksPractical = 9.0m,
                        MarksSemesterId = 1,
                        MarksDescription = "Đạt"
                    }
                    };
                    foreach (var m in marks)
                    {
                        context.Marks.Add(m);
                    }
                    context.SaveChanges();
                }

                // ------------------ Seed AttendanceMarks ------------------
                if (!context.AttendanceMarks.Any())
                {
                    var attendanceMarks = new AttendanceMarks[]
                    {
                        new AttendanceMarks
                        {
                            StudentId = 2,
                            ClassId = 1,
                            AttendanceDate = DateTime.Now,
                            AttendanceStatus = "P"
                        }
                    };
                    foreach (var am in attendanceMarks)
                    {
                        context.AttendanceMarks.Add(am);
                    }
                    context.SaveChanges();
                }

                // ------------------ Seed Assignments ------------------
                if (!context.Assignments.Any())
                {
                    var assignments = new Assignment[]
                    {
                        new Assignment
                        {
                            AssignmentTitle = "Bài tập Toán",
                            AssignmentClassId = 1,
                            AssignmentTeacherId = 1,
                            AssignmentCreateAt = DateTime.Now,
                            AssignmentStart = DateTime.Now,
                            AssignmentStatus = 1
                        }
                    };
                    foreach (var a in assignments)
                    {
                        context.Assignments.Add(a);
                    }
                    context.SaveChanges();
                }

                // ------------------ Seed Submits ------------------
                if (!context.Submits.Any())
                {
                    var submits = new Submit[]
                    {
                        new Submit
                        {
                            SubmitAssignmentId = 1,
                            SubmitStudentId = 2,
                            SubmitFile = "submit1.pdf",
                            SubmitDate = DateTime.Now,
                            SubmitStatus = 1
                        }
                    };
                    foreach (var su in submits)
                    {
                        context.Submits.Add(su);
                    }
                    context.SaveChanges();
                }

                // ------------------ Seed Announcements ------------------
                if (!context.Announcements.Any())
                {
                    var announcements = new Announcement[]
                    {
                        new Announcement
                        {
                            AnnouncementClassId = 1,
                            AnnouncementTeacherId = 2,
                            AnnouncementTitle = "Thông báo lớp",
                            AnnouncementDescription = "Mô tả",
                            AnnouncementDate = DateTime.Now
                        }
                    };
                    foreach (var ann in announcements)
                    {
                        context.Announcements.Add(ann);
                    }
                    context.SaveChanges();
                }

                // ------------------ Seed Events ------------------
                if (!context.Events.Any())
                {
                    var events = new Event[]
                    {
                        new Event
                        {
                            EventClassId = 1,
                            EventTeacherId = 2,
                            EventTitle = "Sự kiện",
                            EventDescription = "Mô tả sự kiện",
                            EventDateStart = DateTime.Now,
                            EventDateEnd = DateTime.Now.AddDays(1)
                        }
                    };
                    foreach (var ev in events)
                    {
                        context.Events.Add(ev);
                    }
                    context.SaveChanges();
                }

                // ------------------ Seed Messages ------------------
                if (!context.Messages.Any())
                {
                    var messages = new Message[]
                    {
                        new Message
                        {
                            MessageSenderId = 1,
                            MessageReceiverId = 2,
                            MessageType = "Chat",
                            MessageSenderType = "Admin",
                            MessageDate = DateTime.Now,
                            MessageSubject = "Chào",
                            MessageContent = "Hello"
                        }
                    };
                    foreach (var m in messages)
                    {
                        context.Messages.Add(m);
                    }
                    context.SaveChanges();
                }

                // ------------------ Seed Files ------------------
                if (!context.Files.Any())
                {
                    var fileEntities = new Files[]
                    {
                        new Files
                        {
                            FilesTitle = "Tài liệu",
                            FilesClassId = 1,
                            FilesTeacherId = 1,
                            FilesFilename = "document.pdf",
                            FilesDescription = "Mô tả file"
                        }
                    };
                    foreach (var fe in fileEntities)
                    {
                        context.Files.Add(fe);
                    }
                    context.SaveChanges();
                }

                // ------------------ Seed StudentClasses ------------------
                if (!context.StudentClasses.Any())
                {
                    var studentClasses = new StudentClass[]
                    {
                        new StudentClass
                        {
                            ScStudentId = 2,
                            ScClassId = 1,
                            ScDescription = "Sinh viên lớp CĐ TH 22 WEBC"
                        }
                    };
                    foreach (var sc in studentClasses)
                    {
                        context.StudentClasses.Add(sc);
                    }
                    context.SaveChanges();
                }

                // ------------------ Seed ClassCourses ------------------
                if (!context.ClassCourses.Any())
                {
                    var classCourses = new ClassCourse[]
                    {
                        new ClassCourse
                        {
                            ClassId = 1,
                            CourseId = 1,
                            CcDescription = "Lớp và học phần"
                        }
                    };
                    foreach (var cc in classCourses)
                    {
                        context.ClassCourses.Add(cc);
                    }
                    context.SaveChanges();
                }
            }
        }
    }
}
