using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using smartscheduler.Data;
using smartscheduler.Data.Entities;
using smartscheduler.Hubs;

namespace smartscheduler.Services;

public interface IAdminConfigurationService
{
    Task<AdminConfigurationSummary> GetSummaryAsync();
    Task<IReadOnlyList<AppointmentType>> GetAppointmentTypesAsync();
    Task<AppointmentType> GetAppointmentTypeEditorAsync(int? id = null);
    Task SaveAppointmentTypeAsync(AppointmentType appointmentType);
    Task ToggleAppointmentTypeAsync(int id);
    Task<IReadOnlyList<Location>> GetLocationsAsync();
    Task<Location> GetLocationEditorAsync(int? id = null);
    Task SaveLocationAsync(Location location);
    Task ToggleLocationAsync(int id);
    Task<IReadOnlyList<BusinessHour>> GetBusinessHoursAsync();
    Task SaveBusinessHourAsync(BusinessHour businessHour);
    Task<IReadOnlyList<ClosureBlockItem>> GetClosureBlocksAsync();
    Task<ClosureBlock> GetClosureBlockEditorAsync(int? id = null);
    Task SaveClosureBlockAsync(ClosureBlock closureBlock);
    Task DeleteClosureBlockAsync(int id);
}

public sealed class AdminConfigurationService(
    ApplicationDbContext dbContext,
    IHubContext<ScheduleHub> scheduleHub) : IAdminConfigurationService
{
    public async Task<AdminConfigurationSummary> GetSummaryAsync() =>
        new(
            await dbContext.AppointmentTypes.CountAsync(),
            await dbContext.AppointmentTypes.CountAsync(type => type.IsActive),
            await dbContext.Locations.CountAsync(),
            await dbContext.Locations.CountAsync(location => location.IsActive),
            await dbContext.BusinessHours.CountAsync(),
            await dbContext.ClosureBlocks.CountAsync(),
            await dbContext.AuditLogs.AsNoTracking().OrderByDescending(log => log.CreatedAt).Select(log => (DateTime?)log.CreatedAt).FirstOrDefaultAsync());

    public async Task<IReadOnlyList<AppointmentType>> GetAppointmentTypesAsync() =>
        await dbContext.AppointmentTypes.AsNoTracking().OrderBy(type => type.Name).ToListAsync();

    public async Task<AppointmentType> GetAppointmentTypeEditorAsync(int? id = null) =>
        id is null
            ? new AppointmentType { DurationMinutes = 30, Color = "#14B8A6", IsActive = true }
            : await dbContext.AppointmentTypes.AsNoTracking().FirstOrDefaultAsync(type => type.Id == id.Value)
                ?? new AppointmentType { DurationMinutes = 30, Color = "#14B8A6", IsActive = true };

    public async Task SaveAppointmentTypeAsync(AppointmentType appointmentType)
    {
        var isNew = appointmentType.Id == 0;
        if (isNew)
        {
            dbContext.AppointmentTypes.Add(appointmentType);
        }
        else
        {
            dbContext.AppointmentTypes.Update(appointmentType);
        }

        await dbContext.SaveChangesAsync();
        await RecordConfigurationEventAsync("Appointment type saved", $"{appointmentType.Name} was {(isNew ? "created" : "updated")}.", "AppointmentType", isNew ? "Create" : "Edit");
    }

    public async Task ToggleAppointmentTypeAsync(int id)
    {
        var appointmentType = await dbContext.AppointmentTypes.FindAsync(id);
        if (appointmentType is null)
        {
            return;
        }

        appointmentType.IsActive = !appointmentType.IsActive;
        await dbContext.SaveChangesAsync();
        await RecordConfigurationEventAsync("Appointment type status changed", $"{appointmentType.Name} is now {(appointmentType.IsActive ? "active" : "inactive")}.", "AppointmentType", "Toggle");
    }

    public async Task<IReadOnlyList<Location>> GetLocationsAsync() =>
        await dbContext.Locations.AsNoTracking().OrderBy(location => location.Name).ToListAsync();

    public async Task<Location> GetLocationEditorAsync(int? id = null) =>
        id is null
            ? new Location { TimeZone = "America/New_York", IsActive = true }
            : await dbContext.Locations.AsNoTracking().FirstOrDefaultAsync(location => location.Id == id.Value)
                ?? new Location { TimeZone = "America/New_York", IsActive = true };

    public async Task SaveLocationAsync(Location location)
    {
        var isNew = location.Id == 0;
        if (isNew)
        {
            dbContext.Locations.Add(location);
        }
        else
        {
            dbContext.Locations.Update(location);
        }

        await dbContext.SaveChangesAsync();
        await RecordConfigurationEventAsync("Location saved", $"{location.Name} was {(isNew ? "created" : "updated")}.", "Location", isNew ? "Create" : "Edit");
    }

    public async Task ToggleLocationAsync(int id)
    {
        var location = await dbContext.Locations.FindAsync(id);
        if (location is null)
        {
            return;
        }

        location.IsActive = !location.IsActive;
        await dbContext.SaveChangesAsync();
        await RecordConfigurationEventAsync("Location status changed", $"{location.Name} is now {(location.IsActive ? "active" : "inactive")}.", "Location", "Toggle");
    }

    public async Task<IReadOnlyList<BusinessHour>> GetBusinessHoursAsync()
    {
        if (!await dbContext.BusinessHours.AnyAsync())
        {
            foreach (var day in Enum.GetValues<DayOfWeek>())
            {
                dbContext.BusinessHours.Add(new BusinessHour
                {
                    DayOfWeek = day,
                    OpensAt = new TimeOnly(8, 30),
                    ClosesAt = new TimeOnly(17, 0),
                    IsClosed = day is DayOfWeek.Saturday or DayOfWeek.Sunday
                });
            }
            await dbContext.SaveChangesAsync();
        }

        return await dbContext.BusinessHours
            .AsNoTracking()
            .Include(hour => hour.Location)
            .OrderBy(hour => hour.LocationId)
            .ThenBy(hour => hour.DayOfWeek)
            .ToListAsync();
    }

    public async Task SaveBusinessHourAsync(BusinessHour businessHour)
    {
        dbContext.BusinessHours.Update(businessHour);
        await dbContext.SaveChangesAsync();
        await RecordConfigurationEventAsync("Business hours updated", $"{businessHour.DayOfWeek} hours were updated.", "BusinessHours", "Edit");
    }

    public async Task<IReadOnlyList<ClosureBlockItem>> GetClosureBlocksAsync() =>
        await dbContext.ClosureBlocks
            .AsNoTracking()
            .Include(block => block.Location)
            .OrderBy(block => block.StartsAt)
            .Select(block => new ClosureBlockItem(
                block.Id,
                block.Location == null ? "Organization" : block.Location.Name,
                block.Scope,
                block.StartsAt,
                block.EndsAt,
                block.Reason))
            .ToListAsync();

    public async Task<ClosureBlock> GetClosureBlockEditorAsync(int? id = null) =>
        id is null
            ? new ClosureBlock
            {
                Scope = ClosureScope.Organization,
                StartsAt = DateTime.Today.AddDays(7).AddHours(8),
                EndsAt = DateTime.Today.AddDays(7).AddHours(17),
                Reason = "Office closure"
            }
            : await dbContext.ClosureBlocks.AsNoTracking().FirstOrDefaultAsync(block => block.Id == id.Value)
                ?? new ClosureBlock { Scope = ClosureScope.Organization, StartsAt = DateTime.Today.AddDays(7).AddHours(8), EndsAt = DateTime.Today.AddDays(7).AddHours(17), Reason = "Office closure" };

    public async Task SaveClosureBlockAsync(ClosureBlock closureBlock)
    {
        var isNew = closureBlock.Id == 0;
        if (closureBlock.Scope == ClosureScope.Organization)
        {
            closureBlock.LocationId = null;
        }

        if (isNew)
        {
            dbContext.ClosureBlocks.Add(closureBlock);
        }
        else
        {
            dbContext.ClosureBlocks.Update(closureBlock);
        }

        await dbContext.SaveChangesAsync();
        await RecordConfigurationEventAsync("Closure block saved", $"{closureBlock.Reason} was {(isNew ? "created" : "updated")}.", "ClosureBlock", isNew ? "Create" : "Edit");
    }

    public async Task DeleteClosureBlockAsync(int id)
    {
        var closureBlock = await dbContext.ClosureBlocks.FindAsync(id);
        if (closureBlock is null)
        {
            return;
        }

        dbContext.ClosureBlocks.Remove(closureBlock);
        await dbContext.SaveChangesAsync();
        await RecordConfigurationEventAsync("Closure block deleted", $"{closureBlock.Reason} was deleted.", "ClosureBlock", "Delete");
    }

    private async Task RecordConfigurationEventAsync(string title, string message, string entityName, string action)
    {
        dbContext.Notifications.Add(new Notification
        {
            Title = title,
            Message = message,
            CreatedAt = DateTime.UtcNow
        });
        dbContext.AuditLogs.Add(new AuditLog
        {
            EntityName = entityName,
            Action = action,
            Actor = "System",
            CreatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
        await scheduleHub.Clients.All.SendAsync("ScheduleChanged", title);
    }
}

public sealed record AdminConfigurationSummary(
    int AppointmentTypes,
    int ActiveAppointmentTypes,
    int Locations,
    int ActiveLocations,
    int BusinessHourRules,
    int ClosureBlocks,
    DateTime? LastUpdated);

public sealed record ClosureBlockItem(
    int Id,
    string Location,
    ClosureScope Scope,
    DateTime StartsAt,
    DateTime EndsAt,
    string Reason);
