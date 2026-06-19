namespace qutCUT.Models;

public sealed class MediaFolder
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Untitled Folder";
    public string? ParentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
