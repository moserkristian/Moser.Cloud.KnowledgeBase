using Moser.RagAi.Assistant.Application;
using Moser.RagAi.Assistant.Infrastructure;
using Moser.RagAi.Web.Components;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using System;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddAssistant();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapGet("/inbox/file/{fileName}", (string fileName, HttpRequest request, IAssistantWorkspace workspace) =>
{
    var path = workspace.ResolveSourcePath(fileName);
    if (path is null || !File.Exists(path))
    {
        return Results.NotFound();
    }

    var ext = Path.GetExtension(path);
    var type = ext.ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".doc" => "application/msword",
        ".rtf" => "application/rtf",
        ".eml" => "message/rfc822",
        ".txt" => "text/plain; charset=utf-8",
        _ => "application/octet-stream"
    };

    // Preview stays inline (no fileDownloadName). ?download=1 forces attachment.
    var download = request.Query.ContainsKey("download");
    return download
        ? Results.File(path, type, fileDownloadName: Path.GetFileName(path), enableRangeProcessing: true)
        : Results.File(path, type, enableRangeProcessing: true);
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
