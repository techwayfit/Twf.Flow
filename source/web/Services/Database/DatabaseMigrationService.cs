using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Twf.Flow.Web.Data;
using Twf.Flow.Web.Services;
using Twf.Flow.Web.Services.Seeding;

namespace Twf.Flow.Web.Services.Database;

/// <summary>
/// Handles database migrations and seeding for the workflow application.
/// Responsible for creating schema, adding columns, and importing initial data.
/// All operations are idempotent and safe to run on every application startup.
/// </summary>
public class DatabaseMigrationService : IDatabaseMigrationService
{
    private readonly WorkflowDbContext _context;
    private readonly INodeTypeSeeder _nodeTypeSeeder;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseMigrationService> _logger;

 public DatabaseMigrationService(
      WorkflowDbContext context,
INodeTypeSeeder nodeTypeSeeder,
  IConfiguration configuration,
        ILogger<DatabaseMigrationService> logger)
    {
   _context = context;
   _nodeTypeSeeder = nodeTypeSeeder;
   _configuration = configuration;
     _logger = logger;
    }

    public async Task MigrateSchemaAsync()
    {
        _logger.LogInformation("Starting database schema migration");

        try
        {
      // Ensure basic database and tables are created
            _context.Database.EnsureCreated();
          _logger.LogInformation("Database created or verified to exist");

            // Apply additive schema changes
            await CreateWorkflowInstancesTableAsync();
      await AddNodeTypeColumnsAsync();

         _logger.LogInformation("Database schema migration completed successfully");
 }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database schema migration failed");
    throw;
 }
    }

    public async Task SeedDataAsync()
    {
        _logger.LogInformation("Starting database data seeding");

try
        {
            // Seed built-in node type definitions
 await SeedNodeTypesAsync();

            // Import workflow JSON files from disk
     await ImportWorkflowsFromDirectoryAsync();

       _logger.LogInformation("Database data seeding completed successfully");
      }
 catch (Exception ex)
  {
            _logger.LogError(ex, "Database data seeding failed");
     throw;
        }
    }

    /// <summary>
    /// Creates the WorkflowInstances table if it doesn't exist.
    /// This is an additive change that won't affect existing databases.
    /// </summary>
    private async Task CreateWorkflowInstancesTableAsync()
    {
        _logger.LogDebug("Creating WorkflowInstances table if not exists");

     try
        {
       await _context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS WorkflowInstances (
         Id              TEXT    NOT NULL PRIMARY KEY,
        WorkflowDefinitionId TEXT    NOT NULL,
        WorkflowName         TEXT  NOT NULL,
                  Status        TEXT    NOT NULL,
           StartedAt            TEXT    NOT NULL,
    CompletedAt      TEXT    NULL,
JsonData      TEXT    NOT NULL
   );
 CREATE INDEX IF NOT EXISTS IX_WorkflowInstances_WorkflowDefinitionId
          ON WorkflowInstances (WorkflowDefinitionId);
    CREATE INDEX IF NOT EXISTS IX_WorkflowInstances_StartedAt
            ON WorkflowInstances (StartedAt);
        CREATE INDEX IF NOT EXISTS IX_WorkflowInstances_Status
   ON WorkflowInstances (Status);
     ");

       _logger.LogDebug("WorkflowInstances table and indexes created successfully");
  }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error creating WorkflowInstances table (may already exist)");
    // Non-fatal: table might already exist
        }
    }

 /// <summary>
    /// Adds new columns to NodeTypes table if they don't exist.
    /// SQLite doesn't support IF NOT EXISTS for ALTER TABLE, so we catch exceptions.
    /// </summary>
    private async Task AddNodeTypeColumnsAsync()
    {
        _logger.LogDebug("Adding columns to NodeTypes table if not exists");

     // Add IdPrefix column
        try
        {
      await _context.Database.ExecuteSqlRawAsync(
          "ALTER TABLE NodeTypes ADD COLUMN IdPrefix TEXT NOT NULL DEFAULT 'node'");
            _logger.LogDebug("Added IdPrefix column to NodeTypes table");
    }
        catch (Exception)
        {
       _logger.LogDebug("IdPrefix column already exists in NodeTypes table");
        }

   // Add FullTypeName column
        try
      {
await _context.Database.ExecuteSqlRawAsync(
       "ALTER TABLE NodeTypes ADD COLUMN FullTypeName TEXT");
  _logger.LogDebug("Added FullTypeName column to NodeTypes table");
}
        catch (Exception)
        {
  _logger.LogDebug("FullTypeName column already exists in NodeTypes table");
     }
    }

    /// <summary>
    /// Seeds built-in node type definitions.
    /// Re-seeds if IdPrefix is missing from existing types.
 /// </summary>
    private async Task SeedNodeTypesAsync()
    {
    _logger.LogInformation("Seeding node type definitions");

        try
        {
     await _nodeTypeSeeder.SeedAsync();
   _logger.LogInformation("Node type definitions seeded successfully");
    }
  catch (Exception ex)
  {
    _logger.LogError(ex, "Failed to seed node type definitions");
       throw;
        }
    }

    /// <summary>
    /// Imports workflow definitions from JSON files in the configured directory.
    /// Skips workflows that have already been imported (matched by ID).
    /// </summary>
    private async Task ImportWorkflowsFromDirectoryAsync()
    {
        var workflowDir = _configuration.GetValue<string>("WorkflowDataDirectory")
       ?? Path.Combine(Directory.GetCurrentDirectory(), "workflows");

        _logger.LogInformation("Importing workflows from directory: {Directory}", workflowDir);

        try
        {
            var (imported, skipped, failed) = await WorkflowSeeder.SeedFromDirectoryAsync(
                workflowDir,
             _context,
           _logger);

  _logger.LogInformation(
                "Workflow import completed: {Imported} imported, {Skipped} skipped, {Failed} failed",
       imported,
                skipped,
 failed);

      if (failed > 0)
            {
  _logger.LogWarning("{FailedCount} workflows failed to import", failed);
         }
     }
        catch (Exception ex)
        {
      _logger.LogError(ex, "Failed to import workflows from directory");
            throw;
        }
    }
}
