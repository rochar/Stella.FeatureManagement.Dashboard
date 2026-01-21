var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL with a database and pgAdmin
var postgresPassword = builder.AddParameter("postgres-password", secret: true);

var postgres = builder.AddPostgres("postgres", password: postgresPassword)
.WithHostPort(5432)
//.WithDataVolume()
.WithPgAdmin()
.AddDatabase("features");

// Add your existing API project with a reference to the database
var api = builder.AddProject<Projects.Example_Api>("api")
    .WithReference(postgres)
    .WaitFor(postgres)
    .WithUrlForEndpoint("http", url => url.Url = "/features/dashboard");

// Add the React UI (Vite) with an explicitly named endpoint
builder.AddViteApp("ui", "../Stella.FeatureManagement.Dashboard.UI")
    .WithHttpEndpoint(port: 5173, name: "vite-http")
    .WithReference(api)
    .WithEnvironment("VITE_API_URL", api.GetEndpoint("http"));

builder.Build().Run();