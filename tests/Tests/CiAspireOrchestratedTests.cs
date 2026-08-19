using System.Threading.Tasks;

namespace Moser.Enterprise.Blueprint.Tests;

[CollectionDefinition("CiDistributedAppTestCollection")]
public sealed class CiDistributedAppTestCollection : ICollectionFixture<CiDistributedAppTestFixture>
{
}

[Collection("CiDistributedAppTestCollection")]
public class CiAspireOrchestratedTests(CiDistributedAppTestFixture fixture)
{
    [Fact]
    public async Task HealthCheck_ValidRequest_ShouldReturnResponse()
    {
        var response = await fixture.HttpClient!.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
