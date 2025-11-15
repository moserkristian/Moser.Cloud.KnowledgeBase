using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var exampleApi = builder.AddProject<Projects.Example_API>("example-api");

var catalogApi = builder.AddProject<Projects.Catalog_API>("catalog-api");

builder.AddProject<Projects.Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(catalogApi)
    .WaitFor(catalogApi);

builder.Build().Run();
