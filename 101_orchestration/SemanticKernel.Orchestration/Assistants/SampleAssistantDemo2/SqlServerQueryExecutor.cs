using Jarvis.Common.Shared.Utils.SqlUtils;
using Microsoft.Data.SqlClient;
using Microsoft.SemanticKernel;
using SemanticKernel.Orchestration.Helpers;
using SemanticKernel.Orchestration.Orchestrators;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernel.Orchestration.Assistants.SampleAssistantDemo2;

public class SqlServerQueryExecutor : BaseAssistant, IConversationOrchestrator
{
    private SqlServerSharedState _sharedState;

    public SqlServerQueryExecutor(
        KernelStore kernelStore,
        IUserQuestionManager userQuestionManager) : base("SqlServerSchemaAssistant")
    {
        RegisterFunctionDelegate(
             "ExecuteQuery",
             KernelFunctionFactory.CreateFromMethod(ExecuteQuery),
             async (args) => await ExecuteQuery(
                 args["databaseName"]?.ToString(),
                 args["query"].ToString()!));
        _kernelStore = kernelStore;
        _userQuestionManager = userQuestionManager;
    }

    public void InitializeWithSharedState(SqlServerSharedState sharedState)
    {
        _sharedState = sharedState;
        _sharedState.SetQueryExecutor(this);
    }

    private readonly SqlServerQueryExecutorState _state = new();
    private readonly KernelStore _kernelStore;
    private readonly IUserQuestionManager _userQuestionManager;

    [Description("Query the database for table schema if you didn't already loaded")]
    public async Task<AssistantResponse> ExecuteQuery(
       [Description("Name of the database it can be null if the user still did not choose a database")] string? databaseName,
       [Description("Query to execute in natural language")] string query)
    {
        bool missingData = false;
        if (string.IsNullOrEmpty(databaseName))
        {
            databaseName = await InnerChooseDatabase();
            missingData = true;
        }

        //ok we know the database name, now we need to know if we really know the schema of the database
        if (!_sharedState.SchemaState.DatabaseSchema.TryGetValue(databaseName, out var databaseSchema))
        {
            //ok we do not have database schema
            databaseSchema = _sharedState.SchemaAssistant.InnerGetDatabaseSchema(databaseName);
            missingData = true;
        }

        //ok now we are sure that database is the currect one and also that we have the schema
        string realQuery = query;
        if (missingData)
        { 
            //if we have missing data it means that the query we received is probably not generated
            //from a schema, so we need to rewrite.
            realQuery = await RewriteQuery(query, databaseSchema);
        }

        throw new NotImplementedException();
    }

    private async Task<string> RewriteQuery(string query, SqlServerSchemaAssistant.DatabaseSchema databaseSchema)
    {
        //Define the function
        var describeLambda = [Description("Execute a query against database")] (
            [Description("SQL query in SQL Server syntax")] string query
        ) =>
        {
            return;
        };
        var function = KernelFunctionFactory.CreateFromMethod(describeLambda, "describe");
        var settings = new PromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Required([function], autoInvoke: false)
        };

        string prompt = $@"You are a SQL Server expert, you will be given a query descripted in natural
language form or in SQL form and you need to generate a valid SQL query given the following schema of the database.

Database schema: {databaseSchema.ToPrompt()}
User Query: {query}";

        var smartKernel = _kernelStore.GetKernel("gpt4o");
        var result = await smartKernel.InvokePromptAsync(prompt, new(settings));
        var content = result.GetValue<ChatMessageContent>();
        var functionResponse = content.Items.OfType<FunctionCallContent>().SingleOrDefault();
        
        return functionResponse.Arguments["query"].ToString();
    }

    private async Task<string> InnerChooseDatabase()
    {
        //the model did not choose a database yet, we can proceed
        var dbList = _sharedState.SchemaAssistant.InnerGetDatabaseList();

        //now let the user choose the database
        var databaseName = await _userQuestionManager.AskForSelectionAsync(
            "Please choose a database",
            dbList);
        _sharedState.SetCurrentDatabase(databaseName);
        //now that we have database list we can proceed.
        return databaseName;
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
