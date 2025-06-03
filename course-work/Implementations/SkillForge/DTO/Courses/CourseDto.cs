namespace SkillForge.DTO.Courses
{
    public class CourseDto
    {
        public int CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string InstructorUsername { get; set; } = string.Empty;
    }

}
