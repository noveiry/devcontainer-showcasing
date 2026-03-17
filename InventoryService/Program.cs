
var builder = WebApplication.CreateBuilder(args);

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI();

// In-memory inventory database
var inventory = new List<InventoryItem>
{
    new InventoryItem { ProductId = "PROD001", Quantity = 50, ReorderLevel = 10, Location = "Warehouse A" },
    new InventoryItem { ProductId = "PROD002", Quantity = 30, ReorderLevel = 5, Location = "Warehouse A" },
    new InventoryItem { ProductId = "PROD003", Quantity = 100, ReorderLevel = 20, Location = "Warehouse B" },
    new InventoryItem { ProductId = "PROD004", Quantity = 75, ReorderLevel = 15, Location = "Warehouse B" },
    new InventoryItem { ProductId = "PROD005", Quantity = 40, ReorderLevel = 8, Location = "Warehouse A" }
};

// Inventory Management Endpoints
app.MapGet("/api/inventory", (ILogger<Program> logger) =>
{
    logger.LogInformation("Retrieving all inventory items. Total items: {Count}", inventory.Count);
    return Results.Ok(inventory);
});

app.MapGet("/api/inventory/{productId}", (string productId, ILogger<Program> logger) =>
{
    logger.LogInformation("Checking inventory for product {ProductId}", productId);
    
    var item = inventory.FirstOrDefault(i => i.ProductId == productId);
    
    if (item == null)
    {
        logger.LogWarning("Inventory not found for product {ProductId}", productId);
        return Results.NotFound(new { Message = "Inventory item not found" });
    }
    
    logger.LogInformation("Product {ProductId} inventory: {Quantity} units in {Location}", 
        productId, item.Quantity, item.Location);
    
    return Results.Ok(item);
});

app.MapPost("/api/inventory/reserve", (ReservationRequest request, ILogger<Program> logger) =>
{
    logger.LogInformation("Reserving {Quantity} units of product {ProductId}", 
        request.Quantity, request.ProductId);
    
    var item = inventory.FirstOrDefault(i => i.ProductId == request.ProductId);
    
    if (item == null)
    {
        logger.LogWarning("Inventory not found for product {ProductId}", request.ProductId);
        return Results.NotFound(new { Message = "Inventory item not found" });
    }
    
    if (item.Quantity < request.Quantity)
    {
        logger.LogWarning("Insufficient inventory for product {ProductId}. Available: {Available}, Requested: {Requested}",
            request.ProductId, item.Quantity, request.Quantity);
        return Results.BadRequest(new { Message = "Insufficient inventory", Available = item.Quantity });
    }
    
    item.Quantity -= request.Quantity;
    
    logger.LogInformation("Reserved {Quantity} units of product {ProductId}. Remaining: {Remaining}",
        request.Quantity, request.ProductId, item.Quantity);
    
    if (item.Quantity <= item.ReorderLevel)
    {
        logger.LogWarning("Product {ProductId} inventory is below reorder level. Current: {Current}, Reorder Level: {ReorderLevel}",
            request.ProductId, item.Quantity, item.ReorderLevel);
    }
    
    return Results.Ok(new { Success = true, RemainingQuantity = item.Quantity });
});

app.MapPost("/api/inventory/restock", (RestockRequest request, ILogger<Program> logger) =>
{
    logger.LogInformation("Restocking {Quantity} units of product {ProductId}", 
        request.Quantity, request.ProductId);
    
    var item = inventory.FirstOrDefault(i => i.ProductId == request.ProductId);
    
    if (item == null)
    {
        // Create new inventory item
        item = new InventoryItem 
        { 
            ProductId = request.ProductId, 
            Quantity = request.Quantity,
            ReorderLevel = 10,
            Location = "Warehouse A"
        };
        inventory.Add(item);
        
        logger.LogInformation("Created new inventory item for product {ProductId} with {Quantity} units",
            request.ProductId, request.Quantity);
    }
    else
    {
        item.Quantity += request.Quantity;
        
        logger.LogInformation("Restocked product {ProductId}. New quantity: {Quantity}",
            request.ProductId, item.Quantity);
    }
    
    return Results.Ok(new { Success = true, NewQuantity = item.Quantity });
});

app.MapGet("/api/inventory/lowstock", (ILogger<Program> logger) =>
{
    logger.LogInformation("Retrieving low stock items");
    
    var lowStockItems = inventory.Where(i => i.Quantity <= i.ReorderLevel).ToList();
    
    logger.LogInformation("Found {Count} low stock items", lowStockItems.Count);
    
    return Results.Ok(lowStockItems);
});

app.MapPut("/api/inventory/{productId}", (string productId, InventoryUpdate update, ILogger<Program> logger) =>
{
    logger.LogInformation("Updating inventory for product {ProductId}", productId);
    
    var item = inventory.FirstOrDefault(i => i.ProductId == productId);
    
    if (item == null)
    {
        logger.LogWarning("Inventory not found for product {ProductId}", productId);
        return Results.NotFound(new { Message = "Inventory item not found" });
    }
    
    item.Quantity = update.Quantity;
    item.ReorderLevel = update.ReorderLevel;
    item.Location = update.Location;
    
    logger.LogInformation("Updated inventory for product {ProductId}", productId);
    
    return Results.Ok(item);
});

app.Run("http://0.0.0.0:5001");

// Models
public class InventoryItem
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int ReorderLevel { get; set; }
    public string Location { get; set; } = string.Empty;
}

public class ReservationRequest
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class RestockRequest
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class InventoryUpdate
{
    public int Quantity { get; set; }
    public int ReorderLevel { get; set; }
    public string Location { get; set; } = string.Empty;
}