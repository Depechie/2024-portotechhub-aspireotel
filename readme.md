# .NET Aspire with OpenTelemetry

## Create a new .NET Aspire project

To create a new .NET Aspire project, run the following command:

```
dotnet new aspire-starter --name PortoTechhub
dotnet new aspire-starter --output PortoTechhub
```

The `--name` parameter specifies the name of the project, and the `--output` parameter specifies the output subdirectory.

## Run the project

You need to trust the ASP.NET Core localhost certificate before running the app. Run the following command:

```
dotnet dev-certs https --trust
```

Open a new terminal window and navigate to the solution directory. Run the .NET Aspire project with following command:

```
dotnet run --project PortoTechhub.AppHost
```

> [!NOTE]
> Go over the project!  
> Explain the AppHost project and how the orchestration works with the given C# code  
> Explain the OpenTelemetry integration through the service defaults project  
> Explain other aspects of the service defaults project  
> Explain service discovery and how it is tied to environment variables  
> Explain other environment variables

## Add integrations

It is possible to add integrations to the project that are provided by .NET Aspire team.
An integration is a NuGet package that contains a set of features that can be added to the AppHost project and used/added in a client project.

Community driven integrations are also available. You can find them in the [Aspire Community GitHub repository](https://github.com/CommunityToolkit/Aspire).

### Add Redis cache integration

[Redis Output Cache](https://learn.microsoft.com/en-us/dotnet/aspire/caching/stackexchange-redis-output-caching-integration?tabs=dotnet-cli&pivots=redis)

[Redis Output Cache example](https://learn.microsoft.com/en-us/dotnet/aspire/caching/caching-integrations?tabs=dotnet-cli)

Go to the AppHost project directory and run the following command:

```
dotnet add package Aspire.Hosting.Redis
```

> [!NOTE]
> Explain the addition of the Redis cache integration in Program.cs of the AppHost project  
> Explain the WithReference extension method  

In the Web project directory run the following command

```
dotnet add package Aspire.StackExchange.Redis.OutputCaching
```

> [!NOTE]
> Explain the addition of the Redis cache integration in Program.cs of the Web project  

In the API project directory run the following command

```
dotnet add package Aspire.StackExchange.Redis.DistributedCaching
```

> [!NOTE]
> Explain the addition of the Redis cache integration in Program.cs of the Web project  

While the Aspire project is running, you can look at the Redis cache through RedisInsight and see the keys.

Look for the `forecast` key in the Redis cache.