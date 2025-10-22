using ClickHouse.Client.ADO;

namespace GameReleases.Infrastructure.Data;

public class ClickHouseContext
{
    private readonly string _connectionString;

    public ClickHouseContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task ExecuteNonQueryAsync(string query)
    {
        using var connection = new ClickHouseConnection(_connectionString);
        await connection.OpenAsync();
        using var command = new ClickHouseCommand();
        command.CommandText = query;
        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<T>> ExecuteQueryAsync<T>(string query) where T : class, new()
    {
        var results = new List<T>();

        using var connection = new ClickHouseConnection(_connectionString);
        await connection.OpenAsync();
        using var command = new ClickHouseCommand();
        command.CommandText = query;
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var obj = new T();
            var properties = typeof(T).GetProperties();

            foreach (var prop in properties)
            {
                if (reader[prop.Name] != DBNull.Value)
                {
                    prop.SetValue(obj, reader[prop.Name]);
                }
            }

            results.Add(obj);
        }

        return results;
    }
}