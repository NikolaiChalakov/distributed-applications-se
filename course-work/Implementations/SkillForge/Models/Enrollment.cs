using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SkillForge.Models
{
    public class Enrollment
    {
        public int EnrollmentId { get; set; }
        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        [Required]
        public User User { get; set; } = null!;
        [Required]
        public int CourseId { get; set; }

        [ForeignKey(nameof(CourseId))]
        [Required]
        public Course Course { get; set; } = null!;
        [Required]
        public DateTime EnrolledAt { get; set; }

    }
}
