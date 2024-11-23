using LogIntercepting.Helper.LogHelpers;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace LogIntercepting.Helper;

public class DumpLoggingProvider : ILoggerProvider
{
    private readonly AccumulatorLogger _logger;
    private RequestBodyLogger _httpRequestBodyLogger;

    public static DumpLoggingProvider Instance = null!;

    public DumpLoggingProvider()
    {
        _logger = new AccumulatorLogger();
        Instance = this;
    }

    public void StartCorrelation(string correlationKey)
    {
        _logger.CorrelationKey.Value = correlationKey;
    }

    public IHttpClientAsyncLogger CreateHttpRequestBodyLogger(ILogger logger) =>
        _httpRequestBodyLogger = new RequestBodyLogger(logger);

    public ILogger CreateLogger(string categoryName)
    {
        return _logger;
    }

    public void Dispose()
    {
    }

    public IReadOnlyDictionary<string, LlmCallData> GetLLMCalls()
    {
        return _logger.GetLLMCalls();
    }

    public IEnumerable<LogInfo> GetLogs() => _logger.GetLogs();

    public record LlmCallData(string CorrelationKey, string Prompt, List<LLMCall> LlmCalls);

    class AccumulatorLogger : ILogger
    {
        private readonly List<LogInfo> _logs;
        private readonly Dictionary<string, LlmCallData> _llmCalls;

        internal AsyncLocal<string> CorrelationKey = new AsyncLocal<string>();

        public AccumulatorLogger()
        {
            _logs = new List<LogInfo>();
            _llmCalls = new Dictionary<string, LlmCallData>();
        }

        public IReadOnlyDictionary<string, LlmCallData> GetLLMCalls() => new ReadOnlyDictionary<string, LlmCallData>(_llmCalls);

        private string GetCorrelationValue() => CorrelationKey.Value ?? "global";

        public void AddLlmCall(string question, LLMCall llmCall)
        {
            var cv = GetCorrelationValue();
            if (!_llmCalls.TryGetValue(cv, out var llmCallData))
            {
                llmCallData = new LlmCallData(cv, question, new List<LLMCall> { llmCall });
                _llmCalls[cv] = llmCallData;
            }
            else
            {
                llmCallData.LlmCalls.Add(llmCall);
            }
        }

        internal LLMCall? CompleteLLMCall(string correlationId, ResponseFunctionCall[] functions, string response)
        {
            var cv = GetCorrelationValue();
            if (_llmCalls.TryGetValue(cv, out var llmCallData))
            {
                for (int i = llmCallData.LlmCalls.Count - 1; i >= 0; i--)
                {
                    var llmCall = llmCallData.LlmCalls[i];
                    if (llmCall.CorrelationKey == correlationId)
                    {
                        llmCall.Response = response;
                        return llmCall;
                    }
                }
            }

            return null;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return new LogScope(state);
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var stateDictionary = ExtractDictionaryFromState(state);
            var interfaces = state.GetType().GetInterfaces().Select(i => i.Name).ToList();
            LogInfo logInfo = new()
            {
                CorrelationKey = GetCorrelationValue(),
                Log = formatter(state, exception),
                Parameters = stateDictionary
            };
            _logs.Add(logInfo);
        }

        private static Dictionary<string, string> ExtractDictionaryFromState<TState>(TState state)
        {
            Dictionary<string, string> retValue = new();
            if (state is IEnumerable en)
            {
                foreach (var element in en)
                {
                    if (element is KeyValuePair<string, object> stateValue)
                    {
                        retValue[stateValue.Key] = stateValue.Value?.ToString() ?? "";
                    }
                }
            }
            return retValue;
        }

        public List<LogInfo> GetLogs()
        {
            return _logs;
        }
    }

    public class LogInfo
    {
        public string CorrelationKey { get; set; }
        public string Log { get; set; }

        public Dictionary<string, string> Parameters { get; set; }
    }

    private class LogScope : IDisposable
    {
        private object _state;

        public LogScope(object state)
        {
            _state = state;
        }

        public void Dispose()
        {
        }
    }

    sealed class RequestBodyLogger(ILogger logger) : IHttpClientAsyncLogger
    {
        public async ValueTask<object?> LogRequestStartAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
        {
            var requestContent = await request.Content!.ReadAsStringAsync(cancellationToken);
            StringBuilder sb = new();

            if (request.RequestUri.Host.Contains("openai"))
            {
                var jsonObject = JsonDocument.Parse(requestContent).RootElement;
                var messages = jsonObject.GetProperty("messages");
                LLMCall lLMCall = new LLMCall()
                {
                    CorrelationKey = request.Headers.GetValues("x-ms-client-request-id").First(),
                    Request = requestContent,
                };
                DumpLoggingProvider.Instance._logger.AddLlmCall(messages.ToString(), lLMCall);
            }

            logger.LogTrace(sb.ToString());
            return default;
        }

        public ValueTask LogRequestStopAsync(object? context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed, CancellationToken cancellationToken = default)
        {
            var responseContent = response.Content.ReadAsStringAsync().Result;

            var sb = new StringBuilder();
            var functions = GetFunctionInformation(responseContent);
            foreach (var function in functions)
            {
                sb.AppendLine($"Call function {function.Function} with arguments {function.Arguments}");
            }

            sb.AppendLine($"Response: {response}");
            sb.AppendLine($"Response content: {responseContent}");

            if (request.RequestUri.Host.Contains("openai"))
            {
                var correlationId = response.Headers.GetValues("x-ms-client-request-id").First();
                var llmCall = DumpLoggingProvider.Instance._logger.CompleteLLMCall(correlationId, functions, responseContent);
                if (llmCall == null)
                {
                    return ValueTask.CompletedTask;
                }
            }

            logger.LogTrace(sb.ToString());
            return ValueTask.CompletedTask;
        }

        private static ResponseFunctionCall[] GetFunctionInformation(string responseContent)
        {
            try
            {
                var root = JsonDocument.Parse(responseContent);
                var choices = root.RootElement.GetProperty("choices");
                var message = choices[0].GetProperty("message");
                var toolCalls = message.GetProperty("tool_calls");
                return toolCalls
                    .EnumerateArray()
                    .Select(tc => new ResponseFunctionCall(
                        tc.GetProperty("function").GetProperty("name").GetString(),
                        tc.GetProperty("function").GetProperty("arguments").GetString()))
                    .ToArray();
            }
            catch (Exception)
            {
                return Array.Empty<ResponseFunctionCall>();
            }
        }

        public void LogRequestFailed(object? context, HttpRequestMessage request, HttpResponseMessage? response, Exception exception, TimeSpan elapsed) { }
        public ValueTask LogRequestFailedAsync(object? context, HttpRequestMessage request, HttpResponseMessage? response, Exception exception, TimeSpan elapsed, CancellationToken cancellationToken = default) => default;

        public object LogRequestStart(HttpRequestMessage request)
        {
            return default;
        }

        public void LogRequestStop(object context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed)
        {
        }
    }

}
public record ResponseFunctionCall(string Function, string Arguments);
