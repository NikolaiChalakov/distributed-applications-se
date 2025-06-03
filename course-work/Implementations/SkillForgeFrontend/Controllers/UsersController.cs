using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillForgeFrontend.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SkillForgeFrontend.Controllers
{
    [Authorize(Roles = "Admin", AuthenticationSchemes = "MyCookieAuth")]
    public class UsersController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public UsersController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<IActionResult> Index(int page = 1, string? username = null, string? email = null, string sortBy = "username", bool descending = false)
        {
            var token = HttpContext.Session.GetString("jwt");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var client = _clientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var query = $"page={page}&pageSize=10&sortBy={sortBy}&descending={descending.ToString().ToLower()}";
            if (!string.IsNullOrWhiteSpace(username)) query += $"&username={username}";
            if (!string.IsNullOrWhiteSpace(email)) query += $"&email={email}";

            var response = await client.GetAsync($"http://localhost:5199/api/users/paged?{query}");

            if (!response.IsSuccessStatusCode)
                return View(new PagedUserViewModel());

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<PagedUserViewModel>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return View(result);
        }



        public async Task<IActionResult> Sort(string sortBy, bool descending = false)
        {
            var token = HttpContext.Session.GetString("jwt");
            var client = _clientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var url = $"http://localhost:5199/api/users/sort?sortBy={sortBy}&descending={descending.ToString().ToLower()}";
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return RedirectToAction("Index");

            var json = await response.Content.ReadAsStringAsync();
            var users = JsonSerializer.Deserialize<List<UserViewModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var model = new PagedUserViewModel
            {
                Data = users,
                Page = 1,
                PageSize = users.Count,
                TotalCount = users.Count
            };

            return View("Index", model);
        }


        [HttpPost]
        public async Task<IActionResult> Delete(string username)
        {
            var token = HttpContext.Session.GetString("jwt");
            var client = _clientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.DeleteAsync($"http://localhost:5199/api/users?username={username}");

            return RedirectToAction("Index");
        }
    }
}
