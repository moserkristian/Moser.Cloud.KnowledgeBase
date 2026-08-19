using Moser.Enterprise.Blueprint.Assistant.Infrastructure;
using Moser.Enterprise.Blueprint.Web;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using System;
using Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisOutputCache("cache");
builder.AddAssistant();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient<PeopleApiClient>(client =>
{
    var peopleApiUrl = builder.Configuration["PeopleApi:BaseUrl"];

    if (!string.IsNullOrEmpty(peopleApiUrl))
    {
        client.BaseAddress = new Uri(peopleApiUrl);
    }
    else
    {
        client.BaseAddress = new Uri("https+http://people-api");
    }
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
