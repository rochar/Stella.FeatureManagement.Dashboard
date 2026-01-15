# Stella.FeatureManagement.Dashboard
Adds a ready-to-use web dashboard to ASP.NET Core applications for managing feature flags powered by Microsoft.FeatureManagement. It provides a web UI and REST APIs to view, enable, disable, and configure feature flags at runtime.

![Dashboard UI](./docs/dashboard.png)

## Installation

Install via NuGet Package Manager:

```bash
dotnet add package Stella.FeatureManagement.Dashboard
```

Or via Package Manager Console:

```powershell
Install-Package Stella.FeatureManagement.Dashboard
```

## Quick Start
### 1. Configure Feature Flags

Add your feature flags to `appsettings.json`:

```json
{
  "FeatureManagement": {
    "BetaFeature": true
  }
}
```

### 2. Register Services

In your `Program.cs`, add the Feature Management services with the dashboard:

```csharp
...
using Stella.FeatureManagement.Dashboard;
...
// Add Feature Management with Dashboard
builder.Services.AddFeatureManagement()
    .AddDashboard();

var app = builder.Build();

// Map the dashboard endpoints
app.UseDashboard();
    

app.Run();
```

### 3. Access the Dashboard

Once your application is running, access the web dashboard at:

```
https://localhost:<port>/features/dashboard/
```

### 4. REST API

You can also query feature flags programmatically via the REST API:

```bash
# Get all features
GET /features

# Check if a specific feature is enabled
GET /features/BetaFeature

# Response:
# true 
```

