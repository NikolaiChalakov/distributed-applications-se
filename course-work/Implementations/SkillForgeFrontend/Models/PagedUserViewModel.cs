namespace SkillForgeFrontend.Models
{
    public class PagedUserViewModel
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<UserViewModel> Data { get; set; } = new();
    }

}
