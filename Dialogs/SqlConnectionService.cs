using Microsoft.Data.SqlClient;

namespace VS.Toolset.UI.Dialogs;

/// <summary>A verified set of SQL Server connection settings; no connection remains open.</summary>
public sealed class SqlConnectionResult
{
    internal SqlConnectionResult(string connectionString) => ConnectionString = connectionString;

    /// <summary>Gets the connection string, which can contain a password and must be protected by the caller.</summary>
    public string ConnectionString { get; }
}

/// <summary>Provides the SQL operations needed by the connection dialog.</summary>
public interface ISqlConnectionService
{
    /// <summary>Opens and verifies a connection.</summary>
    Task TestAsync(string connectionString, CancellationToken cancellationToken);

    /// <summary>Returns databases visible to the connection's principal.</summary>
    Task<IReadOnlyList<string>> GetDatabasesAsync(string connectionString, CancellationToken cancellationToken);
}

/// <summary>Connects to SQL Server using Microsoft.Data.SqlClient.</summary>
public sealed class SqlConnectionService : ISqlConnectionService
{
    /// <inheritdoc />
    public async Task TestAsync(string connectionString, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetDatabasesAsync(string connectionString, CancellationToken cancellationToken)
    {
        var builder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "" };
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sys.databases WHERE state = 0 AND HAS_DBACCESS(name) = 1 ORDER BY name";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var databases = new List<string>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            databases.Add(reader.GetString(0));
        return databases;
    }
}
