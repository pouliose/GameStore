using GameStore.Api.Data;
using GameStore.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.SeedDatabase();

var app = builder.Build();

app.MapGameEndpoints();
app.MapGenreEndpoints();

app.MigrateDatabase();

app.Run();
