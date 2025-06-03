namespace SkillForgeFrontend.Models
{
    public class PagedCourseViewModel
    {
        public List<CourseViewModel> Data { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }

}
