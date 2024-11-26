using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Caching.Distributed;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.AddNpgsqlDataSource("Todos");

builder.AddRabbitMQClient("messaging", configureConnectionFactory: (connectionFactory) =>
{
    connectionFactory.ClientProvidedName = "app:event-producer";
});

builder.AddRedisDistributedCache("cache");

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var messageConnection = app.Services.GetRequiredService<IConnection>();
var messageChannel = messageConnection.CreateModel();
messageChannel.QueueDeclare("orders", durable: true, exclusive: false);

var properties = messageChannel.CreateBasicProperties();
properties.Persistent = true;
var body = Encoding.UTF8.GetBytes("Hello World!");

ActivitySource activitySource = new("Aspire.RabbitMQ.Client");
TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/weatherforecast", async (IDistributedCache cache) =>
{
    using var activity = activitySource.StartActivity($"Orders publish", ActivityKind.Producer);
    AddActivityToHeader(activity, properties);

    messageChannel.BasicPublish(exchange: string.Empty,
                                routingKey: "orders",
                                basicProperties: properties,
                                body: body);

    var cachedForecast = await cache.GetAsync("forecast");

    if (cachedForecast is null)
    {
        var summaries = new[] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };
        var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();

        await cache.SetAsync("forecast", Encoding.UTF8.GetBytes(JsonSerializer.Serialize(forecast)), new ()
        {
            AbsoluteExpiration = DateTime.Now.AddSeconds(10)
        });

        return forecast;
    }

    return JsonSerializer.Deserialize<IEnumerable<WeatherForecast>>(cachedForecast);
})
.WithName("GetWeatherForecast");

app.MapGet("/todos", async (NpgsqlConnection db) =>
{
    const string sql = """
        SELECT Id, Title, IsComplete
        FROM Todos
        """;

    return await db.QueryAsync<Todo>(sql);
});

app.MapGet("/todos/{id}", async (int id, NpgsqlConnection db) =>
{
    const string sql = """
        SELECT Id, Title, IsComplete
        FROM Todos
        WHERE Id = @id
        """;

    return await db.QueryFirstOrDefaultAsync<Todo>(sql, new { id }) is { } todo
        ? Results.Ok(todo)
        : Results.NotFound();
});

void AddActivityToHeader(Activity activity, IBasicProperties props)
{
    try
    {
        Propagator.Inject(new PropagationContext(activity.Context, Baggage.Current), props, InjectContextIntoHeader);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination_kind", "queue");
        activity?.SetTag("messaging.destination", "orders");
        activity?.SetTag("messaging.rabbitmq.routing_key", "orders");
    }
    catch(Exception ex)
    {
        var t = ex.Message;
    }
}

void InjectContextIntoHeader(IBasicProperties props, string key, string value)
{
    props.Headers ??= new Dictionary<string, object>();
    props.Headers[key] = value;
}

app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

record Todo(int Id, string Title, bool IsComplete);