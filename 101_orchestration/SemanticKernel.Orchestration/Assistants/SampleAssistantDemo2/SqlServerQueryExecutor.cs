using Jarvis.Common.Shared.Utils.SqlUtils;
using Microsoft.Data.SqlClient;
using Microsoft.SemanticKernel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernel.Orchestration.Assistants.SampleAssistantDemo2;

public class SqlServerQueryExecutor : BaseAssistant, IConversationOrchestrator
{
    public SqlServerQueryExecutor() : base("SqlServerSchemaAssistant")
    {
        RegisterFunctionDelegate(
             "ExecuteQuery",
             KernelFunctionFactory.CreateFromMethod(ExecuteQuery),
             async (args) => await ExecuteQuery(
                 args["databaseName"]?.ToString(),
                 args["query"].ToString()!));
    }

    private readonly SqlServerQueryExecutorState _state = new();

    [Description("Query the database for table schema if you didn't already loaded")]
    public async Task<AssistantResponse> ExecuteQuery(
       [Description("Name of the database it can be null if the user still did not choose a database")] string? databaseName,
       [Description("Query to execute in natural language")] string query)
    {

        var sqlStringBuilder = new SqlConnectionStringBuilder(DataAccess.ConnectionString.ConnectionString);
        sqlStringBuilder.InitialCatalog = databaseName;

        System.Configuration.ConnectionStringSettings localConnection = new(
                $"SqlServer{databaseName}",
                sqlStringBuilder.ConnectionString,
                DataAccess.ConnectionString.ProviderName);

        StringBuilder sb = new();
        DataAccess
           .CreateQueryOn(
           localConnection,
           query).ExecuteReader(dr => sb.AppendLine("record"));

        return new AssistantResponse("QueryResult.", sb.ToString());
    }

    internal SqlServerQueryExecutorState GetState() => _state;

    public class SqlServerQueryExecutorState
    {
        public Dictionary<string, QueryResult> Queries { get; set; }

        public string ToPromptFact() => "QueryResult.";

        public string Demands { get; set; }
    }


    public class DataRecord
    {
        public string Column { get; set; }
        public object Value { get; set; }
    }

    public class QueryResult
    {
        public string Query { get; set; }

        public IReadOnlyCollection<DataRecord> Results { get; set; }
    }
}
