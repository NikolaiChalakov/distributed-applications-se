using System.Text.Json.Serialization;

namespace SkillForgeFrontend.Models
{
    public class CourseViewModel
    {
        public int CourseId { get; set; }

        public string Title { get; set; } = "";
        public string Description { get; set; } = "";

        [JsonPropertyName("instructorUsername")]
        public string InstructorUsername { get; set; } = "";
        public bool IsEnrolled { get; set; }
    }
}
