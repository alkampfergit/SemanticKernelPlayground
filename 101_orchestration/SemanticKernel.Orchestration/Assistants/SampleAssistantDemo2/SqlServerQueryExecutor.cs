using Jarvis.Common.Shared.Utils.SqlUtils;
using Microsoft.SemanticKernel;
using SemanticKernel.Orchestration.Helpers;
using SemanticKernel.Orchestration.Helpers.SqlUtils;
using SemanticKernel.Orchestration.Orchestrators;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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
        string realQuery = await RewriteQuery(query, databaseSchema);

        //now execute the query and return the result
        var newConnection = ConnectionManager.ChangeDatabase(DataAccess.ConnectionString, databaseName);

        var result = DataAccess
            .CreateQueryOn(newConnection, realQuery)
            .ExecuteDataset();

        var markdown = ConvertDatasetToMarkdown(result);
        SetGlobalProperty("queryresult", markdown);
        return new AssistantResponse("Query executed, result is in variable queryresult", markdown, true);
    }

    private string ConvertDatasetToMarkdown(DataSet dataSet)
    {
        var table = dataSet.Tables[0];
        StringBuilder markdown = new StringBuilder();

        // Append header row
        for (int i = 0; i < table.Columns.Count; i++)
        {
            markdown.Append($"| {table.Columns[i].ColumnName} ");
        }
        markdown.AppendLine("|");

        // Append separator row
        for (int i = 0; i < table.Columns.Count; i++)
        {
            markdown.Append("|---");
        }
        markdown.AppendLine("|");

        // Append data rows
        foreach (DataRow row in table.Rows)
        {
            for (int i = 0; i < table.Columns.Count; i++)
            {
                var cell = row[i]?.ToString() ?? string.Empty;
                // Escape pipe characters in the cell
                cell = cell.Replace("|", "\\|");
                markdown.Append($"| {cell} ");
            }
            markdown.AppendLine("|");
        }

        return markdown.ToString();
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
