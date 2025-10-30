using MessangerBackend.Data;
using MessangerBackend.Hubs;
using MessangerBackend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace MessangerBackend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();

            var connectionString = "Host=postgres;Port=5432;Database=MessengerDB;User Id=postgres;Password=StrongPassword123!";
            //var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            //    ?? throw new InvalidOperationException("Connection string not found.");

            var jwtSecret = builder.Configuration["JWT:Secret"]
                ?? throw new InvalidOperationException("JWT Secret not configured");

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                        ValidateIssuer = true,
                        ValidIssuer = builder.Configuration["JWT:Issuer"],
                        ValidateAudience = true,
                        ValidAudience = builder.Configuration["JWT:Audience"],
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chatHub"))
                            {
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
            builder.Services.AddScoped<IAuthService, AuthService>();

            builder.Services.AddControllers();

            builder.Services.AddSignalR();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors("AllowAll");
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapGet("/", () => "Backend is running!");
            app.MapGet("/api/health", () => "Healthy!");
            app.MapGet("/api/test", () => new {
                Message = "Backend API is working!",
                Timestamp = DateTime.UtcNow,
                Environment = app.Environment.EnvironmentName
            });

            app.MapHub<ChatHub>("/chatHub");

            app.MapControllers();

            if (app.Environment.IsDevelopment())
            {
                using var scope = app.Services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                dbContext.Database.EnsureCreated();
            }

            app.MapGet("/api/auth/verify", async (HttpContext context, IAuthService authService) => {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Results.Unauthorized();

                var user = await context.RequestServices.GetRequiredService<ApplicationDbContext>()
                    .Users.FindAsync(int.Parse(userId));

                if (user == null) return Results.Unauthorized();

                return Results.Ok(new
                {
                    user.Id,
                    user.Name,
                    user.UserName,
                    user.RegistrationDate
                });
            }).RequireAuthorization();

            app.MapGet("/api/users", async (ApplicationDbContext context) => {
                var users = await context.Users
                    .Select(u => new {
                        u.Id,
                        u.Name,
                        u.UserName,
                        u.RegistrationDate,
                        u.LastActivity,
                        IsOnline = u.LastActivity > DateTime.UtcNow.AddMinutes(-5) 
                    })
                    .ToListAsync();

                return Results.Ok(users);
            }).RequireAuthorization();

            app.MapGet("/api/messages/{userId}", async (HttpContext context, ApplicationDbContext dbContext, int userId) => {
                var currentUserId = int.Parse(context.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

                var messages = await dbContext.Messages
                    .Where(m => (m.SenderId == currentUserId && m.RecipientId == userId) ||
                               (m.SenderId == userId && m.RecipientId == currentUserId))
                    .OrderBy(m => m.SendTime)
                    .Select(m => new {
                        m.Id,
                        m.SenderId,
                        m.RecipientId,
                        m.Body,
                        m.SendTime,
                        m.IsRead
                    })
                    .ToListAsync();

                return Results.Ok(messages);
            }).RequireAuthorization();


            app.Run();
        }
    }
}
