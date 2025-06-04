using System;
using Microsoft.EntityFrameworkCore;

namespace TheEmployeeAPI;

public static class SeedData
{
  public static void MigrateAndSeed(IServiceProvider serviceProvider)
  {
    var context = serviceProvider.GetRequiredService<AppDbContext>();
    // Ensure the database is created and all migrations are applied before seeding data.
    // This is necessary for integration tests because the test runner may use a fresh or in-memory database
    // that does not exist or is not up-to-date with the latest schema. Without this call, attempts to
    // insert or query data could fail due to missing tables or columns.
    context.Database.Migrate();

    if (!context.Employees.Any())
    {
      var employees = new List<Employee>
      {
        new Employee
        {
          FirstName = "John",
          LastName = "Doe",
          SocialSecurityNumber = "123-45-6789",
          Address1 = "123 Main St",
          City = "Anytown",
          State = "NY",
          ZipCode = "12345",
          PhoneNumber = "555-123-4567",
          Email = "john.doe@example.com"
        },
        new Employee
        {
          FirstName = "Jane",
          LastName = "Smith",
          SocialSecurityNumber = "987-65-4321",
          Address1 = "456 Elm St",
          Address2 = "Apt 2B",
          City = "Othertown",
          State = "CA",
          ZipCode = "98765",
          PhoneNumber = "555-987-6543",
          Email = "jane.smith@example.com"
        }
      };

      context.Employees.AddRange(employees);
      context.SaveChanges();
    }

    if (!context.Benefits.Any())
    {
      var benefits = new List<Benefit>
      {
        new Benefit {
          Name = "Health",
          Description = "Medical, dental, and vision coverage",
          BaseCost = 100.00m
        },
        new Benefit {
          Name = "Dental",
          Description = "Dental coverage",
          BaseCost = 50.00m
        },
        new Benefit {
          Name = "Vision",
          Description = "Vision coverage",
          BaseCost = 30.00m
        }
      };

      context.Benefits.AddRange(benefits);
      context.SaveChanges();
    }

    // Check if employee benefits are already assigned
    if (!context.EmployeeBenefits.Any())
    {
        // Get IDs only to avoid tracking conflicts
        var healthBenefitId = context.Benefits.Where(b => b.Name == "Health").Select(b => b.Id).Single();
        var dentalBenefitId = context.Benefits.Where(b => b.Name == "Dental").Select(b => b.Id).Single();
        
        var johnId = context.Employees.Where(e => e.FirstName == "John").Select(e => e.Id).Single();
        var janeId = context.Employees.Where(e => e.FirstName == "Jane").Select(e => e.Id).Single();

        var employeeBenefits = new List<EmployeeBenefit>
        {
            new EmployeeBenefit { EmployeeId = johnId, BenefitId = healthBenefitId, CostToEmployee = 100m },
            new EmployeeBenefit { EmployeeId = johnId, BenefitId = dentalBenefitId },
            new EmployeeBenefit { EmployeeId = janeId, BenefitId = healthBenefitId, CostToEmployee = 100m },
            new EmployeeBenefit { EmployeeId = janeId, BenefitId = dentalBenefitId }
        };

        context.EmployeeBenefits.AddRange(employeeBenefits);
        context.SaveChanges();
    }
  }
}
