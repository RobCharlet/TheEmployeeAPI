using System;
using Microsoft.EntityFrameworkCore;

namespace TheEmployeeAPI;

public class appDbContext : DbContext
{
  public appDbContext(DbContextOptions<appDbContext> options) : base(options)
  {
  }

  protected appDbContext()
  {
  }

  public DbSet<Employee> Employees { get; set; }
}
