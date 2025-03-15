using Utility.OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

// Register OpenTelemetry from NuGet package
builder.Services.AddCustomOpenTelemetry(builder.Configuration);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
//app.UseMiddleware<HttpRequestLoggingMiddleware>();
app.UseMiddleware<HttpRequestLogMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();
