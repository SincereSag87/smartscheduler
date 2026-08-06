using smartscheduler.Data.Entities;

namespace smartscheduler.Services;

public interface IScheduleRepository
{
    DashboardSnapshot GetDashboard();
    IReadOnlyList<CalendarEvent> GetCalendarEvents();
    IReadOnlyList<TeamMemberSchedule> GetTeamSchedules();
    IReadOnlyList<ReportMetric> GetReportMetrics();
}

public sealed class ScheduleDashboardService : IScheduleRepository
{
    private static readonly DateTime Today = DateTime.Today;

    public DashboardSnapshot GetDashboard()
    {
        var events = GetCalendarEvents();
        return new DashboardSnapshot(
            TodayAppointments: events.Count(e => e.StartsAt.Date == Today),
            WeeklyUtilization: 82,
            PendingRequests: events.Count(e => e.Status == AppointmentStatus.Pending),
            TeamAvailable: GetTeamSchedules().Count(e => e.Availability == AvailabilityStatus.Available),
            UpcomingMeetings: events.OrderBy(e => e.StartsAt).Take(5).ToList(),
            Notifications:
            [
                new("Reminder queued", "12 appointment reminders scheduled for this afternoon.", "2 min ago"),
                new("Room changed", "Executive Briefing moved to Virtual.", "18 min ago"),
                new("Coverage alert", "Operations has one open scheduling gap tomorrow.", "41 min ago")
            ]);
    }

    public IReadOnlyList<CalendarEvent> GetCalendarEvents() =>
    [
        new(1, "Client intake", "Avery Johnson", "Maya Chen", Today.AddHours(9), Today.AddHours(9.5), AppointmentStatus.Confirmed, "#22C55E"),
        new(2, "Implementation review", "Northwind Traders", "Diego Ramos", Today.AddHours(10), Today.AddHours(11), AppointmentStatus.Scheduled, "#3B82F6"),
        new(3, "Renewal planning", "Contoso Health", "Priya Shah", Today.AddHours(13), Today.AddHours(14), AppointmentStatus.Pending, "#F59E0B"),
        new(4, "Executive briefing", "Fabrikam", "Lena Ortiz", Today.AddDays(1).AddHours(11), Today.AddDays(1).AddHours(11.75), AppointmentStatus.Completed, "#8B5CF6"),
        new(5, "Reschedule request", "Alpine Ski House", "Maya Chen", Today.AddDays(2).AddHours(15), Today.AddDays(2).AddHours(15.5), AppointmentStatus.Cancelled, "#EF4444")
    ];

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

public sealed record TeamMemberSchedule(
    string Name,
    string Department,
    string WorkingHours,
    AvailabilityStatus Availability,
    int AppointmentsToday);

public sealed record SchedulerNotification(string Title, string Message, string Age);

public sealed record ReportMetric(string Label, int Value, string Trend);
