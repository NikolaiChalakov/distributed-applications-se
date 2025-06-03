using System.ComponentModel.DataAnnotations;

namespace SkillForgeFrontend.Models
{
    public class CreateCourseViewModel
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;
    }
}
