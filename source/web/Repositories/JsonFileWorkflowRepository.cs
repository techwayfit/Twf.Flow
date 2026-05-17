using System.Text.Json;
using Twf.Flow.Web.Models;

namespace Twf.Flow.Web.Repositories;

/// <summary>
/// File-based workflow repository using JSON files
/// </summary>
public class JsonFileWorkflowRepository : IWorkflowRepository
{
    private readonly string _dataDirectory;
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonFileWorkflowRepository(string dataDirectory)
    {
_dataDirectory = dataDirectory;
        _jsonOptions = new JsonSerializerOptions 
        { 
    WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

        // Ensure directory exists
    if (!Directory.Exists(_dataDirectory))
  {
  Directory.CreateDirectory(_dataDirectory);
 }
    }

    public async Task<IEnumerable<WorkflowDefinition>> GetAllAsync()
    {
 var files = Directory.GetFiles(_dataDirectory, "*.json");
     var workflows = new List<WorkflowDefinition>();

     foreach (var file in files)
        {
            try
     {
           var json = await File.ReadAllTextAsync(file);
     var workflow = JsonSerializer.Deserialize<WorkflowDefinition>(json, _jsonOptions);
   if (workflow != null)
     {
     workflows.Add(workflow);
           }
            }
         catch (Exception ex)
     {
          // Log error but continue
      Console.WriteLine($"Error loading workflow from {file}: {ex.Message}");
         }
        }

   return workflows.OrderByDescending(w => w.UpdatedAt);
    }

    public async Task<WorkflowDefinition?> GetByIdAsync(Guid id)
    {
        var filePath = GetFilePath(id);
        
   if (!File.Exists(filePath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<WorkflowDefinition>(json, _jsonOptions);
    }

    public async Task<WorkflowDefinition> CreateAsync(WorkflowDefinition workflow)
    {
        workflow.Id = Guid.NewGuid();
        workflow.CreatedAt = DateTime.UtcNow;
        workflow.UpdatedAt = DateTime.UtcNow;

        await SaveWorkflowAsync(workflow);
        return workflow;
    }

    public async Task<WorkflowDefinition> UpdateAsync(WorkflowDefinition workflow)
    {
   workflow.UpdatedAt = DateTime.UtcNow;
    await SaveWorkflowAsync(workflow);
        return workflow;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
  var filePath = GetFilePath(id);
        
        if (!File.Exists(filePath))
        {
   return false;
 }

        File.Delete(filePath);
     return true;
    }

    public async Task<IEnumerable<WorkflowDefinition>> SearchAsync(string query)
    {
        var allWorkflows = await GetAllAsync();
        
        if (string.IsNullOrWhiteSpace(query))
  {
       return allWorkflows;
      }

   return allWorkflows.Where(w => 
       w.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
 (w.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
      w.Metadata.Tags.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase))
        );
  }

    private async Task SaveWorkflowAsync(WorkflowDefinition workflow)
    {
        var filePath = GetFilePath(workflow.Id);
      var json = JsonSerializer.Serialize(workflow, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json);
 }

    private string GetFilePath(Guid id) => Path.Combine(_dataDirectory, $"{id}.json");
}
