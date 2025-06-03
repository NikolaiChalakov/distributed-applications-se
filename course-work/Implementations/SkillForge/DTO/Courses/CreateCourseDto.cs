    using System.ComponentModel.DataAnnotations;

    namespace SkillForge.DTO.Courses
    {
        public class CreateCourseDto
        {
            [Required]
            public string Title { get; set; }
            [Required]
            public string Description { get; set; }
            [Required]
            public int InstructorId { get; set; }

        }
    }
