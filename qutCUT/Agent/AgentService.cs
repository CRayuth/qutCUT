using CommunityToolkit.Mvvm.ComponentModel;
using qutCUT.Agent.Clients;
using qutCUT.Utilities;

namespace qutCUT.Agent;

public sealed partial class AgentService : ObservableObject
{
    [ObservableProperty] private AnthropicModel _model = AnthropicModel.Sonnet46;
    [ObservableProperty] private string _apiKey = string.Empty;
    [ObservableProperty] private bool _isStreaming;
    [ObservableProperty] private string _streamingText = string.Empty;

    public ChatSession ActiveSession { get; private set; } = new();
    public List<ChatSession> Sessions { get; private set; } = [];

    private readonly ChatSessionStore _store;
    private AnthropicClient? _client;
    private CancellationTokenSource? _currentStream;

    public AgentService(string sessionsDirectory)
    {
        _store = new ChatSessionStore(sessionsDirectory);
        Sessions = _store.LoadAll();
        ApiKey = CredentialStore.Load(CredentialStore.Keys.AnthropicApiKey) ?? string.Empty;
    }

    public void SetApiKey(string key)
    {
        ApiKey = key;
        CredentialStore.Save(CredentialStore.Keys.AnthropicApiKey, key);
        _client?.Dispose();
        _client = string.IsNullOrWhiteSpace(key) ? null : new AnthropicClient(key);
    }

    public void NewSession()
    {
        StopStream();
        ActiveSession = new ChatSession();
        Sessions.Insert(0, ActiveSession);
        OnPropertyChanged(nameof(Sessions));
    }

    public void SelectSession(string id)
    {
        StopStream();
        ActiveSession = Sessions.FirstOrDefault(s => s.Id == id) ?? ActiveSession;
        OnPropertyChanged(nameof(ActiveSession));
    }

    public async Task SendAsync(
        string userText,
        string systemPrompt,
        List<AnthropicToolSchema> tools,
        Func<string, string, string, Task<string>> onToolCall,
        Action<string>? onTextDelta = null,
        CancellationToken ct = default)
    {
        if (_client is null) throw new InvalidOperationException("API key not configured.");

        var userMsg = new ChatMessage { Role = "user", Content = userText };
        ActiveSession.Messages.Add(userMsg);

        _currentStream = CancellationTokenSource.CreateLinkedTokenSource(ct);
        IsStreaming = true;
        StreamingText = string.Empty;

        try
        {
            var anthropicMessages = BuildAnthropicMessages();
            anthropicMessages.Add(new AnthropicMessage
            {
                Role = "user",
                Content = [new AnthropicTextContent { Text = userText }]
            });

            await RunAgentLoopAsync(systemPrompt, anthropicMessages, tools, onToolCall, onTextDelta, _currentStream.Token);
        }
        finally
        {
            IsStreaming = false;
            _store.Save(ActiveSession);
        }
    }

    private async Task RunAgentLoopAsync(
        string system,
        List<AnthropicMessage> messages,
        List<AnthropicToolSchema> tools,
        Func<string, string, string, Task<string>> onToolCall,
        Action<string>? onTextDelta,
        CancellationToken ct)
    {
        while (true)
        {
            var assistantText  = new System.Text.StringBuilder();
            var pendingTools   = new List<ToolCall>();
            var stopReason     = string.Empty;

            await foreach (var evt in _client!.StreamAsync(Model, system, messages, tools, ct: ct))
            {
                switch (evt)
                {
                    case StreamTextDelta delta:
                        assistantText.Append(delta.Text);
                        StreamingText = assistantText.ToString();
                        onTextDelta?.Invoke(delta.Text);
                        break;

                    case StreamToolUse toolUse:
                        pendingTools.Add(new ToolCall
                        {
                            Id        = toolUse.Id,
                            Name      = toolUse.Name,
                            InputJson = toolUse.InputJson
                        });
                        break;

                    case StreamMessageStop stop:
                        stopReason = stop.StopReason;
                        break;

                    case StreamError err:
                        Log.Agent.LogError("Stream error: {msg}", err.Message);
                        return;
                }
            }

            // Record assistant turn
            var assistantContents = new List<AnthropicContent>();
            if (assistantText.Length > 0)
                assistantContents.Add(new AnthropicTextContent { Text = assistantText.ToString() });
            foreach (var tc in pendingTools)
                assistantContents.Add(new AnthropicToolUseContent { Id = tc.Id, Name = tc.Name });

            messages.Add(new AnthropicMessage { Role = "assistant", Content = assistantContents });
            ActiveSession.Messages.Add(new ChatMessage
            {
                Role        = "assistant",
                Content     = assistantText.ToString(),
                ToolCalls   = pendingTools.Count > 0 ? [.. pendingTools] : null
            });

            if (stopReason != "tool_use" || pendingTools.Count == 0) break;

            // Execute tools and send results back
            var toolResults = new List<AnthropicContent>();
            foreach (var tc in pendingTools)
            {
                string resultJson;
                bool isError = false;
                try { resultJson = await onToolCall(tc.Id, tc.Name, tc.InputJson); }
                catch (Exception ex) { resultJson = ex.Message; isError = true; }

                tc.ResultJson = resultJson;
                tc.IsError    = isError;
                toolResults.Add(new AnthropicToolResultContent
                {
                    ToolUseId = tc.Id,
                    Content   = resultJson,
                    IsError   = isError
                });
            }

            messages.Add(new AnthropicMessage { Role = "user", Content = toolResults });
        }
    }

    public void StopStream()
    {
        _currentStream?.Cancel();
        _currentStream = null;
        IsStreaming = false;
    }

    private List<AnthropicMessage> BuildAnthropicMessages()
    {
        var list = new List<AnthropicMessage>();
        foreach (var msg in ActiveSession.Messages)
        {
            var content = new List<AnthropicContent>
            {
                new AnthropicTextContent { Text = msg.Content }
            };
            list.Add(new AnthropicMessage { Role = msg.Role, Content = content });
        }
        return list;
    }
}
