using System.Text.Json;
using qutCUT.Agent.Clients;
using qutCUT.Utilities;

namespace qutCUT.Agent;

public sealed class ChatSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "New Chat";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<ChatMessage> Messages { get; set; } = [];
}

public sealed class ChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    public List<ToolCall>? ToolCalls { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public sealed class ToolCall
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string InputJson { get; set; } = string.Empty;
    public string? ResultJson { get; set; }
    public bool IsError { get; set; }
}

public sealed class ChatSessionStore(string sessionsDirectory)
{
    public List<ChatSession> LoadAll()
    {
        if (!Directory.Exists(sessionsDirectory)) return [];
        return Directory.EnumerateFiles(sessionsDirectory, "*.json")
            .Select(f =>
            {
                try { return JsonSerializer.Deserialize<ChatSession>(File.ReadAllText(f), JsonOptions.Default); }
                catch { return null; }
            })
            .Where(s => s is not null)
            .Select(s => s!)
            .OrderByDescending(s => s.UpdatedAt)
            .ToList();
    }

    public void Save(ChatSession session)
    {
        Directory.CreateDirectory(sessionsDirectory);
        var path = Path.Combine(sessionsDirectory, $"{session.Id}.json");
        session.UpdatedAt = DateTime.UtcNow;
        File.WriteAllText(path, JsonSerializer.Serialize(session, JsonOptions.Default));
    }

    public void Delete(string sessionId)
    {
        var path = Path.Combine(sessionsDirectory, $"{sessionId}.json");
        if (File.Exists(path)) File.Delete(path);
    }
}
