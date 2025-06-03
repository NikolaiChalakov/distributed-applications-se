using System.ComponentModel.DataAnnotations;

namespace SkillForge.DTO.Users
{
    public class CreateUserDto
    {
        [Required]
        public string Username { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string Role { get; set; } = string.Empty;
        

    }
}
