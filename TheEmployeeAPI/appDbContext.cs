using Microsoft.EntityFrameworkCore;

namespace TheEmployeeAPI;

public class AppDbContext : DbContext
{
  public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
  {
  }

  protected AppDbContext()
  {
  }

  public DbSet<Employee> Employees { get; set; }
}
