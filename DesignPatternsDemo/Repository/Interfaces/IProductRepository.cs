using DesignPatternsDemo.Models.Data;

namespace DesignPatternsDemo.Repository.Interfaces;

/// <summary>
/// Contract for product data-access operations.
/// </summary>
public interface IProductRepository
{
    Task<IEnumerable<ProductEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProductEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductEntity> AddAsync(ProductEntity entity, CancellationToken cancellationToken = default);
    Task<ProductEntity?> UpdateAsync(ProductEntity entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
