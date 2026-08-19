using System;
using System.Collections.Generic;

namespace Moser.Enterprise.Blueprint.People.Domain;

public sealed class Employee
{
    public Employee(
        Guid id,
        string displayName,
        string email,
        string title,
        string department,
        string location,
        string? managerEmail,
        IReadOnlyList<string> policyTopics)
    {
        Id = id;
        DisplayName = displayName;
        Email = email;
        Title = title;
        Department = department;
        Location = location;
        ManagerEmail = managerEmail;
        PolicyTopics = policyTopics;
    }

    public Guid Id { get; }
    public string DisplayName { get; }
    public string Email { get; }
    public string Title { get; }
    public string Department { get; }
    public string Location { get; }
    public string? ManagerEmail { get; }
    public IReadOnlyList<string> PolicyTopics { get; }
}
