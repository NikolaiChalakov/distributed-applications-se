using System.ComponentModel.DataAnnotations;

namespace SkillForge.Models
{
    public class Course
    {        
            public int CourseId { get; set; }
            [Required]
            [MaxLength(150)]
            public string Title { get; set; } = null!;
            [Required]
            [MaxLength(1000)]
            public string Description { get; set; } = null!;
            [Required]
            public int InstructorId { get; set; }
            public User? Instructor { get; set; } = null!;
            [Required]
            public DateTime CreatedAt { get; set; }

            public ICollection<Enrollment>? Enrollments { get; set; }
        



    }
}
