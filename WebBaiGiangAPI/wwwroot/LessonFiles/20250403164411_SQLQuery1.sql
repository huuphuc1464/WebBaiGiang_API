SELECT DISTINCT 
    de.DepartmentId, de.DepartmentTitle, 
    te.UsersId, te.UsersName, 
    co.CourseId, co.CourseTitle, 
	cc.ClassId,
    cl.ClassId, cl.ClassTitle
FROM Departments de
LEFT JOIN Users te ON de.DepartmentId = te.UsersDepartmentId AND te.UsersRoleId = 2
LEFT JOIN Courses co ON de.DepartmentId = co.CourseDepartmentId
LEFT JOIN ClassCourses cc ON co.CourseId = cc.CourseId
left join Classes cl on cc.ClassId = cl.ClassId
ORDER BY de.DepartmentId, co.CourseId, cc.ClassId, cl.ClassId;

select * from Departments de
left join Users te on de.DepartmentId = te.UsersDepartmentId
left join Courses co on de.DepartmentId = co.CourseDepartmentId
left join ClassCourses cc on co.CourseId = cc.CourseId
left join Classes cl on cc.ClassId = cl.ClassId
where te.UsersRoleId = 2


select * from StudentClasses, Users, Classes
where ScStudentId = UsersId and ScClassId = ClassId
		and ScStatus = 1 and ScClassId = 6

select * from Messages

exec sp_help Announcements

SELECT name FROM sys.tables order by name;

select CourseImage, CourseTitle, CourseShortdescription, CourseDescription, CourseUpdateAt, DepartmentTitle, UsersName
from Courses co
left join Departments de on co.CourseDepartmentId = de.DepartmentId
left join ClassCourses cc on co.CourseId = cc.CourseId
left join Classes cl on cc.ClassId = cl.ClassId
left join TeacherClasses tc on cl.ClassId = tc.TcClassId
left join Users u on tc.TcUsersId = u.UsersId

SELECT 
    co.CourseImage, 
    co.CourseTitle, 
    co.CourseShortdescription, 
    co.CourseDescription, 
    co.CourseUpdateAt, 
    de.DepartmentTitle, 
    STRING_AGG(u.UsersName, ', ') AS TeacherNames,
    AVG(f.FeedbackRate) AS AvgRating  -- Tính trung bình số sao
FROM Courses co
LEFT JOIN Departments de ON co.CourseDepartmentId = de.DepartmentId
LEFT JOIN ClassCourses cc ON co.CourseId = cc.CourseId
LEFT JOIN Classes cl ON cc.ClassId = cl.ClassId
LEFT JOIN TeacherClasses tc ON cl.ClassId = tc.TcClassId
LEFT JOIN Users u ON tc.TcUsersId = u.UsersId
LEFT JOIN Feedbacks f ON cl.ClassId = f.FeedbackClassId  -- Join với bảng Feedbacks

GROUP BY co.CourseImage, co.CourseTitle, co.CourseShortdescription, 
         co.CourseDescription, co.CourseUpdateAt, de.DepartmentTitle

HAVING AVG(f.FeedbackRate) >= 3;  -- Lọc khóa học có trung bình đánh giá là 3


SELECT 
    co.CourseImage, 
    co.CourseTitle, 
    co.CourseShortdescription, 
    co.CourseDescription, 
    co.CourseUpdateAt, 
    de.DepartmentTitle, 
    STRING_AGG(u.UsersName, ', ') AS TeacherNames,
    COALESCE(AVG(f.FeedbackRate), 0) AS AvgRating  -- Trung bình đánh giá (nếu không có thì = 0)
FROM Courses co
LEFT JOIN Departments de ON co.CourseDepartmentId = de.DepartmentId
LEFT JOIN ClassCourses cc ON co.CourseId = cc.CourseId
LEFT JOIN Classes cl ON cc.ClassId = cl.ClassId
LEFT JOIN TeacherClasses tc ON cl.ClassId = tc.TcClassId
LEFT JOIN Users u ON tc.TcUsersId = u.UsersId
LEFT JOIN Feedbacks f ON cl.ClassId = f.FeedbackClassId  -- Liên kết với Feedbacks qua lớp học

WHERE 
    (@Keyword IS NULL OR 
     co.CourseTitle LIKE '%' + @Keyword + '%' OR 
     cl.ClassTitle LIKE '%' + @Keyword + '%')  -- Tìm kiếm theo khóa học hoặc lớp học

GROUP BY co.CourseImage, co.CourseTitle, co.CourseShortdescription, 
         co.CourseDescription, co.CourseUpdateAt, de.DepartmentTitle

HAVING 
    (@MinRating IS NULL OR AVG(f.FeedbackRate) >= @MinRating);  -- Lọc theo số sao đánh giá

select * from ClassCourses
select * from Announcements

SELECT TOP 1 u.UsersEmail, u.UsersName
FROM TeacherClasses tc
INNER JOIN Users u ON tc.TcUsersId = u.UsersId
WHERE tc.TcClassId = 6;

select * from Feedbacks
select * from StudentClasses
select * from TeacherClasses
select * from Users
select * from Lessons	

-- quay lại trạng thái ban đầu (chưa có table)
-- dotnet ef database update 0
-- dotnet ef database update


SELECT 
    c.ClassId,
    c.ClassTitle,
    tc.TcUsersId,
    s.SemesterTitle AS Semester
FROM TeacherClasses tc
JOIN Classes c ON tc.TcClassId = c.ClassId
JOIN Semesters s ON c.ClassSemesterId = s.SemesterId
WHERE tc.TcUsersId = 2
AND c.ClassSemesterId = 2


	select COUNT(attendanceDate) from AttendanceMarks where ClassId = 3 group by AttendanceDate

SELECT 
    am.StudentId,
    COALESCE(u.UsersName, N'Không xác định') AS UsersName,
    u.UsersEmail,
    COUNT(*) AS AbsenceCount
FROM AttendanceMarks am
JOIN Students s ON am.StudentId = s.StudentId
LEFT JOIN Users u ON s.StudentId = u.UsersId -- Kiểm tra khóa ngoại phù hợp
WHERE am.ClassId = 4 AND am.AttendanceStatus = 'No'
GROUP BY am.StudentId, u.UsersName, u.UsersEmail
HAVING COUNT(*) >= 2 AND u.UsersEmail IS NOT NULL;

select * from Events


select * from LessonFiles
select * from Lessons 
select * from Announcements

delete from Lessons where LessonId = 14

SELECT c.CourseId, c.CourseTitle, COUNT(l.LessonId) AS LessonCount
FROM Courses c
LEFT JOIN Lessons l ON c.CourseId = l.LessonCourseId
GROUP BY c.CourseId, c.CourseTitle;


SELECT cl.ClassId, cl.ClassTitle, COUNT(l.LessonId) AS LessonCount
FROM Classes cl
LEFT JOIN Lessons l ON cl.ClassId = l.LessonClassId
GROUP BY cl.ClassId, cl.ClassTitle;

SELECT t.UsersId, t.UsersName, COUNT(l.LessonId) AS LessonCount
FROM Users t
LEFT JOIN Lessons l ON t.UsersId = l.LessonTeacherId
where UsersRoleId =2
GROUP BY t.UsersId, t.UsersName;

SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'StatusLearns';
SELECT * FROM statuslearns;

select sl.*, u.UsersName, u.UsersEmail, s.StudentCode, l.LessonName from StatusLearns sl
join Users u on sl.SlStudentId = u.UsersId
join Students s on sl.SlStudentId = s.StudentId
join Lessons l on sl.SlLessonId = l.LessonId

select l.LessonId, l.LessonName, s.StudentCode, u.UsersName, COUNT(l.LessonId)
from StatusLearns sl
join Lessons l on sl.SlLessonId = l.LessonId
join ClassCourses cc on l.LessonClassId = cc.ClassId and l.LessonCourseId = cc.CourseId
join Students s on sl.SlStudentId = s.StudentId
join Users u on sl.SlStudentId = u.UsersId
where l.LessonClassId = 4
group by l.LessonId, l.LessonName, s.StudentCode, u.UsersName


SELECT 
    s.StudentId, 
    s.StudentCode, 
    u.UsersName, 
    cc.ClassId, 
    COUNT(DISTINCT CASE WHEN sl.SlStatus = 1 THEN sl.SlLessonId END) AS CompletedLessons,
    COUNT(DISTINCT l.LessonId) AS TotalLessons
FROM ClassCourses cc
JOIN Lessons l ON l.LessonClassId = cc.ClassId AND l.LessonCourseId = cc.CourseId
LEFT JOIN StatusLearns sl ON sl.SlLessonId = l.LessonId
LEFT JOIN Students s ON sl.SlStudentId = s.StudentId
LEFT JOIN Users u ON s.StudentId = u.UsersId
WHERE cc.ClassId = 4
GROUP BY s.StudentId, s.StudentCode, u.UsersName, cc.ClassId
ORDER BY CompletedLessons DESC;

select * from ClassCourses cc 
join Lessons l on l.LessonClassId = cc.ClassId and l.LessonCourseId = cc.CourseId
where cc.ClassId = 4 and cc.CourseId =  1 and l.LessonCreateAt <= '2025-03-26'


WITH LessonFiltered AS (
    SELECT l.LessonId, l.LessonCreateAt
    FROM Lessons l
    WHERE EXISTS (
        SELECT 1 FROM ClassCourses cc
        WHERE cc.ClassId = 4 
            AND cc.CourseId = 1
            AND l.LessonClassId = cc.ClassId 
            AND l.LessonCourseId = cc.CourseId
    )
    AND (l.LessonCreateAt <= '2025-03-24')
),

TotalLessons AS (
    SELECT COUNT(*) AS Total FROM LessonFiltered
),

StudentProgress AS (
    SELECT 
        s.StudentId, 
        s.StudentCode, 
        u.UsersName, 
        sl.SlLearnedDate,
        COUNT(lf.LessonId) AS CompletedLessons,
        (SELECT Total FROM TotalLessons) AS TotalLessons,
        CASE 
            WHEN (SELECT Total FROM TotalLessons) > 0 
            THEN ROUND(COUNT(lf.LessonId) * 100.0 / (SELECT Total FROM TotalLessons), 2) 
            ELSE 0.0 
        END AS CompletionRate
    FROM Students s
    JOIN StatusLearns sl ON s.StudentId = sl.SlStudentId
    JOIN LessonFiltered lf ON sl.SlLessonId = lf.LessonId
    JOIN Users u ON u.UsersId = s.StudentId
    WHERE sl.SlStatus = 1
        AND (sl.SlLearnedDate <= '2025-03-24')
    GROUP BY s.StudentId, s.StudentCode, u.UsersName, sl.SlLearnedDate
)

SELECT top 10 *
FROM StudentProgress
ORDER BY CompletedLessons DESC, SlLearnedDate ASC

select * from Lessons l
left join StatusLearns sl on l.LessonId = sl.SlLessonId
where LessonCourseId = 1 and  LessonClassId = 4 and LessonCreateAt >= '2025-03-25' and SlLearnedDate >= '2025-03-25'

select * from Lessons l
left join StatusLearns sl on l.LessonId = sl.SlLessonId
where LessonCourseId = 1 and LessonClassId = 4 and LessonCreateAt <= '2025-03-26 23:59:59.99999' and SlLearnedDate <= '2025-03-26 23:59:59.99999'

select * from TeacherClasses tc
join ClassCourses cc on tc.TcClassCourseId = cc.CcId
where cc.CourseId = 1 and tc.TcUsersId = 2

SELECT LessonClassCourseId, LessonId, COUNT(*) 
FROM Lessons 
GROUP BY LessonClassCourseId, LessonId
HAVING COUNT(*) > 1;

select * from ClassCourses cc 
join Classes cl on cc.ClassId = cl.ClassId
join Courses co on cc.CourseId = co.CourseId
join TeacherClasses tc on tc.TcClassCourseId = cc.CcId 
join Lessons l on cc.CcId = l.LessonClassCourseId
left join StatusLearns sl on l.LessonId = sl.SlLessonId

select * from Courses c
join ClassCourses cc on c.CourseId =  cc.CourseId 
where cc.CcId = 6

select * from Quizzes
select * from QuizQuestions
select * from QuizResults

DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql += 'SELECT * FROM ' + QUOTENAME(TABLE_NAME) + '; ' 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE';
EXEC sp_executesql @sql;

exec sp_help QuizQuestions

select * from TeacherClasses
join ClassCourses on TcClassCourseId = CcId

select * from ClassCourses 
join Quizzes on CcId = QuizClassCourseId
where ClassId = 2 and CcId = 1 