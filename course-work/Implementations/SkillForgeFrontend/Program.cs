namespace SkillForgeFrontend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddHttpClient();
            builder.Services.AddSession(); // За съхранение на JWT
            /*  builder.Services.AddAuthentication(options =>
              {
                  options.DefaultScheme = "MyCookieAuth"; // ✅ по подразбиране
              })
  .AddCookie("MyCookieAuth", options =>
  {
      options.LoginPath = "/Account/Login";
      options.AccessDeniedPath = "/Home/Index"; // 🟢 директно към началната
  });*/
            builder.Services.AddAuthentication("MyCookieAuth")
      .AddCookie("MyCookieAuth", options =>
      {
          options.Cookie.Name = "MyAuthCookie";
          options.Cookie.HttpOnly = true;
          options.ExpireTimeSpan = TimeSpan.FromHours(1); // optional timeout
          options.SlidingExpiration = true;

          // ⬇️ Important part — session-based cookie (expires when browser closes)
          options.Cookie.IsEssential = true;
          options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
          options.Cookie.SameSite = SameSiteMode.Lax;
          options.Cookie.MaxAge = null; // <-- Ensures it's session-only
          options.LoginPath = "/Account/Login";
          options.AccessDeniedPath = "/Courses/Index";
      });



            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseSession();
            app.UseAuthentication();  
            app.UseAuthorization();


            app.MapControllerRoute(
      name: "default",
      pattern: "{controller=Account}/{action=Login}/{id?}"); 


            app.Run();
        }
    }
}
