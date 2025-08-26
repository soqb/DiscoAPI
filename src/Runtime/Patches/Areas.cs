using System.Linq;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using DiscoAPI.Common.Assets;
using FortressOccident;
using HarmonyLib;
using Il2CppSystem.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace DiscoAPI.Runtime.Patches;

public static class AreaPatches
{
    [HarmonyPatch(typeof(ApplicationManager), nameof(ApplicationManager.ChangeArea))]
    [HarmonyPrefix]
    private static void OnChangeArea(ApplicationManager __instance, string areaId, string destinationId, ref bool isGameLoad)
    {
        if (__instance.CurrentSceneProperties != null)
        {
            var currentScene = __instance.CurrentSceneProperties.SceneName;
            Area? foundPresentArea = AreaUtils.FromScenePath(currentScene);
            if (foundPresentArea != null)
            {
                __instance.ScenePropertiesList.Remove(foundPresentArea.GetSceneProperties());
                FastLoadManager.m_FastLoadManager.StartCoroutine(UnloadArea(foundPresentArea).WrapToIl2Cpp());
            }
        }

        Area? foundNextArea = AreaUtils.Areas.FirstOrDefault(a => a.id == areaId);
        if (foundNextArea == null) return;
        
        __instance.ScenePropertiesList.Add(foundNextArea.GetSceneProperties());
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
        if (foundArea == null) return;
        
        __result = PatchedSceneLoadRoutine(__result, foundArea).WrapToIl2Cpp();
    }

    private static System.Collections.IEnumerator PatchedSceneLoadRoutine(IEnumerator original, Area foundArea)
    {
        yield return AreaUtils.LoadModScene(foundArea);
        
        FastLoadManager.m_FastLoadManager.allScenes.TryAdd(foundArea.scenePath, SceneManager.GetSceneAt(SceneManager.sceneCount - 1));

        yield return null;
        
        if (!FastLoadManager.m_FastLoadManager.navMeshDataCollection.dict.ContainsKey(foundArea.id))
        {
            var navLoadOp = AreaUtils.LoadNavmeshData(foundArea);
            yield return navLoadOp;
            
            FastLoadManager.m_FastLoadManager.navMeshDataCollection.dict.Add(foundArea.id, navLoadOp.Result);
        }

        while (original.MoveNext())
            yield return original.Current;
    }


    [HarmonyPatch(typeof(NavMeshDataCollection), nameof(NavMeshDataCollection.GetNavMeshDataByScene))]
    [HarmonyPrefix]
    private static bool OnGetNavMeshDataByScene(ref NavMeshData __result, string scenePath)
    {
        var sceneNameNoHardcodePath = scenePath[14..^6];
        var foundArea = AreaUtils.Areas.FirstOrDefault(a => a.id == sceneNameNoHardcodePath);
        if (foundArea == null) return true;

        __result = FastLoadManager.m_FastLoadManager.navMeshDataCollection.dict[sceneNameNoHardcodePath];
        return false;
    }

}
