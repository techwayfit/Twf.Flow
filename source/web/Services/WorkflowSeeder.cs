using System.Text.Json;
using Twf.Flow.Web.Data;
using Twf.Flow.Web.Models;

namespace Twf.Flow.Web.Services;

/// <summary>
/// Imports workflow JSON files from disk into the SQLite database on startup.
/// Already-imported workflows (matched by ID) are skipped, so this is safe to
/// run on every boot.
/// </summary>
public static class WorkflowSeeder
{
    private static readonly JsonSerializerOptions _readOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private static readonly JsonSerializerOptions _writeOptions = new()
    {
        WriteIndented = false,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public static async Task<(int imported, int skipped, int failed)> SeedFromDirectoryAsync(
        string directory,
        WorkflowDbContext db,
        ILogger logger)
    {
        if (!Directory.Exists(directory))
        {
            logger.LogInformation("Workflow JSON directory '{Dir}' not found — skipping import.", directory);
            return (0, 0, 0);
        }

        var files = Directory.GetFiles(directory, "*.json");
        if (files.Length == 0)
        {
            logger.LogInformation("No workflow JSON files found in '{Dir}'.", directory);
            return (0, 0, 0);
        }

        // Load existing IDs once to avoid N+1 DB queries
        var existingIds = db.Workflows.Select(w => w.Id).ToHashSet();

        int imported = 0, skipped = 0, failed = 0;

        foreach (var file in files)
        {
            WorkflowDefinition? workflow;
            try
            {
                var json = await File.ReadAllTextAsync(file);
                workflow = JsonSerializer.Deserialize<WorkflowDefinition>(json, _readOptions);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not parse workflow file '{File}' — skipping.", file);
                failed++;
                continue;
            }

            if (workflow == null)
            {
                logger.LogWarning("File '{File}' deserialised to null — skipping.", file);
                failed++;
                continue;
            }

            if (existingIds.Contains(workflow.Id))
            {
                logger.LogDebug("Workflow {Id} ('{Name}') already in DB — skipping.", workflow.Id, workflow.Name);
                skipped++;
                continue;
            }

            try
            {
                if (workflow.CreatedAt == default) workflow.CreatedAt = DateTime.UtcNow;
                if (workflow.UpdatedAt == default) workflow.UpdatedAt = DateTime.UtcNow;

                db.Workflows.Add(new WorkflowEntity
                {
                    Id          = workflow.Id,
                    Name        = workflow.Name ?? Path.GetFileNameWithoutExtension(file),
                    Description = workflow.Description,
                    JsonData    = JsonSerializer.Serialize(workflow, _writeOptions),
                    CreatedAt   = workflow.CreatedAt,
                    UpdatedAt   = workflow.UpdatedAt,
                });

                existingIds.Add(workflow.Id);
                logger.LogInformation("Queued import: {Id} ('{Name}') from '{File}'.",
                    workflow.Id, workflow.Name, Path.GetFileName(file));
                imported++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to queue workflow {Id} from '{File}'.", workflow.Id, file);
                failed++;
            }
        }

        if (imported > 0)
            await db.SaveChangesAsync();

        logger.LogInformation(
            "Workflow import complete — {Imported} imported, {Skipped} skipped, {Failed} failed.",
            imported, skipped, failed);

        return (imported, skipped, failed);
    }
}
