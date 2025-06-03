using System.ComponentModel.DataAnnotations;

namespace SkillForge.Models
{
    public class User
    {
        public int UserId { get; set; }
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;
        [Required]
        [MaxLength(50)]
        public string Password { get; set; } = string.Empty;
        [Required]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;
        [Required]
        [MaxLength(10)]
        public string Role { get; set; } = string.Empty;
        [Required]
        public DateTime CreatedAt { get; set; }

        public ICollection<Course>? CreatedCourses { get; set; } 
        public ICollection<Enrollment>? Enrollments { get; set; }

    }
}
