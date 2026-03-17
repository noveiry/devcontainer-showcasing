using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// Add HttpClient for calling other services
builder.Services.AddHttpClient();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI();

// Order Management Endpoints
app.MapPost("/api/orders", async (HttpContext context, IHttpClientFactory clientFactory) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var client = clientFactory.CreateClient();
    
    logger.LogInformation("Creating new order");
    
    try
    {
        // Read order from request body
        var order = await context.Request.ReadFromJsonAsync<Order>();
        
        if (order == null)
        {
            return Results.BadRequest("Invalid order data");
        }
        
        // Call Product Service to validate product
        var productResponse = await client.GetAsync($"http://product-service:8084/api/products/{order.ProductId}");
        if (!productResponse.IsSuccessStatusCode)
        {
            logger.LogWarning("Product {ProductId} not found", order.ProductId);
            return Results.BadRequest("Product not found");
        }
        
        var product = await productResponse.Content.ReadFromJsonAsync<Product>();
        
        // Call Inventory Service to check stock
        var inventoryResponse = await client.GetAsync($"http://inventory-service:5001/api/inventory/{order.ProductId}");
        var inventory = await inventoryResponse.Content.ReadFromJsonAsync<InventoryItem>();
        
        if (inventory == null || inventory.Quantity < order.Quantity)
        {
            logger.LogWarning("Insufficient inventory for product {ProductId}", order.ProductId);
            return Results.BadRequest("Insufficient inventory");
        }
        
        // Calculate total
        order.TotalAmount = product!.Price * order.Quantity;
        order.OrderId = Guid.NewGuid().ToString();
        order.OrderDate = DateTime.UtcNow;
        order.Status = "Confirmed";
        
        // Call Payment Service
        var paymentRequest = new { OrderId = order.OrderId, Amount = order.TotalAmount };
        var paymentResponse = await client.PostAsJsonAsync("http://payment-service:8083/api/payments", paymentRequest);
        
        if (!paymentResponse.IsSuccessStatusCode)
        {
            logger.LogError("Payment failed for order {OrderId}", order.OrderId);
            order.Status = "Payment Failed";
            return Results.Ok(order);
        }
        
        var payment = await paymentResponse.Content.ReadFromJsonAsync<Payment>();
        order.PaymentId = payment!.PaymentId;
        
        logger.LogInformation("Order {OrderId} created successfully with amount {Amount}", order.OrderId, order.TotalAmount);
        
        return Results.Ok(order);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error creating order");
        return Results.Problem("Error creating order");
    }
});

app.MapGet("/api/orders/{orderId}", (string orderId, ILogger<Program> logger) =>
{
    logger.LogInformation("Retrieving order {OrderId}", orderId);
    
    // Simulate order retrieval
    var order = new Order
    {
        OrderId = orderId,
        CustomerId = "CUST001",
        ProductId = "PROD001",
        Quantity = 2,
        TotalAmount = 199.98m,
        OrderDate = DateTime.UtcNow.AddHours(-2),
        Status = "Confirmed"
    };
    
    return Results.Ok(order);
});

app.MapGet("/api/orders", (ILogger<Program> logger) =>
{
    logger.LogInformation("Retrieving all orders");
    
    var orders = new[]
    {
        new Order { OrderId = "ORD001", CustomerId = "CUST001", ProductId = "PROD001", Quantity = 2, TotalAmount = 199.98m, Status = "Confirmed" },
        new Order { OrderId = "ORD002", CustomerId = "CUST002", ProductId = "PROD002", Quantity = 1, TotalAmount = 499.99m, Status = "Shipped" }
    };
    
    return Results.Ok(orders);
});

app.Run("http://0.0.0.0:5002");

// Models
public class Order
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PaymentId { get; set; }
}

public class Product
{
    public string ProductId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class InventoryItem
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class Payment
{
    public string PaymentId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}