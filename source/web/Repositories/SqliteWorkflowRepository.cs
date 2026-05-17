using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Twf.Flow.Web.Data;
using Twf.Flow.Web.Models;

namespace Twf.Flow.Web.Repositories;

/// <summary>
/// SQLite-based workflow repository
/// </summary>
public class SqliteWorkflowRepository : IWorkflowRepository
{
    private readonly WorkflowDbContext _context;
    private readonly JsonSerializerOptions _jsonOptions;

    public SqliteWorkflowRepository(WorkflowDbContext context)
    {
        _context = context;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<IEnumerable<WorkflowDefinition>> GetAllAsync()
    {
        var entities = await _context.Workflows
            .OrderByDescending(w => w.UpdatedAt)
            .ToListAsync();

        return entities.Select(ToWorkflowDefinition);
    }

    public async Task<WorkflowDefinition?> GetByIdAsync(Guid id)
    {
        var entity = await _context.Workflows.FindAsync(id);
        return entity != null ? ToWorkflowDefinition(entity) : null;
    }

    public async Task<WorkflowDefinition> CreateAsync(WorkflowDefinition workflow)
    {
        workflow.Id = Guid.NewGuid();
        workflow.CreatedAt = DateTime.UtcNow;
        workflow.UpdatedAt = DateTime.UtcNow;

        var entity = ToEntity(workflow);
        _context.Workflows.Add(entity);
        await _context.SaveChangesAsync();

        return workflow;
    }

    public async Task<WorkflowDefinition> UpdateAsync(WorkflowDefinition workflow)
    {
        workflow.UpdatedAt = DateTime.UtcNow;

        var entity = await _context.Workflows.FindAsync(workflow.Id);
        if (entity == null)
        {
            throw new InvalidOperationException($"Workflow {workflow.Id} not found");
        }

        entity.Name = workflow.Name;
        entity.Description = workflow.Description;
        entity.JsonData = JsonSerializer.Serialize(workflow, _jsonOptions);
        entity.UpdatedAt = workflow.UpdatedAt;

        await _context.SaveChangesAsync();
        return workflow;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _context.Workflows.FindAsync(id);
        if (entity == null)
        {
            return false;
        }

        _context.Workflows.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<WorkflowDefinition>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return await GetAllAsync();
        }

        var entities = await _context.Workflows
            .Where(w => w.Name.Contains(query) || (w.Description != null && w.Description.Contains(query)))
            .OrderByDescending(w => w.UpdatedAt)
            .ToListAsync();

        return entities.Select(ToWorkflowDefinition);
    }

    private WorkflowDefinition ToWorkflowDefinition(WorkflowEntity entity)
    {
        var workflow = JsonSerializer.Deserialize<WorkflowDefinition>(entity.JsonData, _jsonOptions)
            ?? new WorkflowDefinition();

        // Ensure entity properties take precedence
        workflow.Id = entity.Id;
        workflow.Name = entity.Name;
        workflow.Description = entity.Description;
        workflow.CreatedAt = entity.CreatedAt;
        workflow.UpdatedAt = entity.UpdatedAt;

        return workflow;
    }

    private WorkflowEntity ToEntity(WorkflowDefinition workflow)
    {
        return new WorkflowEntity
        {
            Id = workflow.Id,
            Name = workflow.Name,
            Description = workflow.Description,
            JsonData = JsonSerializer.Serialize(workflow, _jsonOptions),
            CreatedAt = workflow.CreatedAt,
            UpdatedAt = workflow.UpdatedAt
        };
    }
}
