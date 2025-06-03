using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillForgeFrontend.Models;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace SkillForgeFrontend.Controllers
{
    [Authorize(AuthenticationSchemes = "MyCookieAuth")]
    public class CoursesController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public CoursesController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<IActionResult> Index(string? title, string? instructor, string? sortBy, bool descending = false, int page = 1, int pageSize = 10)
        {
            var token = HttpContext.Session.GetString("jwt");
            var username = User.Identity?.Name;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(username))
                return RedirectToAction("Login", "Account");

            var client = _clientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // ✅ Build query string
            var query = $"http://localhost:5199/api/courses/paged?page={page}&pageSize={pageSize}";

            if (!string.IsNullOrWhiteSpace(title))
                query += $"&title={Uri.EscapeDataString(title)}";

            if (!string.IsNullOrWhiteSpace(instructor))
                query += $"&instructor={Uri.EscapeDataString(instructor)}";

            if (!string.IsNullOrWhiteSpace(sortBy))
                query += $"&sortBy={Uri.EscapeDataString(sortBy)}";

            query += $"&descending={descending.ToString().ToLower()}";

            var courseResponse = await client.GetAsync(query);
            if (!courseResponse.IsSuccessStatusCode)
                return View(new PagedCourseViewModel());

            var courseJson = await courseResponse.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<PagedCourseApiResponse>(courseJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var result = new PagedCourseViewModel
            {
                TotalCount = apiResponse.TotalCount,
                Page = apiResponse.Page,
                PageSize = apiResponse.PageSize,
                Data = apiResponse.Data
            };


            // Fetch current user
            var userResponse = await client.GetAsync($"http://localhost:5199/api/users/search?username={username}&email={email}");
            if (!userResponse.IsSuccessStatusCode || result == null)
                return View(result);

            var userJson = await userResponse.Content.ReadAsStringAsync();
            var users = JsonSerializer.Deserialize<List<UserViewModel>>(userJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var currentUser = users?.FirstOrDefault();
            if (currentUser == null)
                return View(result);

            // Fetch enrollments
            var enrollmentResponse = await client.GetAsync("http://localhost:5199/api/enrollments/all");
            if (!enrollmentResponse.IsSuccessStatusCode)
                return View(result);

            var enrollmentJson = await enrollmentResponse.Content.ReadAsStringAsync();
            var enrollments = JsonSerializer.Deserialize<List<EnrollmentViewModel>>(enrollmentJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var enrolledCourseIds = enrollments
                .Where(e => e.UserId == currentUser.Id)
                .Select(e => e.CourseId)
                .ToHashSet();

            foreach (var course in result.Data)
            {
                course.IsEnrolled = enrolledCourseIds.Contains(course.CourseId);
            }

            return View(result);
        }


        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateCourseViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var token = HttpContext.Session.GetString("jwt");
            var username = User.Identity?.Name;

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            var client = _clientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // 🔍 Получаваме user от API-то, за да вземем InstructorId
            var userResponse = await client.GetAsync($"http://localhost:5199/api/users/search?username={username}");
            if (!userResponse.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Cannot identify current user.");
                return View(model);
            }

            var userJson = await userResponse.Content.ReadAsStringAsync();
            var users = JsonSerializer.Deserialize<List<UserViewModel>>(userJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var instructor = users?.FirstOrDefault();

            if (instructor == null)
            {
                ModelState.AddModelError("", "Instructor not found.");
                return View(model);
            }

            // 🔄 Подготвяме CreateCourseDto
            var payload = new
            {
                model.Title,
                model.Description,
                InstructorId = instructor.Id
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("http://localhost:5199/api/courses", content);

            if (response.IsSuccessStatusCode)
                return RedirectToAction("Index");

            var errorMsg = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("", $"Failed to create course: {errorMsg}");
            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var token = HttpContext.Session.GetString("jwt");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var client = _clientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"http://localhost:5199/api/courses/{id}");
            if (!response.IsSuccessStatusCode)
                return RedirectToAction("Index");

            var json = await response.Content.ReadAsStringAsync();
            var course = JsonSerializer.Deserialize<CourseViewModel>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return View(course);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(CourseViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var token = HttpContext.Session.GetString("jwt");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var client = _clientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PutAsync("http://localhost:5199/api/courses", content);

            if (response.IsSuccessStatusCode)
                return RedirectToAction("Index");

            var error = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("", $"Failed to update course: {error}");
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Join(int id)
        {
            var token = HttpContext.Session.GetString("jwt");
            var username = User.Identity?.Name;
           
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(username))
                return RedirectToAction("Login", "Account");

            var client = _clientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var payload = new { CourseId = id };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("http://localhost:5199/api/enrollments/join", content);

            if (response.IsSuccessStatusCode)
                return RedirectToAction("Index");

            TempData["Error"] = "Failed to join course.";
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> Sort(string sortBy, bool descending = false)
        {
            var token = HttpContext.Session.GetString("jwt");
            var client = _clientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var url = $"http://localhost:5199/api/courses/sort?sortBy={sortBy}&descending={descending.ToString().ToLower()}";
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return RedirectToAction("Index");

            var json = await response.Content.ReadAsStringAsync();
            var courses = JsonSerializer.Deserialize<List<CourseViewModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return View("Index", courses);
        }
       


        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var token = HttpContext.Session.GetString("jwt");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var client = _clientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.DeleteAsync($"http://localhost:5199/api/courses/{id}");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                TempData["Error"] = $"Delete failed: {error}";
            }

            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> Unenroll(int id)
        {
            var token = HttpContext.Session.GetString("jwt");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var client = _clientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.DeleteAsync($"http://localhost:5199/api/enrollments/leave?courseId={id}");

            if (response.IsSuccessStatusCode)
                return RedirectToAction("Index");

            TempData["Error"] = "Failed to unenroll.";
            return RedirectToAction("Index");
        }

    }
}
