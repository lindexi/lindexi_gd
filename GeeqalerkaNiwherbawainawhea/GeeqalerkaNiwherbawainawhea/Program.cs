using System.Data;
using System.Data.SQLite;
using System.Diagnostics;

namespace GeeqalerkaNiwherbawainawhea;

internal class Program
{
    static async Task Main(string[] args)
    {
        var dbFileAbsolutePath = @"SQLite.db3";
        var connectionString = $"data source = {dbFileAbsolutePath}; version = 3;";

        using var connection = new SQLiteConnection(connectionString);
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        using var queryCommand = new SQLiteCommand(connection);
        queryCommand.CommandText = @"SELECT *
FROM   [Words]
WHERE  [Word] = @Word";
        var parameter = queryCommand.CreateParameter();
        parameter.ParameterName = "@Word";
        queryCommand.Parameters.Add(parameter);

        for (int i = 0; i < 10000; i++)
        {
            parameter.Value = "xxxxxxx" + i;

            await using var dataReader = await queryCommand.ExecuteReaderAsync();

            while (await dataReader.ReadAsync())
            {
                var word = dataReader.GetString("Word");
                var pronounce = dataReader.GetString("Pronounce");
            }
        }

        //using var dataAdapter = new SQLiteDataAdapter(queryCommand);
        //dataAdapter.Fill(ds);

        stopwatch.Stop(); // 1.7 秒
        stopwatch.Restart();

        using (SQLiteTransaction transaction = connection.BeginTransaction())
        {
            using var command = new SQLiteCommand(connection);
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO [Words] ( [Word],  [Pronounce]) VALUES (@Word, @Pronounce)";

            var parameter1 = command.CreateParameter();
            parameter1.ParameterName = "@Word";
            var parameter2 = command.CreateParameter();
            parameter2.ParameterName = "@Pronounce";

            command.Parameters.Add(parameter1);
            command.Parameters.Add(parameter2);

            for (int i = 0; i < 10000_00; i++)
            {
                //command.Parameters.AddWithValue("@Word","xxxxx"+i);
                //command.Parameters.AddWithValue("@Pronounce", "1");
                parameter1.Value = "xxxxxxx" + i;
                parameter2.Value = i.ToString();

                await command.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }

        // 5 秒
        stopwatch.Stop();
    }
}
