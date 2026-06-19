using System.Text.Json.Serialization;

namespace qutCUT.Agent.Clients;

public enum AnthropicModel
{
    [JsonStringEnumMemberName("claude-haiku-4-5-20251001")]
    Haiku45,
    [JsonStringEnumMemberName("claude-sonnet-4-6")]
    Sonnet46,
    [JsonStringEnumMemberName("claude-opus-4-8")]
    Opus48
}

public static class AnthropicModelIds
{
    public static string ToId(AnthropicModel model) => model switch
    {
        AnthropicModel.Haiku45  => "claude-haiku-4-5-20251001",
        AnthropicModel.Sonnet46 => "claude-sonnet-4-6",
        AnthropicModel.Opus48   => "claude-opus-4-8",
        _ => "claude-sonnet-4-6"
    };

    public static string DisplayName(AnthropicModel model) => model switch
    {
        AnthropicModel.Haiku45  => "Claude Haiku 4.5",
        AnthropicModel.Sonnet46 => "Claude Sonnet 4.6",
        AnthropicModel.Opus48   => "Claude Opus 4.8",
        _ => "Claude"
    };
}

public sealed class AnthropicMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";  // "user" | "assistant"

    [JsonPropertyName("content")]
    public List<AnthropicContent> Content { get; set; } = [];
}

public abstract class AnthropicContent
{
    [JsonPropertyName("type")]
    public abstract string Type { get; }
}

public sealed class AnthropicTextContent : AnthropicContent
{
    public override string Type => "text";

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

public sealed class AnthropicToolUseContent : AnthropicContent
{
    public override string Type => "tool_use";

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("input")]
    public System.Text.Json.JsonElement Input { get; set; }
}

public sealed class AnthropicToolResultContent : AnthropicContent
{
    public override string Type => "tool_result";

    [JsonPropertyName("tool_use_id")]
    public string ToolUseId { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("is_error")]
    public bool IsError { get; set; }
}

public sealed class AnthropicToolSchema
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("input_schema")]
    public object InputSchema { get; set; } = new();
}

// Streaming event types
public abstract class AnthropicStreamEvent
{
    public abstract string EventType { get; }
}

public sealed class StreamTextDelta(string text) : AnthropicStreamEvent
{
    public override string EventType => "text_delta";
    public string Text { get; } = text;
}

public sealed class StreamToolUse(string id, string name, string inputJson) : AnthropicStreamEvent
{
    public override string EventType => "tool_use";
    public string Id { get; } = id;
    public string Name { get; } = name;
    public string InputJson { get; } = inputJson;
}

public sealed class StreamMessageStop : AnthropicStreamEvent
{
    public override string EventType => "message_stop";
    public string StopReason { get; set; } = string.Empty;
}

public sealed class StreamError(string message) : AnthropicStreamEvent
{
    public override string EventType => "error";
    public string Message { get; } = message;
}
