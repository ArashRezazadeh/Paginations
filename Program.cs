using System.Data;
using Bogus;
using keyset.Data;
using keyset.Model;
using keyset.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
// builder.Services.AddOpenApi();

var connectionString = "DataSource=myshareddb;mode=memory;cache=shared";
var connection = new SqliteConnection(connectionString);
connection.Open();
builder.Services.AddMemoryCache();
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));

builder.Services.AddScoped<IProductServices, ProductServices>();

builder.Services.AddCors(option =>
{
    option.AddPolicy("CorsPolicy", builder =>
    {
        builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("X-Pagination");
    });
});

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();

    if (!context.Products.Any())
    {
        var productFaker = new Faker<Product>()
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(p => p.Price, f => f.Finance.Amount(50, 2000))
            .RuleFor(p => p.CategoryId, f => f.Random.Int(1, 5));
        var products = productFaker.Generate(10000);

        context.Products.AddRange(products);
        context.SaveChanges();
    }
}



// app.MapGet("products", async (AppDbContext db) =>
// {
//     var products = await db.Products.OrderBy(p => p.Id).Take(10).ToListAsync();

//     return products;
// });

app.UseCors("CorsPolicy");
app.MapControllers();
app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
