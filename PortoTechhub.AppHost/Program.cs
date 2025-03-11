using Azure.Provisioning.PostgreSql;

var builder = DistributedApplication.CreateBuilder(args);

var messaging = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin()
    .PublishAsContainer();

var cache = builder.AddRedis("cache")
    .WithRedisInsight();

//https://github.com/dotnet/aspire/issues/6671
var todosDbName = "Todos";

var username = builder.AddParameter("username", "user", secret: true);
var password = builder.AddParameter("password", "password", secret: true);

// var postgres = builder.AddPostgres("postgres")
var postgres = builder.AddAzurePostgresFlexibleServer("postgres")
    .WithPasswordAuthentication(username, password)
    .RunAsContainer();

var todosDb = postgres.AddDatabase(todosDbName);

var apiService = builder.AddProject<Projects.PortoTechhub_ApiService>("apiservice")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithReference(todosDb)
    .WaitFor(todosDb);

var workerService = builder.AddProject<Projects.PortoTechhub_WorkerService>("workerservice")
    .WithReference(messaging)
    .WaitFor(messaging);

builder.AddProject<Projects.PortoTechhub_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
