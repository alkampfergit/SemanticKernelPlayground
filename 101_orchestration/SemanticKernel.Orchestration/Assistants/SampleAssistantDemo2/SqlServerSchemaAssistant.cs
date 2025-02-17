using Jarvis.Common.Shared.Utils.SqlUtils;
using Microsoft.Data.SqlClient;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernel.Orchestration.Assistants.SampleAssistantDemo2;

public class SqlServerSchemaAssistant : BaseAssistant, IConversationOrchestrator
{
    private readonly SqlServerSchemaAssistantState _state = new();

    public SqlServerSchemaAssistant() : base("SqlServerSchemaAssistant")
    {
        RegisterFunctionDelegate(
             "GetDatabaseList",
             KernelFunctionFactory.CreateFromMethod(GetDatabaseList),
             async (_) => await GetDatabaseList());

        RegisterFunctionDelegate(
             "RetrieveTableSchema",
             KernelFunctionFactory.CreateFromMethod(RetrieveTableSchema),
             async (args) => await RetrieveTableSchema(
                 args["databaseName"].ToString()!));

        RegisterFunctionDelegate(
            "GetTableSchemaRepresentation",
            KernelFunctionFactory.CreateFromMethod(GetTableSchemaRepresentation),
            async (args) => await GetTableSchemaRepresentation(
                args["databaseName"].ToString()!),
            isFinal: true);
    }

    internal SqlServerSchemaAssistantState GetState() => _state;

    [Description("Get the list of the database in the server")]
    public Task<AssistantResponse> GetDatabaseList()
    {
        var databaseList = DataAccess
            .CreateQuery("SELECT name FROM sys.databases")
            .ExecuteList<string>();

        _state.DataBaseList = databaseList;
        return Task.FromResult(new AssistantResponse("retrieved list of database.", databaseList));
    }

    [Description("Get schema of tables of a database if you need the schema to answer a user question")]
    public async Task<AssistantResponse> GetTableSchemaRepresentation(
     [Description("Name of the database")] string databaseName)
    {
        if (!_state.DatabaseSchema.TryGetValue(databaseName, out var databaseSchema))
        {
            return new AssistantResponse("I don't have the schema of the database, please call RetrieveTableSchema first.");
        }

        return databaseSchema.ToPrompt();
    }

    [Description("Query the database for table schema if you didn't already loaded")]
    public async Task<AssistantResponse> RetrieveTableSchema(
        [Description("Name of the database")] string databaseName)
    {
        var sqlStringBuilder = new SqlConnectionStringBuilder(DataAccess.ConnectionString.ConnectionString);
        sqlStringBuilder.InitialCatalog = databaseName;

        System.Configuration.ConnectionStringSettings localConnection = new(
                $"SqlServer{databaseName}",
                sqlStringBuilder.ConnectionString,
                DataAccess.ConnectionString.ProviderName);

        var tableList = DataAccess
            .CreateQueryOn(
            localConnection,
            @"SELECT
                s.name AS SchemaName,
                t.name AS TableName,
                c.name AS ColumnName,
                tp.name AS DataType
            FROM sys.schemas s
            INNER JOIN sys.tables t ON s.schema_id = t.schema_id
            INNER JOIN sys.columns c ON t.object_id = c.object_id
            INNER JOIN sys.types tp ON c.user_type_id = tp.user_type_id
            ORDER BY SchemaName, TableName, ColumnName;")
            .Hydrate(dr => new RawSchemaInfo(
                dr["SchemaName"] as string,
                dr["TableName"] as string,
                dr["ColumnName"] as string,
                dr["DataType"] as string));

        //now I have all the table, for each table I need to get the columns

        var tables = tableList
            .GroupBy(t => new TableNameInfo(t.SchemaName, t.TableName))
            .Select(g => new TableInfo(
                g.Key,
                g.Select(t => new TableColumnInfo(
                    t.ColumnName,
                    t.DataType))
                .ToList()))
            .ToList();

        DatabaseSchema databaseSchema = new(tables);
        _state.DatabaseSchema[databaseName] = databaseSchema;

        return new AssistantResponse("retrieved list of tables.", databaseSchema);
    }

    public class SqlServerSchemaAssistantState
    {
        public IReadOnlyCollection<string> DataBaseList { get; set; }

        /// <summary>
        /// Database information
        /// </summary>
        public Dictionary<string, DatabaseSchema> DatabaseSchema { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public string ToPromptFact()
        {
            StringBuilder sb = new StringBuilder();

            if (DataBaseList != null)
            {
                sb.AppendLine("Database list:");
                foreach (var db in DataBaseList)
                {
                    sb.AppendLine($"{db}");
                }

                if (DatabaseSchema.Count > 0)
                {
                    sb.AppendLine("\nI already retrieved schema for the following databases. If you need to know the schema call GetTableSchemaRepresentation");
                    foreach (var dbinfo in DatabaseSchema)
                    {
                        sb.Append(dbinfo.Key);
                    }
                }
            }

            return sb.ToString();
        }
    }

    public record DatabaseSchema(IReadOnlyCollection<TableInfo> Tables)
    {
        internal AssistantResponse ToPrompt()
        {
            StringBuilder stringBuilder = new();
            foreach (var table in Tables)
            {
                stringBuilder.AppendLine($"Table {table.TableNameInfo.SchemaName}.{table.TableNameInfo.TableName}");

                foreach (var column in table.Columns)
                {
                    stringBuilder.AppendLine($"- {column.ColumnName} : {column.DataType}");
                }

                stringBuilder.AppendLine($"End schema of table {table.TableNameInfo.SchemaName}.{table.TableNameInfo.TableName}");
            }

            return stringBuilder.ToString();
        }
    }

    public record class TableNameInfo(string SchemaName, string TableName);
    public record TableInfo(TableNameInfo TableNameInfo, IReadOnlyCollection<TableColumnInfo> Columns);
    public record TableColumnInfo(string ColumnName, string DataType);

    private record RawSchemaInfo(string SchemaName, string TableName, string ColumnName, string DataType);
}
