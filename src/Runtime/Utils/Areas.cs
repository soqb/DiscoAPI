using System.Linq;
using DiscoAPI.Common.Assets;
using DiscoAPI.Runtime.Assets;
using FortressOccident;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Voidforge;

namespace DiscoAPI.Runtime.Utils;

public static class AreaUtils
{
    public static IAssetArena<Area> Areas => DiscoRunner.manager.Assets.GetArena<Area>();
    public static bool IsModdedArea(string scenePath) => Areas.Any(a => a.scenePath == scenePath);
    public static Area? FromScenePath(string scenePath) => Areas.FirstOrDefault(a => a.scenePath == scenePath);
    public static Area? FromSceneName(string sceneName) => Areas.FirstOrDefault(a => a.scenePath.Contains(sceneName));

    public static async DiscoTask<bool> LoadModScene(Area area)
    {
        var src = DiscoRunner.GetSource(area.source!);
        if (src == null) return false;
        var bundle = await src.Router.SceneBundle.Get();

        if (bundle == null)
        {
            DiscoRunner.Log.LogError($"a request to load scene bundle containing {area.scenePath} failed!");
            return false;
        }

        var foundScenePath = bundle.GetAllScenePaths().FirstOrDefault(s => s == area.scenePath);
        if (foundScenePath == null)
        {
            DiscoRunner.Log.LogError($"could not load {area.scenePath} from bundle {bundle.name}!");
            return false;
        }

        await SceneManager.LoadSceneAsync(foundScenePath, LoadSceneMode.Additive);
        return true;
    }

    public static async DiscoTask<NavMeshData?> LoadNavmeshData(Area area)
    {
        var src = DiscoRunner.GetSource(area.source!);
        if (src == null) return null;
        var navmesh = await src.Router.NavMeshes.Get(area.navMeshPath);
        if (navmesh == null)
        {
            DiscoRunner.Log.LogError($"failed to load navmesh data at {area.navMeshPath} for source {area.source}");
            return null;
        }
        return navmesh;
    }

    public static SceneProperties GetSceneProperties(Area area)
    {
        // note: SceneProperties.DisplayName returns null due to missing localization
        return new SceneProperties()
        {
            SceneName = area.scenePath,
            SceneId = area.id,
            SaveGameId = area.id,
            VisualRadius = area.visualRadius,
            IsOutside = area.isOutside,
            IsDreamScene = area.isDreamScene,
            HasCustomZoomLimits = area.hasCustomZoomLimits,
            CustomMinimumZoomLimit = area.customMinZoomLimit,
            CustomMaximumZoomLimit = area.customMaxZoomLimit
        };
    }
    
    public static void ChangeArea(IAssetRef<Area> area, string destinationId, bool isGameLoad, bool showLoadingScreen,
        bool hideLoadingScreen)
    {
        SingletonScriptable<ApplicationManager>.Singleton.ChangeArea(area.Location.id.ToString(), destinationId, isGameLoad, showLoadingScreen, hideLoadingScreen);
    }
}