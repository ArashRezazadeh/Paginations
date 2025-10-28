using keyset.Data;
using keyset.DTOs;
using keyset.Model;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace keyset.Services;


public class ProductServices : IProductServices
{

    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;
    private const string _totalPagesKey = "TotalPages";
    public ProductServices(AppDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
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

    // public async Task<PagedProductResponseDTO> GetPageProductAsync(int pageSize, int? lastProductId = null)
    // {
    //     var query = _context.Products.AsQueryable();
    //     var totalPages = await GetTotalPageAsync(pageSize);
    //     if (lastProductId.HasValue)
    //     {
    //         query = query.Where(p => p.Id > lastProductId.Value);
    //     }

    //     var pageProduct = await query
    //         .OrderBy(p => p.Id)
    //         .Take(pageSize)
    //         .Select(x => new ProductDTO
    //         {
    //             Id = x.Id,
    //             CategoryId = x.CategoryId,
    //             Name = x.Name,
    //             Price = x.Price,
    //         })
    //         .ToListAsync();

    //     var lastId = pageProduct.LastOrDefault()?.Id;
    //     var hasNextPage = await _context.Products.AnyAsync(x => x.Id > lastId);

    //     return new PagedProductResponseDTO
    //     {
    //         Items = pageProduct,
    //         PageSize = pageSize,
    //         HasNextPage = hasNextPage,
    //         HasPreviousPage = lastProductId.HasValue,
    //         TotalPages = totalPages
    //     };
    // }
    
    public async Task<PagedProductResponseDTO> GetPageProductAsync(int pageSize, int? lastProductId = null)
    {
        var totalPages = await GetTotalPageAsync(pageSize);
        List<Product> products;
        Dictionary<int, decimal> AveragePricePerCategory;
        bool hasNextPage = false;
        bool hasPreviousPage = false;

        // First page request
        if (lastProductId == null)
        {
            products = new List<Product>();
            for (int i = 1; i <= totalPages; i++)
            {
                var product = await _context.Products.FindAsync(i);
                if (product != null)
                {
                    products.Add(product);
                }
            }

            hasNextPage = products.Count == pageSize;
            hasPreviousPage = false;
            AveragePricePerCategory = await GetAveragePricePerCategoryAsync(products);
        }
        // Last page request
        else if (lastProductId == ((totalPages - 1) * pageSize))
        {
            products = new List<Product>();

            for (int i = lastProductId.Value; i < lastProductId.Value + pageSize; i++)
            {
                var product = await _context.Products.FindAsync(i);
                if (product != null)
                {
                    products.Add(product);
                }

                hasNextPage = false;
                hasPreviousPage = true;
            }
            AveragePricePerCategory = await GetAveragePricePerCategoryAsync(products);
        }
        else
        {
            _context.ChangeTracker.Clear();
            IQueryable<Product> query = _context.Products;

            query = query.Where(p => p.Id > lastProductId.Value);

            products = await query.OrderBy(p => p.Id).Take(pageSize).ToListAsync();

            var lastId = products.LastOrDefault()?.Id;
            hasNextPage = lastId.HasValue &&
                await _context.Products.AnyAsync(p => p.Id > lastId);
            hasPreviousPage = true;
            AveragePricePerCategory = await GetAveragePricePerCategoryAsync(products);
        }
        

         return new PagedProductResponseDTO
         {
             Items = products.Select(p => new ProductDTO
             {
                 Id = p.Id,
                 Name = p.Name,
                 Price = p.Price,
                 CategoryId = p.CategoryId
             }).ToList(),
             PageSize = pageSize,
             HasPreviousPage = hasPreviousPage,
             HasNextPage = hasNextPage,
             TotalPages = totalPages,
            AveragePricePerCategory = AveragePricePerCategory       
         };
    }

    public async Task<int> GetTotalPageAsync(int pageSize)
    {
        if (!_cache.TryGetValue(_totalPagesKey, out int totalPages))
        {
            _context.ChangeTracker.Clear();
            var totalCount = await _context.Products.CountAsync();

            totalPages = (int)Math.Ceiling(totalPages / (double)pageSize);
            _cache.Set(_totalPagesKey, totalPages, TimeSpan.FromMinutes(2));
        }


        return totalPages;
    }


    public void InvalidDataCache()
    {
        _cache.Remove(_totalPagesKey);
    }

    private async Task<Dictionary<int, decimal>>GetAveragePricePerCategoryAsync (List<Product> products)
    {
        if (products == null || !products.Any())
        {
            return new Dictionary<int, decimal>();
        }


        var aggregateByTask = Task.Run(() =>
        {
            var aggregateBy = products.AggregateBy(
                product => product.CategoryId,
                x => (Sum: 0m, Count: 0),
                (acc, product) => (acc.Sum + product.Price, acc.Count + 1)
            );

            var averagePriceByCategory = aggregateBy.ToDictionary(
                kvp => kvp.Key,
                kvp => Math.Round(kvp.Value.Sum / kvp.Value.Count, 2)
            );

            return averagePriceByCategory;
        });

        return await aggregateByTask;
    }
}