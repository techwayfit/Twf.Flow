using Microsoft.EntityFrameworkCore;
using Twf.Flow.Web.Data;

namespace Twf.Flow.Web.Repositories;

public class SqliteNodeTypeRepository : INodeTypeRepository
{
    private readonly WorkflowDbContext _db;

    public SqliteNodeTypeRepository(WorkflowDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<NodeTypeEntity>> GetAllAsync()
        => await _db.NodeTypes.OrderBy(n => n.Category).ThenBy(n => n.Name).ToListAsync();

    public async Task<IEnumerable<NodeTypeEntity>> GetByCategoryAsync(string category)
        => await _db.NodeTypes.Where(n => n.Category == category).OrderBy(n => n.Name).ToListAsync();

    public async Task<NodeTypeEntity?> GetByNodeTypeAsync(string nodeType)
        => await _db.NodeTypes.FirstOrDefaultAsync(n => n.NodeType == nodeType);

    public async Task<NodeTypeEntity?> GetByIdAsync(int id)
        => await _db.NodeTypes.FindAsync(id);

    public async Task<NodeTypeEntity> CreateAsync(NodeTypeEntity entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        _db.NodeTypes.Add(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task<NodeTypeEntity> UpdateAsync(NodeTypeEntity entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _db.NodeTypes.Update(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _db.NodeTypes.FindAsync(id);
        if (entity == null) return false;
        _db.NodeTypes.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AnyAsync()
        => await _db.NodeTypes.AnyAsync();
}
