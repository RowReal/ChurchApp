using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class DataSeederService
    {
        private readonly AppDbContext _context;

        public DataSeederService(AppDbContext context)
        {
            _context = context;
        }

        public async Task SeedDataAsync()
        {
            // Create database if it doesn't exist
            await _context.Database.EnsureCreatedAsync();

            // Seed Directorates if none exist
            if (!await _context.Directorates.AnyAsync())
            {
                await SeedDirectorates();
            }

            // Seed Departments if none exist
            if (!await _context.Departments.AnyAsync())
            {
                await SeedDepartments();
            }

            // Create MEAT Admin user if none exist
            if (!await _context.Workers.AnyAsync())
            {
                await SeedAdminUser();
            }
        }

        private async Task SeedDirectorates()
        {
            var directorates = new[]
            {
                new Directorate { Name = "FIG", Code = "FIG", Description = "Finance and General Purposes" },
                new Directorate { Name = "MEAT", Code = "MEAT", Description = "Membership, Education and Training" },
                new Directorate { Name = "TRADOC", Code = "TRADOC", Description = "Training and Doctrine" },
                new Directorate { Name = "BRICKS", Code = "BRICKS", Description = "Building and Infrastructure" },
                new Directorate { Name = "MINISTRY", Code = "MINISTRY", Description = "Ministry Operations" },
                new Directorate { Name = "SERVICES", Code = "SERVICES", Description = "Church Services" },
                new Directorate { Name = "CHILDREN", Code = "CHILDREN", Description = "Children Ministry" },
                new Directorate { Name = "ICARE", Code = "ICARE", Description = "Information and Care" },
                new Directorate { Name = "MUSIC", Code = "MUSIC", Description = "Music Ministry" },
                new Directorate { Name = "CIPM", Code = "CIPM", Description = "Church Information and Publicity" },
                new Directorate { Name = "Marriage Counselling", Code = "MARRIAGE", Description = "Marriage Counselling" }
            };

            await _context.Directorates.AddRangeAsync(directorates);
            await _context.SaveChangesAsync();
        }

        private async Task SeedDepartments()
        {
            // Get the directorates from database to get their IDs
            var fig = await _context.Directorates.FirstAsync(d => d.Code == "FIG");
            var meat = await _context.Directorates.FirstAsync(d => d.Code == "MEAT");
            var tradoc = await _context.Directorates.FirstAsync(d => d.Code == "TRADOC");

            var departments = new[]
            {
        new Department {
            Name = "Ushering",
            Code = "USH",
            DirectorateId = fig.Id,
            Description = "Ushering Department"
        },
        new Department {
            Name = "Protocol",
            Code = "PRO",
            DirectorateId = fig.Id,
            Description = "Protocol Department"
        },
        new Department {
            Name = "Worker Training",
            Code = "WTR",
            DirectorateId = meat.Id,
            Description = "Worker Training Department"
        },
        new Department {
            Name = "Membership",
            Code = "MEM",
            DirectorateId = meat.Id,
            Description = "Membership Department"
        },
        new Department {
            Name = "Sunday School",
            Code = "SSC",
            DirectorateId = tradoc.Id,
            Description = "Sunday School Department"
        }
    };

            await _context.Departments.AddRangeAsync(departments);
            await _context.SaveChangesAsync();
        }

        private async Task SeedAdminUser()
        {
            var meatDirectorate = await _context.Directorates.FirstAsync(d => d.Code == "MEAT");

            var adminUser = new Worker
            {
                WorkerId = "MEAT001",
                FirstName = "MEAT",
                LastName = "Administrator",
                Email = "meat.admin@bcc.org",
                PasswordHash = HashPassword("admin123"),
                DirectorateId = meatDirectorate.Id,
                Role = AuthService.Roles.HeadOfDirectorate, // Use the role constant
                OrdinationLevel = "Not Ordained", // Add this
                OrdinationStatus = "Not Ordained",
                IsFirstLogin = false,
                IsActive = true,
                CompliancePercentage = 100,
                CanAccessAdminPanel = true,
                CanManageWorkers = true,
                CanApproveProfiles = true,
                CanViewReports = true
            };

            await _context.Workers.AddAsync(adminUser);
            await _context.SaveChangesAsync();
        }

        private string HashPassword(string password)
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));
        }
    }
}