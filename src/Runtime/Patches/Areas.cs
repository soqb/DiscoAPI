using System.Linq;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using DiscoAPI.Common.Assets;
using DiscoAPI.Runtime.Utils;
using FortressOccident;
using HarmonyLib;
using Il2CppSystem.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace DiscoAPI.Runtime.Patches;

internal static class AreaPatches
{
    [HarmonyPatch(typeof(ApplicationManager), nameof(ApplicationManager.ChangeArea))]
    [HarmonyPrefix]
    private static void OnChangeArea(ApplicationManager __instance, string areaId, string destinationId, ref bool isGameLoad)
    {
        OverlayRegistry.CleanupCurrentOverlays();

        if (__instance.CurrentSceneProperties != null)
        {
            var currentScene = __instance.CurrentSceneProperties.SceneName;
            Area? foundPresentArea = AreaUtils.FromScenePath(currentScene);
            if (foundPresentArea != null)
            {
                __instance.ScenePropertiesList.Remove(AreaUtils.GetSceneProperties(foundPresentArea));
                FastLoadManager.m_FastLoadManager.StartCoroutine(UnloadArea(foundPresentArea).WrapToIl2Cpp());
            }
        }

        Area? foundNextArea = AreaUtils.Areas.FirstOrDefault(a => a.id == areaId);
        if (foundNextArea == null) return;

        __instance.ScenePropertiesList.Add(AreaUtils.GetSceneProperties(foundNextArea));
    }

    private static System.Collections.IEnumerator UnloadArea(Area area)
    {
        var oldSceneFound = FastLoadManager.m_FastLoadManager.allScenes.TryGetValue(area.scenePath, out Scene oldScene);
        if (!oldSceneFound)
        {
            DiscoRunner.Log.LogError($"called to unload scene {area.scenePath} that was not loaded in FastLoadManager!");
            yield break;
        }

        FastLoadManager.m_FastLoadManager.allScenes.Remove(area.scenePath);

        yield return SceneManager.UnloadSceneAsync(oldScene);
        yield return null;

        Resources.UnloadUnusedAssets();
        DiscoRunner.Log.LogInfo($"unload of area '{area.id}' complete");
    }

    [HarmonyPatch(typeof(SceneTransitionManager), nameof(SceneTransitionManager.LoadSceneCoR))]
    [HarmonyPostfix]
    private static void OnLoadSceneCoRPostfix(ref IEnumerator __result, string sceneName, string destinationId, bool showLoadingScreen,
        bool hideLoadingScreen, bool isMapChanging)
    {
        var foundArea = AreaUtils.FromScenePath(sceneName); // sceneName is actually a path

        if (foundArea != null)
        {
            __result = PatchedSceneLoadRoutine(__result, foundArea).WrapToIl2Cpp();
        }
        else
        {
            __result = VanillaSceneOverlayRoutine(__result, sceneName).WrapToIl2Cpp();
        }
    }

    private static System.Collections.IEnumerator PatchedSceneLoadRoutine(IEnumerator original, Area foundArea)
    {
        var task = AreaUtils.LoadModScene(foundArea);
        yield return task.ToCoroutine();

        if (!task.Result)
        {
            DiscoRunner.Log.LogError($"failed to load scene {foundArea.scenePath}");
            yield break;
        }

        FastLoadManager.m_FastLoadManager.allScenes.TryAdd(foundArea.scenePath, SceneManager.GetSceneAt(SceneManager.sceneCount - 1));

        if (OverlayRegistry.HasPrefabOverlays(foundArea.id))
        {
            var prefabOverlays = OverlayRegistry.GetPrefabOverlaysForScene(foundArea.id);
            foreach (var prefabOverlay in prefabOverlays)
            {
                var instantiateTask = AreaUtils.InstantiatePrefabOverlay(prefabOverlay.prefabPath, prefabOverlay.sourceGuid);
                yield return instantiateTask.ToCoroutine();

                if (instantiateTask.Result == null)
                {
                    DiscoRunner.Log.LogError($"failed to instantiate prefab overlay {prefabOverlay.prefabPath}");
                }
            }
        }

        if (foundArea.vtPath != null)
        {
            var vt = VirtualTextures.CustomVirtualTextureManager.VtFromArea(foundArea);

            yield return null;

            original.MoveNext();
            yield return original.Current;

            GameObject vtContainer = new();
            vtContainer.transform.position = Vector3.zero;
            vtContainer.transform.localScale = Vector3.one * 0.55f;
            VirtualTextures.VTSceneContents.InstantiateForTexture(vtContainer.transform, vt);
        }
        else
        {
            yield return WaitFor.EndOfFrame();
        }

        if (!FastLoadManager.m_FastLoadManager.navMeshDataCollection.dict.ContainsKey(foundArea.id))
        {
            var navLoadOp = AreaUtils.LoadNavmeshData(foundArea);
            yield return navLoadOp.ToCoroutine();

            FastLoadManager.m_FastLoadManager.navMeshDataCollection.dict.Add(foundArea.id, navLoadOp.Result);
        }

        yield return original;
    }

    private static System.Collections.IEnumerator VanillaSceneOverlayRoutine(IEnumerator original, string scenePath)
    {
        yield return original;

        string sceneName = scenePath.Replace(".unity", "").Split('/')[^1];

        if (OverlayRegistry.HasPrefabOverlays(sceneName))
        {
            var prefabOverlays = OverlayRegistry.GetPrefabOverlaysForScene(sceneName);
            foreach (var prefabOverlay in prefabOverlays)
            {
                DiscoRunner.Log.LogInfo($"Instantiating prefab overlay {prefabOverlay.prefabPath} for {sceneName}");
                var instantiateTask = AreaUtils.InstantiatePrefabOverlay(prefabOverlay.prefabPath, prefabOverlay.sourceGuid);
                yield return instantiateTask.ToCoroutine();

                if (instantiateTask.Result == null)
                {
                    DiscoRunner.Log.LogError($"failed to instantiate prefab overlay {prefabOverlay.prefabPath}");
                }
            }
        }
    }

    [HarmonyPatch(typeof(NavMeshDataCollection), nameof(NavMeshDataCollection.GetNavMeshDataByScene))]
    [HarmonyPrefix]
    private static bool OnGetNavMeshDataByScene(ref NavMeshData? __result, string scenePath)
    {
        var sceneNameNoHardcodePath = scenePath[14..^6];
        var foundArea = AreaUtils.Areas.FirstOrDefault(a => a.id == sceneNameNoHardcodePath);
        if (foundArea == null) return true;

        __result = FastLoadManager.m_FastLoadManager.navMeshDataCollection.dict[sceneNameNoHardcodePath];
        return false;
    }

}