namespace Twf.Flow.Web.Data;

/// <summary>
/// Coordinates the work of multiple repositories and ensures that all changes
/// are committed as a single transaction.
/// Implements the Unit of Work pattern for database operations.
/// </summary>
public interface IUnitOfWork : IDisposable
{
  /// <summary>
    /// Gets the workflow repository.
    /// </summary>
    Twf.Flow.Web.Repositories.IWorkflowRepository Workflows { get; }

    /// <summary>
    /// Gets the node type repository.
    /// </summary>
    Twf.Flow.Web.Repositories.INodeTypeRepository NodeTypes { get; }

/// <summary>
    /// Gets the workflow instance repository.
/// </summary>
    Twf.Flow.Web.Repositories.IWorkflowInstanceRepository Instances { get; }

    /// <summary>
    /// Saves all changes made in this unit of work to the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a new database transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
  /// Commits the current transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
