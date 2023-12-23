using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticKernelExperiments.Helper
{
    internal class DumpLoggingProvider : ILoggerProvider
    {
        private readonly AccumulatorLogger _logger;
        private RequestBodyLogger _httpRequestBodyLogger;

        public DumpLoggingProvider()
        {
            _logger = new AccumulatorLogger();
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

        class AccumulatorLogger : ILogger
        {
            private readonly List<string> _logs;

            public AccumulatorLogger()
            {
                _logs = new List<string>();
            }

            public IDisposable BeginScope<TState>(TState state) where TState : notnull
            {
                return new LogScope();
            }

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                var stateDictionary = ExtractDictionaryFromState(state);
                var interfaces = state.GetType().GetInterfaces().Select(i => i.Name).ToList();
                _logs.Add(formatter(state, exception));
            }

            private static Dictionary<string, object> ExtractDictionaryFromState<TState>(TState state)
            {
                Dictionary<string, object> retValue = new();
                if (state is IEnumerable en)
                {
                    foreach (var element in en)
                    {
                        if (element is KeyValuePair<string, object> stateValue)
                        {
                            retValue[stateValue.Key] = stateValue.Value;
                        }
                    }
                }
                return retValue;
            }

            public List<string> GetLogs()
            {
                return _logs;
            }
        }

        private class LogScope : IDisposable
        {
            public void Dispose()
            {
            }
        }

        sealed class RequestBodyLogger(ILogger logger) : IHttpClientAsyncLogger
        {
            public async ValueTask<object?> LogRequestStartAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
            {
                var requestContent = await request.Content!.ReadAsStringAsync(cancellationToken);

                // I need to pase the request content as json object to extract some informations.
                var jsonObject = JsonDocument.Parse(requestContent).RootElement;
                var messages = jsonObject.GetProperty("messages");

                StringBuilder sb = new();
                sb.AppendLine($"Call LLM: {request.RequestUri}");
                foreach (var message in messages.EnumerateArray())
                {
                    var content = message.GetProperty("content").GetString();
                    sb.AppendLine($"{message.GetProperty("role").GetString()}: {content}");
                }

                JsonElement tools = jsonObject.GetProperty("tools");

                sb.AppendLine("Functions:");
                foreach (JsonElement tool in tools.EnumerateArray())
                {
                    // Extracting function object
                    JsonElement function = tool.GetProperty("function");

                    // Extracting function name and description
                    string functionName = function.GetProperty("name").GetString();
                    string functionDescription = function.GetProperty("description").GetString();

                    sb.AppendLine($"Function Name: {functionName}");
                    sb.AppendLine($"Description: {functionDescription}");

                    // Extracting parameters
                    JsonElement parameters = function.GetProperty("parameters");
                    foreach (JsonProperty parameter in parameters.EnumerateObject())
                    {
                        sb.AppendLine($"Parameter name {parameter.Name} Value; {parameter.Value}");
                    }
                    sb.AppendLine();
                }

                foreach (var header in request.Headers)
                {
                    sb.AppendLine($"{header.Key}: {header.Value}");
                }

                logger.LogTrace(sb.ToString());
                return default;
            }

            public object? LogRequestStart(HttpRequestMessage request)
            {
                var requestContent = request.Content!.ReadAsStringAsync().Result;

                logger.LogTrace("Request: {Request}", request);
                logger.LogTrace("Request content: {Content}", requestContent);
                return default;
            }

            public void LogRequestStop(object? context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed)
            {
                var responseContent = response.Content.ReadAsStringAsync().Result;
                logger.LogTrace("Response: {Response}", response);
                logger.LogTrace("Response content: {Content}", responseContent);
            }
            public ValueTask LogRequestStopAsync(object? context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed, CancellationToken cancellationToken = default)
            {
                var responseContent = response.Content.ReadAsStringAsync().Result;
                logger.LogTrace("Response: {Response}", response);
                logger.LogTrace("Response content: {Content}", responseContent);
                return ValueTask.CompletedTask;
            }
            public void LogRequestFailed(object? context, HttpRequestMessage request, HttpResponseMessage? response, Exception exception, TimeSpan elapsed) { }
            public ValueTask LogRequestFailedAsync(object? context, HttpRequestMessage request, HttpResponseMessage? response, Exception exception, TimeSpan elapsed, CancellationToken cancellationToken = default) => default;
        }
    }
}
