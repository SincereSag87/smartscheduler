using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using smartscheduler.Data.Entities;

namespace smartscheduler.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<AppointmentType> AppointmentTypes => Set<AppointmentType>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<AvailabilityBlock> Availability => Set<AvailabilityBlock>();
    public DbSet<BusinessHour> BusinessHours => Set<BusinessHour>();
    public DbSet<ClosureBlock> ClosureBlocks => Set<ClosureBlock>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Department>().HasData(
            new Department { Id = 1, Name = "Client Success", Color = "#14B8A6" },
            new Department { Id = 2, Name = "Operations", Color = "#3B82F6" },
            new Department { Id = 3, Name = "Implementation", Color = "#8B5CF6" });

        builder.Entity<Location>().HasData(
            new Location { Id = 1, Name = "Downtown Office", TimeZone = "America/New_York", IsActive = true },
            new Location { Id = 2, Name = "Virtual", TimeZone = "America/New_York", IsActive = true });
        builder.Entity<Location>()
            .Property(location => location.IsActive)
            .HasDefaultValue(true);

        builder.Entity<AppointmentType>().HasData(
            new AppointmentType { Id = 1, Name = "Consultation", DurationMinutes = 30, Color = "#14B8A6", IsActive = true },
            new AppointmentType { Id = 2, Name = "Implementation Review", DurationMinutes = 60, Color = "#3B82F6", IsActive = true },
            new AppointmentType { Id = 3, Name = "Executive Briefing", DurationMinutes = 45, Color = "#8B5CF6", IsActive = true });
        builder.Entity<AppointmentType>()
            .Property(type => type.IsActive)
            .HasDefaultValue(true);
    }
}
