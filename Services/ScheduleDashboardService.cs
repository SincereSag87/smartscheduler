using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using smartscheduler.Data;
using smartscheduler.Data.Entities;
using smartscheduler.Hubs;

namespace smartscheduler.Services;

public interface IScheduleRepository
{
    Task<DashboardSnapshot> GetDashboardAsync();
    Task<IReadOnlyList<CalendarEvent>> GetCalendarEventsAsync(DateTime? startsOnOrAfter = null, DateTime? endsBefore = null);
    Task<IReadOnlyList<AppointmentListItem>> GetAppointmentsAsync();
    Task<AppointmentFormOptions> GetAppointmentFormOptionsAsync();
    Task<AppointmentEditor> GetAppointmentEditorAsync(int? id = null);
    Task<ScheduleOperationResult> SaveAppointmentAsync(AppointmentEditor editor);
    Task UpdateAppointmentStatusAsync(int id, AppointmentStatus status);
    Task<ScheduleOperationResult> RescheduleAppointmentAsync(int id, DateTime startsAt);
    IReadOnlyList<TeamMemberSchedule> GetTeamSchedules();
    IReadOnlyList<ReportMetric> GetReportMetrics();
}

public sealed class ScheduleDashboardService(
    ApplicationDbContext dbContext,
    IHubContext<ScheduleHub> scheduleHub) : IScheduleRepository
{
    public async Task<DashboardSnapshot> GetDashboardAsync()
    {
        var today = DateTime.Today;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var weekEnd = weekStart.AddDays(7);
        var events = await GetCalendarEventsAsync(today, today.AddDays(14));
        var weeklyAppointments = await dbContext.Appointments
            .AsNoTracking()
            .CountAsync(appointment => appointment.StartsAt >= weekStart && appointment.StartsAt < weekEnd);
        var activeEmployees = await dbContext.Employees.CountAsync(employee => employee.IsActive);
        var completedThisWeek = await dbContext.Appointments
            .AsNoTracking()
            .CountAsync(appointment =>
                appointment.StartsAt >= weekStart &&
                appointment.StartsAt < weekEnd &&
                appointment.Status == AppointmentStatus.Completed);

        var weeklyUtilization = weeklyAppointments == 0
            ? 0
            : Math.Clamp((int)Math.Round(completedThisWeek / (double)Math.Max(weeklyAppointments, 1) * 100), 0, 100);

        return new DashboardSnapshot(
            TodayAppointments: events.Count(e => e.StartsAt.Date == today),
            WeeklyUtilization: weeklyUtilization,
            PendingRequests: events.Count(e => e.Status == AppointmentStatus.Pending),
            TeamAvailable: activeEmployees,
            UpcomingMeetings: events.OrderBy(e => e.StartsAt).Take(5).ToList(),
            Notifications:
            [
                new("Appointment queue synced", $"{events.Count} appointments loaded from SQL Server.", "now"),
                new("Pending requests", $"{events.Count(e => e.Status == AppointmentStatus.Pending)} appointments need confirmation.", "today"),
                new("Team coverage", $"{activeEmployees} employees are active for scheduling.", "today")
            ]);
    }

    public async Task<IReadOnlyList<CalendarEvent>> GetCalendarEventsAsync(DateTime? startsOnOrAfter = null, DateTime? endsBefore = null)
    {
        var query = dbContext.Appointments
            .AsNoTracking()
            .Include(appointment => appointment.Employee)
            .Include(appointment => appointment.AppointmentType)
            .OrderBy(appointment => appointment.StartsAt)
            .AsQueryable();

        if (startsOnOrAfter.HasValue)
        {
            query = query.Where(appointment => appointment.StartsAt >= startsOnOrAfter.Value);
        }

        if (endsBefore.HasValue)
        {
            query = query.Where(appointment => appointment.StartsAt < endsBefore.Value);
        }

        return await query
            .Select(appointment => new CalendarEvent(
                appointment.Id,
                appointment.Subject,
                appointment.CustomerName,
                appointment.Employee == null ? "Unassigned" : appointment.Employee.FullName,
                appointment.StartsAt,
                appointment.EndsAt,
                appointment.Status,
                StatusColor(appointment.Status)))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AppointmentListItem>> GetAppointmentsAsync() =>
        await dbContext.Appointments
            .AsNoTracking()
            .Include(appointment => appointment.Employee)
            .Include(appointment => appointment.AppointmentType)
            .Include(appointment => appointment.Location)
            .OrderBy(appointment => appointment.StartsAt)
            .Select(appointment => new AppointmentListItem(
                appointment.Id,
                appointment.Subject,
                appointment.CustomerName,
                appointment.CustomerEmail,
                appointment.Employee == null ? "Unassigned" : appointment.Employee.FullName,
                appointment.AppointmentType == null ? "General" : appointment.AppointmentType.Name,
                appointment.Location == null ? "Unassigned" : appointment.Location.Name,
                appointment.StartsAt,
                appointment.EndsAt,
                appointment.Status,
                appointment.IsRecurring))
            .ToListAsync();

    public async Task<AppointmentFormOptions> GetAppointmentFormOptionsAsync() =>
        new(
            await dbContext.Employees.AsNoTracking().Where(employee => employee.IsActive).OrderBy(employee => employee.FullName).ToListAsync(),
            await dbContext.AppointmentTypes.AsNoTracking().OrderBy(type => type.Name).ToListAsync(),
            await dbContext.Locations.AsNoTracking().OrderBy(location => location.Name).ToListAsync());

    public async Task<AppointmentEditor> GetAppointmentEditorAsync(int? id = null)
    {
        if (id is null)
        {
            var options = await GetAppointmentFormOptionsAsync();
            var appointmentType = options.AppointmentTypes.FirstOrDefault();
            var startsAt = DateTime.Today.AddDays(1).AddHours(9);

            return new AppointmentEditor
            {
                EmployeeId = options.Employees.FirstOrDefault()?.Id ?? 0,
                AppointmentTypeId = appointmentType?.Id ?? 0,
                LocationId = options.Locations.FirstOrDefault()?.Id ?? 0,
                StartsAt = startsAt,
                EndsAt = startsAt.AddMinutes(appointmentType?.DurationMinutes ?? 30)
            };
        }

        var appointment = await dbContext.Appointments
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id.Value);

        if (appointment is null)
        {
            return await GetAppointmentEditorAsync();
        }

        return new AppointmentEditor
        {
            Id = appointment.Id,
            Subject = appointment.Subject,
            CustomerName = appointment.CustomerName,
            CustomerEmail = appointment.CustomerEmail,
            StartsAt = appointment.StartsAt,
            EndsAt = appointment.EndsAt,
            Status = appointment.Status,
            IsRecurring = appointment.IsRecurring,
            EmployeeId = appointment.EmployeeId,
            AppointmentTypeId = appointment.AppointmentTypeId,
            LocationId = appointment.LocationId
        };
    }

    public async Task<ScheduleOperationResult> SaveAppointmentAsync(AppointmentEditor editor)
    {
        var validation = await ValidateAppointmentAsync(editor);
        if (!validation.Success)
        {
            return validation;
        }

        var appointment = editor.Id == 0
            ? new Appointment()
            : await dbContext.Appointments.FindAsync(editor.Id) ?? new Appointment { Id = editor.Id };

        appointment.Subject = editor.Subject.Trim();
        appointment.CustomerName = editor.CustomerName.Trim();
        appointment.CustomerEmail = editor.CustomerEmail.Trim();
        appointment.StartsAt = editor.StartsAt;
        appointment.EndsAt = editor.EndsAt;
        appointment.Status = editor.Status;
        appointment.IsRecurring = editor.IsRecurring;
        appointment.EmployeeId = editor.EmployeeId;
        appointment.AppointmentTypeId = editor.AppointmentTypeId;
        appointment.LocationId = editor.LocationId;

        if (appointment.Id == 0)
        {
            dbContext.Appointments.Add(appointment);
        }
        else
        {
            dbContext.Appointments.Update(appointment);
        }

        await dbContext.SaveChangesAsync();
        await BroadcastScheduleChangedAsync("Appointment saved.");
        return ScheduleOperationResult.Ok("Appointment saved.");
    }

    public async Task UpdateAppointmentStatusAsync(int id, AppointmentStatus status)
    {
        var appointment = await dbContext.Appointments.FindAsync(id);
        if (appointment is null)
        {
            return;
        }

        appointment.Status = status;
        await dbContext.SaveChangesAsync();
        await BroadcastScheduleChangedAsync($"Appointment marked {status}.");
    }

    public async Task<ScheduleOperationResult> RescheduleAppointmentAsync(int id, DateTime startsAt)
    {
        var appointment = await dbContext.Appointments
            .Include(item => item.AppointmentType)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (appointment is null)
        {
            return ScheduleOperationResult.Fail("Appointment was not found.");
        }

        var duration = appointment.AppointmentType?.DurationMinutes ?? (int)(appointment.EndsAt - appointment.StartsAt).TotalMinutes;
        var editor = new AppointmentEditor
        {
            Id = appointment.Id,
            Subject = appointment.Subject,
            CustomerName = appointment.CustomerName,
            CustomerEmail = appointment.CustomerEmail,
            StartsAt = startsAt,
            EndsAt = startsAt.AddMinutes(Math.Max(duration, 15)),
            Status = AppointmentStatus.Scheduled,
            IsRecurring = appointment.IsRecurring,
            EmployeeId = appointment.EmployeeId,
            AppointmentTypeId = appointment.AppointmentTypeId,
            LocationId = appointment.LocationId
        };
        var validation = await ValidateAppointmentAsync(editor);
        if (!validation.Success)
        {
            return validation;
        }

        appointment.StartsAt = startsAt;
        appointment.EndsAt = startsAt.AddMinutes(Math.Max(duration, 15));
        appointment.Status = AppointmentStatus.Scheduled;
        await dbContext.SaveChangesAsync();
        await BroadcastScheduleChangedAsync("Appointment rescheduled.");
        return ScheduleOperationResult.Ok("Appointment rescheduled.");
    }

    public IReadOnlyList<TeamMemberSchedule> GetTeamSchedules() =>
    [
        new("Maya Chen", "Client Success", "8:30 AM - 5:00 PM", AvailabilityStatus.Available, 7),
        new("Diego Ramos", "Operations", "9:00 AM - 6:00 PM", AvailabilityStatus.Busy, 5),
        new("Priya Shah", "Implementation", "8:00 AM - 4:30 PM", AvailabilityStatus.Available, 6),
        new("Lena Ortiz", "Client Success", "10:00 AM - 6:00 PM", AvailabilityStatus.TimeOff, 2)
    ];

    public IReadOnlyList<ReportMetric> GetReportMetrics() =>
    [
        new("Daily appointments", 34, "+12%"),
        new("Weekly utilization", 82, "+6%"),
        new("Monthly completion", 91, "+4%"),
        new("Average lead time", 18, "-3%")
    ];

    private static string StatusColor(AppointmentStatus status) =>
        status switch
        {
            AppointmentStatus.Confirmed => "#22C55E",
            AppointmentStatus.Scheduled => "#3B82F6",
            AppointmentStatus.Pending => "#F59E0B",
            AppointmentStatus.Cancelled => "#EF4444",
            AppointmentStatus.Completed => "#8B5CF6",
            _ => "#14B8A6"
        };

    private async Task<ScheduleOperationResult> ValidateAppointmentAsync(AppointmentEditor editor)
    {
        if (editor.EndsAt <= editor.StartsAt)
        {
            return ScheduleOperationResult.Fail("Appointment end time must be after the start time.");
        }

        var employee = await dbContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == editor.EmployeeId);

        if (employee is null || !employee.IsActive)
        {
            return ScheduleOperationResult.Fail("Choose an active employee before booking.");
        }

        var startsAtTime = TimeOnly.FromDateTime(editor.StartsAt);
        var endsAtTime = TimeOnly.FromDateTime(editor.EndsAt);
        if (startsAtTime < employee.WorkdayStart || endsAtTime > employee.WorkdayEnd)
        {
            return ScheduleOperationResult.Fail(
                $"{employee.FullName} works {employee.WorkdayStart:h\\:mm} - {employee.WorkdayEnd:h\\:mm}.",
                await SuggestNextAvailableTimeAsync(editor));
        }

        var hasOverlap = await dbContext.Appointments
            .AsNoTracking()
            .AnyAsync(appointment =>
                appointment.Id != editor.Id &&
                appointment.EmployeeId == editor.EmployeeId &&
                appointment.Status != AppointmentStatus.Cancelled &&
                appointment.StartsAt < editor.EndsAt &&
                editor.StartsAt < appointment.EndsAt);

        if (hasOverlap)
        {
            return ScheduleOperationResult.Fail(
                "This employee already has an appointment during that time.",
                await SuggestNextAvailableTimeAsync(editor));
        }

        var timeOffBlock = await dbContext.Availability
            .AsNoTracking()
            .Where(block =>
                block.EmployeeId == editor.EmployeeId &&
                block.Status == AvailabilityStatus.TimeOff &&
                block.StartsAt < editor.EndsAt &&
                editor.StartsAt < block.EndsAt)
            .OrderBy(block => block.StartsAt)
            .FirstOrDefaultAsync();

        if (timeOffBlock is not null)
        {
            return ScheduleOperationResult.Fail(
                $"This time overlaps time off: {timeOffBlock.Note}.",
                await SuggestNextAvailableTimeAsync(editor));
        }

        return ScheduleOperationResult.Ok("Appointment is available.");
    }

    private async Task<DateTime?> SuggestNextAvailableTimeAsync(AppointmentEditor editor)
    {
        var employee = await dbContext.Employees.AsNoTracking().FirstOrDefaultAsync(item => item.Id == editor.EmployeeId);
        if (employee is null)
        {
            return null;
        }

        var duration = editor.EndsAt - editor.StartsAt;
        var candidate = editor.StartsAt.AddMinutes(30);
        for (var i = 0; i < 80; i++)
        {
            var startTime = TimeOnly.FromDateTime(candidate);
            var endTime = TimeOnly.FromDateTime(candidate.Add(duration));
            if (startTime >= employee.WorkdayStart && endTime <= employee.WorkdayEnd)
            {
                var conflict = await dbContext.Appointments.AsNoTracking().AnyAsync(appointment =>
                    appointment.Id != editor.Id &&
                    appointment.EmployeeId == editor.EmployeeId &&
                    appointment.Status != AppointmentStatus.Cancelled &&
                    appointment.StartsAt < candidate.Add(duration) &&
                    candidate < appointment.EndsAt);
                var blocked = await dbContext.Availability.AsNoTracking().AnyAsync(block =>
                    block.EmployeeId == editor.EmployeeId &&
                    block.Status == AvailabilityStatus.TimeOff &&
                    block.StartsAt < candidate.Add(duration) &&
                    candidate < block.EndsAt);

                if (!conflict && !blocked)
                {
                    return candidate;
                }
            }

            candidate = candidate.AddMinutes(30);
            if (TimeOnly.FromDateTime(candidate) > employee.WorkdayEnd)
            {
                candidate = candidate.Date.AddDays(1).Add(employee.WorkdayStart.ToTimeSpan());
            }
        }

        return null;
    }

    private async Task BroadcastScheduleChangedAsync(string message)
    {
        await scheduleHub.Clients.All.SendAsync("ScheduleChanged", message);
    }
}

public sealed record DashboardSnapshot(
    int TodayAppointments,
    int WeeklyUtilization,
    int PendingRequests,
    int TeamAvailable,
    IReadOnlyList<CalendarEvent> UpcomingMeetings,
    IReadOnlyList<SchedulerNotification> Notifications);

public sealed record CalendarEvent(
    int Id,
    string Subject,
    string Customer,
    string Employee,
    DateTime StartsAt,
    DateTime EndsAt,
    AppointmentStatus Status,
    string Color);

public sealed record AppointmentListItem(
    int Id,
    string Subject,
    string Customer,
    string CustomerEmail,
    string Employee,
    string AppointmentType,
    string Location,
    DateTime StartsAt,
    DateTime EndsAt,
    AppointmentStatus Status,
    bool IsRecurring);

public sealed class AppointmentEditor
{
    public int Id { get; set; }
    public string Subject { get; set; } = "Discovery call";
    public string CustomerName { get; set; } = "New account";
    public string CustomerEmail { get; set; } = "contact@example.com";
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public bool IsRecurring { get; set; }
    public int EmployeeId { get; set; }
    public int AppointmentTypeId { get; set; }
    public int LocationId { get; set; }
}

public sealed record AppointmentFormOptions(
    IReadOnlyList<Employee> Employees,
    IReadOnlyList<AppointmentType> AppointmentTypes,
    IReadOnlyList<Location> Locations);

public sealed record ScheduleOperationResult(bool Success, string Message, DateTime? SuggestedStart = null)
{
    public static ScheduleOperationResult Ok(string message) => new(true, message);
    public static ScheduleOperationResult Fail(string message, DateTime? suggestedStart = null) => new(false, message, suggestedStart);
}

public sealed record TeamMemberSchedule(
    string Name,
    string Department,
    string WorkingHours,
    AvailabilityStatus Availability,
    int AppointmentsToday);

public sealed record SchedulerNotification(string Title, string Message, string Age);

public sealed record ReportMetric(string Label, int Value, string Trend);
