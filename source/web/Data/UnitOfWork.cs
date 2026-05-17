using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Twf.Flow.Web.Repositories;

namespace Twf.Flow.Web.Data;

/// <summary>
/// Implements the Unit of Work pattern, coordinating repository operations
/// and managing database transactions.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly WorkflowDbContext _context;
    private readonly ILogger<UnitOfWork> _logger;
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    public IWorkflowRepository Workflows { get; }
    public INodeTypeRepository NodeTypes { get; }
    public IWorkflowInstanceRepository Instances { get; }

    public UnitOfWork(
   WorkflowDbContext context,
        IWorkflowRepository workflows,
        INodeTypeRepository nodeTypes,
      IWorkflowInstanceRepository instances,
        ILogger<UnitOfWork> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Workflows = workflows ?? throw new ArgumentNullException(nameof(workflows));
        NodeTypes = nodeTypes ?? throw new ArgumentNullException(nameof(nodeTypes));
        Instances = instances ?? throw new ArgumentNullException(nameof(instances));
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var changes = await _context.SaveChangesAsync(cancellationToken);

            if (changes > 0)
            {
                _logger.LogDebug("Saved {ChangeCount} changes to database", changes);
            }

            return changes;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database update failed");
            throw;
        }
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            throw new InvalidOperationException("A transaction is already in progress");
        }

        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        _logger.LogDebug("Database transaction started");
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction in progress");
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
            _logger.LogDebug("Database transaction committed");
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            _logger.LogWarning("Attempted to rollback but no transaction in progress");
            return;
        }

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
            _logger.LogDebug("Database transaction rolled back");
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _transaction?.Dispose();
                // Note: Don't dispose context - it's managed by DI container
            }

            _disposed = true;
        }
    }
}
