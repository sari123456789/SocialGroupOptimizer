using System.Text.Json;
using System.Text.Json.Serialization;
using MyProject.SolverService.Jobs;
using MyProject.SolverService.Solving;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<ISolverJobStore, InMemorySolverJobStore>();
builder.Services.AddSingleton<CpSatPlacementSolver>();
builder.Services.AddSingleton<SolverJobProcessor>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
