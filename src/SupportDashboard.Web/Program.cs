using SupportDashboard.Core.Interfaces;
using SupportDashboard.Core.Models;
using SupportDashboard.Infrastructure.InMemory;
using SupportDashboard.Infrastructure.Simulation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Sample data until the ServiceNow/Azure sync jobs and the SQLite database are built.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IIncidentRepository, InMemoryIncidentRepository>();
builder.Services.AddSingleton<IRunRepository<FunctionExecution>, InMemoryFunctionExecutionRepository>();
builder.Services.AddSingleton<IRunRepository<PipelineRun>, InMemoryPipelineRunRepository>();
builder.Services.AddSingleton<IRunRepository<WorkflowRun>, InMemoryWorkflowRunRepository>();
builder.Services.AddSingleton<IncidentSimulator>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
