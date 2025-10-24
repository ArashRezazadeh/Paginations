

using keyset.Data;
using keyset.DTOs;
using keyset.Model;

namespace keyset.Services;


public interface IProductServices
{
    Task<IEnumerable<ProductDTO>> GetAllProductsAsync();
    Task<PagedProductResponseDTO> GetPageProductAsync(int pageSize, int? lastProductId = null);
}