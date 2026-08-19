using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Moser.Enterprise.Blueprint.Web;

public sealed class PeopleApiClient(HttpClient httpClient)
{
    public async Task<EmployeeDto[]> GetEmployeesAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetFromJsonAsync<EmployeeDto[]>(
            "/api/v1/employees",
            cancellationToken).ConfigureAwait(false);

        return response ?? [];
    }
}

public sealed record EmployeeDto(
    Guid Id,
    string DisplayName,
    string Email,
    string Title,
    string Department,
    string Location,
    string? ManagerEmail,
    IReadOnlyList<string> PolicyTopics);
