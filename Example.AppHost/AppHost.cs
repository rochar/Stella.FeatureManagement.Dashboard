var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL with a database
var postgres = builder.AddPostgres("postgres")
    .AddDatabase("exampledb");

// Add your existing API project with a reference to the database
var api = builder.AddProject<Projects.Example_Api>("api")
    .WithReference(postgres)
    .WithUrlForEndpoint("http", url => url.Url = "/features/dashboard");

// Add the React UI (Vite) with an explicitly named endpoint
builder.AddViteApp("ui", "../Stella.FeatureManagement.Dashboard.UI")
    .WithHttpEndpoint(port: 5173, name: "vite-http")
    .WithReference(api)
    .WithEnvironment("VITE_API_URL", api.GetEndpoint("http"));

builder.Build().Run();