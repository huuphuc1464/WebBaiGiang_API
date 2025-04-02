using System;
using System.Net.NetworkInformation;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebBaiGiangAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    CityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CityName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.CityId);
                });

            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    CountryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CountryName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.CountryId);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    DepartmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DepartmentDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DepartmentCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.DepartmentId);
                });

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    CourseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseDepartmentId = table.Column<int>(type: "int", nullable: false),
                    CourseTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CourseTotalSemester = table.Column<int>(type: "int", nullable: false),
                    CourseImage = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CourseShortdescription = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CourseDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CourseUpdateAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.CourseId);
                    table.ForeignKey(
                        name: "FK_Courses_Departments_CourseDepartmentId",
                        column: x => x.CourseDepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExamTypes",
                columns: table => new
                {
                    EtypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EtypeTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    EtypeDescription = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamTypes", x => x.EtypeId);
                });

            migrationBuilder.CreateTable(
                name: "LoginLevels",
                columns: table => new
                {
                    LevelId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LevelTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    LevelDescription = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginLevels", x => x.LevelId);
                });

            migrationBuilder.CreateTable(
                name: "Months",
                columns: table => new
                {
                    MonthId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MonthTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Months", x => x.MonthId);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "SchoolYears",
                columns: table => new
                {
                    SyearId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SyearTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SyearDescription = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolYears", x => x.SyearId);
                });

            migrationBuilder.CreateTable(
                name: "Semesters",
                columns: table => new
                {
                    SemesterId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SemesterTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SemesterDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SemesterStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SemesterEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Semesters", x => x.SemesterId);
                });

            migrationBuilder.CreateTable(
                name: "States",
                columns: table => new
                {
                    StateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_States", x => x.StateId);
                });

            migrationBuilder.CreateTable(
                name: "Exams",
                columns: table => new
                {
                    ExamId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ExamEtypeId = table.Column<int>(type: "int", nullable: false),
                    ExamMonth = table.Column<int>(type: "int", nullable: false),
                    ExamDescription = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exams", x => x.ExamId);
                    table.ForeignKey(
                        name: "FK_Exams_ExamTypes_ExamEtypeId",
                        column: x => x.ExamEtypeId,
                        principalTable: "ExamTypes",
                        principalColumn: "EtypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Exams_Months_ExamMonth",
                        column: x => x.ExamMonth,
                        principalTable: "Months",
                        principalColumn: "MonthId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Classes",
                columns: table => new
                {
                    ClassId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ClassDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClassSemesterId = table.Column<int>(type: "int", nullable: false),
                    ClassSyearId = table.Column<int>(type: "int", nullable: false),
                    ClassUpdateAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classes", x => x.ClassId);
                    table.ForeignKey(
                        name: "FK_Classes_SchoolYears_ClassSyearId",
                        column: x => x.ClassSyearId,
                        principalTable: "SchoolYears",
                        principalColumn: "SyearId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Classes_Semesters_ClassSemesterId",
                        column: x => x.ClassSemesterId,
                        principalTable: "Semesters",
                        principalColumn: "SemesterId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UsersId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsersRoleId = table.Column<int>(type: "int", nullable: false),
                    UsersName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UsersUsername = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UsersPassword = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UsersEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UsersMobile = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    UsersDob = table.Column<DateOnly>(type: "date", nullable: true),
                    UsersImage = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UsersAdd = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UsersCity = table.Column<int>(type: "int", nullable: true),
                    UsersState = table.Column<int>(type: "int", nullable: true),
                    UsersCountry = table.Column<int>(type: "int", nullable: true),
                    UsersDepartmentId = table.Column<int>(type: "int", nullable: true),
                    UserLevelId = table.Column<int>(type: "int", nullable: false),
                    UserGender = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UsersId);
                    table.ForeignKey(
                        name: "FK_Users_Cities_UsersCity",
                        column: x => x.UsersCity,
                        principalTable: "Cities",
                        principalColumn: "CityId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Countries_UsersCountry",
                        column: x => x.UsersCountry,
                        principalTable: "Countries",
                        principalColumn: "CountryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Departments_UsersDepartmentId",
                        column: x => x.UsersDepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_LoginLevels_UserLevelId",
                        column: x => x.UserLevelId,
                        principalTable: "LoginLevels",
                        principalColumn: "LevelId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Roles_UsersRoleId",
                        column: x => x.UsersRoleId,
                        principalTable: "Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_States_UsersState",
                        column: x => x.UsersState,
                        principalTable: "States",
                        principalColumn: "StateId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClassCourses",
                columns: table => new
                {
                    CcId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    CcDescription = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassCourses", x => x.CcId);
                    table.ForeignKey(
                        name: "FK_ClassCourses_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "ClassId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassCourses_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "CourseId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Lessons",
                columns: table => new
                {
                    LessonId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    //LessonClassId = table.Column<int>(type: "int", nullable: false),
                    //LessonCourseId = table.Column<int>(type: "int", nullable: false),
                    LessonTeacherId = table.Column<int>(type: "int", nullable: false),
                    LessonClassCourseId = table.Column<int>(type: "int", nullable: false),
                    LessonDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LessonChapter = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    LessonWeek = table.Column<int>(type: "int", nullable: true),
                    LessonName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LessonStatus = table.Column<bool>(type: "bit", nullable: false),
                    //LessonCourseStatus = table.Column<bool>(type: "bit", nullable: true),
                    LessonCreateAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LessonUpdateAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lessons", x => x.LessonId);
                    //table.ForeignKey(
                    //    name: "FK_Lessons_Classes_LessonClassId",
                    //    column: x => x.LessonClassId,
                    //    principalTable: "Classes",
                    //    principalColumn: "ClassId",
                    //    onDelete: ReferentialAction.Restrict);

                    table.ForeignKey(
                        name: "FK_Lessons_Users_LessonTeacherId",
                        column: x => x.LessonTeacherId,
                        principalTable: "Users",
                        principalColumn: "UsersId",
                        onDelete: ReferentialAction.Restrict);
                    
                    table.ForeignKey(
                        name: "FK_Lessons_ClassCourses_LessonClassCourseId",
                        column: x => x.LessonClassCourseId,
                        principalTable: "ClassCourses",
                        principalColumn: "CcId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    SubjectId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubjectClassId = table.Column<int>(type: "int", nullable: false),
                    SubjectTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SubjectDescription = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.SubjectId);
                    table.ForeignKey(
                        name: "FK_Subjects_Classes_SubjectClassId",
                        column: x => x.SubjectClassId,
                        principalTable: "Classes",
                        principalColumn: "ClassId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Announcements",
                columns: table => new
                {
                    AnnouncementId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnnouncementClassId = table.Column<int>(type: "int", nullable: false),
                    AnnouncementTeacherId = table.Column<int>(type: "int", nullable: false),
                    AnnouncementTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AnnouncementDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnnouncementDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcements", x => x.AnnouncementId);
                    table.ForeignKey(
                        name: "FK_Announcements_Classes_AnnouncementClassId",
                        column: x => x.AnnouncementClassId,
                        principalTable: "Classes",
                        principalColumn: "ClassId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Announcements_Users_AnnouncementTeacherId",
                        column: x => x.AnnouncementTeacherId,
                        principalTable: "Users",
                        principalColumn: "UsersId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventClassId = table.Column<int>(type: "int", nullable: false),
                    EventTeacherId = table.Column<int>(type: "int", nullable: false),
                    EventTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    EventDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventDateStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EventDateEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EventZoomLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventPassword = table.Column<string>(type: "nvarchar(max)", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.EventId);
                    table.ForeignKey(
                        name: "FK_Events_Classes_EventClassId",
                        column: x => x.EventClassId,
                        principalTable: "Classes",
                        principalColumn: "ClassId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Events_Users_EventTeacherId",
                        column: x => x.EventTeacherId,
                        principalTable: "Users",
                        principalColumn: "UsersId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Feedbacks",
                columns: table => new
                {
                    FeedbackId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeedbackUsersId = table.Column<int>(type: "int", nullable: false),
                    FeedbackClassId = table.Column<int>(type: "int", nullable: false),
                    FeedbackContent = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FeedbackRate = table.Column<int>(type: "int", nullable: false),
                    FeedbackDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FeedbackStatus = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedbacks", x => x.FeedbackId);
                    table.ForeignKey(
                        name: "FK_Feedbacks_Classes_FeedbackClassId",
                        column: x => x.FeedbackClassId,
                        principalTable: "Classes",
                        principalColumn: "ClassId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Feedbacks_Users_FeedbackUsersId",
                        column: x => x.FeedbackUsersId,
                        principalTable: "Users",
                        principalColumn: "UsersId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Files",
                columns: table => new
                {
                    FilesId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FilesTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilesClassId = table.Column<int>(type: "int", nullable: false),
                    FilesTeacherId = table.Column<int>(type: "int", nullable: false),
                    FilesFilename = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilesDescription = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Files", x => x.FilesId);
                    table.ForeignKey(
                        name: "FK_Files_Classes_FilesClassId",
                        column: x => x.FilesClassId,
                        principalTable: "Classes",
                        principalColumn: "ClassId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Files_Users_FilesTeacherId",
                        column: x => x.FilesTeacherId,
                        principalTable: "Users",
                        principalColumn: "UsersId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    MessageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageSenderId = table.Column<int>(type: "int", nullable: true),
                    MessageReceiverId = table.Column<int>(type: "int", nullable: true),
                    MessageType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MessageSenderType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MessageDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MessageSubject = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    MessageContent = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.MessageId);
                    table.ForeignKey(
                        name: "FK_Messages_Users_MessageReceiverId",
                        column: x => x.MessageReceiverId,
                        principalTable: "Users",
                        principalColumn: "UsersId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Messages_Users_MessageSenderId",
                        column: x => x.MessageSenderId,
                        principalTable: "Users",
                        principalColumn: "UsersId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Quizzes",
                columns: table => new
                {
                    QuizId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuizClassId = table.Column<int>(type: "int", nullable: false),
                    QuizTeacherId = table.Column<int>(type: "int", nullable: false),
                    QuizTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    QuizCreateAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuizUpdateAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuizStartAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuizEndAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuizDescription = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quizzes", x => x.QuizId);
                    table.ForeignKey(
                        name: "FK_Quizzes_Classes_QuizClassId",
                        column: x => x.QuizClassId,
                        principalTable: "Classes",
                        principalColumn: "ClassId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Quizzes_Users_QuizTeacherId",
                        column: x => x.QuizTeacherId,
                        principalTable: "Users",
                        principalColumn: "UsersId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    StudentCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    StudentRollno = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StudentFatherName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    StudentDetails = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.StudentId);
                    table.ForeignKey(
                        name: "FK_Students_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "UsersId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeacherClasses",
                columns: table => new
                {
                    TcId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TcUsersId = table.Column<int>(type: "int", nullable: false),
                    TcClassCourseId = table.Column<int>(type: "int", nullable: false),
                    TcDescription = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherClasses", x => x.TcId);
                    table.ForeignKey(
                        name: "FK_TeacherClasses_ClasseCourse_TcClassCourseId",
                        column: x => x.TcClassCourseId,
                        principalTable: "ClassCourses",
                        principalColumn: "CcId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherClasses_Users_TcUsersId",
                        column: x => x.TcUsersId,
                        principalTable: "Users",
                        principalColumn: "UsersId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserLogs",
                columns: table => new
                {
                    UlogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UlogUsersId = table.Column<int>(type: "int", nullable: false),
                    UlogUsername = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UlogLoginDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UlogLogoutDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogs", x => x.UlogId);
                    table.ForeignKey(
                        name: "FK_UserLogs_Users_UlogUsersId",
                        column: x => x.UlogUsersId,
                        principalTable: "Users",
                        principalColumn: "UsersId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LessonFiles",
                columns: table => new
                {
                    LfId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LfLessonId = table.Column<int>(type: "int", nullable: false),
                    LfPath = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    LfType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonFiles", x => x.LfId);
                    table.ForeignKey(
                        name: "FK_LessonFiles_Lessons_LfLessonId",
                        column: x => x.LfLessonId,
                        principalTable: "Lessons",
                        principalColumn: "LessonId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuizQuestions",
                columns: table => new
                {
                    QqId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QqQuizId = table.Column<int>(type: "int", nullable: false),
                    QqQuestion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QqOption1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QqOption2 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QqOption3 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QqOption4 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QqCorrect = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    QqDescription = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizQuestions", x => x.QqId);
                    table.ForeignKey(
                        name: "FK_QuizQuestions_Quizzes_QqQuizId",
                        column: x => x.QqQuizId,
                        principalTable: "Quizzes",
                        principalColumn: "QuizId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceMarks",
                columns: table => new
                {
                    AttendanceMarksId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    AttendanceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AttendanceStatus = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceMarks", x => x.AttendanceMarksId);
                    table.ForeignKey(
                        name: "FK_AttendanceMarks_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "ClassId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceMarks_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Marks",
                columns: table => new
                {
                    MarksId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MarksExamId = table.Column<int>(type: "int", nullable: true),
                    MarksStudentId = table.Column<int>(type: "int", nullable: true),
                    MarksSubjectId = table.Column<int>(type: "int", nullable: true),
                    MarksWritten = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    MarksPractical = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    MarksSemesterId = table.Column<int>(type: "int", nullable: true),
                    MarksDescription = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Marks", x => x.MarksId);
                    table.ForeignKey(
                        name: "FK_Marks_Exams_MarksExamId",
                        column: x => x.MarksExamId,
                        principalTable: "Exams",
                        principalColumn: "ExamId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Marks_Semesters_MarksSemesterId",
                        column: x => x.MarksSemesterId,
                        principalTable: "Semesters",
                        principalColumn: "SemesterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Marks_Students_MarksStudentId",
                        column: x => x.MarksStudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Marks_Subjects_MarksSubjectId",
                        column: x => x.MarksSubjectId,
                        principalTable: "Subjects",
                        principalColumn: "SubjectId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuizResults",
                columns: table => new
                {
                    QrId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QrQuizId = table.Column<int>(type: "int", nullable: false),
                    QrStudentId = table.Column<int>(type: "int", nullable: false),
                    QrTotalQuestion = table.Column<int>(type: "int", nullable: false),
                    QrAnswer = table.Column<int>(type: "int", nullable: false),
                    QrDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizResults", x => x.QrId);
                    table.ForeignKey(
                        name: "FK_QuizResults_Quizzes_QrQuizId",
                        column: x => x.QrQuizId,
                        principalTable: "Quizzes",
                        principalColumn: "QuizId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuizResults_Students_QrStudentId",
                        column: x => x.QrStudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentClasses",
                columns: table => new
                {
                    ScId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScStudentId = table.Column<int>(type: "int", nullable: false),
                    ScClassId = table.Column<int>(type: "int", nullable: false),
                    ScDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScCreateAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ScStatus = table.Column<int>(type: "int", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentClasses", x => x.ScId);
                    table.ForeignKey(
                        name: "FK_StudentClasses_Classes_ScClassId",
                        column: x => x.ScClassId,
                        principalTable: "Classes",
                        principalColumn: "ClassId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentClasses_Students_ScStudentId",
                        column: x => x.ScStudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Assignments",
                columns: table => new
                {
                    AssignmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignmentTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AssignmentFilename = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AssignmentDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssignmentTeacherId = table.Column<int>(type: "int", nullable: true),
                    AssignmentClassId = table.Column<int>(type: "int", nullable: false),
                    AssignmentDeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssignmentCreateAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignmentStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignmentStatus = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assignments", x => x.AssignmentId);
                    table.ForeignKey(
                        name: "FK_Assignments_Classes_AssignmentClassId",
                        column: x => x.AssignmentClassId,
                        principalTable: "Classes",
                        principalColumn: "ClassId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assignments_Users_AssignmentTeacherId",
                        column: x => x.AssignmentTeacherId,
                        principalTable: "Users",
                        principalColumn: "UsersId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Submits",
                columns: table => new
                {
                    SubmitId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubmitAssignmentId = table.Column<int>(type: "int", nullable: false),
                    SubmitStudentId = table.Column<int>(type: "int", nullable: false),
                    SubmitFile = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SubmitDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmitStatus = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Submits", x => x.SubmitId);
                    table.ForeignKey(
                        name: "FK_Submits_Assignments_SubmitAssignmentId",
                        column: x => x.SubmitAssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "AssignmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Submits_Students_SubmitStudentId",
                        column: x => x.SubmitStudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StatusLearns",
                columns: table => new
                {
                    SlId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SlStudentId = table.Column<int>(type: "int", nullable: false),
                    SlLessonId = table.Column<int>(type: "int", nullable: false),
                    SlStatus = table.Column<bool>(type: "bit", nullable: false),
                    SlLearnedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusLearns", x => x.SlId);
                    table.ForeignKey(
                        name: "FK_StatusLearns_Students_SlStudentId",
                        column: x => x.SlStudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StatusLearns_Lessons_SlLessonId",
                        column: x => x.SlLessonId,
                        principalTable: "Lessons",
                        principalColumn: "LessonId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_AnnouncementClassId",
                table: "Announcements",
                column: "AnnouncementClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_AnnouncementTeacherId",
                table: "Announcements",
                column: "AnnouncementTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_AssignmentClassId",
                table: "Assignments",
                column: "AssignmentClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_AssignmentTeacherId",
                table: "Assignments",
                column: "AssignmentTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceMarks_ClassId",
                table: "AttendanceMarks",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceMarks_StudentId",
                table: "AttendanceMarks",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Cities_CityName",
                table: "Cities",
                column: "CityName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassCourses_ClassId",
                table: "ClassCourses",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassCourses_CourseId",
                table: "ClassCourses",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_ClassSemesterId",
                table: "Classes",
                column: "ClassSemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_ClassSyearId",
                table: "Classes",
                column: "ClassSyearId");

            migrationBuilder.CreateIndex(
                name: "IX_Countries_CountryName",
                table: "Countries",
                column: "CountryName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_DepartmentCode",
                table: "Departments",
                column: "DepartmentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_EventClassId",
                table: "Events",
                column: "EventClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_EventTeacherId",
                table: "Events",
                column: "EventTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_ExamEtypeId",
                table: "Exams",
                column: "ExamEtypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_ExamMonth",
                table: "Exams",
                column: "ExamMonth");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_FeedbackClassId",
                table: "Feedbacks",
                column: "FeedbackClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_FeedbackUsersId",
                table: "Feedbacks",
                column: "FeedbackUsersId");

            migrationBuilder.CreateIndex(
                name: "IX_Files_FilesClassId",
                table: "Files",
                column: "FilesClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Files_FilesTeacherId",
                table: "Files",
                column: "FilesTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonFiles_LfLessonId",
                table: "LessonFiles",
                column: "LfLessonId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_Lessons_LessonClassId",
            //    table: "Lessons",
            //    column: "LessonClassId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_Lessons_LessonCourseId",
            //    table: "Lessons",
            //    column: "LessonCourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_LessonClassCourseId",
                table: "Lessons",
                column: "LessonClassCourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Marks_MarksExamId",
                table: "Marks",
                column: "MarksExamId");

            migrationBuilder.CreateIndex(
                name: "IX_Marks_MarksSemesterId",
                table: "Marks",
                column: "MarksSemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_Marks_MarksStudentId",
                table: "Marks",
                column: "MarksStudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Marks_MarksSubjectId",
                table: "Marks",
                column: "MarksSubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_MessageReceiverId",
                table: "Messages",
                column: "MessageReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_MessageSenderId",
                table: "Messages",
                column: "MessageSenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Months_MonthTitle",
                table: "Months",
                column: "MonthTitle",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuizQuestions_QqQuizId",
                table: "QuizQuestions",
                column: "QqQuizId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizResults_QrQuizId",
                table: "QuizResults",
                column: "QrQuizId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizResults_QrStudentId",
                table: "QuizResults",
                column: "QrStudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_QuizClassId",
                table: "Quizzes",
                column: "QuizClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_QuizTeacherId",
                table: "Quizzes",
                column: "QuizTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_RoleName",
                table: "Roles",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SchoolYears_SyearTitle",
                table: "SchoolYears",
                column: "SyearTitle",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_States_StateName",
                table: "States",
                column: "StateName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentClasses_ScClassId",
                table: "StudentClasses",
                column: "ScClassId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentClasses_ScStudentId",
                table: "StudentClasses",
                column: "ScStudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_StudentCode",
                table: "Students",
                column: "StudentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_SubjectClassId",
                table: "Subjects",
                column: "SubjectClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_SubjectTitle",
                table: "Subjects",
                column: "SubjectTitle",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Submits_SubmitAssignmentId",
                table: "Submits",
                column: "SubmitAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Submits_SubmitStudentId",
                table: "Submits",
                column: "SubmitStudentId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherClasses_TcClassCourseId",
                table: "TeacherClasses",
                column: "TcClassCourseId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherClasses_TcUsersId",
                table: "TeacherClasses",
                column: "TcUsersId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogs_UlogUsersId",
                table: "UserLogs",
                column: "UlogUsersId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserLevelId",
                table: "Users",
                column: "UserLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UsersCity",
                table: "Users",
                column: "UsersCity");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UsersCountry",
                table: "Users",
                column: "UsersCountry");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UsersDepartmentId",
                table: "Users",
                column: "UsersDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UsersEmail",
                table: "Users",
                column: "UsersEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_UsersMobile",
                table: "Users",
                column: "UsersMobile",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_UsersRoleId",
                table: "Users",
                column: "UsersRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UsersState",
                table: "Users",
                column: "UsersState");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UsersUsername",
                table: "Users",
                column: "UsersUsername",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Announcements");

            migrationBuilder.DropTable(
                name: "AttendanceMarks");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "Feedbacks");

            migrationBuilder.DropTable(
                name: "Files");

            migrationBuilder.DropTable(
                name: "LessonFiles");

            migrationBuilder.DropTable(
                name: "Marks");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "QuizQuestions");

            migrationBuilder.DropTable(
                name: "QuizResults");

            migrationBuilder.DropTable(
                name: "StudentClasses");

            migrationBuilder.DropTable(
                name: "Submits");

            migrationBuilder.DropTable(
                name: "UserLogs");

            migrationBuilder.DropTable(
                name: "StatusLearns");

            migrationBuilder.DropTable(
                name: "Lessons");

            migrationBuilder.DropTable(
                name: "Exams");

            migrationBuilder.DropTable(
                name: "Subjects");

            migrationBuilder.DropTable(
                name: "Quizzes");

            migrationBuilder.DropTable(
                name: "Assignments");

            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.DropTable(
                name: "TeacherClasses");

            migrationBuilder.DropTable(
                name: "ClassCourses");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "ExamTypes");

            migrationBuilder.DropTable(
                name: "Months");

            migrationBuilder.DropTable(
                name: "Classes");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "SchoolYears");

            migrationBuilder.DropTable(
                name: "Semesters");

            migrationBuilder.DropTable(
                name: "Cities");

            migrationBuilder.DropTable(
                name: "Countries");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "LoginLevels");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "States");

        }
    }
}
