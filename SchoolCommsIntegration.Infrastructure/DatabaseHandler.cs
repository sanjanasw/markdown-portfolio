using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace SchoolCommsIntegration.Infrastructure;

public interface IDatabaseHandler
{
    Task<T> GetFirstAsync<T>(string connectionString, string storedProcedure, DynamicParameters parameters);

    Task<IEnumerable<T>> GetListAsync<T>(string connectionString, string storedProcedure, DynamicParameters parameters);

    Task<DataTable> GetDataTableAsync(string connectionString, string storedProcedure,
        DynamicParameters? parameters = null);

    Task<int> ExecuteAsync(string connectionString, string storedProcedure, DynamicParameters? parameters = null);
}

public class DatabaseHandler : IDatabaseHandler
{
    public async Task<T> GetFirstAsync<T>(string connectionString, string storedProcedure, DynamicParameters parameters)
    {
        await using var connection = CreateConnection(connectionString);
        return await connection.QueryFirstAsync<T>(storedProcedure, parameters,
            commandType: CommandType.StoredProcedure);
    }

    public async Task<IEnumerable<T>> GetListAsync<T>(string connectionString, string storedProcedure,
        DynamicParameters parameters)
    {
        await using var connection = CreateConnection(connectionString);
        return await connection.QueryAsync<T>(storedProcedure, parameters, commandType: CommandType.StoredProcedure);
    }

    public async Task<DataTable> GetDataTableAsync(string connectionString, string storedProcedure,
        DynamicParameters? parameters = null)
    {
        await using var connection = CreateConnection(connectionString);
        await using var reader =
            await connection.ExecuteReaderAsync(storedProcedure, parameters, commandType: CommandType.StoredProcedure);

        var dataTable = new DataTable();
        dataTable.Load(reader);
        return dataTable;
    }

    public async Task<int> ExecuteAsync(string connectionString, string storedProcedure,
        DynamicParameters? parameters = null)
    {
        await using var connection = CreateConnection(connectionString);
        return await connection.ExecuteAsync(storedProcedure, parameters, commandType: CommandType.StoredProcedure);
    }

    protected virtual SqlConnection CreateConnection(string connectionString)
    {
        return new SqlConnection(connectionString);
    }
}