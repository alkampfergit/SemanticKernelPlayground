using Microsoft.SemanticKernel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticKernel.Orchestration.Assistants.SampleAssistantDemo2;

internal class SqlServerAssistant : BaseAssistant
{
    public SqlServerAssistant() : base("SqlServerAssistant")
    {
       
    }

    private async Task<string> GetSqlServerVersion(IDictionary<string, object> arguments)
    {
        return "SQL Server 2019";
    }
}
