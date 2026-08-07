using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using smartscheduler.Data;
using smartscheduler.Data.Entities;
using smartscheduler.Hubs;

namespace smartscheduler.Services;

public interface IAvailabilityService
{
    Task<IReadOnlyList<AvailabilityListItem>> GetAvailabilityAsync(DateTime? startsOnOrAfter = null, DateTime? endsBefore = null);
    Task<AvailabilityEditor> GetAvailabilityEditorAsync(int? id = null);
    Task<AvailabilityOperationResult> SaveAvailabilityAsync(AvailabilityEditor editor);
    Task DeleteAvailabilityAsync(int id);
}

public sealed class AvailabilityService(
    ApplicationDbContext dbContext,
    IHubContext<ScheduleHub> scheduleHub) : IAvailabilityService
{
    public async Task<IReadOnlyList<AvailabilityListItem>> GetAvailabilityAsync(DateTime? startsOnOrAfter = null, DateTime? endsBefore = null)
    {
        var query = dbContext.Availability
            .AsNoTracking()
            .Include(block => block.Employee)
            .OrderBy(block => block.StartsAt)
            .AsQueryable();

        if (startsOnOrAfter.HasValue)
        {
            query = query.Where(block => block.EndsAt >= startsOnOrAfter.Value);
        }

        if (endsBefore.HasValue)
        {
            query = query.Where(block => block.StartsAt < endsBefore.Value);
        }

        return await query.Select(block => new AvailabilityListItem(
                block.Id,
                block.Employee == null ? "Unassigned" : block.Employee.FullName,
                block.EmployeeId,
                block.StartsAt,
                block.EndsAt,
                block.Status,
                block.Note))
            .ToListAsync();
    }

    public async Task<AvailabilityEditor> GetAvailabilityEditorAsync(int? id = null)
    {
        if (id is null)
        {
            return new AvailabilityEditor
            {
                EmployeeId = await dbContext.Employees.AsNoTracking().Where(employee => employee.IsActive).OrderBy(employee => employee.FullName).Select(employee => employee.Id).FirstOrDefaultAsync(),
                StartsAt = DateTime.Today.AddDays(1).AddHours(12),
                EndsAt = DateTime.Today.AddDays(1).AddHours(13),
                Status = AvailabilityStatus.TimeOff,
                Note = "Time off"
            };
        }

        var block = await dbContext.Availability.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id.Value);
        if (block is null)
        {
            return await GetAvailabilityEditorAsync();
        }

        return new AvailabilityEditor
        {
            Id = block.Id,
            EmployeeId = block.EmployeeId,
            StartsAt = block.StartsAt,
            EndsAt = block.EndsAt,
            Status = block.Status,
            Note = block.Note
        };
    }

    public async Task<AvailabilityOperationResult> SaveAvailabilityAsync(AvailabilityEditor editor)
    {
        if (editor.EmployeeId == 0)
        {
            return AvailabilityOperationResult.Fail("Choose an employee.");
        }

        if (editor.EndsAt <= editor.StartsAt)
        {
            return AvailabilityOperationResult.Fail("End time must be after start time.");
        }

        var employeeExists = await dbContext.Employees.AnyAsync(employee => employee.Id == editor.EmployeeId);
        if (!employeeExists)
        {
            return AvailabilityOperationResult.Fail("Employee was not found.");
        }

        var block = editor.Id == 0
            ? new AvailabilityBlock()
            : await dbContext.Availability.FindAsync(editor.Id) ?? new AvailabilityBlock { Id = editor.Id };

        block.EmployeeId = editor.EmployeeId;
        block.StartsAt = editor.StartsAt;
        block.EndsAt = editor.EndsAt;
        block.Status = editor.Status;
        block.Note = editor.Note.Trim();

        var isNew = block.Id == 0;
        if (isNew)
        {
            dbContext.Availability.Add(block);
        }
        else
        {
            dbContext.Availability.Update(block);
        }

        await dbContext.SaveChangesAsync();
        await RecordOperationalEventAsync(
            "Availability updated",
            $"{block.Status} block was {(isNew ? "created" : "updated")} for {block.StartsAt:MMM d, h:mm tt}.",
            isNew ? "Create" : "Edit");
        await scheduleHub.Clients.All.SendAsync("ScheduleChanged", "Availability updated.");
        return AvailabilityOperationResult.Ok("Availability saved.");
    }

    public async Task DeleteAvailabilityAsync(int id)
    {
        var block = await dbContext.Availability.FindAsync(id);
        if (block is null)
        {
            return;
        }

        dbContext.Availability.Remove(block);
        await dbContext.SaveChangesAsync();
        await RecordOperationalEventAsync(
            "Availability removed",
            $"{block.Status} block was deleted.",
            "Delete");
        await scheduleHub.Clients.All.SendAsync("ScheduleChanged", "Availability removed.");
    }

    private async Task RecordOperationalEventAsync(string title, string message, string action)
    {
        dbContext.Notifications.Add(new Notification
        {
            Title = title,
            Message = message,
            CreatedAt = DateTime.UtcNow
        });
        dbContext.AuditLogs.Add(new AuditLog
        {
            EntityName = "Availability",
            Action = action,
            Actor = "System",
            CreatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
    }
}

public sealed record AvailabilityListItem(
    int Id,
    string Employee,
    int EmployeeId,
    DateTime StartsAt,
    DateTime EndsAt,
    AvailabilityStatus Status,
    string Note);

public sealed class AvailabilityEditor
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public AvailabilityStatus Status { get; set; }
    public string Note { get; set; } = string.Empty;
}

public sealed record AvailabilityOperationResult(bool Success, string Message)
{
    public static AvailabilityOperationResult Ok(string message) => new(true, message);
    public static AvailabilityOperationResult Fail(string message) => new(false, message);
}
