using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BlazorDashboardKit.Abstractions;
using BlazorDashboardKit.Models;

namespace BlazorDashboardKit.Stores;

public sealed class JsonFileDashboardStore : IDashboardStore
{
    private readonly string _root;
    public JsonFileDashboardStore(string rootPath)
    {
        _root = rootPath;
        Directory.CreateDirectory(_root);
    }

    private string PathFor(string ownerKey)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ownerKey)));
        return Path.Combine(_root, hash + ".json");
    }

    public async Task<DashboardCollection?> LoadAsync(string ownerKey, CancellationToken ct = default)
    {
        var path = PathFor(ownerKey);
        if (!File.Exists(path)) return null;
        try
        {
            await using var fs = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<DashboardCollection>(fs, cancellationToken: ct);
        }
        catch (JsonException)
        {
            // A corrupt/partial/hand-edited file must not crash the dashboard.
            // Quarantine it (preserve the data for inspection, never silently
            // delete) and recover by treating the owner as having no dashboard.
            try
            {
                File.Move(path, $"{path}.corrupt-{DateTime.UtcNow:yyyyMMddHHmmssfff}", overwrite: false);
            }
            catch (IOException)
            {
                // Best-effort quarantine; recover regardless.
            }
            return null;
        }
    }

    public async Task SaveAsync(string ownerKey, DashboardCollection collection, CancellationToken ct = default)
    {
        var path = PathFor(ownerKey);
        var tmp = path + ".tmp";
        await using (var fs = File.Create(tmp))
            await JsonSerializer.SerializeAsync(fs, collection, cancellationToken: ct);
        File.Move(tmp, path, overwrite: true);
    }
}
