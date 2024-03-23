using System;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using SemanticMemory.Helper;

namespace LogIntercepting.Helper;

public class Message
{
    private readonly StreamingResponse<StreamingChatCompletionsUpdate> _streamingChatCompletions;

    public Message(string role, string content)
    {
        Role = role;
        Content = content;
    }

    [JsonConstructor]
    public Message()
    {
    }

    public Message(StreamingResponse<StreamingChatCompletionsUpdate> streamingChatCompletions)
    {
        _streamingChatCompletions = streamingChatCompletions;
        Content = "";
        Role = "assistant";
        Task.Run(ReadStreamResponse);
    }

    private async Task ReadStreamResponse()
    {
        try
        {
            if (_streamingChatCompletions != null)
            {
                using (_streamingChatCompletions)
                {
                    await foreach (var update in _streamingChatCompletions)
                    {
                        Content += update.ContentUpdate;
                        OnContentChanged();
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public static Message CreateSystemMessage(string message)
    {
        return new Message("system", message);
    }

    public static Message CreateAssistantMessage(string message)
    {
        return new Message("assistant", message);
    }

    public static Message CreateUserMessage(string message)
    {
        return new Message("user", message);
    }

    public event EventHandler? ContentChanged;

    private void OnContentChanged()
    {
        ContentChanged?.Invoke(this, EventArgs.Empty);
    }

    [JsonPropertyName("role")] public string Role { get; set; } = null!;

    [JsonPropertyName("content")] public string Content { get; set; } = null!;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("function_call")]
    public SemanticMemory.Helper.FunctionCall FunctionCall { get; set; } = null!;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("tool_calls")]
    public ToolCall[] ToolCalls { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("tool_call_id")]
    public string ToolCallId { get; set; }

    public ChatRole GetChatRole()
    {
        if ("system".Equals(Role, StringComparison.OrdinalIgnoreCase))
        {
            return ChatRole.System;
        }
        else if ("assistant".Equals(Role, StringComparison.OrdinalIgnoreCase))
        {
            return ChatRole.Assistant;
        }
        else if ("user".Equals(Role, StringComparison.OrdinalIgnoreCase))
        {
            return ChatRole.User;
        }
        else
        {
            throw new System.Exception("Unknown role: " + Role);
        }
    }
}