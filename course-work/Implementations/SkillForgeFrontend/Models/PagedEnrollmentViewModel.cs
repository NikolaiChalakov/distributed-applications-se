namespace SkillForgeFrontend.Models
{
    public class PagedEnrollmentViewModel
    {
        public List<EnrollmentAdminViewModel> Data { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }

}
