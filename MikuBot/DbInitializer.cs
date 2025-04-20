using Microsoft.Data.Sqlite;

namespace TelegramBot;

public static class DbInitializer
{
    public static void EnsureDatabase()
    {
        using var connection = new SqliteConnection("Data Source=bot.db");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = """
                                  CREATE TABLE IF NOT EXISTS PremiumUsers (
                                      TelegramId TEXT PRIMARY KEY
                                  );
                              """;
        command.ExecuteNonQuery();
    }
}