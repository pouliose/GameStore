using CsvHelper;
using GameStore.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace GameStore.Api.Data;

public static class DataExtensions
{
    public static void MigrateDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GameStoreContext>();
        dbContext.Database.Migrate();
    }

    public static void SeedDatabase(this WebApplicationBuilder builder)
    {
        const string connStringProperty = "GameStoreConnection";
        var gamesCsvPath = Path.Combine(builder.Environment.ContentRootPath, "Resources", "games.csv");

        builder.Services.AddDbContext<GameStoreContext>(options =>
            options
                .UseSqlServer(builder.Configuration.GetConnectionString(connStringProperty))
                .UseSeeding((context, _) =>
                {

                    if (context.Set<Game>().Any())
                    {
                        return;
                    }

                    if (!File.Exists(gamesCsvPath))
                    {
                        throw new FileNotFoundException("The games CSV seed file was not found.", gamesCsvPath);
                    }

                    var genres = context.Set<Genre>()
                        .ToDictionary(genre => genre.Name, StringComparer.OrdinalIgnoreCase);

                    using var reader = new StreamReader(gamesCsvPath);
                    using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                    foreach (var record in csv.GetRecords<GameCsvRecord>())
                    {
                        if (!genres.TryGetValue(record.Genre, out var genre))
                        {
                            genre = new Genre { Name = record.Genre };
                            context.Set<Genre>().Add(genre);
                            genres[record.Genre] = genre;
                        }

                        context.Set<Game>().Add(new Game
                        {
                            Name = record.Name,
                            GenreId = genre.Id,
                            Price = record.Price,
                            ReleaseDate = record.ReleaseDate
                        });
                    }

                    context.SaveChanges();
                }));
    }

    private sealed class GameCsvRecord
    {
        public required string Name { get; set; }
        public required string Genre { get; set; }
        public decimal Price { get; set; }
        public DateOnly ReleaseDate { get; set; }
    }
}
