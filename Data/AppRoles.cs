namespace smartscheduler.Data;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Scheduler = "Scheduler";
    public const string Employee = "Employee";

    public const string AdminOnly = Admin;
    public const string ManagerOrAdmin = $"{Admin},{Manager}";
    public const string SchedulerOrManagerOrAdmin = $"{Admin},{Manager},{Scheduler}";
}
