var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<AdaptiveLearningService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCors", policy =>
    {
        policy.
            WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "http://localhost:5174",
                "http://127.0.0.1:5174",
                "https://blue-sand-0d701e30f.7.azurestaticapps.net"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Add services to the container.
// Use the Microsoft OpenAPI helpers

var app = builder.Build();

// Configure the HTTP request pipeline.
// Enable Swagger middleware and UI
app.MapOpenApi(); // Generates the JSON document
    app.UseSwaggerUI(options => 
    {
        // Point to the built-in OpenAPI JSON endpoint
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });

app.UseHttpsRedirection();

app.UseCors("FrontendCors");

app.MapControllers();

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
});

app.MapGet("/health", () => Results.Ok (new
 {
    status = "ok", 
    timeUtc = DateTimeOffset.UtcNow
}));

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
