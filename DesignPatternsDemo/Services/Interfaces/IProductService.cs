using DesignPatternsDemo.Models.Client.Requests;
using DesignPatternsDemo.Models.Client.Responses;

namespace DesignPatternsDemo.Services.Interfaces;

/// <summary>
/// Contract for product business-logic operations.
/// </summary>
public interface IProductService
{
    Task<IEnumerable<ProductResponse>> GetAllProductsAsync(CancellationToken cancellationToken = default);
    Task<ProductResponse?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductResponse> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductResponse?> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteProductAsync(Guid id, CancellationToken cancellationToken = default);
}
