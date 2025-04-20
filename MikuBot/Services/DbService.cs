using Microsoft.Data.Sqlite;

namespace TelegramBot.Services;

public static class DbService
{
    public static async Task<bool> CheckUsersPremium(long telegramId)
    {
        await using var connection = new SqliteConnection("Data Source=db/bot.db");
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM PremiumUsers WHERE TelegramId = @id;";
        command.Parameters.AddWithValue("@id", telegramId);

        var result = await command.ExecuteScalarAsync();
        var count = Convert.ToInt32(result);

        return count == 1;
    }
}