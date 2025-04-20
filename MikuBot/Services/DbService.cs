using Microsoft.Data.Sqlite;

namespace TelegramBot.Services;

public static class DbService
{
    public static async Task<bool> CheckUsersPremium(long telegramId)
    {
        await using var connection = new SqliteConnection("Data Source=bot.db");
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
                                SELECT count(*) FROM PremiumUsers WHERE TelegramId = @id;
                              """;
        command.Parameters.AddWithValue("@id", telegramId);

        await using var reader = await command.ExecuteReaderAsync();
        var isPremium = await reader.ReadAsync();

        return isPremium;
    }
}