var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ProductRepository>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Product API", Version = "v1" });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product API v1"));

app.UseHttpsRedirection();

// ── Product Endpoints ──────────────────────────────────────────────────────

// 1. GET all products
app.MapGet("/products", (ProductRepository repo) =>
    Results.Ok(repo.GetAll()))
    .WithName("GetAllProducts")
    .WithTags("Products")
    .Produces<IEnumerable<Product>>(200);

// 2. GET product by ID
app.MapGet("/products/{id:int}", (int id, ProductRepository repo) =>
    repo.GetById(id) is Product product
        ? Results.Ok(product)
        : Results.NotFound(new { message = $"Product {id} not found." }))
    .WithName("GetProductById")
    .WithTags("Products")
    .Produces<Product>(200)
    .Produces(404);

// 3. POST create product
app.MapPost("/products", (Product product, ProductRepository repo) =>
{
    var created = repo.Create(product);
    return Results.Created($"/products/{created.Id}", created);
})
    .WithName("CreateProduct")
    .WithTags("Products")
    .Produces<Product>(201);

// 4. PUT update product
app.MapPut("/products/{id:int}", (int id, Product updated, ProductRepository repo) =>
{
    if (!repo.Update(id, updated))
        return Results.NotFound(new { message = $"Product {id} not found." });
    return Results.NoContent();
})
    .WithName("UpdateProduct")
    .WithTags("Products")
    .Produces(204)
    .Produces(404);

// 5. PATCH update product price
app.MapPatch("/products/{id:int}/price", (int id, PricePatch patch, ProductRepository repo) =>
{
    if (!repo.UpdatePrice(id, patch.Price))
        return Results.NotFound(new { message = $"Product {id} not found." });
    return Results.NoContent();
})
    .WithName("UpdateProductPrice")
    .WithTags("Products")
    .Produces(204)
    .Produces(404);

// 6. DELETE product
app.MapDelete("/products/{id:int}", (int id, ProductRepository repo) =>
{
    if (!repo.Delete(id))
        return Results.NotFound(new { message = $"Product {id} not found." });
    return Results.NoContent();
})
    .WithName("DeleteProduct")
    .WithTags("Products")
    .Produces(204)
    .Produces(404);

app.Run();

// ── Models ─────────────────────────────────────────────────────────────────

record Product(int Id, string Name, string? Description, decimal Price, int Stock);
record PricePatch(decimal Price);

// ── In-Memory Repository ───────────────────────────────────────────────────

class ProductRepository
{
    private readonly List<Product> _products =
    [
        new(1, "Widget A", "A great widget", 9.99m, 100),
        new(2, "Widget B", "An even better widget", 19.99m, 50),
        new(3, "Gadget X", "Top-tier gadget", 49.99m, 25),
    ];

    private int _nextId = 4;

    public IEnumerable<Product> GetAll() => _products;

    public Product? GetById(int id) => _products.FirstOrDefault(p => p.Id == id);

    public Product Create(Product product)
    {
        var created = product with { Id = _nextId++ };
        _products.Add(created);
        return created;
    }

    public bool Update(int id, Product updated)
    {
        var index = _products.FindIndex(p => p.Id == id);
        if (index < 0) return false;
        _products[index] = updated with { Id = id };
        return true;
    }

    public bool UpdatePrice(int id, decimal price)
    {
        var index = _products.FindIndex(p => p.Id == id);
        if (index < 0) return false;
        _products[index] = _products[index] with { Price = price };
        return true;
    }

    public bool Delete(int id)
    {
        var product = _products.FirstOrDefault(p => p.Id == id);
        if (product is null) return false;
        _products.Remove(product);
        return true;
    }
}
