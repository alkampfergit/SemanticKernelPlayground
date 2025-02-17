using System;
using System.Collections.Generic;
using System.Text;
using static SemanticKernel.Orchestration.Assistants.SampleAssistantDemo2.SqlServerQueryExecutor;
using static SemanticKernel.Orchestration.Assistants.SampleAssistantDemo2.SqlServerSchemaAssistant;

namespace SemanticKernel.Orchestration.Assistants.SampleAssistantDemo2;

public class SqlServerSharedState
{
    public SqlServerSchemaAssistantState SchemaState { get; } = new();
    public SqlServerQueryExecutorState QueryState { get; } = new();

    public string ToPromptFact()
    {
        StringBuilder sb = new();
        sb.AppendLine(SchemaState.ToPromptFact());
        sb.AppendLine(QueryState.ToPromptFact());
        return sb.ToString();
    }
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
