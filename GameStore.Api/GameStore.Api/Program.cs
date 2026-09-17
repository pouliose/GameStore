using GameStore.Api;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

List<GameDto> games =
[
    new(1, "The Legend of Zelda: Breath of the Wild", "Action-adventure", 59.99m, new DateOnly(2017, 3, 3)),
    new (2, "Super Mario Odyssey", "Platformer", 49.99m, new DateOnly(2017, 10, 27)),
    new (3, "Red Dead Redemption 2", "Action-adventure", 39.99m, new DateOnly(2018, 10, 26)),
    new (4, "The Witcher 3: Wild Hunt", "Action RPG", 29.99m, new DateOnly(2015, 5, 19)),
    new (5, "Minecraft", "Sandbox", 26.95m, new DateOnly(2011, 11, 18))
];


//GET /games
app.MapGet("/games", () => games);

const string GetGameEndpointName = "GetGameById";

//GET games/{id}
app.MapGet("/games/{id}", (int id) =>
{
    var game = games.FirstOrDefault(g => g.Id == id);
    return game is not null ? Results.Ok(game) : Results.NotFound();
}).WithName(GetGameEndpointName);

app.MapPost("/games", (CreateGameDto createGameDto) =>
{
    var newId = games.Max(g => g.Id) + 1;
    var newGame = new GameDto(newId, createGameDto.Name, createGameDto.Genre, createGameDto.Price, createGameDto.ReleaseDate);
    games.Add(newGame);
    return Results.CreatedAtRoute(GetGameEndpointName, new { id = newId }, newGame);
});

app.Run();
