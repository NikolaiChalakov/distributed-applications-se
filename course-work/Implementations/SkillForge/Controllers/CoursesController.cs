using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillForge.Data;
using SkillForge.DTO.Courses;
using SkillForge.Models;

namespace SkillForge.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]

    public class CoursesController : ControllerBase
    {
        public readonly AppDbContext _context;


        public CoursesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var courses = await _context.Courses
                .Include(c => c.Instructor)
                .Select(c => new CourseDto
                {
                    CourseId = c.CourseId,
                    Title = c.Title,
                    Description = c.Description,
                    InstructorUsername = c.Instructor.Username
                })
                .ToListAsync();

            return Ok(courses);
        }



        [HttpGet("title/{title}")]
        public async Task<IActionResult> GetByTitle([FromRoute] string title)
        {
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Title == title);

            if (course != null)
            {
                return Ok(course);
            }
            else
            {
                return BadRequest($"No such course was found with title={title}");
            }
        }
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Instructor)
                .Where(c => c.CourseId == id)
                .Select(c => new CourseDto
                {
                    CourseId = c.CourseId,
                    Title = c.Title,
                    Description = c.Description,
                    InstructorUsername = c.Instructor.Username
                })
                .FirstOrDefaultAsync();

            if (course == null)
                return NotFound("Course not found");

            return Ok(course);
        }
        [HttpGet("paged")]
        public async Task<IActionResult> GetPagedCourses(
     [FromQuery] int page = 1,
     [FromQuery] int pageSize = 10,
     [FromQuery] string? title = null,
     [FromQuery] string? instructor = null,
     [FromQuery] string sortBy = "title",
     [FromQuery] bool descending = false)
        {
            var query = _context.Courses.Include(c => c.Instructor).AsQueryable();

            if (!string.IsNullOrWhiteSpace(title))
                query = query.Where(c => c.Title.Contains(title));

            if (!string.IsNullOrWhiteSpace(instructor))
                query = query.Where(c => c.Instructor.Username.Contains(instructor));

            // ✅ Sorting logic
            query = sortBy.ToLower() switch
            {
                "title" => descending ? query.OrderByDescending(c => c.Title) : query.OrderBy(c => c.Title),
                "instructor" => descending ? query.OrderByDescending(c => c.Instructor.Username) : query.OrderBy(c => c.Instructor.Username),
                _ => query.OrderBy(c => c.Title)
            };

            var totalCount = await query.CountAsync();

            var pagedCourses = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Data = pagedCourses.Select(c => new
                {
                    c.CourseId,
                    c.Title,
                    c.Description,
                    InstructorUsername = c.Instructor.Username
                })
            });
        }




       

        [HttpPost]
        public async Task<IActionResult> Create(CreateCourseDto courseDto)
        {
            var exists = _context.Courses.FirstOrDefault(c => c.Title == courseDto.Title);
            Course course=new Course();
            if (exists != null)
            {
                return BadRequest("The course already exists!");
            }
            else
            {
                course.Title = courseDto.Title;
                course.Description = courseDto.Description;
                course.InstructorId=courseDto.InstructorId;
                course.CreatedAt = DateTime.UtcNow;
                await _context.Courses.AddAsync(course);
                await _context.SaveChangesAsync();
                return Ok(new CourseDto
                {
                    CourseId = course.CourseId,
                    Title = course.Title,
                    Description = course.Description,
                    InstructorUsername = (await _context.Users.FindAsync(course.InstructorId))?.Username
                });

            }


        }

        [HttpPut]
        public async Task<IActionResult> Update(CourseDto dto)
        {
            var updateCourse = await _context.Courses.FirstOrDefaultAsync(c => c.CourseId == dto.CourseId);
            if (updateCourse == null)
                return BadRequest("Can't update a course that doesn't exist!");

            updateCourse.Title = dto.Title;
            updateCourse.Description = dto.Description;

            _context.Courses.Update(updateCourse);
            await _context.SaveChangesAsync();

            return Ok();
        }


        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string? title, [FromQuery] string? instructorUsername)
        {
            var query = _context.Courses
                .Include(c => c.Instructor) 
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(title))
            {
                query = query.Where(c => c.Title.Contains(title));
            }

            if (!string.IsNullOrWhiteSpace(instructorUsername))
            {
                query = query.Where(c => c.Instructor != null && c.Instructor.Username.Contains(instructorUsername));
            }

            var result = await query.ToListAsync();

            if (!result.Any())
            {
                return NotFound("No courses match the provided criteria.");
            }

            return Ok(result);
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseId == id);
            if (course == null)
                return NotFound("The course doesn't exist!");

            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
                return Unauthorized();

            bool isAdmin = user.Role == "Admin";
            bool isOwner = course.InstructorId == user.UserId;

            if (!isAdmin && !isOwner)
                return Forbid("You can only delete your own courses.");

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();
            return Ok("Course was deleted.");
        }

    }
}
