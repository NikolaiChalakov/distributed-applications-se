using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillForge.Data;
using SkillForge.DTO.Courses;
using SkillForge.DTO.Enrollements;
using SkillForge.Models;

namespace SkillForge.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]

    public class EnrollmentsController : ControllerBase
    {
        public readonly AppDbContext _context;
        

        public EnrollmentsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var enrollments = await _context.Users.ToListAsync();
            return Ok(enrollments);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            var enrollment = await _context.Enrollments.FirstOrDefaultAsync(u => u.UserId == id);

            if (enrollment != null)
            {
                return Ok(enrollment);
            }
            else
            {
                return BadRequest($"No such enrollment was found with id={id}");
            }
        }

        

        [HttpPut]
        public async Task<IActionResult> Update(Enrollment enrollment)
        {
            var updateEnrollment = _context.Enrollments.FirstOrDefault(e => e.EnrollmentId == enrollment.EnrollmentId);
            if (updateEnrollment != null)
            {
                updateEnrollment.User= enrollment.User;
                updateEnrollment.Course = enrollment.Course;
                updateEnrollment.EnrolledAt = enrollment.EnrolledAt;
                _context.Enrollments.Update(updateEnrollment);
                _context.SaveChangesAsync();
                return Ok(updateEnrollment);
            }
            else
            {
                return BadRequest("Can't update an enrollment that doesn't exist!");
            }


        }
        [HttpGet("paged")]
        public async Task<IActionResult> GetPagedEnrollments(
     [FromQuery] int page = 1,
     [FromQuery] int pageSize = 10,
     [FromQuery] string? username = null,
     [FromQuery] string? courseTitle = null,
     [FromQuery] string sortBy = "username",
     [FromQuery] bool descending = false)
        {
            var query = _context.Enrollments
                .Include(e => e.User)
                .Include(e => e.Course)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(username))
                query = query.Where(e => e.User.Username.Contains(username));

            if (!string.IsNullOrWhiteSpace(courseTitle))
                query = query.Where(e => e.Course.Title.Contains(courseTitle));

            query = sortBy.ToLower() switch
            {
                "username" => descending ? query.OrderByDescending(e => e.User.Username) : query.OrderBy(e => e.User.Username),
                "coursetitle" => descending ? query.OrderByDescending(e => e.Course.Title) : query.OrderBy(e => e.Course.Title),
                "enrolledat" => descending ? query.OrderByDescending(e => e.EnrolledAt) : query.OrderBy(e => e.EnrolledAt),
                _ => query.OrderBy(e => e.User.Username)
            };

            var totalCount = await query.CountAsync();

            var pagedData = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new EnrollmentAdminViewModel
                {
                    Username = e.User.Username,
                    CourseTitle = e.Course.Title,
                    EnrolledAt = e.EnrolledAt
                })
                .ToListAsync();

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Data = pagedData
            });
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string? username, [FromQuery] string? courseTitle)
        {
            var query = _context.Enrollments
                .Include(e => e.User)
                .Include(e => e.Course)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(username))
            {
                query = query.Where(e => e.User != null && e.User.Username.Contains(username));
            }

            if (!string.IsNullOrWhiteSpace(courseTitle))
            {
                query = query.Where(e => e.Course != null && e.Course.Title.Contains(courseTitle));
            }

            var result = await query.ToListAsync();

            if (!result.Any())
            {
                return NotFound("No enrollments match the provided criteria.");
            }

            return Ok(result);
        }

        [HttpPost("join")]
        public async Task<IActionResult> Enroll([FromBody] EnrollmentDto dto)
        {
            var username = User.Identity?.Name;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return Unauthorized();

            var alreadyEnrolled = await _context.Enrollments
                .AnyAsync(e => e.CourseId == dto.CourseId && e.UserId == user.UserId);

            if (alreadyEnrolled)
                return BadRequest("Already enrolled.");

            var courseExists = await _context.Courses.AnyAsync(c => c.CourseId == dto.CourseId);
            if (!courseExists)
                return BadRequest("Course does not exist.");

            var enrollment = new Enrollment
            {
                CourseId = dto.CourseId,
                UserId = user.UserId,
                EnrolledAt = DateTime.UtcNow
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            return Ok("Enrollment successful.");
        }

        
        
        [HttpGet("all")]
        public async Task<IActionResult> GetAllEnrollments()
        {
            var result = await _context.Enrollments
                .Include(e => e.User)
                .Include(e => e.Course)
                .Select(e => new
                {
                    e.EnrollmentId,
                    e.EnrolledAt,
                    UserId = e.User.UserId,
                    Username = e.User.Username,
                    CourseId = e.Course.CourseId,
                    CourseTitle = e.Course.Title
                })
                .ToListAsync();

            return Ok(result);
        }

        [HttpDelete("leave")]
        public async Task<IActionResult> LeaveCourse([FromQuery] int courseId)
        {
            var username = User.Identity?.Name;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return Unauthorized();

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.UserId == user.UserId && e.CourseId == courseId);

            if (enrollment == null)
                return NotFound("You are not enrolled in this course.");

            _context.Enrollments.Remove(enrollment);
            await _context.SaveChangesAsync();

            return Ok("You have successfully unenrolled.");
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("admin-delete")]
        public async Task<IActionResult> Delete([FromQuery] int id)
        {
            var enrollment = _context.Enrollments.FirstOrDefault(e => e.UserId == id);
            if (enrollment != null)
            {
                _context.Enrollments.Remove(enrollment);
                _context.SaveChanges();
                return Ok("Enrollment was deleted");
            }
            else
            {
                return BadRequest("The user has checked out!");
            }


        }
    }
}
