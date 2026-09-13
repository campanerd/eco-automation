using Dapper;

using Microsoft.Data.Sqlite;

namespace ECO.Infrastructure.Persistence;

public static class SqliteSchema
{
    public static void EnsureCreated(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        connection.Execute(
            """
            CREATE TABLE IF NOT EXISTS macro (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS macro_step (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                macro_id INTEGER NOT NULL REFERENCES macro(id),
                step_order INTEGER NOT NULL,
                delay_before_ms INTEGER NOT NULL,
                step_type TEXT NOT NULL,
                x INTEGER,
                y INTEGER,
                key_name TEXT,
                text TEXT,
                duration_ms INTEGER
            );

            CREATE TABLE IF NOT EXISTS schedule (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                macro_id INTEGER NOT NULL REFERENCES macro(id),
                scheduled_at TEXT,
                recurrence_rule TEXT,
                is_active INTEGER NOT NULL DEFAULT 1
            );
            """);
    }
}
