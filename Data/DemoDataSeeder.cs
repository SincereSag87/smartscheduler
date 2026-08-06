using Microsoft.EntityFrameworkCore;
using smartscheduler.Data.Entities;

namespace smartscheduler.Data;

public static class DemoDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (!await dbContext.Departments.AnyAsync())
        {
            dbContext.Departments.AddRange(
                new Department { Name = "Client Success", Color = "#14B8A6" },
                new Department { Name = "Operations", Color = "#3B82F6" },
                new Department { Name = "Implementation", Color = "#8B5CF6" });
            await dbContext.SaveChangesAsync();
        }

        if (!await dbContext.AppointmentTypes.AnyAsync())
        {
            dbContext.AppointmentTypes.AddRange(
                new AppointmentType { Name = "Consultation", DurationMinutes = 30, Color = "#14B8A6" },
                new AppointmentType { Name = "Implementation Review", DurationMinutes = 60, Color = "#3B82F6" },
                new AppointmentType { Name = "Executive Briefing", DurationMinutes = 45, Color = "#8B5CF6" });
            await dbContext.SaveChangesAsync();
        }

        if (!await dbContext.Locations.AnyAsync())
        {
            dbContext.Locations.AddRange(
                new Location { Name = "Downtown Office", TimeZone = "America/New_York" },
                new Location { Name = "Virtual", TimeZone = "America/New_York" });
            await dbContext.SaveChangesAsync();
        }

        if (!await dbContext.Employees.AnyAsync())
        {
            var departments = await dbContext.Departments.OrderBy(department => department.Id).ToListAsync();
            dbContext.Employees.AddRange(
                new Employee { FullName = "Maya Chen", Email = "maya.chen@smartscheduler.local", Role = "Scheduler", DepartmentId = departments[0].Id, WorkdayStart = new TimeOnly(8, 30), WorkdayEnd = new TimeOnly(17, 0) },
                new Employee { FullName = "Diego Ramos", Email = "diego.ramos@smartscheduler.local", Role = "Operations Lead", DepartmentId = departments[Math.Min(1, departments.Count - 1)].Id, WorkdayStart = new TimeOnly(9, 0), WorkdayEnd = new TimeOnly(18, 0) },
                new Employee { FullName = "Priya Shah", Email = "priya.shah@smartscheduler.local", Role = "Implementation Manager", DepartmentId = departments[Math.Min(2, departments.Count - 1)].Id, WorkdayStart = new TimeOnly(8, 0), WorkdayEnd = new TimeOnly(16, 30) },
                new Employee { FullName = "Lena Ortiz", Email = "lena.ortiz@smartscheduler.local", Role = "Account Manager", DepartmentId = departments[0].Id, WorkdayStart = new TimeOnly(10, 0), WorkdayEnd = new TimeOnly(18, 0), IsActive = false });
            await dbContext.SaveChangesAsync();
        }

        if (await dbContext.Appointments.AnyAsync())
        {
            return;
        }

        var employees = await dbContext.Employees.OrderBy(employee => employee.FullName).ToListAsync();
        var appointmentTypes = await dbContext.AppointmentTypes.OrderBy(type => type.Id).ToListAsync();
        var locations = await dbContext.Locations.OrderBy(location => location.Id).ToListAsync();
        var today = DateTime.Today;

        dbContext.Appointments.AddRange(
            Create("Client intake", "Avery Johnson", "avery@example.com", today.AddHours(9), AppointmentStatus.Confirmed, employees[0], appointmentTypes[0], locations[1]),
            Create("Implementation review", "Northwind Traders", "scheduling@northwind.example", today.AddHours(10), AppointmentStatus.Scheduled, employees[Math.Min(1, employees.Count - 1)], appointmentTypes[Math.Min(1, appointmentTypes.Count - 1)], locations[0]),
            Create("Renewal planning", "Contoso Health", "ops@contoso.example", today.AddHours(13), AppointmentStatus.Pending, employees[Math.Min(2, employees.Count - 1)], appointmentTypes[0], locations[1]),
            Create("Executive briefing", "Fabrikam", "leaders@fabrikam.example", today.AddDays(1).AddHours(11), AppointmentStatus.Completed, employees[0], appointmentTypes[Math.Min(2, appointmentTypes.Count - 1)], locations[1]),
            Create("Reschedule request", "Alpine Ski House", "desk@alpine.example", today.AddDays(2).AddHours(15), AppointmentStatus.Cancelled, employees[0], appointmentTypes[0], locations[0]));

        await dbContext.SaveChangesAsync();
    }

    private static Appointment Create(
        string subject,
        string customerName,
        string customerEmail,
        DateTime startsAt,
        AppointmentStatus status,
        Employee employee,
        AppointmentType appointmentType,
        Location location) =>
        new()
        {
            Subject = subject,
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            StartsAt = startsAt,
            EndsAt = startsAt.AddMinutes(appointmentType.DurationMinutes),
            Status = status,
            EmployeeId = employee.Id,
            AppointmentTypeId = appointmentType.Id,
            LocationId = location.Id,
            IsRecurring = subject.Contains("review", StringComparison.OrdinalIgnoreCase)
        };
}
