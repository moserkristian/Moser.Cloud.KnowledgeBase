using Moser.Enterprise.Blueprint.People.Domain;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Moser.Enterprise.Blueprint.People.Application;

public sealed record EmployeeDto(
    Guid Id,
    string DisplayName,
    string Email,
    string Title,
    string Department,
    string Location,
    string? ManagerEmail,
    IReadOnlyList<string> PolicyTopics);

public interface IPeopleDirectory
{
    Task<IReadOnlyList<EmployeeDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<EmployeeDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
}

public static class EmployeeMapping
{
    public static EmployeeDto ToDto(this Employee employee) => new(
        employee.Id,
        employee.DisplayName,
        employee.Email,
        employee.Title,
        employee.Department,
        employee.Location,
        employee.ManagerEmail,
        employee.PolicyTopics);
}
