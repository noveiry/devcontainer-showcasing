
var builder = WebApplication.CreateBuilder(args);

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI();


// In-memory payment database
var payments = new List<Payment>();

// Payment Processing Endpoints
app.MapPost("/api/payments", (PaymentRequest request, ILogger<Program> logger) =>
{
    logger.LogInformation("Processing payment for order {OrderId}, Amount: {Amount}", 
        request.OrderId, request.Amount);
    
    try
    {
        // Simulate payment processing delay
        Thread.Sleep(Random.Shared.Next(100, 500));
        
        // Simulate random payment failures (10% chance)
        var isSuccess = Random.Shared.Next(0, 10) > 0;
        
        var payment = new Payment
        {
            PaymentId = Guid.NewGuid().ToString(),
            OrderId = request.OrderId,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod ?? "CreditCard",
            Status = isSuccess ? "Completed" : "Failed",
            TransactionDate = DateTime.UtcNow,
            TransactionId = $"TXN{Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper()}"
        };
        
        if (!isSuccess)
        {
            payment.FailureReason = "Insufficient funds";
            logger.LogWarning("Payment failed for order {OrderId}. Reason: {Reason}", 
                request.OrderId, payment.FailureReason);
        }
        else
        {
            logger.LogInformation("Payment {PaymentId} completed successfully for order {OrderId}. Transaction ID: {TransactionId}",
                payment.PaymentId, request.OrderId, payment.TransactionId);
        }
        
        payments.Add(payment);
        
        return isSuccess 
            ? Results.Ok(payment) 
            : Results.BadRequest(payment);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error processing payment for order {OrderId}", request.OrderId);
        return Results.Problem("Payment processing error");
    }
});

app.MapGet("/api/payments/{paymentId}", (string paymentId, ILogger<Program> logger) =>
{
    logger.LogInformation("Retrieving payment {PaymentId}", paymentId);
    
    var payment = payments.FirstOrDefault(p => p.PaymentId == paymentId);
    
    if (payment == null)
    {
        logger.LogWarning("Payment {PaymentId} not found", paymentId);
        return Results.NotFound(new { Message = "Payment not found" });
    }
    
    logger.LogInformation("Payment {PaymentId} retrieved. Status: {Status}", paymentId, payment.Status);
    return Results.Ok(payment);
});

app.MapGet("/api/payments", (ILogger<Program> logger) =>
{
    logger.LogInformation("Retrieving all payments. Total payments: {Count}", payments.Count);
    return Results.Ok(payments);
});

app.MapGet("/api/payments/order/{orderId}", (string orderId, ILogger<Program> logger) =>
{
    logger.LogInformation("Retrieving payments for order {OrderId}", orderId);
    
    var orderPayments = payments.Where(p => p.OrderId == orderId).ToList();
    
    logger.LogInformation("Found {Count} payments for order {OrderId}", orderPayments.Count, orderId);
    return Results.Ok(orderPayments);
});

app.MapPost("/api/payments/{paymentId}/refund", (string paymentId, RefundRequest request, ILogger<Program> logger) =>
{
    logger.LogInformation("Processing refund for payment {PaymentId}, Amount: {Amount}", 
        paymentId, request.Amount);
    
    var payment = payments.FirstOrDefault(p => p.PaymentId == paymentId);
    
    if (payment == null)
    {
        logger.LogWarning("Payment {PaymentId} not found for refund", paymentId);
        return Results.NotFound(new { Message = "Payment not found" });
    }
    
    if (payment.Status != "Completed")
    {
        logger.LogWarning("Cannot refund payment {PaymentId} with status {Status}", paymentId, payment.Status);
        return Results.BadRequest(new { Message = "Can only refund completed payments" });
    }
    
    if (request.Amount > payment.Amount)
    {
        logger.LogWarning("Refund amount {RefundAmount} exceeds payment amount {PaymentAmount}", 
            request.Amount, payment.Amount);
        return Results.BadRequest(new { Message = "Refund amount exceeds payment amount" });
    }
    
    var refund = new Refund
    {
        RefundId = Guid.NewGuid().ToString(),
        PaymentId = paymentId,
        Amount = request.Amount,
        Reason = request.Reason,
        Status = "Processed",
        RefundDate = DateTime.UtcNow
    };
    
    // Update payment status if full refund
    if (request.Amount == payment.Amount)
    {
        payment.Status = "Refunded";
    }
    else
    {
        payment.Status = "Partially Refunded";
    }
    
    logger.LogInformation("Refund {RefundId} processed for payment {PaymentId}. Amount: {Amount}", 
        refund.RefundId, paymentId, request.Amount);
    
    return Results.Ok(refund);
});

app.MapGet("/api/payments/stats/summary", (ILogger<Program> logger) =>
{
    logger.LogInformation("Generating payment statistics summary");
    
    var stats = new
    {
        TotalPayments = payments.Count,
        CompletedPayments = payments.Count(p => p.Status == "Completed"),
        FailedPayments = payments.Count(p => p.Status == "Failed"),
        TotalAmount = payments.Where(p => p.Status == "Completed").Sum(p => p.Amount),
        AverageAmount = payments.Where(p => p.Status == "Completed").Any() 
            ? payments.Where(p => p.Status == "Completed").Average(p => p.Amount) 
            : 0,
        SuccessRate = payments.Count > 0 
            ? (double)payments.Count(p => p.Status == "Completed") / payments.Count * 100 
            : 0
    };
    
    logger.LogInformation("Payment statistics: Total={Total}, Completed={Completed}, Failed={Failed}, Success Rate={SuccessRate:F2}%",
        stats.TotalPayments, stats.CompletedPayments, stats.FailedPayments, stats.SuccessRate);
    
    return Results.Ok(stats);
});

app.Run("http://0.0.0.0:5003");

// Models
public class Payment
{
    public string PaymentId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
}

public class PaymentRequest
{
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }
}

public class Refund
{
    public string RefundId { get; set; } = string.Empty;
    public string PaymentId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime RefundDate { get; set; }
}

public class RefundRequest
{
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
}