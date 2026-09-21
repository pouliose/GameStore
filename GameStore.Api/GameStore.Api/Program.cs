using GameStore.Api.Data;
using GameStore.Api.Endpoints;
using GameStore.Api.Models;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

builder.SeedDatabase();

var app = builder.Build();

app.MapGameEndpoints();

app.MigrateDatabase();

app.Run();
