using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Twf.Flow.Web.Data;
using Twf.Flow.Web.Models;

namespace Twf.Flow.Web.Tools;

/// <summary>
/// Utility for migrating workflows between JSON files and SQLite database
/// </summary>
public class WorkflowMigrationTool
{
    private readonly string _jsonDirectory;
    private readonly string _connectionString;
    private readonly JsonSerializerOptions _jsonOptions;

    public WorkflowMigrationTool(string jsonDirectory, string connectionString)
    {
     _jsonDirectory = jsonDirectory;
      _connectionString = connectionString;
      _jsonOptions = new JsonSerializerOptions 
        { 
      PropertyNameCaseInsensitive = true,
   WriteIndented = true 
        };
    }

    /// <summary>
    /// Migrate workflows from JSON files to SQLite database
    /// </summary>
    public async Task MigrateJsonToSqliteAsync()
    {
        Console.WriteLine("Starting migration from JSON to SQLite...");

        if (!Directory.Exists(_jsonDirectory))
        {
      Console.WriteLine($"Error: Directory '{_jsonDirectory}' does not exist.");
    return;
     }

     var options = new DbContextOptionsBuilder<WorkflowDbContext>()
        .UseSqlite(_connectionString)
  .Options;

        using var context = new WorkflowDbContext(options);
        
    // Ensure database is created
   await context.Database.EnsureCreatedAsync();

        var files = Directory.GetFiles(_jsonDirectory, "*.json");
     Console.WriteLine($"Found {files.Length} workflow files.");

        int successCount = 0;
      int errorCount = 0;

        foreach (var file in files)
  {
            try
     {
        var json = await File.ReadAllTextAsync(file);
      var workflow = JsonSerializer.Deserialize<WorkflowDefinition>(json, _jsonOptions);

             if (workflow == null)
       {
        Console.WriteLine($"??  Skipping {Path.GetFileName(file)}: Invalid JSON");
           errorCount++;
   continue;
       }

            // Check if workflow already exists
  var existing = await context.Workflows.FindAsync(workflow.Id);
       if (existing != null)
    {
         Console.WriteLine($"??  Skipping {workflow.Name}: Already exists in database");
 continue;
           }

                var entity = new WorkflowEntity
      {
           Id = workflow.Id,
          Name = workflow.Name,
  Description = workflow.Description,
    JsonData = JsonSerializer.Serialize(workflow, _jsonOptions),
            CreatedAt = workflow.CreatedAt,
             UpdatedAt = workflow.UpdatedAt
   };

        context.Workflows.Add(entity);
    await context.SaveChangesAsync();

    Console.WriteLine($"? Migrated: {workflow.Name}");
      successCount++;
            }
            catch (Exception ex)
        {
        Console.WriteLine($"? Error migrating {Path.GetFileName(file)}: {ex.Message}");
             errorCount++;
       }
  }

  Console.WriteLine($"\nMigration complete!");
        Console.WriteLine($"  Success: {successCount}");
        Console.WriteLine($"  Errors: {errorCount}");
    }

    /// <summary>
    /// Export workflows from SQLite database to JSON files
    /// </summary>
    public async Task ExportSqliteToJsonAsync()
    {
        Console.WriteLine("Starting export from SQLite to JSON...");

    var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseSqlite(_connectionString)
       .Options;

      using var context = new WorkflowDbContext(options);

        if (!await context.Database.CanConnectAsync())
        {
            Console.WriteLine("Error: Cannot connect to database.");
          return;
   }

     // Ensure output directory exists
 Directory.CreateDirectory(_jsonDirectory);

        var workflows = await context.Workflows.ToListAsync();
     Console.WriteLine($"Found {workflows.Count} workflows in database.");

   int successCount = 0;
   int errorCount = 0;

        foreach (var entity in workflows)
        {
    try
     {
    var workflow = JsonSerializer.Deserialize<WorkflowDefinition>(entity.JsonData, _jsonOptions);
           
      if (workflow == null)
     {
 Console.WriteLine($"??  Skipping {entity.Name}: Invalid workflow data");
                errorCount++;
  continue;
 }

    var filePath = Path.Combine(_jsonDirectory, $"{entity.Id}.json");
             
        if (File.Exists(filePath))
            {
            Console.WriteLine($"??  Skipping {workflow.Name}: File already exists");
               continue;
           }

                var json = JsonSerializer.Serialize(workflow, _jsonOptions);
   await File.WriteAllTextAsync(filePath, json);

        Console.WriteLine($"? Exported: {workflow.Name}");
        successCount++;
   }
 catch (Exception ex)
        {
     Console.WriteLine($"? Error exporting {entity.Name}: {ex.Message}");
  errorCount++;
         }
    }

        Console.WriteLine($"\nExport complete!");
  Console.WriteLine($"  Success: {successCount}");
        Console.WriteLine($"  Errors: {errorCount}");
    }

    /// <summary>
    /// Validate all workflows in JSON directory
    /// </summary>
    public async Task ValidateJsonWorkflowsAsync()
    {
        Console.WriteLine("Validating JSON workflows...");

        if (!Directory.Exists(_jsonDirectory))
        {
            Console.WriteLine($"Error: Directory '{_jsonDirectory}' does not exist.");
          return;
      }

    var files = Directory.GetFiles(_jsonDirectory, "*.json");
        Console.WriteLine($"Found {files.Length} workflow files.");

        int validCount = 0;
      int invalidCount = 0;

        foreach (var file in files)
      {
            try
            {
                var json = await File.ReadAllTextAsync(file);
    var workflow = JsonSerializer.Deserialize<WorkflowDefinition>(json, _jsonOptions);

          if (workflow == null)
         {
     Console.WriteLine($"? Invalid: {Path.GetFileName(file)} - Cannot deserialize");
         invalidCount++;
            continue;
       }

if (string.IsNullOrEmpty(workflow.Name))
                {
 Console.WriteLine($"? Invalid: {Path.GetFileName(file)} - Missing name");
             invalidCount++;
             continue;
  }

   if (workflow.Id == Guid.Empty)
         {
                  Console.WriteLine($"? Invalid: {Path.GetFileName(file)} - Invalid ID");
  invalidCount++;
 continue;
  }

        Console.WriteLine($"? Valid: {workflow.Name} ({workflow.Nodes.Count} nodes, {workflow.Connections.Count} connections)");
             validCount++;
            }
            catch (Exception ex)
     {
     Console.WriteLine($"? Error validating {Path.GetFileName(file)}: {ex.Message}");
                invalidCount++;
}
        }

        Console.WriteLine($"\nValidation complete!");
        Console.WriteLine($"  Valid: {validCount}");
        Console.WriteLine($"  Invalid: {invalidCount}");
    }

    /// <summary>
    /// Command-line entry point
    /// </summary>
    public static async Task Main(string[] args)
    {
        if (args.Length < 2)
        {
   ShowUsage();
     return;
        }

        var command = args[0].ToLower();
        var jsonDirectory = args[1];
        var connectionString = args.Length > 2 ? args[2] : "Data Source=workflows.db";

     var tool = new WorkflowMigrationTool(jsonDirectory, connectionString);

        switch (command)
    {
 case "import":
       await tool.MigrateJsonToSqliteAsync();
      break;
case "export":
         await tool.ExportSqliteToJsonAsync();
     break;
            case "validate":
     await tool.ValidateJsonWorkflowsAsync();
    break;
        default:
     Console.WriteLine($"Unknown command: {command}");
      ShowUsage();
    break;
        }
    }

    private static void ShowUsage()
    {
        Console.WriteLine("Workflow Migration Tool");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run -- <command> <json-directory> [connection-string]");
 Console.WriteLine();
      Console.WriteLine("Commands:");
        Console.WriteLine("  import     - Import workflows from JSON files to SQLite database");
        Console.WriteLine("  export     - Export workflows from SQLite database to JSON files");
  Console.WriteLine("  validate   - Validate JSON workflow files");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run -- import ./workflows");
    Console.WriteLine("  dotnet run -- export ./workflows \"Data Source=workflows.db\"");
        Console.WriteLine("  dotnet run -- validate ./workflows");
    }
}
