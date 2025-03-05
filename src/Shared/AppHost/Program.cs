var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var exampleApi = builder.AddProject<Projects.Example_Api>("example-api");

var identityApi = builder.AddProject<Projects.Identity_Api>("identity-api");

builder.AddProject<Projects.Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(identityApi)
    .WaitFor(identityApi);

builder.Build().Run();
