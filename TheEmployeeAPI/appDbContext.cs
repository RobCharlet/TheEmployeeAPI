using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Internal;

namespace TheEmployeeAPI;

public class AppDbContext : DbContext
{
  // ISystemClock allows us to control time in tests
  // Instead of DateTime.UtcNow (always changing), we can "freeze" time for testing
  // Production: real time | Tests: fixed time (2022-01-01)
  private readonly ISystemClock _systemClock;

  public AppDbContext(
    DbContextOptions<AppDbContext> options, 
    ISystemClock systemClock
  ) : base(options)
  {
    _systemClock = systemClock;
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

  // Override synchronous SaveChanges to include audit field updates
  public override int SaveChanges()
  {
    // Automatically populate audit fields before saving changes to database
    UpdateAuditFields();
    
    return base.SaveChanges();
  }

  // Override asynchronous SaveChanges to include audit field updates
  // IMPORTANT: Controllers use SaveChangesAsync(), so this override is essential!
  public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
  {
    // Automatically populate audit fields before saving changes to database
    UpdateAuditFields();
    
    return await base.SaveChangesAsync(cancellationToken);
  }

  private void UpdateAuditFields()
  {
    // Get all entities being tracked that inherit from AuditableEntity
    // This includes any entity that needs audit trail functionality
    var entries = ChangeTracker.Entries<AuditableEntity>();

    foreach (var entry in entries)
    {
      // Handle new entity creation - set creation audit fields
      if (entry.State == EntityState.Added)
      {
        // TODO: Replace hardcoded user with actual current user from HttpContext
        entry.Entity.CreatedBy = "TheCreateUser";
        
        // Use ISystemClock instead of DateTime.UtcNow for testability
        // Production: real current time | Tests: fixed time (2022-01-01)
        entry.Entity.CreatedOn = _systemClock.UtcNow.UtcDateTime;
      }

      // Handle entity modification - update modification audit fields
      if (entry.State == EntityState.Modified)
      {
        // TODO: Replace hardcoded user with actual current user from HttpContext
        entry.Entity.LastModifiedBy = "TheCreateUser";
        
        // Use ISystemClock instead of DateTime.UtcNow for testability
        // This allows tests to verify exact timestamps: Assert.Equal(expectedTime, actualTime)
        entry.Entity.LastModifiedOn = _systemClock.UtcNow.UtcDateTime;
      }
    }
  }
}
