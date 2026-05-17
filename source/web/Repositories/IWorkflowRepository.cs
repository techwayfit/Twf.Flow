using Twf.Flow.Web.Models;

namespace Twf.Flow.Web.Repositories;

/// <summary>
/// Interface for workflow persistence
/// </summary>
public interface IWorkflowRepository
{
    Task<IEnumerable<WorkflowDefinition>> GetAllAsync();
    Task<WorkflowDefinition?> GetByIdAsync(Guid id);
    Task<WorkflowDefinition> CreateAsync(WorkflowDefinition workflow);
    Task<WorkflowDefinition> UpdateAsync(WorkflowDefinition workflow);
    Task<bool> DeleteAsync(Guid id);
    Task<IEnumerable<WorkflowDefinition>> SearchAsync(string query);
}
