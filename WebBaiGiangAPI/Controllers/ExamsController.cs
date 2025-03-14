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
    public class ExamsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ExamsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("get-exams")]
        public async Task<ActionResult<IEnumerable<Exam>>> GetExams()
        {
            var exams = from e in _context.Exams
                        join et in _context.ExamTypes on e.ExamEtypeId equals et.EtypeId
                        join m in _context.Months on e.ExamMonth equals m.MonthId
                        orderby e.ExamId descending
                        select new
                        {
                            ExamId = e.ExamId,
                            ExamTitle = e.ExamTitle,
                            ExamEtypeId = e.ExamEtypeId,
                            ExamMonth = e.ExamMonth,
                            ExamDescription = e.ExamDescription,
                            ExamType = et.EtypeTitle,
                            Month = m.MonthTitle,
                        };
            if (!exams.Any() || exams == null)
            {
                return NotFound("Hiện tại không có đề thi nào");
            }
            return Ok(new
            {
                Message = "Danh sách đề thi:",
                exams = exams
            });
        }

        [HttpGet("get-exam-by-id")]
        public async Task<ActionResult<Exam>> GetExamById(int id)
        {
            var exam = from e in _context.Exams
                       join et in _context.ExamTypes on e.ExamEtypeId equals et.EtypeId
                       join m in _context.Months on e.ExamMonth equals m.MonthId
                       where e.ExamId == id
                       orderby e.ExamId descending
                       select new
                       {
                           ExamId = e.ExamId,
                           ExamTitle = e.ExamTitle,
                           ExamEtypeId = e.ExamEtypeId,
                           ExamMonth = e.ExamMonth,
                           ExamDescription = e.ExamDescription,
                           ExamType = et.EtypeTitle,
                           Month = m.MonthTitle,
                       };
            
            if (!exam.Any() || exam == null)
            {
                return NotFound("Không tìm thấy đề thi nào");
            }
            return Ok(new
            {
                Message = "Thông tin đề thi:",
                exam = exam
            });
        }

        [HttpGet("get-exam-by-type-id")]
        public async Task<ActionResult<Exam>> GetExamByTypeId(int id)
        {
            var exam = from e in _context.Exams
                       join et in _context.ExamTypes on e.ExamEtypeId equals et.EtypeId
                       join m in _context.Months on e.ExamMonth equals m.MonthId
                       where e.ExamEtypeId == id
                       orderby e.ExamId descending
                       select new
                       {
                           ExamId = e.ExamId,
                           ExamTitle = e.ExamTitle,
                           ExamEtypeId = e.ExamEtypeId,
                           ExamMonth = e.ExamMonth,
                           ExamDescription = e.ExamDescription,
                           ExamType = et.EtypeTitle,
                           Month = m.MonthTitle,
                       };
            if (!exam.Any() || exam == null)
            {
                return NotFound("Không tìm thấy đề thi nào");
            }
            return Ok(new
            {
                Message = $"Thông tin đề thi theo loại {exam.First().ExamType}:",
                exam = exam
            });
        }

        [HttpGet("get-exam-by-month-id")]
        public async Task<ActionResult<Exam>> GetExamByMonthId(int id)
        {
            var exam = from e in _context.Exams
                       join et in _context.ExamTypes on e.ExamEtypeId equals et.EtypeId
                       join m in _context.Months on e.ExamMonth equals m.MonthId
                       where e.ExamMonth == id
                       orderby e.ExamId descending
                       select new
                       {
                           ExamId = e.ExamId,
                           ExamTitle = e.ExamTitle,
                           ExamEtypeId = e.ExamEtypeId,
                           ExamMonth = e.ExamMonth,
                           ExamDescription = e.ExamDescription,
                           ExamType = et.EtypeTitle,
                           Month = m.MonthTitle,
                       };
            if (!exam.Any() || exam == null)
            {
                return NotFound("Không tìm thấy đề thi nào");
            }
            return Ok(new
            {
                Message = $"Thông tin đề thi theo loại {exam.First().ExamMonth}:",
                exam = exam
            });
        }

        [HttpPut("update-exam")]
        public async Task<IActionResult> UpdateExam(Exam exam)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var existingExam = await _context.Exams.FindAsync(exam.ExamId);
            if (existingExam == null)
            {
                return NotFound("Không tìm thấy đề thi");
            }
            if (!_context.ExamTypes.Any(et => et.EtypeId == exam.ExamEtypeId))
            {
                return BadRequest("EtypeId is not valid");
            }
            if (!_context.Months.Any(m => m.MonthId == exam.ExamMonth))
            {
                return BadRequest("MonthId is not valid");
            }
            existingExam.ExamTitle = Regex.Replace(exam.ExamTitle.Trim(), @"\s+", " ");
            existingExam.ExamDescription = Regex.Replace(exam.ExamDescription.Trim(), @"\s+", " ");
            existingExam.ExamEtypeId = exam.ExamEtypeId;
            existingExam.ExamMonth = exam.ExamMonth;
            _context.Exams.Update(existingExam);
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
                message = "Đã cập nhật thông tin đề thi",
                data = exam
            });
        }

        [HttpPost("add-exam")]
        public async Task<ActionResult<Exam>> PostExam(Exam exam)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (!_context.ExamTypes.Any(et => et.EtypeId == exam.ExamEtypeId))
            {
                return BadRequest("EtypeId is not valid");
            }
            if (!_context.Months.Any(m => m.MonthId == exam.ExamMonth))
            {
                return BadRequest("MonthId is not valid");
            }
            exam.ExamTitle = Regex.Replace(exam.ExamTitle.Trim(), @"\s+", " ");
            exam.ExamDescription = Regex.Replace(exam.ExamDescription.Trim(), @"\s+", " "); 
            _context.Exams.Add(exam);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Đã thêm đề thi mới",
                data = exam
            });
        }

        [HttpDelete("delete-exam")]
        public async Task<IActionResult> DeleteExam(int id)
        {
            try
            {
                var exam = await _context.Exams.FindAsync(id);
                if (exam == null)
                {
                    return NotFound("Không tìm thấy đề thi nào");
                }

                _context.Exams.Remove(exam);
                await _context.SaveChangesAsync();

                return Ok("Xóa đề thi thành công");
            }
            catch (Exception)
            {
                return BadRequest("Không thể xóa đề thi");
            }
        }
    }
}
