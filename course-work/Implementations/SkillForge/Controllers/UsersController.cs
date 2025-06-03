using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SkillForge.Data;
using SkillForge.DTO.Courses;
using SkillForge.DTO.Users;
using SkillForge.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace SkillForge.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]

    public class UsersController : ControllerBase
    {
        public readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public UsersController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _context.Users.ToListAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            var users = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);

            if (users != null)
            {
                return Ok(users);
            }
            else
            {
                return BadRequest($"No such user was found with id={id}");
            }
        }
        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(
     [FromQuery] int page = 1,
     [FromQuery] int pageSize = 10,
     [FromQuery] string? username = null,
     [FromQuery] string? email = null,
     [FromQuery] string sortBy = "username",
     [FromQuery] bool descending = false)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(username))
                query = query.Where(u => u.Username.Contains(username));

            if (!string.IsNullOrWhiteSpace(email))
                query = query.Where(u => u.Email.Contains(email));

            query = sortBy.ToLower() switch
            {
                "username" => descending ? query.OrderByDescending(u => u.Username) : query.OrderBy(u => u.Username),
                "email" => descending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
                _ => query.OrderBy(u => u.Username)
            };

            var totalUsers = await query.CountAsync();

            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                TotalCount = totalUsers,
                Page = page,
                PageSize = pageSize,
                Data = users
            });
        }



        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Create(CreateUserDto userDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var exists = _context.Users.FirstOrDefault(u => u.Username == userDto.Username);
            if (exists != null)
            {
                return BadRequest("The user already exists!");
            }

            var user = new User
            {
                Username = userDto.Username,
                Password = userDto.Password,
                Email = userDto.Email,
                CreatedAt = DateTime.UtcNow,
                Role = userDto.Role ?? "User"
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var claims = new[]
            {
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Role, user.Role),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new
            {
                token = tokenString,
                role = user.Role
            });
        }


        [HttpPut]
        public async Task<IActionResult> Update(User user)
        {
            var updateUser = _context.Users.FirstOrDefault(u => u.UserId == user.UserId);
            if (updateUser != null)
            {
                updateUser.Username = user.Username;
                updateUser.CreatedAt = DateTime.UtcNow;
                updateUser.Password = user.Password;
                _context.Users.Update(updateUser);
                _context.SaveChangesAsync();
                return Ok(updateUser);

            }

            else
            {
                return BadRequest("Can't update a user that doesn't exist!");
            }


        }
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string? username, [FromQuery] string? email)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(username))
            {
                query = query.Where(u => u.Username.Contains(username));
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                query = query.Where(u => u.Email.Contains(email));
            }

            var result = await query.ToListAsync();

            if (!result.Any())
            {
                return NotFound("No users match the provided criteria.");
            }

            return Ok(result);
        }
        [Authorize(Roles = "Admin")]
        [HttpGet("sort")]
        public async Task<IActionResult> Sort([FromQuery] string sortBy = "username", [FromQuery] bool descending = false)
        {
            var query = _context.Users.AsQueryable();

            query = sortBy.ToLower() switch
            {
                "username" => descending ? query.OrderByDescending(u => u.Username) : query.OrderBy(u => u.Username),
                "email" => descending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
                _ => query.OrderBy(u => u.Username)
            };

            var result = await query.Select(u => new UserViewDto
            {
                UserId = u.UserId,
                Username = u.Username,
                Email = u.Email,
                Role = u.Role
            }).ToListAsync();

            return Ok(result);
        }



        [HttpDelete]
        public async Task<IActionResult> Delete([FromQuery] string username)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user != null)
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
                return Ok("User was deleted");
            }
            else
            {
                return BadRequest("The user doesn't exists!");
            }


        }
    }
}
