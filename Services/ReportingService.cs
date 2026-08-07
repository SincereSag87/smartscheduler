using System.Text;
using Microsoft.EntityFrameworkCore;
using smartscheduler.Data;
using smartscheduler.Data.Entities;

namespace smartscheduler.Services;

public interface IReportingService
{
    Task<ReportSnapshot> GetReportAsync(ReportFilter filter);
    Task<string> ExportCsvAsync(ReportFilter filter);
}

public sealed class ReportingService(ApplicationDbContext dbContext) : IReportingService
{
    public async Task<ReportSnapshot> GetReportAsync(ReportFilter filter)
    {
        var normalized = filter.Normalize();
        var appointments = await BuildQuery(normalized)
            .Select(appointment => new AppointmentReportRow(
                appointment.Id,
                appointment.Subject,
                appointment.CustomerName,
                appointment.Employee == null ? "Unassigned" : appointment.Employee.FullName,
                appointment.Employee == null || appointment.Employee.Department == null ? "Unassigned" : appointment.Employee.Department.Name,
                appointment.AppointmentType == null ? "General" : appointment.AppointmentType.Name,
                appointment.Location == null ? "Unassigned" : appointment.Location.Name,
                appointment.StartsAt,
                appointment.EndsAt,
                appointment.Status))
            .ToListAsync();

        var total = appointments.Count;
        var completed = appointments.Count(appointment => appointment.Status == AppointmentStatus.Completed);
        var cancelled = appointments.Count(appointment => appointment.Status == AppointmentStatus.Cancelled);
        var pending = appointments.Count(appointment => appointment.Status == AppointmentStatus.Pending);
        var today = DateTime.Today;

        return new ReportSnapshot(
            Filter: normalized,
            Summary: new ReportSummary(
                DailyAppointments: appointments.Count(appointment => appointment.StartsAt.Date == today),
                WeeklyUtilization: total == 0 ? 0 : Percent(completed, total),
                MonthlyCompletion: total == 0 ? 0 : Percent(completed, total),
                CancelledAppointments: cancelled,
                PendingAppointments: pending,
                EmployeeWorkload: total),
            MonthlyTrends: BuildMonthlyTrends(appointments),
            EmployeeUtilization: BuildEmployeeUtilization(appointments),
            StatusBreakdown: BuildStatusBreakdown(appointments),
            DepartmentSummaries: BuildDepartmentSummaries(appointments),
            Rows: appointments);
    }

    public async Task<string> ExportCsvAsync(ReportFilter filter)
    {
        var report = await GetReportAsync(filter);
        var builder = new StringBuilder();
        builder.AppendLine("Subject,Customer,Employee,Department,Type,Location,StartsAt,EndsAt,Status");

        foreach (var row in report.Rows)
        {
            builder.AppendLine(string.Join(',', [
                Csv(row.Subject),
                Csv(row.Customer),
                Csv(row.Employee),
                Csv(row.Department),
                Csv(row.AppointmentType),
                Csv(row.Location),
                Csv(row.StartsAt.ToString("s")),
                Csv(row.EndsAt.ToString("s")),
                Csv(row.Status.ToString())
            ]));
        }

        return builder.ToString();
    }

    private IQueryable<Appointment> BuildQuery(ReportFilter filter)
    {
        var query = dbContext.Appointments
            .AsNoTracking()
            .Include(appointment => appointment.Employee)
            .ThenInclude(employee => employee!.Department)
            .Include(appointment => appointment.AppointmentType)
            .Include(appointment => appointment.Location)
            .Where(appointment => appointment.StartsAt >= filter.StartDate && appointment.StartsAt < filter.EndDate.AddDays(1));

        if (filter.DepartmentId is not null)
        {
            query = query.Where(appointment => appointment.Employee != null && appointment.Employee.DepartmentId == filter.DepartmentId.Value);
        }

        if (filter.EmployeeId is not null)
        {
            query = query.Where(appointment => appointment.EmployeeId == filter.EmployeeId.Value);
        }

        if (filter.Status is not null)
        {
            query = query.Where(appointment => appointment.Status == filter.Status.Value);
        }

        return query.OrderBy(appointment => appointment.StartsAt);
    }

    private static IReadOnlyList<MonthlyTrend> BuildMonthlyTrends(IReadOnlyList<AppointmentReportRow> appointments) =>
        appointments
            .GroupBy(appointment => new DateTime(appointment.StartsAt.Year, appointment.StartsAt.Month, 1))
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var total = group.Count();
                var completed = group.Count(appointment => appointment.Status == AppointmentStatus.Completed);
                var cancelled = group.Count(appointment => appointment.Status == AppointmentStatus.Cancelled);
                return new MonthlyTrend(group.Key.ToString("MMM yyyy"), total, completed, cancelled, total == 0 ? 0 : Percent(completed, total));
            })
            .ToList();

    private static IReadOnlyList<EmployeeUtilization> BuildEmployeeUtilization(IReadOnlyList<AppointmentReportRow> appointments) =>
        appointments
            .GroupBy(appointment => appointment.Employee)
            .OrderByDescending(group => group.Count())
            .Select(group =>
            {
                var total = group.Count();
                var completed = group.Count(appointment => appointment.Status == AppointmentStatus.Completed);
                return new EmployeeUtilization(group.Key, total, completed, total == 0 ? 0 : Percent(completed, total));
            })
            .ToList();

    private static IReadOnlyList<StatusBreakdown> BuildStatusBreakdown(IReadOnlyList<AppointmentReportRow> appointments) =>
        Enum.GetValues<AppointmentStatus>()
            .Select(status => new StatusBreakdown(status, appointments.Count(appointment => appointment.Status == status)))
            .ToList();

    private static IReadOnlyList<DepartmentSummary> BuildDepartmentSummaries(IReadOnlyList<AppointmentReportRow> appointments) =>
        appointments
            .GroupBy(appointment => appointment.Department)
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var total = group.Count();
                return new DepartmentSummary(
                    group.Key,
                    total,
                    group.Select(appointment => appointment.Employee).Distinct().Count(),
                    total == 0 ? 0 : Percent(group.Count(appointment => appointment.Status == AppointmentStatus.Completed), total));
            })
            .ToList();

    private static int Percent(int numerator, int denominator) =>
        Math.Clamp((int)Math.Round(numerator / (double)Math.Max(denominator, 1) * 100), 0, 100);

    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
}

public sealed record ReportFilter(
    DateTime StartDate,
    DateTime EndDate,
    int? DepartmentId,
    int? EmployeeId,
    AppointmentStatus? Status)
{
    public ReportFilter Normalize()
    {
        var start = StartDate == default ? DateTime.Today.AddDays(-30) : StartDate.Date;
        var end = EndDate == default ? DateTime.Today.AddDays(30) : EndDate.Date;
        return end < start
            ? this with { StartDate = end, EndDate = start }
            : this with { StartDate = start, EndDate = end };
    }
}

public sealed record ReportSnapshot(
    ReportFilter Filter,
    ReportSummary Summary,
    IReadOnlyList<MonthlyTrend> MonthlyTrends,
    IReadOnlyList<EmployeeUtilization> EmployeeUtilization,
    IReadOnlyList<StatusBreakdown> StatusBreakdown,
    IReadOnlyList<DepartmentSummary> DepartmentSummaries,
    IReadOnlyList<AppointmentReportRow> Rows);

public sealed record ReportSummary(
    int DailyAppointments,
    int WeeklyUtilization,
    int MonthlyCompletion,
    int CancelledAppointments,
    int PendingAppointments,
    int EmployeeWorkload);

public sealed record MonthlyTrend(string Month, int Appointments, int Completed, int Cancelled, int Utilization);

public sealed record EmployeeUtilization(string Employee, int Appointments, int Completed, int Utilization);

public sealed record StatusBreakdown(AppointmentStatus Status, int Count);

public sealed record DepartmentSummary(string Department, int Appointments, int Employees, int CompletionRate);

public sealed record AppointmentReportRow(
    int Id,
    string Subject,
    string Customer,
    string Employee,
    string Department,
    string AppointmentType,
    string Location,
    DateTime StartsAt,
    DateTime EndsAt,
    AppointmentStatus Status);
