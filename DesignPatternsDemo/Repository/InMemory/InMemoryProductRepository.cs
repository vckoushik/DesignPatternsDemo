using System.Collections.Concurrent;
using DesignPatternsDemo.Models.Data;
using DesignPatternsDemo.Repository.Interfaces;

namespace DesignPatternsDemo.Repository.InMemory;

/// <summary>
/// Thread-safe in-memory product store backed by <see cref="ConcurrentDictionary{TKey,TValue}"/>.
/// Registered as a Singleton so state persists for the lifetime of the application.
/// </summary>
public sealed class InMemoryProductRepository : IProductRepository
{
    private readonly ConcurrentDictionary<Guid, ProductEntity> _store = new();

    public Task<IEnumerable<ProductEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IEnumerable<ProductEntity>>(_store.Values.ToList());

    public Task<ProductEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<ProductEntity> AddAsync(ProductEntity entity, CancellationToken cancellationToken = default)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAtUtc = DateTime.UtcNow;
        _store[entity.Id] = entity;
        return Task.FromResult(entity);
    }

    public Task<ProductEntity?> UpdateAsync(ProductEntity entity, CancellationToken cancellationToken = default)
    {
        if (!_store.TryGetValue(entity.Id, out var existing))
            return Task.FromResult<ProductEntity?>(null);

        entity.CreatedAtUtc = existing.CreatedAtUtc; // preserve immutable field
        entity.UpdatedAtUtc = DateTime.UtcNow;
        _store[entity.Id] = entity;
        return Task.FromResult<ProductEntity?>(entity);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.TryRemove(id, out _));
}
