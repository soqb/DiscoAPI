using System;
using System.Collections.Generic;
using CollageMode;
using DiscoAPI.Common.Assets;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using SM = Sunshine.Metric;
using PC = PixelCrushers.DialogueSystem;

namespace DiscoAPI.Runtime;

public static class LuaConsoleManager
{
	public static void AttachLuaConsole()
	{
		GameObject obj = new GameObject("luaconsolemgr");
		GameObject.DontDestroyOnLoad(obj);

		var console = obj.AddComponent<PC.LuaConsole>();
		console.firstKey = KeyCode.LeftControl;
		console.secondKey = KeyCode.Return;
	}
}
public class Catalogue
{
	private CollageMode.OperationsSetCatalogue inner;

	public Catalogue(OperationsSetCatalogue inner)
	{
		this.inner = inner;
	}

	public GameObject GetCharacter(string name)
	{
		var op = inner.GetOperations<CollageMode.PlaceCharacterOperation>()[name];
		return op.Cast<CollageMode.PlaceCharacterOperation>().characterPrefab.gameObject;
	}
}

public class CollageCatalogueMarshall
{
	public Catalogue? result = null;
	private UnityAction<Scene, LoadSceneMode>? queuedLoad;

	public const string SCENE_NAME = "Scenes/CollageMode";

	private void FetchCollageCatalogue(Scene scn)
	{
		if (scn.name == null || scn.name != "CollageMode") return;

		GameObject? cm = null;
		foreach (var el in scn.GetRootGameObjects())
		{
			el.SetActiveRecursively(false);

			if (el.name == "CollageMode")
			{
				cm = el;
				break;
			}
		}

		if (cm != null)
		{
			var szr = cm.GetComponent<CollageMode.CollageSerializer>();
			GameObject.DontDestroyOnLoad(szr);
			result = new(szr.operationsSets);
		}
		SceneManager.UnloadScene(scn);
	}

	private Action<Scene, LoadSceneMode> WithFetchingAndCleanup(Action onceDone) => (scn, mode) =>
	{
		SceneManager.remove_sceneLoaded(queuedLoad);
		queuedLoad = null;

		FetchCollageCatalogue(scn);

		onceDone();
	};

	private void LoadCollage(Action onceDone)
	{

		queuedLoad = (UnityAction<Scene, LoadSceneMode>)WithFetchingAndCleanup(onceDone);
		SceneManager.add_sceneLoaded(queuedLoad);

		SceneManager.LoadScene(SCENE_NAME, LoadSceneMode.Additive);
	}

	public void MarshallSceneLoad(Action onceDone)
	{
		var scn = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
		if (scn.name == "Lobby" && result == null)
		{
			LoadCollage(onceDone);
		}
		else
		{
			onceDone();
		}
	}
}

public class ModWorld
{
	public global::World discoWorld;

	public bool IsRunning => discoWorld.isRunning;
	public CharacterSheet you;
	private CollageCatalogueMarshall catalogueMarshall = new();

	public Catalogue? Catalogue => catalogueMarshall.result;

	public ModWorld(World discoWorld)
	{
		this.discoWorld = discoWorld;
		you = CharacterSheet.GetForSM(discoWorld.you);
	}

	public void MarshallSceneLoad(Action onceDone) => catalogueMarshall.MarshallSceneLoad(onceDone);
}

public class CharacterSheet
{
	public static Dictionary<SM.CharacterSheet, CharacterSheet> reverseIndex = new();
	public static CharacterSheet GetForSM(SM.CharacterSheet sheet)
	{
		if (reverseIndex.ContainsKey(sheet)) return reverseIndex[sheet];

		CharacterSheet sh = new(sheet);
		sh.EnsureInitialized();
		reverseIndex.Add(sheet, sh);
		return sh;
	}

	public SM.CharacterSheet sm;
	private Dictionary<AssetLocation, SM.Skill> skillMap = new();

	public SM.Skill? GetRawSkill(IAssetRef<Skill> skill)
	{
		DiscoRunner.Log.LogInfo($"skill is {skill}");
		foreach (var sk in skillMap)
		{
			DiscoRunner.Log.LogInfo($"skill is {sk.Key} to {sk.Value}");
		}
		if (skillMap.TryGetValue(skill.Location, out var raw)) return raw;
		else return null;
	}

	public void Recalc() => sm.Recalc();

	public SM.Skill MoraleRaw => GetRawSkill(DiscoRunner.globalConfig.MoraleSkill)!;
	public SM.Skill HealthRaw => GetRawSkill(DiscoRunner.globalConfig.HealthSkill)!;

	public CharacterSheet(SM.CharacterSheet sm)
	{
		this.sm = sm;
	}

	public void EnsureInitialized()
	{
		DiscoRunner.Log.LogInfo($"init'z called");
		for (int i = 0; i < SkillUtils.Skills.Count; i++)
		{
			var sk = SkillUtils.Skills[i];
			if (sk == null || skillMap.ContainsKey(sk.Location)) continue;

			var type = SkillUtils.Skills.GetRaw(i);
			SM.Skill skill = i < SkillUtils.Skills.baseCount ? sm.GetSkill(type) : new(type, sm);
			DiscoRunner.Log.LogInfo($"init'zing {sk} to {skill}");
			skillMap.Add(sk.Location, skill);
		}

	}
}
