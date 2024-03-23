using LogIntercepting.Helper;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SemanticMemory.Helper;

public class ApiPayload
{
    [JsonPropertyName("messages")] public List<Message> Messages { get; set; } = null!;

    [JsonPropertyName("temperature")] public double Temperature { get; set; }

    [JsonPropertyName("top_p")] public double TopP { get; set; }

    [JsonPropertyName("frequency_penalty")]
    public int FrequencyPenalty { get; set; }

    [JsonPropertyName("presence_penalty")] public int PresencePenalty { get; set; }

    [JsonPropertyName("max_tokens")] public int MaxTokens { get; set; }

    [JsonPropertyName("stop")] public string Stop { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("functions")] public OpenAiFunctionDefinition[] Functions { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("function_call")] public object FunctionsCall { get; set; }

    public string Dump()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Temperature: {Temperature}");
        sb.AppendLine($"TopP: {TopP}");
        sb.AppendLine($"FrequencyPenalty: {FrequencyPenalty}");
        sb.AppendLine($"PresencePenalty: {PresencePenalty}");
        sb.AppendLine($"MaxTokens: {MaxTokens}");
        sb.AppendLine($"Stop: {Stop}");
        sb.AppendLine($"Messages: {Messages.Count}");
        foreach (var message in Messages)
        {
            sb.AppendLine($"  {message.Role}: {message.Content}");
        }
        sb.AppendLine();
        return sb.ToString();
    }
}

public class CallSpecificFunction
{
    public CallSpecificFunction(string name)
    {
        Name = name;
    }

    [JsonPropertyName("name")] public string Name { get; set; }
}

public class ApiResponse
{
    [JsonPropertyName("id")] public string Id { get; set; } = null!;

    [JsonPropertyName("object")] public string Object { get; set; } = null!;

    [JsonPropertyName("created")] public long Created { get; set; }

    [JsonPropertyName("model")] public string Model { get; set; } = null!;

    [JsonPropertyName("choices")] public List<Choice> Choices { get; set; } = null!;

    [JsonPropertyName("usage")] public Usage Usage { get; set; } = null!;
}

public class Choice
{
    [JsonPropertyName("index")] public int Index { get; set; }

    [JsonPropertyName("finish_reason")] public string FinishReason { get; set; } = null!;

    [JsonPropertyName("message")] public Message Message { get; set; } = null!;
}

public class FunctionCall
{
    // name and argumetns properties
    [JsonPropertyName("name")] public string Name { get; set; } = null!;

    [JsonPropertyName("arguments")] public string Arguments { get; set; } = null!;
}

public class Usage
{
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("prompt_tokens")] public int PromptTokens { get; set; }

    [JsonPropertyName("total_tokens")] public int TotalTokens { get; set; }
}

public class ToolCall
{
    [JsonPropertyName("function")]
    public ToolCallFunction Function { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }
}

public class ToolCallFunction
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("arguments")]
    public string Arguments { get; set; }
}