using WiseWork.Components;
using WiseWork.Models;
using WiseWork.Services;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.AspNetCore.SignalR;
using WiseWork.Hubs;
using WiseWork.Components.Pages;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<RoomService>();

builder.Services.AddMudServices();
builder.Services.AddSignalR();
builder.Services.AddSingleton<MongoDBService>();
builder.Services.AddHttpClient("LocalApi", client =>
{
    client.BaseAddress = new Uri("https://localhost:7113/"); // Replace with your actual base URL
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();
app.MapHub<RoomHub>("/roomHub");

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
