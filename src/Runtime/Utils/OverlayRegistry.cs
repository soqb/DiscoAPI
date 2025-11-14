using System.Collections.Generic;
using System.Linq;
using DiscoAPI.Common;
using UnityEngine;

namespace DiscoAPI.Runtime.Utils;

public static class OverlayRegistry
{
    private static readonly Dictionary<string, List<OverlayInfo>> _prefabOverlays = new();
    private static readonly List<GameObject> _currentOverlays = new();

    public record OverlayInfo(string prefabPath, string sourceGuid, int priority, string? parentName = null);

    public static void RegisterPrefabOverlay(IDiscoSource source, string baseSceneName, string prefabPath, int priority = 100, string? parentName = null)
    {
        if (!_prefabOverlays.ContainsKey(baseSceneName))
            _prefabOverlays[baseSceneName] = new List<OverlayInfo>();

        DiscoRunner.Log.LogInfo($"Registered prefab overlay {prefabPath} for {baseSceneName}" + (parentName != null ? $" (parent: {parentName})" : ""));
        _prefabOverlays[baseSceneName].Add(new OverlayInfo(prefabPath, source.Guid, priority, parentName));
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

    public static void TrackInstance(GameObject instance)
    {
        _currentOverlays.Add(instance);
    }

    public static void CleanupCurrentOverlays()
    {
        DiscoRunner.Log.LogInfo($"Cleaning up {_currentOverlays.Count} overlays");

        foreach (var instance in _currentOverlays)
        {
            if (instance != null)
                GameObject.Destroy(instance);
        }

        _currentOverlays.Clear();
    }

    public static void Clear()
    {
        _prefabOverlays.Clear();
        CleanupCurrentOverlays();
    }

    public static bool IsPartOfOverlay(GameObject obj)
    {
        if (obj == null)
            return false;

        if (_currentOverlays.Contains(obj))
            return true;

        foreach (var overlay in _currentOverlays)
        {
            if (overlay != null && obj.transform.IsChildOf(overlay.transform))
                return true;
        }

        return false;
    }
}
