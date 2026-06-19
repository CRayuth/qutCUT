using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using qutCUT.Utilities;

namespace qutCUT.Agent.Clients;

public sealed class AnthropicClient : IDisposable
{
    private const string BaseUrl   = "https://api.anthropic.com/v1/messages";
    private const string ApiVersion = "2023-06-01";

    private readonly HttpClient _http;

    public AnthropicClient(string apiKey)
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("x-api-key", apiKey);
        _http.DefaultRequestHeaders.Add("anthropic-version", ApiVersion);
        _http.DefaultRequestHeaders.Add("anthropic-beta", "interleaved-thinking-2025-05-14");
        _http.Timeout = TimeSpan.FromMinutes(10);
    }

    // Streams content events from the Anthropic API.
    public async IAsyncEnumerable<AnthropicStreamEvent> StreamAsync(
        AnthropicModel model,
        string systemPrompt,
        List<AnthropicMessage> messages,
        List<AnthropicToolSchema>? tools = null,
        int maxTokens = 8192,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var body = new JsonObject
        {
            ["model"]      = AnthropicModelIds.ToId(model),
            ["max_tokens"] = maxTokens,
            ["stream"]     = true,
            ["system"]     = systemPrompt,
            ["messages"]   = JsonSerializer.SerializeToNode(messages, JsonOptions.Default)
        };

        if (tools?.Count > 0)
            body["tools"] = JsonSerializer.SerializeToNode(tools, JsonOptions.Default);

        var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl)
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json")
        };

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            yield return new StreamError(ex.Message);
            yield break;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        // Accumulate tool input JSON for streamed tool_use blocks
        var pendingToolId    = string.Empty;
        var pendingToolName  = string.Empty;
        var pendingToolInput = new StringBuilder();

        string? line;
        while ((line = await reader.ReadLineAsync(ct)) is not null)
        {
            if (!line.StartsWith("data: ", StringComparison.Ordinal)) continue;
            var data = line["data: ".Length..];
            if (data == "[DONE]") break;

            JsonNode? node;
            try { node = JsonNode.Parse(data); }
            catch { continue; }

            var type = node?["type"]?.GetValue<string>();

            switch (type)
            {
                case "content_block_start":
                {
                    var blockType = node?["content_block"]?["type"]?.GetValue<string>();
                    if (blockType == "tool_use")
                    {
                        pendingToolId   = node?["content_block"]?["id"]?.GetValue<string>() ?? "";
                        pendingToolName = node?["content_block"]?["name"]?.GetValue<string>() ?? "";
                        pendingToolInput.Clear();
                    }
                    break;
                }

                case "content_block_delta":
                {
                    var deltaType = node?["delta"]?["type"]?.GetValue<string>();
                    if (deltaType == "text_delta")
                    {
                        var text = node?["delta"]?["text"]?.GetValue<string>() ?? "";
                        if (!string.IsNullOrEmpty(text))
                            yield return new StreamTextDelta(text);
                    }
                    else if (deltaType == "input_json_delta")
                    {
                        pendingToolInput.Append(node?["delta"]?["partial_json"]?.GetValue<string>() ?? "");
                    }
                    break;
                }

                case "content_block_stop":
                {
                    if (!string.IsNullOrEmpty(pendingToolId))
                    {
                        yield return new StreamToolUse(pendingToolId, pendingToolName, pendingToolInput.ToString());
                        pendingToolId   = string.Empty;
                        pendingToolName = string.Empty;
                        pendingToolInput.Clear();
                    }
                    break;
                }

                case "message_delta":
                {
                    var stopReason = node?["delta"]?["stop_reason"]?.GetValue<string>() ?? "";
                    yield return new StreamMessageStop { StopReason = stopReason };
                    break;
                }

                case "error":
                {
                    var msg = node?["error"]?["message"]?.GetValue<string>() ?? "Unknown error";
                    yield return new StreamError(msg);
                    break;
                }
            }
        }
    }

    public void Dispose() => _http.Dispose();
}
