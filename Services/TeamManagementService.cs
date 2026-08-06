using Microsoft.EntityFrameworkCore;
using smartscheduler.Data;
using smartscheduler.Data.Entities;

namespace smartscheduler.Services;

public interface ITeamManagementService
{
    Task<IReadOnlyList<Department>> GetDepartmentsAsync();
    Task<IReadOnlyList<Employee>> GetEmployeesAsync();
    Task<Department?> GetDepartmentAsync(int id);
    Task<Employee?> GetEmployeeAsync(int id);
    Task SaveDepartmentAsync(Department department);
    Task SaveEmployeeAsync(Employee employee);
    Task<DeleteResult> DeleteDepartmentAsync(int id);
    Task DeleteEmployeeAsync(int id);
}

public sealed class TeamManagementService(ApplicationDbContext dbContext) : ITeamManagementService
{
    public async Task<IReadOnlyList<Department>> GetDepartmentsAsync() =>
        await dbContext.Departments
            .AsNoTracking()
            .Include(department => department.Employees)
            .OrderBy(department => department.Name)
            .ToListAsync();

    public async Task<IReadOnlyList<Employee>> GetEmployeesAsync() =>
        await dbContext.Employees
            .AsNoTracking()
            .Include(employee => employee.Department)
            .OrderBy(employee => employee.FullName)
            .ToListAsync();

    public async Task<Department?> GetDepartmentAsync(int id) =>
        await dbContext.Departments.FindAsync(id);

    public async Task<Employee?> GetEmployeeAsync(int id) =>
        await dbContext.Employees.FindAsync(id);

    public async Task SaveDepartmentAsync(Department department)
    {
        if (department.Id == 0)
        {
            dbContext.Departments.Add(department);
        }
        else
        {
            dbContext.Departments.Update(department);
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task SaveEmployeeAsync(Employee employee)
    {
        if (employee.Id == 0)
        {
            dbContext.Employees.Add(employee);
        }
        else
        {
            dbContext.Employees.Update(employee);
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task<DeleteResult> DeleteDepartmentAsync(int id)
    {
        var department = await dbContext.Departments
            .Include(item => item.Employees)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (department is null)
        {
            return DeleteResult.NotFound;
        }

        if (department.Employees.Count > 0)
        {
            return DeleteResult.HasRelatedRecords;
        }

        dbContext.Departments.Remove(department);
        await dbContext.SaveChangesAsync();
        return DeleteResult.Deleted;
    }

    public async Task DeleteEmployeeAsync(int id)
    {
        var employee = await dbContext.Employees.FindAsync(id);
        if (employee is null)
        {
            return;
        }

        dbContext.Employees.Remove(employee);
        await dbContext.SaveChangesAsync();
    }
}

public enum DeleteResult
{
    Deleted,
    NotFound,
    HasRelatedRecords
}
