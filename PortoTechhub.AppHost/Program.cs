var builder = DistributedApplication.CreateBuilder(args);

var messaging = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin()
    .PublishAsContainer();

var cache = builder.AddRedis("cache")
    .WithRedisInsight();

var catalogDbName = "catalog";

var mysql = builder.AddMySql("mysql")
    // Set the name of the database to auto-create on container startup.
    .WithEnvironment("MYSQL_DATABASE", catalogDbName)
    // Mount the SQL scripts directory into the container so that the init scripts run.
    .WithBindMount("../config", "/docker-entrypoint-initdb.d")
    .WithDataVolume()
    // Keep the container running between app host sessions.
    .WithLifetime(ContainerLifetime.Persistent);

// Add the database to the application model so that it can be referenced by other resources.
var catalogDb = mysql.AddDatabase(catalogDbName);

var apiService = builder.AddProject<Projects.PortoTechhub_ApiService>("apiservice")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithReference(catalogDb)
    .WaitFor(catalogDb);

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
