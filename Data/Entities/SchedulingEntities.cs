using System.ComponentModel.DataAnnotations;

namespace smartscheduler.Data.Entities;

public enum AppointmentStatus
{
    Scheduled,
    Confirmed,
    Pending,
    Cancelled,
    Completed
}

public enum AvailabilityStatus
{
    Available,
    Busy,
    TimeOff
}

public enum ClosureScope
{
    Organization,
    Location
}

public sealed class Department
{
    public int Id { get; set; }

    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(16)]
    public string Color { get; set; } = "#14B8A6";

    public List<Employee> Employees { get; set; } = [];
}

public sealed class Employee
{
    public int Id { get; set; }

    [MaxLength(160)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(160)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Role { get; set; } = string.Empty;

    public int DepartmentId { get; set; }
    public Department? Department { get; set; }

    public TimeOnly WorkdayStart { get; set; } = new(8, 30);
    public TimeOnly WorkdayEnd { get; set; } = new(17, 0);
    public bool IsActive { get; set; } = true;
}

public sealed class Location
{
    public int Id { get; set; }

    [MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(80)]
    public string TimeZone { get; set; } = "America/New_York";

    public bool IsActive { get; set; } = true;
}

public sealed class AppointmentType
{
    public int Id { get; set; }

    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    public int DurationMinutes { get; set; }

    [MaxLength(16)]
    public string Color { get; set; } = "#14B8A6";

    public bool IsActive { get; set; } = true;
}

public sealed class BusinessHour
{
    public int Id { get; set; }

    public int? LocationId { get; set; }
    public Location? Location { get; set; }

    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly OpensAt { get; set; } = new(8, 30);
    public TimeOnly ClosesAt { get; set; } = new(17, 0);
    public bool IsClosed { get; set; }
}

public sealed class ClosureBlock
{
    public int Id { get; set; }

    public int? LocationId { get; set; }
    public Location? Location { get; set; }

    public ClosureScope Scope { get; set; } = ClosureScope.Organization;
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }

    [MaxLength(180)]
    public string Reason { get; set; } = string.Empty;
}

public sealed class Appointment
{
    public int Id { get; set; }

    [MaxLength(180)]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(160)]
    public string CustomerName { get; set; } = string.Empty;

    [MaxLength(160)]
    public string CustomerEmail { get; set; } = string.Empty;

    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public bool IsRecurring { get; set; }

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public int AppointmentTypeId { get; set; }
    public AppointmentType? AppointmentType { get; set; }

    public int LocationId { get; set; }
    public Location? Location { get; set; }
}

public sealed class AvailabilityBlock
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public AvailabilityStatus Status { get; set; }

    [MaxLength(200)]
    public string Note { get; set; } = string.Empty;
}

public sealed class Notification
{
    public int Id { get; set; }

    [MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(400)]
    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
}

public sealed class AuditLog
{
    public int Id { get; set; }

    [MaxLength(120)]
    public string EntityName { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(160)]
    public string Actor { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
