using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Internal;
using Testcontainers.PostgreSql;
using TheEmployeeAPI;

var postgreSqlContainer = new PostgreSqlBuilder().Build();
await postgreSqlContainer.StartAsync();

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Enable XML comments
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "TheEmployeeAPI.xml"));
});

// Standard way to return structured data describing errors from an API.
// https://datatracker.ietf.org/doc/html/rfc7807
builder.Services.AddProblemDetails();
// This allows us to request an IValidator<CreateEmployeeRequest> from the DI container and get it, no problemo.
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddControllersWithViews(options => {
    options.Filters.Add<FluentValidationFilter>();
});
// builder.Services.AddControllers(options =>
// {
//     options.Filters.Add<FluentValidationFilter>();
// });
builder.Services.AddRazorPages();

builder.Services.AddHttpContextAccessor();
builder.Services.AddDbContext<AppDbContext>(options =>
    {
        var conn = postgreSqlContainer.GetConnectionString();
        options.UseNpgsql(conn);
        // Turn off EF Core ChangeTracker
        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }
);

// Identity services
builder.Services
    .AddIdentity<User, IdentityRole>(options => {
        // Configure password requirements
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;

        // Configure lockout
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 6;
        options.Lockout.AllowedForNewUsers = true;

        // Configure user requirements
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Cookie authentication
builder.Services.ConfigureApplicationCookie(options => {
   
    // For MVC
    options.LoginPath = "/api/account/login";
    options.LogoutPath = "/api/account/logout";
    options.AccessDeniedPath = "/api/account/access-denied";

    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api")) {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api")) {
            context.Response.StatusCode = 403;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };

    options.ExpireTimeSpan = TimeSpan.FromHours(24);
    // re-issue a new cookie with a new expiration time any time it processes a request which is more than halfway through the expiration window.
    options.SlidingExpiration = true;
    // Indicates whether a cookie is inaccessible by client-side script.
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Name = "TheEmployeeAPI.Auth";
});


// Register ISystemClock for dependency injection
// Production: SystemClock (real time) | Tests: TestSystemClock (fixed time)
// This allows audit fields to be testable with predictable timestamps
builder.Services.AddSingleton<ISystemClock, SystemClock>();

builder.Services.Configure<RouteOptions>(options =>
{
    //Force Urls low caps (/api/users not /api/Users)
    options.LowercaseUrls = true;
});

var app = builder.Build();

//kill container on shutdown
app.Lifetime.ApplicationStopping.Register(() => postgreSqlContainer.DisposeAsync());


// Scope inside of an ASP.NET Core app is typically created 
// when there's an HTTP request, and we don't have one when 
// the app is starting. So we'll just create one and dispose 
// of it after the seeding is complete.
// Prevents Cannot resolve scoped service 'AppDbContext' from root provider
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    SeedData.MigrateAndSeed(services);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


// Authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// CSS/JS/images
app.UseStaticFiles();

// Add controllers to the middleware.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();

//Expose internal types from the web app to the test project
// If not Program' est inaccessible en raison de son niveau de protection in tests.
public partial class Program { }


