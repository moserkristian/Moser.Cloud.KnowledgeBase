using System.Threading.Tasks;

namespace Moser.Enterprise.Blueprint.Tests;

[Collection("DistributedAppTestCollection")]
public class AspireOrchestratedTests(DistributedAppTestFixture fixture)
{
    [Fact]
    public async Task HealthCheck_ValidRequest_ShouldReturnResponse()
    {
        var response = await fixture.HttpClient!.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
