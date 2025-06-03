namespace SkillForgeFrontend.Models
{
    public class PagedCourseApiResponse
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<CourseViewModel> Data { get; set; } = new();
    }
}
