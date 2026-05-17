using Microsoft.EntityFrameworkCore;
using Twf.Flow.Web.Data;
using Twf.Flow.Web.Repositories;
using Twf.Flow.Web.Services;
using Twf.Flow.Web.Services.NodeFactory;
using Twf.Flow.Web.Services.VariableResolution;
using Twf.Flow.Web.Services.Execution;
using Twf.Flow.Web.Services.GraphWalker;
using Twf.Flow.Web.Middleware;
using Twf.Flow.Web.Services.Database;
using Twf.Flow.Core.Http;

var builder = WebApplication.CreateBuilder(args);

// Configure structured logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.FormatterName = "json";
});
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
    options.JsonWriterOptions = new System.Text.Json.JsonWriterOptions
    {
        Indented = false
    };
});
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // Serialize enums as strings instead of integers
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// TODO: Enable FluentValidation once package compatibility is confirmed for .NET 10
// Register FluentValidation validators
// builder.Services.AddScoped<IValidator<WorkflowDefinition>, WorkflowDefinitionValidator>();
// builder.Services.AddScoped<IValidator<WorkflowRunRequest>, WorkflowRunRequestValidator>();
// builder.Services.AddScoped<IValidator<NodeTypeEntity>, NodeTypeEntityValidator>();

// Register global exception handler and ProblemDetails support
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Register pooled HTTP client provider (optimized for AI API calls)
builder.Services.AddSingleton<IHttpClientProvider>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<PooledHttpClientProvider>>();
    return new PooledHttpClientProvider(
        clientLifetime: TimeSpan.FromMinutes(2), // DNS refresh every 2 minutes
        logger: logger);
});

// Register workflow execution services (Refactored architecture)
builder.Services.AddSingleton<IVariableResolver, TemplateVariableResolver>();
builder.Services.AddSingleton<INodeFactory, ReflectionNodeFactory>();
builder.Services.AddSingleton<Twf.Flow.Web.Services.Schema.INodeSchemaProvider, Twf.Flow.Web.Services.Schema.ReflectionNodeSchemaProvider>();
builder.Services.AddScoped<INodeExecutor, RetryableNodeExecutor>();
builder.Services.AddScoped<IWorkflowGraphWalker, WorkflowGraphWalker>();
builder.Services.AddScoped<WorkflowDefinitionRunner>();

// Register database migration service
builder.Services.AddScoped<IDatabaseMigrationService, DatabaseMigrationService>();
builder.Services.AddScoped<Twf.Flow.Web.Services.Seeding.INodeTypeSeeder, Twf.Flow.Web.Services.Seeding.NodeTypeSeederService>();

// Register Unit of Work pattern
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// SQLite is the only supported storage backend.
var connectionString = builder.Configuration.GetConnectionString("WorkflowDb")
    ?? "Data Source=workflows.db";

builder.Services.AddDbContext<WorkflowDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<IWorkflowRepository, SqliteWorkflowRepository>();
builder.Services.AddScoped<INodeTypeRepository, SqliteNodeTypeRepository>();
builder.Services.AddScoped<IWorkflowInstanceRepository, SqliteWorkflowInstanceRepository>();

var app = builder.Build();

// Run database migrations and seeding on startup
using (var scope = app.Services.CreateScope())
{
    var migrationService = scope.ServiceProvider.GetRequiredService<IDatabaseMigrationService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

    try
    {
        await migrationService.MigrateSchemaAsync();
        await migrationService.SeedDataAsync();
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Database migration or seeding failed - application cannot start");
        throw;
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Add correlation ID middleware (before exception handler)
app.UseMiddleware<CorrelationIdMiddleware>();

// Add global exception handler
app.UseExceptionHandler();

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "workflow_subflow_designer",
    pattern: "{id:guid}/{subWorkflowId:guid}",
    defaults: new { controller = "Workflow", action = "Designer" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Workflow}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
