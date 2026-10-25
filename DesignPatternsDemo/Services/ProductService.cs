using AutoMapper;
using DesignPatternsDemo.Models.Client.Requests;
using DesignPatternsDemo.Models.Client.Responses;
using DesignPatternsDemo.Models.Data;
using DesignPatternsDemo.Repository.Interfaces;
using DesignPatternsDemo.Services.Interfaces;

namespace DesignPatternsDemo.Services;

/// <summary>
/// Core product service — orchestrates mapping and delegates persistence to <see cref="IProductRepository"/>.
/// </summary>
public sealed class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly IMapper _mapper;

    public ProductService(IProductRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ProductResponse>> GetAllProductsAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetAllAsync(cancellationToken);
        return _mapper.Map<IEnumerable<ProductResponse>>(entities);
    }

    public async Task<ProductResponse?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : _mapper.Map<ProductResponse>(entity);
    }

    public async Task<ProductResponse> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var entity = _mapper.Map<ProductEntity>(request);
        var created = await _repository.AddAsync(entity, cancellationToken);
        return _mapper.Map<ProductResponse>(created);
    }

    public async Task<ProductResponse?> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var entity = _mapper.Map<ProductEntity>(request);
        entity.Id = id;
        var updated = await _repository.UpdateAsync(entity, cancellationToken);
        return updated is null ? null : _mapper.Map<ProductResponse>(updated);
    }

    public async Task<bool> DeleteProductAsync(Guid id, CancellationToken cancellationToken = default)
        => await _repository.DeleteAsync(id, cancellationToken);
}
