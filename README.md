# Application Insights Showcase - Microservices Solution

## Overview
This solution consists of 4 microservices built with .NET Core 8, all integrated with Azure Application Insights for comprehensive monitoring and telemetry.

### Services:
1. **InventoryService** (Port 5001) - Handles inventory and stock
2. **OrderService** (Port 5002) - Main API that orchestrates orders
3. **PaymentService** (Port 5003) - Processes payments
4. **ProductService** (Port 5004) - Manages product catalog

## Prerequisites
- .NET Core 8 SDK
- Azure subscription with Application Insights resource
- Postman (for API testing)

## Setup Instructions

### 1. Create Azure Application Insights Resource

```bash
# Login to Azure
az login

# Create a resource group
az group create --name rg-appinsights-demo --slocation eastus

# Create Application Insights
az monitor app-insights component create \
  --app appinsights-microservices-demo \
  --location eastus \
  --resource-group rg-appinsights-demo \
  --application-type web

# Get the Instrumentation Key
az monitor app-insights component show \
  --app appinsights-microservices-demo \
  --resource-group rg-appinsights-demo \
  --query instrumentationKey -o tsv
```

### 2. Configure Each Service

For each service, create an `appsettings.json` file:

**OrderService/appsettings.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=YOUR_INSTRUMENTATION_KEY;IngestionEndpoint=https://eastus-8.in.applicationinsights.azure.com/;LiveEndpoint=https://eastus.livediagnostics.monitor.azure.com/;ApplicationId=YOUR_APP_ID"
  }
}
```

Replace `YOUR_INSTRUMENTATION_KEY` with your actual key from step 1.

Create the same `appsettings.json` for ProductService, InventoryService, and PaymentService.

### 3. Create Project Files

For each service, create a `.csproj` file:

**OrderService/OrderService.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.ApplicationInsights.AspNetCore" Version="2.22.0" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
  </ItemGroup>
</Project>
```

Create similar `.csproj` files for ProductService, InventoryService, and PaymentService.

### 4. Project Structure

```
AppInsightsMicroservices/
├── OrderService/
│   ├── Program.cs
│   ├── OrderService.csproj
│   └── appsettings.json
├── ProductService/
│   ├── Program.cs
│   ├── ProductService.csproj
│   └── appsettings.json
├── InventoryService/
│   ├── Program.cs
│   ├── InventoryService.csproj
│   └── appsettings.json
└── PaymentService/
    ├── Program.cs
    ├── PaymentService.csproj
    └── appsettings.json
```

### 5. Run the Services

Open 4 terminal windows and run each service:

**Terminal 1 - OrderService:**
```bash
cd OrderService
dotnet restore
dotnet run
```

**Terminal 2 - ProductService:**
```bash
cd ProductService
dotnet restore
dotnet run
```

**Terminal 3 - InventoryService:**
```bash
cd InventoryService
dotnet restore
dotnet run
```

**Terminal 4 - PaymentService:**
```bash
cd PaymentService
dotnet restore
dotnet run
```

## Postman Collection

### Sample API Calls

**1. Get All Products**
```
GET http://localhost:5003/api/products
```

**2. Get Specific Product**
```
GET http://localhost:5003/api/products/PROD001
```

**3. Check Inventory**
```
GET http://localhost:5001/api/inventory/PROD001
```

**4. Create Order (Full Flow)**
```
POST http://localhost:5002/api/orders
Content-Type: application/json

{
  "customerId": "CUST001",
  "productId": "PROD001",
  "quantity": 2
}
```

**5. Get Payment Stats**
```
GET http://localhost:5003/api/payments/stats/summary
```

**6. Reserve Inventory**
```
POST http://localhost:5001/api/inventory/reserve
Content-Type: application/json

{
  "productId": "PROD001",
  "quantity": 5
}
```

**7. Process Refund**
```
POST http://localhost:5003/api/payments/{paymentId}/refund
Content-Type: application/json

{
  "amount": 99.99,
  "reason": "Customer request"
}
```

## Application Insights Features to Monitor

### 1. **Application Map**
- Navigate to Azure Portal → Your Application Insights resource → Application Map
- View how services communicate with each other
- See dependencies and call volumes

### 2. **Live Metrics**
- Real-time monitoring of requests, failures, and performance
- View incoming requests as they happen
- Monitor CPU, memory usage

### 3. **Performance**
- Analyze operation durations
- Identify slow dependencies
- Track service-to-service call times

### 4. **Failures**
- View failed requests and exceptions
- Analyze error rates by service
- Drill into specific failures

### 5. **Logs (Transaction Search)**
- Search for specific traces
- Filter by severity level
- Correlate logs across services

### 6. **Custom Events**
- All log statements appear as traces
- Track custom metrics
- Analyze user flows

## Key Application Insights Queries (Kusto/KQL)

Access these via Application Insights → Logs:

**1. Request Success Rate by Service:**
```kusto
requests
| summarize 
    Total = count(),
    Success = countif(success == true),
    Failed = countif(success == false),
    SuccessRate = round(100.0 * countif(success == true) / count(), 2)
  by cloud_RoleName
| order by Total desc
```

**2. Average Response Time:**
```kusto
requests
| summarize AvgDuration = avg(duration) by cloud_RoleName, name
| order by AvgDuration desc
```

**3. Failed Requests:**
```kusto
requests
| where success == false
| project timestamp, cloud_RoleName, name, resultCode, duration
| order by timestamp desc
```

**4. Dependencies Between Services:**
```kusto
dependencies
| summarize Count = count() by name, target, type
| order by Count desc
```

**5. Custom Trace Logs:**
```kusto
traces
| where message contains "Order"
| project timestamp, severityLevel, message, cloud_RoleName
| order by timestamp desc
```

**6. Exception Analysis:**
```kusto
exceptions
| summarize Count = count() by type, outerMessage
| order by Count desc
```

## Testing Scenarios

### Scenario 1: Successful Order Flow
1. Call GET `/api/products` to see available products
2. Call POST `/api/orders` with valid product
3. Monitor in Application Insights:
   - Request flow from Order → Product → Inventory → Payment
   - All logs showing success
   - Dependencies graph

### Scenario 2: Payment Failure
1. Create multiple orders (payment service has 10% failure rate)
2. Monitor failures in Application Insights
3. View exception details and failure reasons

### Scenario 3: Insufficient Inventory
1. Call POST `/api/inventory/reserve` with large quantity
2. Try creating order for same product
3. Monitor warning logs and failed order

### Scenario 4: Performance Analysis
1. Create multiple concurrent orders using Postman Runner
2. Monitor performance metrics
3. Identify bottlenecks in service calls

## Viewing Analytics in Azure Portal

1. **Navigate to Application Insights**
   - Azure Portal → Application Insights → Your resource

2. **Key Areas to Explore:**
   - **Overview**: High-level metrics
   - **Application Map**: Service topology
   - **Performance**: Response times and operations
   - **Failures**: Error analysis
   - **Live Metrics**: Real-time monitoring
   - **Logs**: Custom queries
   - **Transaction Search**: Individual request tracking

3. **End-to-End Transaction View:**
   - Go to Performance or Failures
   - Click on any operation
   - View "End-to-end transaction details"
   - See complete flow across all services

## Troubleshooting

**Issue: No data in Application Insights**
- Verify ConnectionString in appsettings.json
- Wait 2-3 minutes for data to appear
- Check if services are running
- Verify internet connectivity

**Issue: Services can't communicate**
- Ensure all services are running
- Check port numbers (5001-5004)
- Verify firewall settings

**Issue: Build errors**
- Run `dotnet restore` in each service folder
- Ensure .NET 8 SDK is installed
- Check for typos in .csproj files

## Advanced Features

### Custom Metrics
Add custom tracking to your services:

```csharp
var telemetryClient = app.Services.GetRequiredService<TelemetryClient>();
telemetryClient.TrackEvent("OrderCreated", new Dictionary<string, string> 
{
    { "OrderId", order.OrderId },
    { "Amount", order.TotalAmount.ToString() }
});
```

### Dependency Tracking
Automatically tracked for HttpClient calls, but you can add custom dependencies:

```csharp
var operation = telemetryClient.StartOperation<DependencyTelemetry>("CustomOperation");
// ... your code
telemetryClient.StopOperation(operation);
```

## Next Steps

1. Add authentication/authorization
2. Implement distributed caching with Redis
3. Add message queue (Azure Service Bus) for async processing
4. Containerize services with Docker
5. Deploy to Azure Kubernetes Service (AKS)
6. Set up Application Insights alerts for failures
7. Create custom dashboards

## Resources

- [Application Insights Documentation](https://docs.microsoft.com/azure/azure-monitor/app/app-insights-overview)
- [.NET Core 8 Documentation](https://docs.microsoft.com/dotnet/core/)
- [Kusto Query Language](https://docs.microsoft.com/azure/data-explorer/kusto/query/)