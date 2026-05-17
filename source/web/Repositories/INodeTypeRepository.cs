using Twf.Flow.Web.Data;

namespace Twf.Flow.Web.Repositories;

public interface INodeTypeRepository
{
    Task<IEnumerable<NodeTypeEntity>> GetAllAsync();
    Task<IEnumerable<NodeTypeEntity>> GetByCategoryAsync(string category);
    Task<NodeTypeEntity?> GetByNodeTypeAsync(string nodeType);
    Task<NodeTypeEntity?> GetByIdAsync(int id);
    Task<NodeTypeEntity> CreateAsync(NodeTypeEntity entity);
    Task<NodeTypeEntity> UpdateAsync(NodeTypeEntity entity);
    Task<bool> DeleteAsync(int id);
    Task<bool> AnyAsync();
}
