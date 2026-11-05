using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using TaskManagerCICD.Data;
using TaskManagerCICD.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSingleton(TimeProvider.System);

// A connection string vem de User Secrets (desenvolvimento) ou de variável de ambiente (servidor)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection não está configurada.");

builder.Services.AddDbContext<TaskManagerDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddScoped<ITaskService, EfTaskService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
