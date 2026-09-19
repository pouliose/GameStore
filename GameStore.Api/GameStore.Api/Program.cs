using GameStore.Api.Data;
using GameStore.Api.Endpoints;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

var connString = "GameStoreConnection";

builder.Services.AddDbContext<GameStoreContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString(connString)));

var app = builder.Build();

app.MapGameEndpoints();

app.MigrateDatabase();

app.Run();
