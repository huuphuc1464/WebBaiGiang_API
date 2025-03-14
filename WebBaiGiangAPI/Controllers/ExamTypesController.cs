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
    public class ExamTypesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ExamTypesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("get-exam-types")]
        public async Task<ActionResult<IEnumerable<ExamType>>> GetExamTypes()
        {
            var examTypes = await _context.ExamTypes.ToListAsync();
            if (!examTypes.Any() || examTypes == null)
            {
                return NotFound("Hiện tại không có loại đề thi nào");
            }
            return Ok(examTypes);
        }

        [HttpGet("get-exam-type")]
        public async Task<ActionResult<ExamType>> GetExamType(int id)
        {
            var examType = await _context.ExamTypes.FindAsync(id);

            if (examType == null)
            {
                return NotFound("Không tìm thấy loại đề thi nào");
            }

            return examType;
        }

        [HttpPut("update-exam-type")]
        public async Task<IActionResult> UpdateExamType(ExamType examType)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingExamType = await _context.ExamTypes.FindAsync(examType.EtypeId);

            if (existingExamType == null)
            {
                return NotFound("Không tìm thấy loại đề thi");
            }

            existingExamType.EtypeTitle = Regex.Replace(examType.EtypeTitle.Trim(), @"\s+", " ");
            existingExamType.EtypeDescription = Regex.Replace(examType.EtypeDescription.Trim(), @"\s+", " ");   
            _context.ExamTypes.Update(existingExamType);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }

            return NoContent();
        }

        [HttpPost("add-exam-type")]
        public async Task<ActionResult<ExamType>> AddExamType(ExamType examType)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            examType.EtypeTitle = Regex.Replace(examType.EtypeTitle.Trim(), @"\s+", " ");
            examType.EtypeDescription = Regex.Replace(examType.EtypeDescription.Trim(), @"\s+", " ");

            _context.ExamTypes.Add(examType);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Loại đề thi đã được thêm",
                data = examType
            });
        }

        [HttpDelete("delete-exam-type")]
        public async Task<IActionResult> DeleteExamType(int id)
        {
            var examType = await _context.ExamTypes.FindAsync(id);
            if (examType == null)
            {
                return NotFound("Không tìm thấy loại đề thi");
            }

            _context.ExamTypes.Remove(examType);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Loại đề thi đã được xóa",
            });
        }
    }
}
