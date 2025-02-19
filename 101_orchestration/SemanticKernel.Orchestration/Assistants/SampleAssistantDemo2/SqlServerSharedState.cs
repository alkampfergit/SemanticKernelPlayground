using OpenAI.Images;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using static SemanticKernel.Orchestration.Assistants.SampleAssistantDemo2.SqlServerQueryExecutor;
using static SemanticKernel.Orchestration.Assistants.SampleAssistantDemo2.SqlServerSchemaAssistant;

namespace SemanticKernel.Orchestration.Assistants.SampleAssistantDemo2;

public class SqlServerSharedState
{
    public SqlServerSchemaAssistantState SchemaState { get; } = new();
    public SqlServerQueryExecutorState QueryState { get; } = new();

    public SqlServerSchemaAssistant SchemaAssistant { get; private set; }
    public SqlServerQueryExecutor QueryExecutor { get; private set; }

    public void SetSchemaAssistant(SqlServerSchemaAssistant assistant) => SchemaAssistant = assistant;
    public void SetQueryExecutor(SqlServerQueryExecutor executor) => QueryExecutor = executor;

    public string CurrentDatabase { get; set; }

    public string ToPromptFact()
    {
        StringBuilder sb = new();
        sb.AppendLine(SchemaState.ToPromptFact());
        sb.AppendLine(QueryState.ToPromptFact());
        if (!string.IsNullOrEmpty(CurrentDatabase))
        {
            sb.AppendLine($"User choose to work with database is {CurrentDatabase}");
        }
        return sb.ToString();
    }

    internal void SetCurrentDatabase(string database)
    {
        CurrentDatabase = database;
    }

    public Dictionary<string, DataSet> QueryResults { get; set; } = new();
}

public class SqlServerSchemaAssistantState
{
    public IReadOnlyCollection<string> DataBaseList { get; set; }
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

public class SqlServerQueryExecutorState
{
    public Dictionary<string, QueryResult> Queries { get; set; }
    public string Demands { get; set; }
    public string ToPromptFact() => "QueryResult.";
}

public record class TableNameInfo(string SchemaName, string TableName);
public record TableInfo(TableNameInfo TableNameInfo, IReadOnlyCollection<TableColumnInfo> Columns);
public record TableColumnInfo(string ColumnName, string DataType);

public record RawSchemaInfo(string SchemaName, string TableName, string ColumnName, string DataType);
