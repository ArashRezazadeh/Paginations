using keyset.Data;
using keyset.DTOs;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace keyset.Services;


public class ProductServices : IProductServices
{

    private readonly AppDbContext _context;
    public ProductServices(AppDbContext context)
    {
        _context = context;
    }
    public async Task<IEnumerable<ProductDTO>> GetAllProductsAsync()
    {
        var output = await _context.Products
            .AsNoTracking()
            .Select(x => new ProductDTO
            {
                Id = x.Id,
                CategoryId = x.CategoryId,
                Name = x.Name,
                Price = x.Price,
            }).ToListAsync();

        return output;
    }

    public async Task<PagedProductResponseDTO> GetPageProductAsync(int pageSize, int? lastProductId = null)
    {
        var query = _context.Products.AsQueryable();

        if (lastProductId.HasValue)
        {
            query = query.Where(p => p.Id > lastProductId.Value);
        }

        var pageProduct = await query
            .OrderBy(p => p.Id)
            .Take(pageSize)
            .Select(x => new ProductDTO
            {
                Id = x.Id,
                CategoryId = x.CategoryId,
                Name = x.Name,
                Price = x.Price,
            })
            .ToListAsync();

        var lastId = pageProduct.LastOrDefault()?.Id;
        var hasNextPage = await _context.Products.AnyAsync(x => x.Id > lastId);

        return new PagedProductResponseDTO
        {
            Items = pageProduct,
            PageSize = pageSize,
            HasNextPage = hasNextPage,
            HasPreviousPage = lastProductId.HasValue
        };
    }
}