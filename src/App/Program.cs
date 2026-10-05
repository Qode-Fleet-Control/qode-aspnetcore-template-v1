var builder = WebApplication.CreateBuilder(args);

// Fleet: listen on 0.0.0.0:$PORT when the fleet (or compose) sets PORT, read at runtime.
// Without it, Kestrel keeps its usual defaults (launchSettings.json / ASPNETCORE_URLS).
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Fleet: only in development. On the fleet the edge terminates TLS and the app speaks
    // plain HTTP on $PORT, so there is no https port to redirect to.
    app.UseHttpsRedirection();
}

// Fleet: HEALTH_PATH in fleet.conf, and a root route so / answers too.
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { name = "App", status = "ok" }));

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
