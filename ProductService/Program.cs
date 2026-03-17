
var builder = WebApplication.CreateBuilder(args);

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI();


// In-memory product database
var products = new List<Product>
{
    new Product { ProductId = "PROD001", Name = "Laptop", Description = "Lenovo laptop", Price = 99.99m, Category = "Electronics" },
    new Product { ProductId = "PROD002", Name = "Monitor", Description = "HD Monitor", Price = 499.99m, Category = "Electronics" },
    new Product { ProductId = "PROD003", Name = "Keyboard", Description = "Gaming Keyboard", Price = 149.99m, Category = "Accessories" },
    new Product { ProductId = "PROD004", Name = "Mouse", Description = "Wireless Mouse", Price = 79.99m, Category = "Accessories" },
    new Product { ProductId = "PROD005", Name = "Headphones", Description = "Something Headphones", Price = 299.99m, Category = "Audio" }
};

// Product Management Endpoints
app.MapGet("/api/products", (ILogger<Program> logger) =>
{
    logger.LogInformation("Retrieving all products. Total products: {Count}", products.Count);
    return Results.Ok(products);
});

app.MapGet("/api/products/{productId}", (string productId, ILogger<Program> logger) =>
{
    logger.LogInformation("Retrieving product {ProductId}", productId);
    
    var product = products.FirstOrDefault(p => p.ProductId == productId);
    
    if (product == null)
    {
        logger.LogWarning("Product {ProductId} not found", productId);
        return Results.NotFound(new { Message = "Product not found" });
    }
    
    logger.LogInformation("Product {ProductId} found: {ProductName}", productId, product.Name);
    return Results.Ok(product);
});

app.MapPost("/api/products", (Product product, ILogger<Program> logger) =>
{
    logger.LogInformation("Creating new product: {ProductName}", product.Name);
    
    if (string.IsNullOrEmpty(product.ProductId))
    {
        product.ProductId = $"PROD{products.Count + 1:D3}";
    }
    
    products.Add(product);
    
    logger.LogInformation("Product {ProductId} created successfully", product.ProductId);
    return Results.Created($"/api/products/{product.ProductId}", product);
});

app.MapPut("/api/products/{productId}", (string productId, Product updatedProduct, ILogger<Program> logger) =>
{
    logger.LogInformation("Updating product {ProductId}", productId);
    
    var product = products.FirstOrDefault(p => p.ProductId == productId);
    
    if (product == null)
    {
        logger.LogWarning("Product {ProductId} not found for update", productId);
        return Results.NotFound(new { Message = "Product not found" });
    }
    
    product.Name = updatedProduct.Name;
    product.Description = updatedProduct.Description;
    product.Price = updatedProduct.Price;
    product.Category = updatedProduct.Category;
    
    logger.LogInformation("Product {ProductId} updated successfully", productId);
    return Results.Ok(product);
});

app.MapDelete("/api/products/{productId}", (string productId, ILogger<Program> logger) =>
{
    logger.LogInformation("Deleting product {ProductId}", productId);
    
    var product = products.FirstOrDefault(p => p.ProductId == productId);
    
    if (product == null)
    {
        logger.LogWarning("Product {ProductId} not found for deletion", productId);
        return Results.NotFound(new { Message = "Product not found" });
    }
    
    products.Remove(product);
    
    logger.LogInformation("Product {ProductId} deleted successfully", productId);
    return Results.NoContent();
});

app.MapGet("/api/products/category/{category}", (string category, ILogger<Program> logger) =>
{
    logger.LogInformation("Retrieving products by category: {Category}", category);
    
    var categoryProducts = products.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
    
    logger.LogInformation("Found {Count} products in category {Category}", categoryProducts.Count, category);
    return Results.Ok(categoryProducts);
});

app.Run("http://0.0.0.0:5004");

// Models
public class Product
{
    public string ProductId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
}