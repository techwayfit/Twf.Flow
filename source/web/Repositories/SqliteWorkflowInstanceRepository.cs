using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Twf.Flow.Web.Data;
using Twf.Flow.Web.Models;

namespace Twf.Flow.Web.Repositories;

public class SqliteWorkflowInstanceRepository : IWorkflowInstanceRepository
{
    private readonly WorkflowDbContext _context;
    private static readonly JsonSerializerOptions _json = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public SqliteWorkflowInstanceRepository(WorkflowDbContext context)
    {
        _context = context;
    }

    public async Task<WorkflowInstance> CreateAsync(WorkflowInstance instance)
    {
        var entity = ToEntity(instance);
        _context.WorkflowInstances.Add(entity);
        await _context.SaveChangesAsync();
        return instance;
    }

    public async Task<WorkflowInstance> UpdateAsync(WorkflowInstance instance)
    {
        var entity = await _context.WorkflowInstances.FindAsync(instance.Id)
            ?? throw new InvalidOperationException($"WorkflowInstance {instance.Id} not found");

        entity.Status      = instance.Status.ToString();
        entity.CompletedAt = instance.CompletedAt;
        entity.JsonData    = JsonSerializer.Serialize(instance, _json);

        await _context.SaveChangesAsync();
        return instance;
    }

    public async Task<WorkflowInstance?> GetByIdAsync(Guid id)
    {
        var entity = await _context.WorkflowInstances.FindAsync(id);
        return entity is null ? null : FromEntity(entity);
    }

    public async Task<IEnumerable<WorkflowInstance>> GetByWorkflowIdAsync(Guid workflowDefinitionId)
    {
        var entities = await _context.WorkflowInstances
            .Where(e => e.WorkflowDefinitionId == workflowDefinitionId)
            .OrderByDescending(e => e.StartedAt)
            .ToListAsync();

        return entities.Select(FromEntity);
    }

    private static WorkflowInstanceEntity ToEntity(WorkflowInstance instance) => new()
    {
        Id                   = instance.Id,
        WorkflowDefinitionId = instance.WorkflowDefinitionId,
        WorkflowName         = instance.WorkflowName,
        Status               = instance.Status.ToString(),
        StartedAt            = instance.StartedAt,
        CompletedAt          = instance.CompletedAt,
        JsonData             = JsonSerializer.Serialize(instance, _json)
    };

    private static WorkflowInstance FromEntity(WorkflowInstanceEntity entity)
    {
        var instance = JsonSerializer.Deserialize<WorkflowInstance>(entity.JsonData, _json)
            ?? new WorkflowInstance();

        // Ensure promoted columns are authoritative
        instance.Id                   = entity.Id;
        instance.WorkflowDefinitionId = entity.WorkflowDefinitionId;
        instance.WorkflowName         = entity.WorkflowName;
        instance.Status               = Enum.Parse<WorkflowRunStatus>(entity.Status);
        instance.StartedAt            = entity.StartedAt;
        instance.CompletedAt          = entity.CompletedAt;

        return instance;
    }
}
