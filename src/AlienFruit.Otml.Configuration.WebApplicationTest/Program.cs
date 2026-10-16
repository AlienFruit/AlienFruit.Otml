using AlienFruit.Otml.Configuration.Extensions;
using AlienFruit.Otml.Configuration.Keyraft;
using AlienFruit.Otml.Configuration.WebApplicationTest;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddKeyraftSecretProvider(o =>
{
    o.BaseUrl = "http://localhost:7200";
    o.Token = "XXXX";
    o.Namespace = "oracular/production";
});
builder.Services.AddEnvironmentVariableSecretProvider();
builder.Configuration.AddOtmlConfigurationFile("appsettings.otml");

var testConfig = builder.Configuration.GetSection(TestConfiguration.Name)
    .Get<TestConfiguration>();


var app = builder.Build();

// Configure the HTTP request pipeline.

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
});

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
