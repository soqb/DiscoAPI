using System.Collections.Generic;
using System.Linq;
using DiscoAPI.Common;

namespace DiscoAPI.Runtime.Utils;

public static class OverlayRegistry
{
    private static readonly Dictionary<string, List<OverlayInfo>> _prefabOverlays = new();

    public record OverlayInfo(string prefabPath, string sourceGuid, int priority);

    public static void RegisterPrefabOverlay(IDiscoSource source, string baseSceneName, string prefabPath, int priority = 100)
    {
        if (!_prefabOverlays.ContainsKey(baseSceneName))
            _prefabOverlays[baseSceneName] = new List<OverlayInfo>();

        DiscoRunner.Log.LogInfo($"Registered prefab overlay {prefabPath} for {baseSceneName}");
        _prefabOverlays[baseSceneName].Add(new OverlayInfo(prefabPath, source.Guid, priority));
    }

    public static IEnumerable<OverlayInfo> GetPrefabOverlaysForScene(string baseSceneName)
    {
        if (!_prefabOverlays.TryGetValue(baseSceneName, out var overlays))
            return Enumerable.Empty<OverlayInfo>();

        return overlays.OrderBy(o => o.priority);
    }

    public static bool HasPrefabOverlays(string baseSceneName)
    {
        return _prefabOverlays.ContainsKey(baseSceneName) && _prefabOverlays[baseSceneName].Count > 0;
    }

    public static void Clear()
    {
        _prefabOverlays.Clear();
    }
}
