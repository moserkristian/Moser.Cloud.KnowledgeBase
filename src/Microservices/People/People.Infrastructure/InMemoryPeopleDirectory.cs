using Moser.Enterprise.Blueprint.People.Application;
using Moser.Enterprise.Blueprint.People.Domain;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Moser.Enterprise.Blueprint.People.Infrastructure;

public sealed class InMemoryPeopleDirectory : IPeopleDirectory
{
    private static readonly IReadOnlyList<Employee> Seed = CreateSeed();

    public Task<IReadOnlyList<EmployeeDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<EmployeeDto> result = Seed.Select(e => e.ToDto()).ToList();
        return Task.FromResult(result);
    }

    public Task<EmployeeDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var employee = Seed.FirstOrDefault(e => e.Id == id);
        return Task.FromResult(employee?.ToDto());
    }

    private static IReadOnlyList<Employee> CreateSeed()
    {
        var ava = Guid.Parse("6f1c2c3a-0d4e-4b1a-9c11-111111111111");
        var tomas = Guid.Parse("6f1c2c3a-0d4e-4b1a-9c11-444444444444");
        var priya = Guid.Parse("6f1c2c3a-0d4e-4b1a-9c11-555555555555");

        return
        [
            new Employee(ava, "Ava Chen", "ava.chen@internal", "Director of People", "HR", "Prague", null, ["pto", "conduct", "remote"]),
            new Employee(Guid.Parse("6f1c2c3a-0d4e-4b1a-9c11-222222222222"), "Marek Novak", "marek.novak@internal", "Payroll lead", "HR", "Bratislava", "ava.chen@internal", ["pto", "expense"]),
            new Employee(Guid.Parse("6f1c2c3a-0d4e-4b1a-9c11-333333333333"), "Sofia Alvarez", "sofia.alvarez@internal", "General Counsel", "Legal", "Madrid", null, ["insider", "gift", "conduct"]),
            new Employee(tomas, "Tomas Kral", "tomas.kral@internal", "CFO", "Finance", "Prague", null, ["expense", "procurement", "refund"]),
            new Employee(priya, "Priya Shah", "priya.shah@internal", "CISO", "Security", "London", null, ["password", "information-security"]),
            new Employee(Guid.Parse("6f1c2c3a-0d4e-4b1a-9c11-666666666666"), "Jonas Berg", "jonas.berg@internal", "Head of Support", "Support", "Stockholm", null, ["sla", "returns"]),
            new Employee(Guid.Parse("6f1c2c3a-0d4e-4b1a-9c11-777777777777"), "Elena Rossi", "elena.rossi@internal", "Vendor manager", "Procurement", "Milan", "tomas.kral@internal", ["vendor", "procurement"]),
            new Employee(Guid.Parse("6f1c2c3a-0d4e-4b1a-9c11-888888888888"), "Chris Walsh", "chris.walsh@internal", "IT service desk", "IT", "Dublin", "priya.shah@internal", ["password", "vendor"])
        ];
    }
}
