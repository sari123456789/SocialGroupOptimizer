using System.Text.Json;
using System.Text.Json.Serialization;
using MyProject.SolverService.Jobs;
using MyProject.SolverService.Solving;

// שירות עצמאי לפתרון חלוקות באמצעות Google OR-Tools CP-SAT.
// MyProject.BL שולח בקשה ב-HTTP, עוקב אחרי jobId ב-polling, ומתרגם את התוצאה חזרה לדומיין.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Singleton — מאגר בזיכרון, מנוע CP-SAT ומתזמר עבודות.
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
