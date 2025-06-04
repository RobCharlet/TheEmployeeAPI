using FluentValidation;
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
builder.Services.AddControllers(options =>
{
    options.Filters.Add<FluentValidationFilter>();
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddDbContext<AppDbContext>(options =>
    {
        var conn = postgreSqlContainer.GetConnectionString();
        options.UseNpgsql(conn);
        // Turn off EF Core ChangeTracker
        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }
);
// Register ISystemClock for dependency injection
// Production: SystemClock (real time) | Tests: TestSystemClock (fixed time)
// This allows audit fields to be testable with predictable timestamps
builder.Services.AddSingleton<ISystemClock, SystemClock>();

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
// Add controllers to the middleware.
app.MapControllers();

app.Run();

//Expose internal types from the web app to the test project
// If not Program' est inaccessible en raison de son niveau de protection in tests.
public partial class Program { }


