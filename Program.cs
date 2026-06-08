using CemaApp.Data;
using CemaApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CemaApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            // 1. Database Configuration
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // 2. Identity Configuration
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            // 3. Cookie Configuration
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
            });

            builder.Services.AddControllersWithViews();

            // 4. In-Memory Storage (Added for fast seat locking)
            builder.Services.AddMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(20); // Session timeout
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            builder.Services.AddScoped<CemaApp.Services.IBookingService, CemaApp.Services.BookingService>();


            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            // Intercept external poster URLs and redirect to online resource
            app.Use(async (context, next) =>
            {
                var requestPath = context.Request.Path.Value ?? string.Empty;
                if (requestPath.StartsWith("/images/posters/", StringComparison.OrdinalIgnoreCase))
                {
                    var path = requestPath.Substring("/images/posters/".Length);
                    if (path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || 
                        path.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                    {
                        var redirectUrl = path + context.Request.QueryString.Value;
                        context.Response.Redirect(redirectUrl);
                        return;
                    }
                    if (path.StartsWith("https:/", StringComparison.OrdinalIgnoreCase))
                    {
                        var redirectUrl = "https://" + path.Substring("https:/".Length) + context.Request.QueryString.Value;
                        context.Response.Redirect(redirectUrl);
                        return;
                    }
                    if (path.StartsWith("http:/", StringComparison.OrdinalIgnoreCase))
                    {
                        var redirectUrl = "http://" + path.Substring("http:/".Length) + context.Request.QueryString.Value;
                        context.Response.Redirect(redirectUrl);
                        return;
                    }
                }
                await next();
            });

            app.UseStaticFiles();

            app.UseRouting();

            // Enable Session middleware
            app.UseSession();

            app.UseAuthentication();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // 4. DB seeder
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    await DbSeeder.SeedRolesAndAdminAsync(services);
                    
                    var dbContext = services.GetRequiredService<AppDbContext>();
                    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                    await DbSeeder.SeedSampleDataAsync(dbContext, userManager);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Seeding error: {ex.Message}");
                }
            }

            app.Run();
        }
    }
}
