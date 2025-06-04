using Microsoft.AspNetCore.Http.Features;
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
  public DbSet<Benefit> Benefits { get; set; }
  public DbSet<EmployeeBenefit> EmployeeBenefits { get; set; }


  // an employee and a benefit can have only 1 line in the EmployeeBenefit table.
  // Prevents duplicates.
  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<EmployeeBenefit>()
      .HasIndex(eb => new { eb.EmployeeId, eb.BenefitId })
      .IsUnique();
  }

}
