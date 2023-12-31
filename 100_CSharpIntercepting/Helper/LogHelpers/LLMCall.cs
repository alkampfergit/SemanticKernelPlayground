using static LogIntercepting.Helper.DumpLoggingProvider;

namespace LogIntercepting.Helper.LogHelpers;

public class LLMCall
{
    public string CorrelationKey { get; set; }

    public string Request { get; set; }
    public string Response { get; set; }
}
