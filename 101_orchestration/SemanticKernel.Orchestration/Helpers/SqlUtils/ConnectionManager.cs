using Microsoft.Data.SqlClient;
using System.Configuration;

namespace SemanticKernel.Orchestration.Helpers.SqlUtils;

public static class ConnectionManager
{
    public static ConnectionStringSettings ChangeDatabase(ConnectionStringSettings originalConnection, string newDatabaseName)
    {
        var sqlStringBuilder = new SqlConnectionStringBuilder(originalConnection.ConnectionString);
        sqlStringBuilder.InitialCatalog = newDatabaseName;

        System.Configuration.ConnectionStringSettings localConnection = new(
                $"SqlServer{newDatabaseName}",
                sqlStringBuilder.ConnectionString,
                originalConnection.ProviderName);

        return localConnection;
    }
}
