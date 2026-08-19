using Moser.Enterprise.Blueprint.People.Infrastructure;

using System.Linq;
using System.Threading.Tasks;

namespace Moser.Enterprise.Blueprint.People.Tests;

public sealed class InMemoryPeopleDirectoryTests
{
    [Fact]
    public async Task List_returns_seeded_employees_with_policy_topics()
    {
        var directory = new InMemoryPeopleDirectory();

        var people = await directory.ListAsync();

        Assert.Equal(8, people.Count);
        Assert.Contains(people, p => p.Email == "ava.chen@internal" && p.PolicyTopics.Contains("pto"));
        Assert.Contains(people, p => p.Email == "priya.shah@internal" && p.Department == "Security");
    }

    [Fact]
    public async Task Get_unknown_id_returns_null()
    {
        var directory = new InMemoryPeopleDirectory();

        var missing = await directory.GetAsync(System.Guid.Parse("00000000-0000-0000-0000-000000000001"));

        Assert.Null(missing);
    }
}
