using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using SkillForgeFrontend.Models;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace SkillForgeFrontend.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public AccountController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var client = _clientFactory.CreateClient();
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("http://localhost:5199/api/login/login", content);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                var root = JsonDocument.Parse(body).RootElement;

                var token = root.GetProperty("token").GetString();
                var role = root.GetProperty("role").GetString(); 

                HttpContext.Session.SetString("jwt", token!);

                // ✅ Добавяме и Role като claim
                var claims = new List<Claim>
{
    new Claim(ClaimTypes.Name, model.Username),
    new Claim(ClaimTypes.Role, role)
};

                var identity = new ClaimsIdentity(claims, "MyCookieAuth");
                await HttpContext.SignInAsync("MyCookieAuth", new ClaimsPrincipal(identity));

                var returnUrl = Request.Query["ReturnUrl"];
                if (!string.IsNullOrEmpty(returnUrl))   
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Users");
            }

            ModelState.AddModelError("", "Invalid login.");
            return View(model);
        }
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var client = _clientFactory.CreateClient();
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("http://localhost:5199/api/users", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Registration failed: {errorMsg}");
                return View(model);
            }

            // ✅ Handle the token returned from the backend
            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);
            var token = doc.RootElement.GetProperty("token").GetString();
            var role = doc.RootElement.GetProperty("role").GetString();

            // ✅ Store token and optionally role in session
            HttpContext.Session.SetString("jwt", token);
            HttpContext.Session.SetString("role", role ?? "");

            // 🔐 Auto-login with cookie (optional)
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, model.Username),
        new Claim(ClaimTypes.Role, role ?? "")
    };

            var identity = new ClaimsIdentity(claims, "MyCookieAuth");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("MyCookieAuth", principal, new AuthenticationProperties
            {
                IsPersistent = false
            });

            return RedirectToAction("Index", "Courses");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("MyCookieAuth");
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

    }
}