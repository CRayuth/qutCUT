using System.Text.Json;

namespace qutCUT.Utilities;

public sealed class DiskCache(string directory)
{
    public DiskCache(string directory, string subdir) : this(Path.Combine(directory, subdir)) { }

    public void Store(string key, byte[] data)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(CachePath(key), data);
    }

    public byte[]? Load(string key)
    {
        var path = CachePath(key);
        return File.Exists(path) ? File.ReadAllBytes(path) : null;
    }

    public void StoreJson<T>(string key, T value)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions.Default);
        Store(key, bytes);
    }

    public T? LoadJson<T>(string key)
    {
        var bytes = Load(key);
        return bytes is null ? default : JsonSerializer.Deserialize<T>(bytes, JsonOptions.Default);
    }

    public bool Contains(string key) => File.Exists(CachePath(key));

    public void Remove(string key)
    {
        var path = CachePath(key);
        if (File.Exists(path)) File.Delete(path);
    }

    public void Clear()
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }

    private string CachePath(string key)
    {
        // Sanitize key to be filesystem-safe
        var safe = string.Join("_", key.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(directory, safe);
    }
}
