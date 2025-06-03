using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillForgeFrontend.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SkillForgeFrontend.Controllers
{
    [Authorize(Roles = "Admin", AuthenticationSchemes = "MyCookieAuth")]
    public class EnrollmentController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public EnrollmentController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? username, string? courseTitle, string sortBy = "username", bool descending = false, int page = 1, int pageSize = 10)
        {
            var token = HttpContext.Session.GetString("jwt");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var client = _clientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var query = $"page={page}&pageSize={pageSize}&sortBy={sortBy}&descending={descending.ToString().ToLower()}";
            if (!string.IsNullOrWhiteSpace(username)) query += $"&username={username}";
            if (!string.IsNullOrWhiteSpace(courseTitle)) query += $"&courseTitle={courseTitle}";

            var response = await client.GetAsync($"http://localhost:5199/api/enrollments/paged?{query}");
            if (!response.IsSuccessStatusCode)
                return View(new PagedEnrollmentViewModel());

            var json = await response.Content.ReadAsStringAsync();

            // Deserialize properly
            var result = JsonSerializer.Deserialize<PagedEnrollmentViewModel>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Ensure model is not null
            if (result == null)
            {
                result = new PagedEnrollmentViewModel
                {
                    Data = new List<EnrollmentAdminViewModel>(),
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = 0
                };
            }

            return View(result);
        }
    }
}
